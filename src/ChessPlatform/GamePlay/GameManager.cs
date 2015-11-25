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
        #region Constants and Fields

        private static readonly TimeSpan ThreadStopTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan IdleTime = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan MoveWaitingIdleTime = TimeSpan.FromMilliseconds(10);

        private readonly object _syncLock = new object();
        private readonly IChessPlayer _white;
        private readonly IChessPlayer _black;
        private readonly Stack<GameBoard> _gameBoards;
        private readonly Thread _thread;
        private readonly SyncValueContainer<GetMoveState> _getMoveStateContainer;
        private readonly GameControl _gameControl;
        private bool _shouldStop;
        private bool _isDisposed;
        private GameManagerState _state;
        private GameResult? _result;
        private AutoDrawType _autoDrawType;

        #endregion

        #region Constructors

        public GameManager(
            [NotNull] IChessPlayer white,
            [NotNull] IChessPlayer black,
            [NotNull] GameBoard gameBoard)
        {
            #region Argument Check

            if (white == null)
            {
                throw new ArgumentNullException(nameof(white));
            }

            if (black == null)
            {
                throw new ArgumentNullException(nameof(black));
            }

            if (gameBoard == null)
            {
                throw new ArgumentNullException(nameof(gameBoard));
            }

            #endregion

            _white = white;
            _black = black;
            _gameBoards = new Stack<GameBoard>(gameBoard.GetHistory());
            _thread = new Thread(ExecuteGame) { Name = GetType().GetFullName(), IsBackground = true };
            _getMoveStateContainer = new SyncValueContainer<GetMoveState>(null, _syncLock);
            _gameControl = new GameControl();
            _state = GameManagerState.Paused;

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

        #endregion

        #region Events

        public event EventHandler GameBoardChanged;

        public event EventHandler PlayerThinkingStarted;

        public event ThreadExceptionEventHandler UnhandledExceptionOccurred;

        #endregion

        #region Public Properties

        public IChessPlayer White
        {
            [DebuggerStepThrough]
            get
            {
                return _white;
            }
        }

        public IChessPlayer Black
        {
            [DebuggerStepThrough]
            get
            {
                return _black;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public PieceColor ActiveColor
        {
            get
            {
                lock (_syncLock)
                {
                    return GetActiveBoard().ActiveColor;
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

        #endregion

        #region Public Methods

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
            #region Argument Check

            if (moveCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(moveCount), moveCount, @"The value must be positive.");
            }

            #endregion

            lock (_syncLock)
            {
                EnsureNotDisposed();

                return _gameBoards.Count > moveCount;
            }
        }

        public bool UndoLastMoves(int moveCount)
        {
            #region Argument Check

            if (moveCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(moveCount), moveCount, @"The value must be positive.");
            }

            #endregion

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

        #endregion

        #region IDisposable Members

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

        #endregion

        #region Private Methods

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().GetFullName());
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

                    var activePlayer = originalActiveBoard.ActiveColor == PieceColor.White ? _white : _black;
                    var request = new GetMoveRequest(originalActiveBoard, state.CancellationToken, _gameControl);
                    var task = activePlayer.CreateGetMoveTask(request);

                    task.ContinueWith(
                        t =>
                        {
                            lock (_syncLock)
                            {
                                var moveState = _getMoveStateContainer.Value;
                                _getMoveStateContainer.Value = null;

                                if (moveState == null || moveState.IsCancelled.Value)
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
                                "[{0}] Unhandled exception has occurred: {1}",
                                MethodBase.GetCurrentMethod().GetQualifiedName(),
                                t.Exception);

                            lock (_syncLock)
                            {
                                _state = GameManagerState.UnhandledExceptionOccurred;
                            }
                        },
                        TaskContinuationOptions.OnlyOnFaulted);

                    RaisePlayerThinkingStartedAsync();
                    task.Start();
                }
            }
        }

        private void AffectStatesInternal(GameManagerState? desiredState)
        {
            var gameBoard = GetActiveBoard();

            _autoDrawType = AutoDrawType.None;
            switch (gameBoard.State)
            {
                case GameState.Checkmate:
                    _result = gameBoard.ActiveColor == PieceColor.White ? GameResult.BlackWon : GameResult.WhiteWon;
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

            if (desiredState.HasValue)
            {
                _state = desiredState.Value;
            }
        }

        private void AffectStates(GameManagerState? desiredState)
        {
            AffectStatesInternal(desiredState);
            RaiseGameBoardChangedAsync();
        }

        private void RaiseGameBoardChangedAsync()
        {
            var handler = GameBoardChanged;
            if (handler == null)
            {
                return;
            }

            AsyncFactotum.ExecuteAsync(() => handler(this, EventArgs.Empty));
        }

        private void RaisePlayerThinkingStartedAsync()
        {
            var handler = PlayerThinkingStarted;
            if (handler == null)
            {
                return;
            }

            AsyncFactotum.ExecuteAsync(() => handler(this, EventArgs.Empty));
        }

        private void RaiseUnhandledExceptionOccurredAsync(Exception exception)
        {
            var handler = UnhandledExceptionOccurred;
            if (handler == null)
            {
                return;
            }

            var eventArgs = new ThreadExceptionEventArgs(exception);
            AsyncFactotum.ExecuteAsync(() => handler(this, eventArgs));
        }

        #endregion

        #region GetMoveState Class

        private sealed class GetMoveState
        {
            #region Constants and Fields

            private readonly CancellationTokenSource _cancellationTokenSource;

            #endregion

            #region Constructors

            public GetMoveState(GameManagerState state, [NotNull] GameBoard activeBoard)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                State = state;
                ActiveBoard = activeBoard.EnsureNotNull();
                IsCancelled = new SyncValueContainer<bool>();
            }

            #endregion

            #region Public Properties

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
                get
                {
                    return _cancellationTokenSource.Token;
                }
            }

            public SyncValueContainer<bool> IsCancelled
            {
                get;
            }

            #endregion

            #region Public Methods

            public void Cancel()
            {
                IsCancelled.Value = true; // MUST be set before cancelling task via CTS

                _cancellationTokenSource.Cancel();
            }

            #endregion
        }

        #endregion
    }
}