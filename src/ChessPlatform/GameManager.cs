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

namespace ChessPlatform
{
    public sealed class GameManager : IDisposable
    {
        #region Constants and Fields

        private static readonly TimeSpan ThreadStopTimeout = TimeSpan.FromSeconds(5d);
        private static readonly TimeSpan IdleTime = TimeSpan.FromMilliseconds(50d);

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

        #endregion

        #region Constructors

        public GameManager([NotNull] IChessPlayer white, [NotNull] IChessPlayer black, string initialPositionFen)
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

            if (string.IsNullOrWhiteSpace(initialPositionFen))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    "initialPositionFen");
            }

            #endregion

            _white = white;
            _black = black;
            _gameBoards = new Stack<GameBoard>();
            _thread = new Thread(this.ExecuteGame) { Name = GetType().GetFullName(), IsBackground = true };
            _getMoveStateContainer = new SyncValueContainer<GetMoveState>(null, _syncLock);
            _state = GameManagerState.Paused;

            var gameBoard = new GameBoard(initialPositionFen);
            _gameBoards.Push(gameBoard);

            _thread.Start();
        }

        #endregion

        #region Events

        public event EventHandler GameBoardChanged;

        #endregion

        #region Public Properties

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

                _state = GameManagerState.Running;
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

            GameManagerState originalState;
            lock (_syncLock)
            {
                if (_isDisposed || _gameBoards.Count <= moveCount)
                {
                    return false;
                }

                originalState = _state;
                _state = GameManagerState.Paused;

                var getMoveState = _getMoveStateContainer.Value;
                if (getMoveState != null)
                {
                    getMoveState.CancellationTokenSource.Cancel();
                }
            }

            while (_getMoveStateContainer.Value != null)
            {
                Thread.Sleep(IdleTime);
            }

            lock (_syncLock)
            {
                if (_isDisposed)
                {
                    return false;
                }

                for (var index = 0; index < moveCount; index++)
                {
                    _gameBoards.Pop();
                }

                _state = originalState;

                AffectStates();

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
                            getMoveState.CancellationTokenSource.Cancel();
                        }

                        continue;
                    }

                    if (_state != GameManagerState.Running || _shouldStop || _isDisposed)
                    {
                        continue;
                    }

                    var activePlayer = originalActiveBoard.ActiveColor == PieceColor.White ? _white : _black;

                    var cancellationTokenSource = new CancellationTokenSource();
                    var task = activePlayer.GetMove(originalActiveBoard, cancellationTokenSource.Token);

                    _getMoveStateContainer.Value = new GetMoveState(
                        _state,
                        originalActiveBoard,
                        cancellationTokenSource);

                    task.ContinueWith(
                        t =>
                        {
                            lock (_syncLock)
                            {
                                var moveState = _getMoveStateContainer.Value;
                                _getMoveStateContainer.Value = null;

                                var move = t.Result.EnsureNotNull();

                                var activeBoard = GetActiveBoard();

                                if (moveState == null || moveState.State != _state
                                    || moveState.ActiveBoard != activeBoard)
                                {
                                    return;
                                }

                                var newGameBoard = activeBoard.MakeMove(move).EnsureNotNull();
                                _gameBoards.Push(newGameBoard);

                                AffectStates();

                                RaiseGameBoardChangedAsync();
                            }
                        },
                        TaskContinuationOptions.OnlyOnRanToCompletion);

                    task.ContinueWith(
                        t => _getMoveStateContainer.Value = null,
                        TaskContinuationOptions.OnlyOnCanceled);

                    task.Start();
                }
            }
        }

        private void AffectStates()
        {
            var gameBoard = GetActiveBoard();

            switch (gameBoard.State)
            {
                case GameState.Checkmate:
                    _result = gameBoard.ActiveColor == PieceColor.White
                        ? GameResult.BlackWon
                        : GameResult.WhiteWon;
                    _state = GameManagerState.GameFinished;
                    break;

                case GameState.Stalemate:
                    _result = GameResult.Draw;
                    _state = GameManagerState.GameFinished;
                    break;

                default:
                    _result = null;
                    break;
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

        #endregion

        #region GetMoveState Class

        private sealed class GetMoveState
        {
            #region Constructors

            public GetMoveState(
                GameManagerState state,
                GameBoard activeBoard,
                CancellationTokenSource cancellationTokenSource)
            {
                this.State = state;
                this.ActiveBoard = activeBoard.EnsureNotNull();
                this.CancellationTokenSource = cancellationTokenSource.EnsureNotNull();
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

            public CancellationTokenSource CancellationTokenSource
            {
                get;
                private set;
            }

            #endregion
        }

        #endregion
    }
}