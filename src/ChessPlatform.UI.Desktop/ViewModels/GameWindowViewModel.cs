using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop.ViewModels
{
    internal sealed class GameWindowViewModel : ViewModelBase
    {
        #region Constants and Fields

        private readonly TaskScheduler _taskScheduler;
        private readonly HashSet<Position> _validMoveTargetPositionsInternal;
        private GameBoard _currentGameBoard;
        private GameWindowSelectionMode _selectionMode;
        private Position? _currentTargetPosition;
        private GameManager _gameManager;
        private IChessPlayer _whitePlayer;
        private IChessPlayer _blackPlayer;
        private GuiHumanChessPlayer _activeGuiHumanChessPlayer;
        private GameBoard[] _boardHistory;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameWindowViewModel"/> class.
        /// </summary>
        public GameWindowViewModel()
        {
            _taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            _validMoveTargetPositionsInternal = new HashSet<Position>();

            _selectionMode = GameWindowSelectionMode.Default;
            this.ValidMoveTargetPositions = _validMoveTargetPositionsInternal.AsReadOnly();

            InitializeNewGameFromDefaultInitialBoard();

            this.SquareViewModels = ChessHelper.AllPositions
                .ToDictionary(Factotum.Identity, position => new BoardSquareViewModel(this, position))
                .AsReadOnly();
        }

        #endregion

        #region Public Properties

        public ReadOnlyDictionary<Position, BoardSquareViewModel> SquareViewModels
        {
            get;
            private set;
        }

        public GameWindowSelectionMode SelectionMode
        {
            [DebuggerStepThrough]
            get
            {
                return _selectionMode;
            }

            private set
            {
                if (value == _selectionMode)
                {
                    return;
                }

                _selectionMode = value;
                RaisePropertyChanged(() => this.SelectionMode);
            }
        }

        public GameBoard CurrentGameBoard
        {
            [DebuggerStepThrough]
            get
            {
                return _currentGameBoard;
            }

            private set
            {
                if (ReferenceEquals(value, _currentGameBoard))
                {
                    return;
                }

                _currentGameBoard = value;
                RaisePropertyChanged(() => this.CurrentGameBoard);
            }
        }

        public Position? CurrentTargetPosition
        {
            [DebuggerStepThrough]
            get
            {
                return _currentTargetPosition;
            }

            set
            {
                if (value == _currentTargetPosition)
                {
                    return;
                }

                _currentTargetPosition = value;
                RaisePropertyChanged(() => this.CurrentTargetPosition);
            }
        }

        public string MoveHistory
        {
            get
            {
                return GetMoveHistory();
            }
        }

        #endregion

        #region Internal Properties

        internal ReadOnlySet<Position> ValidMoveTargetPositions
        {
            get;
            private set;
        }

        internal Position? CurrentSourcePosition
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public void ResetSelectionMode()
        {
            _validMoveTargetPositionsInternal.Clear();
            this.CurrentSourcePosition = null;

            this.SelectionMode = _activeGuiHumanChessPlayer == null
                ? GameWindowSelectionMode.None
                : GameWindowSelectionMode.Default;
        }

        public void SetMovingPieceSelectionMode(Position currentSourcePosition)
        {
            SetModeInternal(currentSourcePosition, GameWindowSelectionMode.MovingPieceSelected);
        }

        public void SetValidMovesOnlySelectionMode(Position currentSourcePosition)
        {
            SetModeInternal(currentSourcePosition, GameWindowSelectionMode.DisplayValidMovesOnly);
        }

        public void InitializeNewGameFromDefaultInitialBoard()
        {
            InitializeNewGame(ChessConstants.DefaultInitialFen);
        }

        public void InitializeNewGame(string fen)
        {
            #region Argument Check

            if (string.IsNullOrWhiteSpace(fen))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    "fen");
            }

            #endregion

            Factotum.DisposeAndNull(ref _gameManager);

            CreateGuiHumanChessPlayer(ref _whitePlayer, PieceColor.White);
            CreateGuiHumanChessPlayer(ref _blackPlayer, PieceColor.Black);

            _activeGuiHumanChessPlayer = null;
            ResetSelectionMode();

            _gameManager = new GameManager(_whitePlayer, _blackPlayer, fen);
            _gameManager.GameBoardChanged += this.GameManager_GameBoardChanged;

            RefreshBoardHistory();
        }

        public void Play()
        {
            _activeGuiHumanChessPlayer = null;
            ResetSelectionMode();

            _gameManager.Play();
        }

        public void MakeMove(PieceMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            var activeGuiHumanChessPlayer = _activeGuiHumanChessPlayer;
            if (activeGuiHumanChessPlayer == null)
            {
                throw new InvalidOperationException("Human player is not active.");
            }

            _activeGuiHumanChessPlayer = null;
            ResetSelectionMode();

            activeGuiHumanChessPlayer.ApplyMove(move);
        }

        public bool CanUndoLastMove()
        {
            var gameManager = _gameManager;
            return gameManager != null && _gameManager.CanUndoLastMoves(1);
        }

        public void UndoLastMove()
        {
            var gameManager = _gameManager;
            if (gameManager == null)
            {
                return;
            }

            gameManager.UndoLastMoves(1);
        }

        [NotNull]
        public string GetMoveHistory()
        {
            var resultBuilder = new StringBuilder();

            var boardHistory = _boardHistory;

            var initialBoard = boardHistory[0];
            resultBuilder.AppendFormat(CultureInfo.InvariantCulture, @"[FEN ""{0}""]", initialBoard.GetFen());

            var previousBoard = initialBoard;
            var moveIndex = unchecked(previousBoard.FullMoveIndex - 1);
            for (var index = 1; index < boardHistory.Length; index++)
            {
                var board = boardHistory[index];

                if (moveIndex != previousBoard.FullMoveIndex)
                {
                    resultBuilder.AppendLine();

                    moveIndex = previousBoard.FullMoveIndex;
                    resultBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}.", moveIndex);

                    if (index == 1 && initialBoard.ActiveColor == PieceColor.Black)
                    {
                        resultBuilder.Append(" ...");
                    }
                }

                var move = board.PreviousMove;

                var castlingInfo = previousBoard.CheckCastlingMove(move);
                if (castlingInfo != null)
                {
                    var isKingSide = (castlingInfo.Option & CastlingOptions.KingSideMask) != 0;
                    resultBuilder.AppendFormat(CultureInfo.InvariantCulture, " {0}", isKingSide ? "O-O" : "O-O-O");
                }
                else
                {
                    resultBuilder.AppendFormat(
                        CultureInfo.InvariantCulture,
                        " {0}",
                        move.ToString(board.LastCapturedPiece != Piece.None));
                }

                if (board.State == GameState.Checkmate)
                {
                    resultBuilder.Append("#");
                }
                else if (board.State.IsCheck())
                {
                    resultBuilder.Append("+");
                }

                previousBoard = board;
            }

            return resultBuilder.ToString();
        }

        #endregion

        #region Private Methods

        private void SetModeInternal(Position currentSourcePosition, GameWindowSelectionMode selectionMode)
        {
            var validMoves = this.CurrentGameBoard.GetValidMovesBySource(currentSourcePosition);
            if (validMoves.Length == 0)
            {
                return;
            }

            _validMoveTargetPositionsInternal.Clear();
            validMoves.DoForEach(move => _validMoveTargetPositionsInternal.Add(move.To));

            this.CurrentSourcePosition = currentSourcePosition;
            this.SelectionMode = selectionMode;
        }

        private void RefreshBoardHistory()
        {
            var boardHistory = _gameManager.GetBoardHistory().EnsureNotNull();
            if (boardHistory.Length == 0)
            {
                throw new InvalidOperationException("History must not be empty.");
            }

            _boardHistory = boardHistory;

            ResetSelectionMode();
            RaisePropertyChanged(() => this.MoveHistory);

            this.CurrentGameBoard = boardHistory.Last();
        }

        private void CreateGuiHumanChessPlayer(ref IChessPlayer player, PieceColor color)
        {
            var guiHumanChessPlayer = player as GuiHumanChessPlayer;
            if (guiHumanChessPlayer != null)
            {
                guiHumanChessPlayer.MoveRequested -= this.GuiHumanChessPlayer_MoveRequested;
                guiHumanChessPlayer.MoveRequestCancelled -= this.GuiHumanChessPlayer_MoveRequestCancelled;
            }

            guiHumanChessPlayer = new GuiHumanChessPlayer(color);
            guiHumanChessPlayer.MoveRequested += this.GuiHumanChessPlayer_MoveRequested;
            guiHumanChessPlayer.MoveRequestCancelled += this.GuiHumanChessPlayer_MoveRequestCancelled;

            player = guiHumanChessPlayer;
        }

        private void OnHumanChessPlayerMoveRequested()
        {
            if (_activeGuiHumanChessPlayer != null)
            {
                throw new InvalidOperationException("The active GUI player is already assigned.");
            }

            _activeGuiHumanChessPlayer =
                (_gameManager.ActiveColor == PieceColor.White ? _whitePlayer : _blackPlayer) as GuiHumanChessPlayer;

            ResetSelectionMode();
        }

        private void OnHumanChessPlayerMoveRequestCancelled()
        {
            _activeGuiHumanChessPlayer = null;
            ResetSelectionMode();
        }

        private void GuiHumanChessPlayer_MoveRequested(object sender, EventArgs eventArgs)
        {
            Task.Factory.StartNew(
                this.OnHumanChessPlayerMoveRequested,
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        private void GuiHumanChessPlayer_MoveRequestCancelled(object sender, EventArgs eventArgs)
        {
            Task.Factory.StartNew(
                this.OnHumanChessPlayerMoveRequestCancelled,
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        private void GameManager_GameBoardChanged(object sender, EventArgs eventArgs)
        {
            Task.Factory.StartNew(
                this.RefreshBoardHistory,
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        #endregion
    }
}