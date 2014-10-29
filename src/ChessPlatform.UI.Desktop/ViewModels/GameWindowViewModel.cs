using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using ChessPlatform.GamePlay;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop.ViewModels
{
    internal sealed class GameWindowViewModel : ViewModelBase
    {
        #region Constants and Fields

        private readonly TaskScheduler _taskScheduler;
        private readonly Dispatcher _dispatcher;
        private readonly HashSet<Position> _validMoveTargetPositionsInternal;
        private GameBoard _currentGameBoard;
        private GameWindowSelectionMode _selectionMode;
        private Position? _currentTargetPosition;
        private GameManager _gameManager;
        private IChessPlayer _whitePlayer;
        private IChessPlayer _blackPlayer;
        private GameBoard[] _boardHistory;
        private bool _isReversedView;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameWindowViewModel"/> class.
        /// </summary>
        public GameWindowViewModel()
        {
            _taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            _dispatcher = Dispatcher.CurrentDispatcher;

            _validMoveTargetPositionsInternal = new HashSet<Position>();

            _selectionMode = GameWindowSelectionMode.Default;
            this.ValidMoveTargetPositions = _validMoveTargetPositionsInternal.AsReadOnly();

            this.SquareViewModels = ChessHelper
                .AllPositions
                .ToDictionary(Factotum.Identity, position => new BoardSquareViewModel(this, position))
                .AsReadOnly();

            SubscribeToChangeOf(() => this.IsReversedView, this.OnIsReversedViewChanged);
            SubscribeToChangeOf(() => this.CurrentGameBoard, this.OnCurrentGameBoardChanged);

            InitializeNewGame(ChessConstants.DefaultInitialFen, null, null);
        }

        #endregion

        #region Public Properties

        [NotNull]
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

        [CanBeNull]
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

        [NotNull]
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

        public GameResult? GameManagerResult
        {
            get
            {
                return _gameManager == null ? null : _gameManager.Result;
            }
        }

        public AutoDrawType GameManagerAutoDrawType
        {
            get
            {
                return _gameManager == null ? AutoDrawType.None : _gameManager.AutoDrawType;
            }
        }

        public bool IsReversedView
        {
            [DebuggerStepThrough]
            get
            {
                return _isReversedView;
            }

            set
            {
                if (_isReversedView == value)
                {
                    return;
                }

                _isReversedView = value;
                RaisePropertyChanged(() => this.IsReversedView);
            }
        }

        public Brush UpperPlayerBrush
        {
            get
            {
                return this.IsReversedView ? UIHelper.WhitePieceBrush : UIHelper.BlackPieceBrush;
            }
        }

        public Brush LowerPlayerBrush
        {
            get
            {
                return this.IsReversedView ? UIHelper.BlackPieceBrush : UIHelper.WhitePieceBrush;
            }
        }

        public string UpperPlayerTitle
        {
            get
            {
                var player = this.IsReversedView ? _whitePlayer : _blackPlayer;
                return GetPlayerTitle(player);
            }
        }

        public string LowerPlayerTitle
        {
            get
            {
                var player = this.IsReversedView ? _blackPlayer : _whitePlayer;
                return GetPlayerTitle(player);
            }
        }

        public string UpperPlayerPieceAdvantage
        {
            get
            {
                return this.IsReversedView
                    ? GetPlayerPieceAdvantage(PieceColor.White)
                    : GetPlayerPieceAdvantage(PieceColor.Black);
            }
        }

        public string LowerPlayerPieceAdvantage
        {
            get
            {
                return this.IsReversedView
                    ? GetPlayerPieceAdvantage(PieceColor.Black)
                    : GetPlayerPieceAdvantage(PieceColor.White);
            }
        }

        #endregion

        #region Internal Properties

        [NotNull]
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

        public void InitializeNewGame(
            [NotNull] string fen,
            [CanBeNull] PlayerInfo whitePlayerInfo,
            [CanBeNull] PlayerInfo blackPlayerInfo)
        {
            #region Argument Check

            if (!GameBoard.IsValidFen(fen))
            {
                throw new ArgumentException(
                    @"The specified FEN has invalid format or represents an invalid position.",
                    "fen");
            }

            #endregion

            Factotum.DisposeAndNull(ref _gameManager);

            CreatePlayer(ref _whitePlayer, whitePlayerInfo, PieceColor.White);
            CreatePlayer(ref _blackPlayer, blackPlayerInfo, PieceColor.Black);

            ResetSelectionMode();

            _gameManager = new GameManager(_whitePlayer, _blackPlayer, fen);
            _gameManager.GameBoardChanged += this.GameManager_GameBoardChanged;
            _gameManager.PlayerThinkingStarted += this.GameManager_PlayerThinkingStarted;

            RefreshBoardHistory();
            OnNewGameStarted();
        }

        public void Play()
        {
            if (_gameManager == null)
            {
                throw new InvalidOperationException("Game Manager is not initialized.");
            }

            ResetSelectionMode();

            _gameManager.Play();
        }

        public void MakeMove(GameMove move)
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

        #endregion

        #region Private Methods: Regular

        private static string GetPlayerTitle(IChessPlayer player)
        {
            var isHumanPlayer = player is GuiHumanChessPlayer;
            return isHumanPlayer ? "Human Player" : "Computer";
        }

        private static Dictionary<PieceType, int> GetPieceCounts([NotNull] IGameBoard board, PieceColor color)
        {
            return ChessConstants
                .PieceTypesExceptNone
                .ToDictionary(Factotum.Identity, item => board.GetPositions(item.ToPiece(color)).Length);
        }

        private string GetPlayerPieceAdvantage(PieceColor color)
        {
            var currentGameBoard = this.CurrentGameBoard;
            if (currentGameBoard == null)
            {
                return string.Empty;
            }

            var counts = GetPieceCounts(currentGameBoard, color);
            var opponentCounts = GetPieceCounts(currentGameBoard, color.Invert());

            const string Separator = " ";

            var result = ChessConstants
                .PieceTypesExceptNone
                .Select(
                    item =>
                        new
                        {
                            Item = item,
                            Advantage = counts.GetValueOrDefault(item) - opponentCounts.GetValueOrDefault(item)
                        })
                .Where(obj => obj.Advantage > 0)
                .OrderByDescending(obj => obj.Item)
                .Select(obj => Enumerable.Repeat(UIHelper.PieceToSymbolMap[obj.Item], obj.Advantage).Join(Separator))
                .Join(Separator);

            return result;
        }

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

            this.SelectionMode = humanChessPlayer == null || _gameManager == null
                || _gameManager.State == GameManagerState.GameFinished
                ? GameWindowSelectionMode.None
                : GameWindowSelectionMode.Default;
        }

        [CanBeNull]
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
            _validMoveTargetPositionsInternal.Clear();

            var currentGameBoard = this.CurrentGameBoard;
            if (currentGameBoard == null)
            {
                return;
            }

            var validMoves = currentGameBoard.GetValidMovesBySource(currentSourcePosition);
            if (validMoves.Length == 0)
            {
                return;
            }

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
            this.CurrentGameBoard = currentGameBoard;

            RaisePropertyChanged(() => this.IsComputerPlayerActive);
        }

        [NotNull]
        private string GetMoveHistory()
        {
            const string EventName = "Chess Platform Game";

            var resultBuilder = new StringBuilder();

            var boardHistory = _boardHistory;
            var gameManager = _gameManager;

            var currentBoard = boardHistory.Last();

            resultBuilder
                .AppendFormat(CultureInfo.InvariantCulture, @"[Event ""{0}""]", EventName)
                .AppendLine();

            ////resultBuilder
            ////    .AppendFormat(CultureInfo.InvariantCulture, @"[Site ""{0}""]", Environment.MachineName)
            ////    .AppendLine();

            resultBuilder
                .AppendFormat(CultureInfo.InvariantCulture, @"[Date ""{0:yyyy.MM.dd}""]", DateTime.Now)
                .AppendLine();

            resultBuilder
                .AppendFormat(CultureInfo.InvariantCulture, @"[Result ""{0}""]", currentBoard.ResultString)
                .AppendLine();

            if (gameManager != null)
            {
                resultBuilder
                    .AppendFormat(CultureInfo.InvariantCulture, @"[White ""{0}""]", gameManager.White.Name)
                    .AppendLine();

                resultBuilder
                    .AppendFormat(CultureInfo.InvariantCulture, @"[Black ""{0}""]", gameManager.Black.Name)
                    .AppendLine();
            }

            var initialBoard = boardHistory[0];

            var initialFen = initialBoard.GetFen();
            var isDefaultInitialPosition = initialFen == ChessConstants.DefaultInitialFen;
            if (!isDefaultInitialPosition)
            {
                resultBuilder.AppendLine(@"[SetUp ""1""]");

                resultBuilder
                    .AppendFormat(CultureInfo.InvariantCulture, @"[FEN ""{0}""]", initialFen)
                    .AppendLine();
            }

            var previousBoard = initialBoard;
            var moveIndex = unchecked(previousBoard.FullMoveIndex - 1);
            for (var index = 1; index < boardHistory.Length; index++)
            {
                var board = boardHistory[index];

                if (moveIndex != previousBoard.FullMoveIndex)
                {
                    if (resultBuilder.Length != 0)
                    {
                        resultBuilder.AppendLine();
                    }

                    moveIndex = previousBoard.FullMoveIndex;
                    resultBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}.", moveIndex);

                    if (index == 1 && initialBoard.ActiveColor == PieceColor.Black)
                    {
                        resultBuilder.Append(" ...");
                    }
                }

                var move = board.PreviousMove.EnsureNotNull();

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

            if (currentBoard.State.IsGameFinished() || currentBoard.GetAutoDrawType() != AutoDrawType.None)
            {
                resultBuilder.AppendLine();
                resultBuilder.Append(currentBoard.ResultString);
            }

            if (boardHistory.Length > 1)
            {
                resultBuilder.AppendLine();
                resultBuilder.AppendLine();
                resultBuilder.AppendFormat(CultureInfo.InvariantCulture, @"{{ FEN ""{0}"" }}", currentBoard.GetFen());
            }

            return resultBuilder.ToString();
        }

        private void OnGameBoardChanged()
        {
            _dispatcher.Invoke(this.RefreshBoardHistory, DispatcherPriority.Render);

            var gameManager = _gameManager;
            if (gameManager != null)
            {
                _dispatcher.Invoke(gameManager.Play, DispatcherPriority.ContextIdle);
            }
        }

        [NotNull]
        private GuiHumanChessPlayer CreateGuiHumanChessPlayer(PieceColor color)
        {
            var player = new GuiHumanChessPlayer(color);
            player.MoveRequested += this.GuiHumanChessPlayer_MoveRequested;
            player.MoveRequestCancelled += this.GuiHumanChessPlayer_MoveRequestCancelled;

            return player;
        }

        private void CreatePlayer(ref IChessPlayer player, [CanBeNull] PlayerInfo playerInfo, PieceColor color)
        {
            var guiHumanChessPlayer = player as GuiHumanChessPlayer;
            if (guiHumanChessPlayer != null)
            {
                guiHumanChessPlayer.MoveRequested -= this.GuiHumanChessPlayer_MoveRequested;
                guiHumanChessPlayer.MoveRequestCancelled -= this.GuiHumanChessPlayer_MoveRequestCancelled;
            }

            player = playerInfo == null ? CreateGuiHumanChessPlayer(color) : playerInfo.PlayerFactory(color);
        }

        [CanBeNull]
        private IChessPlayer GetActivePlayer()
        {
            return GetActivePlayer(_gameManager);
        }

        private void AffectPlayerInfo()
        {
            RaisePropertyChanged(() => this.UpperPlayerBrush);
            RaisePropertyChanged(() => this.LowerPlayerBrush);

            RaisePropertyChanged(() => this.UpperPlayerTitle);
            RaisePropertyChanged(() => this.LowerPlayerTitle);

            RaisePropertyChanged(() => this.UpperPlayerPieceAdvantage);
            RaisePropertyChanged(() => this.LowerPlayerPieceAdvantage);
        }

        private void OnNewGameStarted()
        {
            AffectPlayerInfo();
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

        #endregion

        #region Private Methods: Event Handlers

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

        private void OnIsReversedViewChanged(object sender, EventArgs e)
        {
            AffectPlayerInfo();
        }

        private void OnCurrentGameBoardChanged(object sender, EventArgs e)
        {
            AffectPlayerInfo();
        }

        #endregion
    }
}