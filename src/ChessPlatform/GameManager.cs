using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Omnifactotum;
using Omnifactotum.Annotations;
using ThreadState = System.Threading.ThreadState;

namespace ChessPlatform
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
                throw new ArgumentNullException("white");
            }

            if (black == null)
            {
                throw new ArgumentNullException("black");
            }

            if (gameBoard == null)
            {
                throw new ArgumentNullException("gameBoard");
            }

            #endregion

            _white = white;
            _black = black;
            _gameBoards = new Stack<GameBoard>(gameBoard.GetHistory());
            _thread = new Thread(this.ExecuteGame) { Name = GetType().GetFullName(), IsBackground = true };
            _getMoveStateContainer = new SyncValueContainer<GetMoveState>(null, _syncLock);
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
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The game is not paused (current state: {0}).",
                            _state.GetName()));
                }

                AffectStates(GameManagerState.Running);
                RaiseGameBoardChangedAsync();
            }
        }

        public void Pause()
        {
            lock (_syncLock)
            {
                EnsureNotDisposed();

                throw new NotImplementedException();
            }
        }

        public bool CanUndoLastMoves(int moveCount)
        {
            #region Argument Check

            if (moveCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "moveCount",
                    moveCount,
                    @"The value must be positive.");
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
                throw new ArgumentOutOfRangeException(
                    "moveCount",
                    moveCount,
                    @"The value must be positive.");
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
                RaiseGameBoardChangedAsync();
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
            try
            {
                ExecuteGameInternal();

                var getMoveState = _getMoveStateContainer.Value;
                if (getMoveState != null)
                {
                    getMoveState.Cancel();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "[{0}] Unhandled exception has occurred: {1}",
                    MethodBase.GetCurrentMethod().GetQualifiedName(),
                    ex);

                lock (_syncLock)
                {
                    _state = GameManagerState.UnhandledExceptionOccurred;
                }
            }
        }

        private void ExecuteGameInternal()
        {
            while (!_shouldStop && !_isDisposed)
            {
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
                        if ((getMoveState.State != _state || getMoveState.ActiveBoard != originalActiveBoard))
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

                    var isCancelled = state.IsCancelled;

                    var activePlayer = originalActiveBoard.ActiveColor == PieceColor.White ? _white : _black;
                    var task = activePlayer.GetMove(originalActiveBoard, state.CancellationToken);

                    task.ContinueWith(
                        t =>
                        {
                            lock (_syncLock)
                            {
                                var moveState = _getMoveStateContainer.Value;
                                _getMoveStateContainer.Value = null;

                                if (isCancelled.Value)
                                {
                                    return;
                                }

                                var move = t.Result.EnsureNotNull();

                                var activeBoard = GetActiveBoard();

                                if (moveState == null || moveState.State != _state
                                    || moveState.ActiveBoard != activeBoard)
                                {
                                    return;
                                }

                                var newGameBoard = activeBoard.MakeMove(move).EnsureNotNull();
                                _gameBoards.Push(newGameBoard);

                                AffectStates(null);
                                RaiseGameBoardChangedAsync();
                            }
                        },
                        TaskContinuationOptions.OnlyOnRanToCompletion);

                    task.ContinueWith(
                        t => _getMoveStateContainer.Value = null,
                        TaskContinuationOptions.OnlyOnCanceled);

                    RaisePlayerThinkingStartedAsync();
                    task.Start();
                }
            }
        }

        private void AffectStates(GameManagerState? desiredState)
        {
            var gameBoard = GetActiveBoard();

            _autoDrawType = AutoDrawType.None;
            switch (gameBoard.State)
            {
                case GameState.Checkmate:
                    _result = gameBoard.ActiveColor == PieceColor.White
                        ? GameResult.BlackWon
                        : GameResult.WhiteWon;
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

        private void RaiseGameBoardChangedAsync()
        {
            var handler = this.GameBoardChanged;
            if (handler == null)
            {
                return;
            }

            Task.Factory.StartNew(() => handler(this, EventArgs.Empty));
        }

        private void RaisePlayerThinkingStartedAsync()
        {
            var handler = this.PlayerThinkingStarted;
            if (handler == null)
            {
                return;
            }

            Task.Factory.StartNew(() => handler(this, EventArgs.Empty));
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
                this.State = state;
                this.ActiveBoard = activeBoard.EnsureNotNull();
                this.IsCancelled = new SyncValueContainer<bool>();
            }

            #endregion

            #region Public Properties

            public GameManagerState State
            {
                get;
                private set;
            }

            public GameBoard ActiveBoard
            {
                get;
                private set;
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
                private set;
            }

            #endregion

            #region Public Methods

            public void Cancel()
            {
                this.IsCancelled.Value = true; // MUST be set before cancelling task via CTS

                _cancellationTokenSource.Cancel();
            }

            #endregion
        }

        #endregion
    }
}