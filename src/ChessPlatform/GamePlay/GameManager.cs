using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Omnifactotum;
using Omnifactotum.Annotations;
using ThreadState = System.Threading.ThreadState;

namespace ChessPlatform.GamePlay
{
    public sealed class GameManager : IDisposable
    {
        private const string ThisTypeName = nameof(GameManager);

        private static readonly string ThisTypeFullName = typeof(GameManager).GetFullName();

        private static readonly TimeSpan ThreadStopTimeout = TimeSpan.FromSeconds(50);
        private static readonly TimeSpan IdleTime = TimeSpan.FromMilliseconds(10);
        private static readonly TimeSpan MoveWaitingIdleTime = TimeSpan.FromMilliseconds(10);

        private static long _instanceCounter;

        private readonly object _syncLock = new object();
        private readonly long _instanceIndex;
        private readonly Stack<GameBoard> _gameBoards;
        private readonly Thread _thread;
        private readonly SyncValueContainer<GetMoveState> _getMoveStateContainer;
        private readonly GameControl _gameControl;
        private readonly Stopwatch _whiteTotalStopwatch;
        private readonly Stopwatch _blackTotalStopwatch;
        private readonly Stopwatch _whiteLastMoveStopwatch;
        private readonly Stopwatch _blackLastMoveStopwatch;
        private bool _shouldStop;
        private bool _isDisposed;
        private GameManagerState _state;
        private GameResult? _result;
        private AutoDrawType _autoDrawType;

        public GameManager(
            [NotNull] IChessPlayer white,
            [NotNull] IChessPlayer black,
            [NotNull] GameBoard gameBoard)
        {
            if (white is null)
            {
                throw new ArgumentNullException(nameof(white));
            }

            if (black is null)
            {
                throw new ArgumentNullException(nameof(black));
            }

            if (gameBoard is null)
            {
                throw new ArgumentNullException(nameof(gameBoard));
            }

            _instanceIndex = Interlocked.Increment(ref _instanceCounter);

            White = white;
            Black = black;
            _gameBoards = new Stack<GameBoard>(gameBoard.GetHistory());

            _thread = new Thread(ExecuteGame)
            {
                Name = $@"{ThisTypeName}: Thread #{_instanceIndex}",
                IsBackground = true,
                Priority = ThreadPriority.Normal
            };

            _getMoveStateContainer = new SyncValueContainer<GetMoveState>(null, _syncLock);
            _gameControl = new GameControl();
            _state = GameManagerState.Paused;

            _whiteTotalStopwatch = new Stopwatch();
            _blackTotalStopwatch = new Stopwatch();
            _whiteLastMoveStopwatch = new Stopwatch();
            _blackLastMoveStopwatch = new Stopwatch();

            _thread.Start();
        }

        public GameManager(
            [NotNull] IChessPlayer white,
            [NotNull] IChessPlayer black,
            [NotNull] string initialPositionFen)
            : this(white, black, new GameBoard(initialPositionFen))
        {
            // Nothing to do
        }

        public event EventHandler GameBoardChanged;

        public event EventHandler PlayerThinkingStarted;

        public event ThreadExceptionEventHandler UnhandledExceptionOccurred;

        public IChessPlayer White
        {
            [DebuggerStepThrough]
            get;
        }

        public IChessPlayer Black
        {
            [DebuggerStepThrough]
            get;
        }

        public TimeSpan WhiteTotalElapsed => _whiteTotalStopwatch.Elapsed;

        public TimeSpan BlackTotalElapsed => _blackTotalStopwatch.Elapsed;

        public TimeSpan WhiteLastMoveElapsed => _whiteLastMoveStopwatch.Elapsed;

        public TimeSpan BlackLastMoveElapsed => _blackLastMoveStopwatch.Elapsed;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public GameSide ActiveSide
        {
            get
            {
                lock (_syncLock)
                {
                    return GetActiveBoard().ActiveSide;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public GameManagerState State
        {
            get
            {
                lock (_syncLock)
                {
                    return _state;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public GameResult? Result
        {
            get
            {
                lock (_syncLock)
                {
                    return _result;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public AutoDrawType AutoDrawType
        {
            get
            {
                lock (_syncLock)
                {
                    return _autoDrawType;
                }
            }
        }

        public void Play()
        {
            lock (_syncLock)
            {
                EnsureNotDisposed();

                if (_state != GameManagerState.Paused)
                {
                    return;
                }

                AffectStates(GameManagerState.Running);
            }
        }

        public void Pause()
        {
            lock (_syncLock)
            {
                EnsureNotDisposed();

                _state = GameManagerState.Paused;
            }
        }

        public void RequestMoveNow()
        {
            lock (_syncLock)
            {
                switch (_state)
                {
                    case GameManagerState.Running:
                        _gameControl.RequestMoveNow();
                        return;

                    case GameManagerState.Paused:
                    case GameManagerState.GameFinished:
                    case GameManagerState.UnhandledExceptionOccurred:
                        throw new ChessPlatformException($@"The game is not running (state: {_state}).");

                    default:
                        throw _state.CreateEnumValueNotImplementedException();
                }
            }
        }

        public bool CanUndoLastMoves(int moveCount)
        {
            if (moveCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(moveCount), moveCount, @"The value must be positive.");
            }

            lock (_syncLock)
            {
                EnsureNotDisposed();

                return _gameBoards.Count > moveCount;
            }
        }

        public bool UndoLastMoves(int moveCount)
        {
            if (moveCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(moveCount), moveCount, @"The value must be positive.");
            }

            lock (_syncLock)
            {
                if (_isDisposed || _gameBoards.Count <= moveCount)
                {
                    return false;
                }

                var originalState = _state == GameManagerState.GameFinished ? GameManagerState.Running : _state;
                _state = GameManagerState.Paused;

                var getMoveState = _getMoveStateContainer.Value;
                if (getMoveState != null)
                {
                    getMoveState.Cancel();
                    _getMoveStateContainer.Value = null;
                }

                for (var index = 0; index < moveCount; index++)
                {
                    _gameBoards.Pop();
                }

                AffectStates(originalState);
            }

            return true;
        }

        public GameBoard[] GetBoardHistory()
        {
            lock (_syncLock)
            {
                EnsureNotDisposed();

                return _gameBoards.Reverse().ToArray();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            lock (_syncLock)
            {
                if (_isDisposed)
                {
                    return;
                }

                _shouldStop = true;
                _isDisposed = true;

                if (_thread.ThreadState == ThreadState.Unstarted)
                {
                    return;
                }
            }

            var joined = _thread.Join(ThreadStopTimeout);
            if (joined)
            {
                return;
            }

            lock (_syncLock)
            {
                _thread.Abort();
            }

            _thread.Join();
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException($@"{ThisTypeFullName} #{_instanceIndex}");
            }
        }

        private GameBoard GetActiveBoard()
        {
            return _gameBoards.Peek().EnsureNotNull();
        }

        private void ExecuteGame()
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            try
            {
                ExecuteGameInternal();

                var getMoveState = _getMoveStateContainer.Value;
                getMoveState?.Cancel();
            }
            catch (Exception ex)
                when (!ex.IsFatal())
            {
                Trace.TraceError(
                    $@"{Environment.NewLine}[{currentMethodName}] Unhandled exception has occurred: {ex}{
                        Environment.NewLine}");

                lock (_syncLock)
                {
                    _state = GameManagerState.UnhandledExceptionOccurred;
                }

                RaiseUnhandledExceptionOccurredAsync(ex);
            }
        }

        private void ExecuteGameInternal()
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            while (!_shouldStop && !_isDisposed)
            {
                if (_state == GameManagerState.UnhandledExceptionOccurred)
                {
                    throw new InvalidOperationException("Exception in the player logic has occurred.");
                }

                if (_state != GameManagerState.Running)
                {
                    Thread.Sleep(IdleTime);
                    continue;
                }

                lock (_syncLock)
                {
                    if (_shouldStop || _isDisposed)
                    {
                        continue;
                    }

                    var originalActiveBoard = GetActiveBoard();

                    var getMoveState = _getMoveStateContainer.Value;
                    if (getMoveState != null)
                    {
                        if (getMoveState.State != _state || getMoveState.ActiveBoard != originalActiveBoard)
                        {
                            getMoveState.Cancel();
                        }

                        Thread.Sleep(MoveWaitingIdleTime);
                        continue;
                    }

                    if (_state != GameManagerState.Running || _shouldStop || _isDisposed)
                    {
                        continue;
                    }

                    var state = new GetMoveState(_state, originalActiveBoard);
                    _getMoveStateContainer.Value = state;

                    var activeSide = originalActiveBoard.ActiveSide;
                    var activePlayer = activeSide == GameSide.White ? White : Black;
                    var request = new GetMoveRequest(originalActiveBoard, state.CancellationToken, _gameControl);
                    var task = activePlayer.CreateGetMoveTask(request);

                    task.ContinueWith(
                        t =>
                        {
                            lock (_syncLock)
                            {
                                var moveState = _getMoveStateContainer.Value;
                                _getMoveStateContainer.Value = null;

                                if (moveState is null || moveState.IsCancelled.Value)
                                {
                                    return;
                                }

                                var activeBoard = GetActiveBoard();

                                if (moveState.State != _state || moveState.ActiveBoard != activeBoard)
                                {
                                    return;
                                }

                                var move = t.Result.EnsureNotNull().FirstMove.EnsureNotNull();
                                var newGameBoard = activeBoard.MakeMove(move).EnsureNotNull();
                                _gameBoards.Push(newGameBoard);

                                AffectStates(GameManagerState.Paused);
                            }
                        },
                        TaskContinuationOptions.OnlyOnRanToCompletion);

                    task.ContinueWith(
                        t => _getMoveStateContainer.Value = null,
                        TaskContinuationOptions.OnlyOnCanceled);

                    task.ContinueWith(
                        t =>
                        {
                            Trace.TraceError(
                                $@"{Environment.NewLine}{Environment.NewLine}[{currentMethodName
                                    }] Unhandled exception has occurred: {t.Exception}{Environment.NewLine}{
                                    Environment.NewLine}");

                            lock (_syncLock)
                            {
                                _whiteTotalStopwatch.Stop();
                                _blackTotalStopwatch.Stop();
                                _whiteLastMoveStopwatch.Stop();
                                _blackLastMoveStopwatch.Stop();

                                _state = GameManagerState.UnhandledExceptionOccurred;
                            }
                        },
                        TaskContinuationOptions.OnlyOnFaulted);

                    Stopwatch totalStopwatch;
                    Stopwatch lastMoveStopwatch;
                    if (activeSide == GameSide.White)
                    {
                        totalStopwatch = _whiteTotalStopwatch;
                        lastMoveStopwatch = _whiteLastMoveStopwatch;
                    }
                    else
                    {
                        totalStopwatch = _blackTotalStopwatch;
                        lastMoveStopwatch = _blackLastMoveStopwatch;
                    }

                    totalStopwatch.Start();
                    lastMoveStopwatch.Restart();

                    RaisePlayerThinkingStartedAsync();
                    task.Start();
                }
            }
        }

        private void AffectStatesInternal(GameManagerState desiredState)
        {
            _whiteTotalStopwatch.Stop();
            _blackTotalStopwatch.Stop();
            _whiteLastMoveStopwatch.Stop();
            _blackLastMoveStopwatch.Stop();

            var gameBoard = GetActiveBoard();

            _autoDrawType = AutoDrawType.None;
            switch (gameBoard.State)
            {
                case GameState.Checkmate:
                    _result = gameBoard.ActiveSide == GameSide.White ? GameResult.BlackWon : GameResult.WhiteWon;
                    _state = GameManagerState.GameFinished;
                    return;

                case GameState.Stalemate:
                    _result = GameResult.Draw;
                    _state = GameManagerState.GameFinished;
                    return;

                default:
                    _result = null;

                    _autoDrawType = gameBoard.GetAutoDrawType();
                    if (_autoDrawType != AutoDrawType.None)
                    {
                        _result = GameResult.Draw;
                        _state = GameManagerState.GameFinished;
                        return;
                    }

                    break;
            }

            _state = desiredState;
        }

        private void AffectStates(GameManagerState desiredState)
        {
            AffectStatesInternal(desiredState);
            RaiseGameBoardChangedAsync();
        }

        private void RaiseGameBoardChangedAsync()
        {
            var handler = GameBoardChanged;
            if (handler is null)
            {
                return;
            }

            AsyncFactotum.ExecuteAsync(() => handler(this, EventArgs.Empty));
        }

        private void RaisePlayerThinkingStartedAsync()
        {
            var handler = PlayerThinkingStarted;
            if (handler is null)
            {
                return;
            }

            AsyncFactotum.ExecuteAsync(() => handler(this, EventArgs.Empty));
        }

        private void RaiseUnhandledExceptionOccurredAsync(Exception exception)
        {
            var handler = UnhandledExceptionOccurred;
            if (handler is null)
            {
                return;
            }

            var eventArgs = new ThreadExceptionEventArgs(exception);
            AsyncFactotum.ExecuteAsync(() => handler(this, eventArgs));
        }

        private sealed class GetMoveState
        {
            private readonly CancellationTokenSource _cancellationTokenSource;

            public GetMoveState(GameManagerState state, [NotNull] GameBoard activeBoard)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                State = state;
                ActiveBoard = activeBoard.EnsureNotNull();
                IsCancelled = new SyncValueContainer<bool>();
            }

            public GameManagerState State
            {
                get;
            }

            public GameBoard ActiveBoard
            {
                get;
            }

            public CancellationToken CancellationToken
            {
                [DebuggerNonUserCode]
                get => _cancellationTokenSource.Token;
            }

            public SyncValueContainer<bool> IsCancelled
            {
                get;
            }

            public void Cancel()
            {
                IsCancelled.Value = true; // MUST be set before cancelling task via CTS

                _cancellationTokenSource.Cancel();
            }
        }
    }
}