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
            EnsureNotDisposed();

            throw new NotImplementedException();
        }

        public void UndoLastMove()
        {
            EnsureNotDisposed();

            throw new NotImplementedException();
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
            var getMoveTaskContainer = new SyncValueContainer<Task<PieceMove>>(null, _syncLock);

            while (!_shouldStop && !_isDisposed)
            {
                if (_state != GameManagerState.Running)
                {
                    Thread.Sleep(IdleTime);
                    continue;
                }

                lock (_syncLock)
                {
                    if (_state != GameManagerState.Running || _shouldStop || _isDisposed
                        || getMoveTaskContainer.Value != null)
                    {
                        continue;
                    }

                    var originalActiveBoard = GetActiveBoard();
                    var activePlayer = originalActiveBoard.ActiveColor == PieceColor.White ? _white : _black;

                    var task = new Task<PieceMove>(() => activePlayer.GetMove(originalActiveBoard));
                    getMoveTaskContainer.Value = task;

                    task.ContinueWith(
                        t =>
                        {
                            lock (_syncLock)
                            {
                                getMoveTaskContainer.Value = null;

                                var move = t.Result.EnsureNotNull();

                                var activeBoard = GetActiveBoard();
                                if (activeBoard != originalActiveBoard)
                                {
                                    // The board reference had changes since the time that the task was created
                                    // This could happen if, for instance, a last move was undone in GUI
                                    return;
                                }

                                var newGameBoard = activeBoard.MakeMove(move).EnsureNotNull();
                                _gameBoards.Push(newGameBoard);

                                switch (newGameBoard.State)
                                {
                                    case GameState.Checkmate:
                                        _result = newGameBoard.ActiveColor == PieceColor.White
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

                            RaiseGameBoardChanged();
                        },
                        TaskContinuationOptions.OnlyOnRanToCompletion);

                    task.Start();
                }
            }
        }

        private void RaiseGameBoardChanged()
        {
            var handler = this.GameBoardChanged;
            if (handler == null)
            {
                return;
            }

            handler(this, EventArgs.Empty);
        }

        #endregion
    }
}