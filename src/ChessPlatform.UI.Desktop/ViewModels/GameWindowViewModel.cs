using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private readonly Timer _timeUpdateTimer;
        private GameBoard _currentGameBoard;
        private GameWindowSelectionMode _selectionMode;
        private Position? _currentTargetPosition;
        private GameManager _gameManager;
        private IChessPlayer _whitePlayer;
        private IChessPlayer _blackPlayer;
        private GameBoard[] _boardHistory;
        private bool _isReversedView;
        private string _whitePlayerFeedback;
        private string _blackPlayerFeedback;
        private bool _shouldShowPlayerFeedback;
        private string _whiteTotalElapsedString;
        private string _blackTotalElapsedString;
        private bool _shouldShowPlayersTimers;

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
            ValidMoveTargetPositions = _validMoveTargetPositionsInternal.AsReadOnly();

            _shouldShowPlayerFeedback = true;
            _shouldShowPlayersTimers = true;

            _timeUpdateTimer = new Timer(
                OnUpdatePlayersTimes,
                null,
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan);

            SquareViewModels = ChessHelper
                .AllPositions
                .ToDictionary(Factotum.Identity, position => new BoardSquareViewModel(this, position))
                .AsReadOnly();

            SubscribeToChangeOf(() => IsReversedView, OnIsReversedViewChanged);
            SubscribeToChangeOf(() => CurrentGameBoard, OnCurrentGameBoardChanged);

            InitializeNewGame(
                ChessConstants.DefaultInitialFen,
                ViewModelHelper.CreateGuiHumanChessPlayerInfo(),
                ViewModelHelper.CreateGuiHumanChessPlayerInfo());
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
                RaisePropertyChanged(() => SelectionMode);
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
                RaisePropertyChanged(() => CurrentGameBoard);
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
                RaisePropertyChanged(() => CurrentTargetPosition);
            }
        }

        [NotNull]
        public string MoveHistory => GetMoveHistory();

        public bool IsComputerPlayerActive => GetActiveHumanPlayer() == null;

        public GameResult? GameManagerResult => _gameManager?.Result;

        public AutoDrawType GameManagerAutoDrawType => _gameManager?.AutoDrawType ?? AutoDrawType.None;

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
                RaisePropertyChanged(() => IsReversedView);
            }
        }

        public bool ShouldShowPlayerFeedback
        {
            [DebuggerStepThrough]
            get
            {
                return _shouldShowPlayerFeedback;
            }

            set
            {
                if (value == _shouldShowPlayerFeedback)
                {
                    return;
                }

                _shouldShowPlayerFeedback = value;
                RaisePropertyChanged(() => ShouldShowPlayerFeedback);
            }
        }

        public bool ShouldShowPlayersTimers
        {
            [DebuggerStepThrough]
            get
            {
                return _shouldShowPlayersTimers;
            }

            set
            {
                if (value == _shouldShowPlayersTimers)
                {
                    return;
                }

                _shouldShowPlayersTimers = value;
                RaisePropertyChanged(() => ShouldShowPlayersTimers);
            }
        }

        public Brush UpperPlayerBrush => IsReversedView ? UIHelper.WhitePieceBrush : UIHelper.BlackPieceBrush;

        public Brush LowerPlayerBrush => IsReversedView ? UIHelper.BlackPieceBrush : UIHelper.WhitePieceBrush;

        public string UpperPlayerTitle => GetPlayerTitle(IsReversedView ? _whitePlayer : _blackPlayer);

        public string LowerPlayerTitle => GetPlayerTitle(IsReversedView ? _blackPlayer : _whitePlayer);

        public string UpperPlayerPieceAdvantage
            => IsReversedView ? GetPlayerPieceAdvantage(PieceColor.White) : GetPlayerPieceAdvantage(PieceColor.Black);

        public string LowerPlayerPieceAdvantage
            => IsReversedView ? GetPlayerPieceAdvantage(PieceColor.Black) : GetPlayerPieceAdvantage(PieceColor.White);

        public string UpperPlayerFeedback => IsReversedView ? _whitePlayerFeedback : _blackPlayerFeedback;

        public string LowerPlayerFeedback => IsReversedView ? _blackPlayerFeedback : _whitePlayerFeedback;

        public bool CanRequestMoveNow => IsComputerPlayerActive && _gameManager.State == GameManagerState.Running;

        public string UpperPlayerTotalElapsed => IsReversedView ? _whiteTotalElapsedString : _blackTotalElapsedString;

        public string LowerPlayerTotalElapsed => IsReversedView ? _blackTotalElapsedString : _whiteTotalElapsedString;

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
            [NotNull] IPlayerInfo whitePlayerInfo,
            [NotNull] IPlayerInfo blackPlayerInfo)
        {
            #region Argument Check

            if (!GameBoard.IsValidFen(fen))
            {
                throw new ArgumentException(
                    @"The specified FEN has invalid format or represents an invalid position.",
                    nameof(fen));
            }

            #endregion

            Factotum.DisposeAndNull(ref _gameManager);

            RecreatePlayer(ref _whitePlayer, whitePlayerInfo, PieceColor.White);
            RecreatePlayer(ref _blackPlayer, blackPlayerInfo, PieceColor.Black);

            ResetSelectionMode();

            _gameManager = new GameManager(_whitePlayer, _blackPlayer, fen);
            _gameManager.GameBoardChanged += GameManager_GameBoardChanged;
            _gameManager.PlayerThinkingStarted += GameManager_PlayerThinkingStarted;
            _gameManager.UnhandledExceptionOccurred += GameManager_UnhandledExceptionOccurred;

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

            _timeUpdateTimer.Change(0, 50);
            _gameManager.Play();
        }

        public void RequestMoveNow()
        {
            if (CanRequestMoveNow)
            {
                _gameManager.RequestMoveNow();
            }
        }

        public void MakeMove(GameMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
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

            _whitePlayerFeedback = _blackPlayerFeedback = null;
            AffectPlayerInfo();
        }

        #endregion

        #region Private Methods: Regular

        private static string GetPlayerTitle(IChessPlayer player)
        {
            var isHumanPlayer = player is GuiHumanChessPlayer;
            return isHumanPlayer ? "Human Player" : "Computer";
        }

        private static Dictionary<PieceType, int> GetPieceCounts([NotNull] GameBoard board, PieceColor color)
        {
            return ChessConstants
                .PieceTypesExceptNone
                .ToDictionary(Factotum.Identity, item => board.GetPositions(item.ToPiece(color)).Length);
        }

        private string GetPlayerPieceAdvantage(PieceColor color)
        {
            var currentGameBoard = CurrentGameBoard;
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
            CurrentSourcePosition = null;

            if (selectionMode.HasValue)
            {
                SelectionMode = selectionMode.Value;
                return;
            }

            var humanChessPlayer = GetActiveHumanPlayer();

            SelectionMode = humanChessPlayer == null || _gameManager == null
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

            var currentGameBoard = CurrentGameBoard;
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

            CurrentSourcePosition = currentSourcePosition;
            SelectionMode = selectionMode;
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
            RaisePropertyChanged(() => MoveHistory);

            var currentGameBoard = boardHistory.Last();
            CurrentGameBoard = currentGameBoard;

            RaisePropertyChanged(() => IsComputerPlayerActive);
            RaisePropertyChanged(() => CanRequestMoveNow);
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

                var standardAlgebraicNotation = previousBoard.GetStandardAlgebraicNotation(board.PreviousMove);
                resultBuilder.Append($" {standardAlgebraicNotation}");

                previousBoard = board;
            }

            if (currentBoard.State.IsGameFinished() || currentBoard.GetAutoDrawType() != AutoDrawType.None)
            {
                resultBuilder.AppendLine();
                resultBuilder.Append(currentBoard.ResultString);
            }

            return resultBuilder.ToString();
        }

        private void OnGameBoardChanged()
        {
            _dispatcher.Invoke(RefreshBoardHistory, DispatcherPriority.Render);

            var gameManager = _gameManager;
            if (gameManager != null)
            {
                _dispatcher.Invoke(gameManager.Play, DispatcherPriority.ContextIdle);
            }
        }

        private void RecreatePlayer(ref IChessPlayer player, [NotNull] IPlayerInfo playerInfo, PieceColor color)
        {
            if (player != null)
            {
                player.FeedbackProvided -= Player_FeedbackProvided;
            }

            var oldGuiHumanChessPlayer = player as GuiHumanChessPlayer;
            if (oldGuiHumanChessPlayer != null)
            {
                oldGuiHumanChessPlayer.MoveRequested -= GuiHumanChessPlayer_MoveRequested;
                oldGuiHumanChessPlayer.MoveRequestCancelled -= GuiHumanChessPlayer_MoveRequestCancelled;
            }

            player = playerInfo.CreatePlayer(color).EnsureNotNull();

            player.FeedbackProvided += Player_FeedbackProvided;

            var newGuiHumanChessPlayer = player as GuiHumanChessPlayer;
            if (newGuiHumanChessPlayer != null)
            {
                newGuiHumanChessPlayer.MoveRequested += GuiHumanChessPlayer_MoveRequested;
                newGuiHumanChessPlayer.MoveRequestCancelled += GuiHumanChessPlayer_MoveRequestCancelled;
            }
        }

        [CanBeNull]
        private IChessPlayer GetActivePlayer()
        {
            return GetActivePlayer(_gameManager);
        }

        private void AffectPlayerInfo()
        {
            ExecuteOnDispatcher(AffectPlayerInfoUnsafe, DispatcherPriority.Send);
        }

        private void AffectPlayerInfoUnsafe()
        {
            RaisePropertyChanged(() => UpperPlayerBrush);
            RaisePropertyChanged(() => LowerPlayerBrush);

            RaisePropertyChanged(() => UpperPlayerTitle);
            RaisePropertyChanged(() => LowerPlayerTitle);

            RaisePropertyChanged(() => UpperPlayerPieceAdvantage);
            RaisePropertyChanged(() => LowerPlayerPieceAdvantage);

            RaisePropertyChanged(() => UpperPlayerFeedback);
            RaisePropertyChanged(() => LowerPlayerFeedback);

            RaisePropertyChanged(() => UpperPlayerTotalElapsed);
            RaisePropertyChanged(() => LowerPlayerTotalElapsed);
        }

        private void OnNewGameStarted()
        {
            _whitePlayerFeedback = null;
            _blackPlayerFeedback = null;
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
            var activeColor = GetActivePlayer()?.Color;

            if (!activeColor.HasValue || activeColor.Value == PieceColor.White)
            {
                _whitePlayerFeedback = null;
            }

            if (!activeColor.HasValue || activeColor.Value == PieceColor.Black)
            {
                _blackPlayerFeedback = null;
            }

            AffectPlayerInfo();

            RaisePropertyChanged(() => IsComputerPlayerActive);
            RaisePropertyChanged(() => CanRequestMoveNow);
        }

        #endregion

        #region Private Methods: Event Handlers

        private static void GameManager_UnhandledExceptionOccurred(
            object sender,
            ThreadExceptionEventArgs eventArgs)
        {
            App.ProcessUnhandledException(eventArgs.Exception);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FormatElapsedTime(TimeSpan elapsedTime)
            => elapsedTime.ToString(@"d'.'hh':'mm':'ss'.'f");

        private void GuiHumanChessPlayer_MoveRequested(object sender, EventArgs eventArgs)
        {
            Task.Factory.StartNew(
                OnHumanChessPlayerMoveRequested,
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        private void GuiHumanChessPlayer_MoveRequestCancelled(object sender, EventArgs eventArgs)
        {
            Task.Factory.StartNew(
                OnHumanChessPlayerMoveRequestCancelled,
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        private void GameManager_GameBoardChanged(object sender, EventArgs eventArgs)
        {
            Task.Factory.StartNew(
                OnGameBoardChanged,
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        private void GameManager_PlayerThinkingStarted(object sender, EventArgs eventArgs)
        {
            Task.Factory.StartNew(
                OnPlayerThinkingStarted,
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        private void Player_FeedbackProvided(object sender, ChessPlayerFeedbackEventArgs args)
        {
            var feedback =
                $@"D={args.Depth}/{args.MaxDepth}{Environment.NewLine}{args.Variation.ValueString}{Environment.NewLine
                    }PV: {args.Board.GetStandardAlgebraicNotation(args.Variation.Moves)}";

            if (args.Color == PieceColor.White)
            {
                _whitePlayerFeedback = feedback;
            }
            else
            {
                _blackPlayerFeedback = feedback;
            }

            AffectPlayerInfo();
        }

        private void OnIsReversedViewChanged(object sender, EventArgs e)
        {
            AffectPlayerInfo();
        }

        private void OnCurrentGameBoardChanged(object sender, EventArgs e)
        {
            AffectPlayerInfo();
        }

        private void OnUpdatePlayersTimes(object state)
        {
            var gameManager = _gameManager;
            if (gameManager == null)
            {
                _whiteTotalElapsedString = null;
                _blackTotalElapsedString = null;
                return;
            }

            var whiteTotalElapsedString = FormatElapsedTime(gameManager.WhiteTotalElapsed);
            var blackTotalElapsedString = FormatElapsedTime(gameManager.BlackTotalElapsed);

            var oldWhiteTotalElapsedString = Interlocked.Exchange(
                ref _whiteTotalElapsedString,
                whiteTotalElapsedString);

            var oldBlackTotalElapsedString = Interlocked.Exchange(
                ref _blackTotalElapsedString,
                blackTotalElapsedString);

            if (oldWhiteTotalElapsedString != _whiteTotalElapsedString
                || oldBlackTotalElapsedString != _blackTotalElapsedString)
            {
                AffectPlayerInfo();
            }
        }

        #endregion
    }
}