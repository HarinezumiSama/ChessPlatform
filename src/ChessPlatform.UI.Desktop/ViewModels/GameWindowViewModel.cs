using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChessPlatform.ComputerPlayers;
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

            this.SquareViewModels = ChessHelper.AllPositions
                .ToDictionary(Factotum.Identity, position => new BoardSquareViewModel(this, position))
                .AsReadOnly();

            InitializeNewGameFromDefaultInitialBoard();
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

        public bool IsComputerPlayerActive
        {
            get
            {
                return GetActiveHumanPlayer() == null;
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
            ResetSelectionModeInternal(null);
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
            //CreateGuiHumanChessPlayer(ref _blackPlayer, PieceColor.Black);
            //_blackPlayer = new DummyPlayer(PieceColor.Black);
            _blackPlayer = new SmartEnoughPlayer(PieceColor.Black, 4);

            ResetSelectionMode();

            _gameManager = new GameManager(_whitePlayer, _blackPlayer, fen);
            _gameManager.GameBoardChanged += this.GameManager_GameBoardChanged;
            _gameManager.PlayerThinkingStarted += this.GameManager_PlayerThinkingStarted;

            RefreshBoardHistory();
        }

        public void Play()
        {
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

            var activeHumanPlayer = GetActiveHumanPlayer();
            if (activeHumanPlayer == null)
            {
                throw new InvalidOperationException("Human player is not active.");
            }

            ResetSelectionMode();

            activeHumanPlayer.ApplyMove(move);
        }

        public bool CanUndoLastMove()
        {
            var gameManager = _gameManager;
            if (gameManager == null)
            {
                return false;
            }

            var undoMoveCount = GetUndoMoveCount(gameManager);
            return _gameManager.CanUndoLastMoves(undoMoveCount);
        }

        public void UndoLastMove()
        {
            var gameManager = _gameManager;
            if (gameManager == null)
            {
                return;
            }

            var undoMoveCount = GetUndoMoveCount(gameManager);
            gameManager.UndoLastMoves(undoMoveCount);
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
                    var movedPiece = previousBoard[move.From].GetPieceType();

                    resultBuilder.AppendFormat(
                        CultureInfo.InvariantCulture,
                        " {0}{1}",
                        movedPiece == PieceType.Pawn || movedPiece == PieceType.None
                            ? string.Empty
                            : movedPiece.GetFenChar().ToString(CultureInfo.InvariantCulture),
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

            var currentBoard = boardHistory.Last();
            if (currentBoard.State.IsOneOf(GameState.Checkmate, GameState.Stalemate))
            {
                resultBuilder.AppendLine();
                resultBuilder.Append(currentBoard.ResultString);
            }

            return resultBuilder.ToString();
        }

        #endregion

        #region Private Methods

        private void ResetSelectionModeInternal(GameWindowSelectionMode? selectionMode)
        {
            _validMoveTargetPositionsInternal.Clear();
            this.CurrentSourcePosition = null;

            if (selectionMode.HasValue)
            {
                this.SelectionMode = selectionMode.Value;
                return;
            }

            var humanChessPlayer = GetActiveHumanPlayer();

            this.SelectionMode = humanChessPlayer == null
                ? GameWindowSelectionMode.None
                : GameWindowSelectionMode.Default;
        }

        private IChessPlayer GetActivePlayer(GameManager gameManager)
        {
            if (gameManager == null)
            {
                return null;
            }

            var activeColor = gameManager.ActiveColor;
            return activeColor == PieceColor.White ? _whitePlayer : _blackPlayer;
        }

        private GuiHumanChessPlayer GetActiveHumanPlayer()
        {
            return GetActivePlayer() as GuiHumanChessPlayer;
        }

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

        private int GetUndoMoveCount(GameManager gameManager)
        {
            if (_whitePlayer is GuiHumanChessPlayer && _blackPlayer is GuiHumanChessPlayer)
            {
                return 1;
            }

            var activePlayer = GetActivePlayer(gameManager);
            var result = !(activePlayer is GuiHumanChessPlayer) ? 1 : 2;
            return result;
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

            var currentGameBoard = boardHistory.Last();

            this.SquareViewModels.Values.DoForEach(
                item =>
                    item.IsLastMoveTarget =
                        currentGameBoard.PreviousMove != null && item.Position == currentGameBoard.PreviousMove.To);

            this.CurrentGameBoard = currentGameBoard;

            RaisePropertyChanged(() => this.IsComputerPlayerActive);
        }

        private void OnGameBoardChanged()
        {
            RefreshBoardHistory();
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

        private IChessPlayer GetActivePlayer()
        {
            return GetActivePlayer(_gameManager);
        }

        private void OnHumanChessPlayerMoveRequested()
        {
            ResetSelectionMode();
        }

        private void OnHumanChessPlayerMoveRequestCancelled()
        {
            ResetSelectionMode();
        }

        private void OnPlayerThinkingStarted()
        {
            RaisePropertyChanged(() => this.IsComputerPlayerActive);
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
                this.OnGameBoardChanged,
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        private void GameManager_PlayerThinkingStarted(object sender, EventArgs eventArgs)
        {
            Task.Factory.StartNew(
                this.OnPlayerThinkingStarted,
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        #endregion
    }
}