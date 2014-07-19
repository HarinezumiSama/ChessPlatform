using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using ChessPlatform.Internal;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public sealed class GameBoard : IGameBoard
    {
        #region Constants and Fields

        private const int ThreefoldCount = 3;
        private const int ValidMoveCapacity = 64;

        private readonly GameBoardData _gameBoardData;

        private readonly PieceColor _activeColor;
        private readonly GameState _state;
        private readonly AutoDrawType _autoDrawType;
        private readonly CastlingOptions _castlingOptions;
        private readonly EnPassantCaptureInfo _enPassantCaptureInfo;
        private readonly ReadOnlyDictionary<GameMove, GameMoveInfo> _validMoves;
        private readonly int _halfMoveCountBy50MoveRule;
        private readonly int _fullMoveIndex;
        private readonly GameMove _previousMove;
        private readonly Piece _lastCapturedPiece;
        private readonly string _resultString;
        private readonly bool _validateAfterMove;
        private readonly GameBoard _previousBoard;
        private readonly ReadOnlyDictionary<PackedGameBoard, int> _repetitions;

        private PackedGameBoard _packedGameBoard;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameBoard"/> class
        ///     using the default initial chess position.
        /// </summary>
        public GameBoard()
            : this(false)
        {
            // Nothing to do
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameBoard"/> class
        ///     using the default initial chess position.
        /// </summary>
        public GameBoard(bool validateAfterMove)
        {
            _validateAfterMove = validateAfterMove;
            _gameBoardData = new GameBoardData();
            _lastCapturedPiece = Piece.None;

            SetupDefault(
                out _activeColor,
                out _castlingOptions,
                out _enPassantCaptureInfo,
                out _halfMoveCountBy50MoveRule,
                out _fullMoveIndex);

            FinishInitialization(
                true,
                out _validMoves,
                out _state,
                out _autoDrawType,
                out _resultString,
                out _repetitions);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameBoard"/> class
        ///     using the specified FEN.
        /// </summary>
        public GameBoard([NotNull] string fen)
            : this(fen, false)
        {
            // Nothing to do
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameBoard"/> class
        ///     using the specified FEN.
        /// </summary>
        public GameBoard([NotNull] string fen, bool validateAfterMove)
        {
            #region Argument Check

            if (string.IsNullOrWhiteSpace(fen))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    "fen");
            }

            #endregion

            _validateAfterMove = validateAfterMove;
            _gameBoardData = new GameBoardData();
            _lastCapturedPiece = Piece.None;

            SetupByFen(
                fen,
                out _activeColor,
                out _castlingOptions,
                out _enPassantCaptureInfo,
                out _halfMoveCountBy50MoveRule,
                out _fullMoveIndex);

            FinishInitialization(
                true,
                out _validMoves,
                out _state,
                out _autoDrawType,
                out _resultString,
                out _repetitions);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameBoard"/> class
        ///     using the specified previous state and specified move.
        /// </summary>
        private GameBoard([NotNull] GameBoard previousBoard, [CanBeNull] GameMove move)
        {
            #region Argument Check

            if (previousBoard == null)
            {
                throw new ArgumentNullException("previousBoard");
            }

            #endregion

            _previousBoard = previousBoard;
            _validateAfterMove = previousBoard._validateAfterMove;
            _gameBoardData = previousBoard._gameBoardData.Copy();
            _activeColor = previousBoard._activeColor.Invert();
            _castlingOptions = previousBoard._castlingOptions;

            _fullMoveIndex = previousBoard._fullMoveIndex + (move != null && _activeColor == PieceColor.White ? 1 : 0);

            if (move == null)
            {
                _enPassantCaptureInfo = previousBoard._enPassantCaptureInfo;
                _previousMove = previousBoard._previousMove;
                _lastCapturedPiece = previousBoard._lastCapturedPiece;
                _halfMoveCountBy50MoveRule = previousBoard._halfMoveCountBy50MoveRule;
            }
            else
            {
                _enPassantCaptureInfo = _gameBoardData.GetEnPassantCaptureInfo(move);

                var makeMoveData = _gameBoardData.MakeMove(
                    move,
                    previousBoard._activeColor,
                    previousBoard._enPassantCaptureInfo,
                    ref _castlingOptions);

                _previousMove = makeMoveData.Move;
                _lastCapturedPiece = makeMoveData.CapturedPiece;

                _halfMoveCountBy50MoveRule = makeMoveData.ShouldKeepCountingBy50MoveRule
                    ? previousBoard._halfMoveCountBy50MoveRule + 1
                    : 0;
            }

            FinishInitialization(
                false,
                out _validMoves,
                out _state,
                out _autoDrawType,
                out _resultString,
                out _repetitions);
        }

        #endregion

        #region Public Properties

        public string ResultString
        {
            [DebuggerStepThrough]
            get
            {
                return _resultString;
            }
        }

        [CanBeNull]
        public GameBoard PreviousBoard
        {
            [DebuggerStepThrough]
            get
            {
                return _previousBoard;
            }
        }

        #endregion

        #region IGameBoard Members: Properties

        public PieceColor ActiveColor
        {
            [DebuggerStepThrough]
            get
            {
                return _activeColor;
            }
        }

        public GameState State
        {
            [DebuggerStepThrough]
            get
            {
                return _state;
            }
        }

        public CastlingOptions CastlingOptions
        {
            [DebuggerStepThrough]
            get
            {
                return _castlingOptions;
            }
        }

        public EnPassantCaptureInfo EnPassantCaptureInfo
        {
            [DebuggerStepThrough]
            get
            {
                return _enPassantCaptureInfo;
            }
        }

        public ReadOnlyDictionary<GameMove, GameMoveInfo> ValidMoves
        {
            [DebuggerStepThrough]
            get
            {
                return _validMoves;
            }
        }

        public int FullMoveCountBy50MoveRule
        {
            [DebuggerStepThrough]
            get
            {
                return _halfMoveCountBy50MoveRule / 2;
            }
        }

        public int FullMoveIndex
        {
            [DebuggerStepThrough]
            get
            {
                return _fullMoveIndex;
            }
        }

        IGameBoard IGameBoard.PreviousBoard
        {
            [DebuggerStepThrough]
            get
            {
                return this.PreviousBoard;
            }
        }

        public GameMove PreviousMove
        {
            [DebuggerStepThrough]
            get
            {
                return _previousMove;
            }
        }

        public Piece LastCapturedPiece
        {
            [DebuggerStepThrough]
            get
            {
                return _lastCapturedPiece;
            }
        }

        public bool CanMakeNullMove
        {
            get
            {
                return !_state.IsAnyCheck();
            }
        }

        public Piece this[Position position]
        {
            get
            {
                return _gameBoardData[position];
            }
        }

        #endregion

        #region Internal Properties

        internal int HalfMoveCountBy50MoveRule
        {
            [DebuggerStepThrough]
            get
            {
                return _halfMoveCountBy50MoveRule;
            }
        }

        #endregion

        #region Public Methods

        [DebuggerStepThrough]
        public static bool IsValidFen(string fen)
        {
            if (!ChessHelper.IsValidFenFormat(fen))
            {
                return false;
            }

            try
            {
                //// TODO [vmcl] Create FEN verification which is NOT exception based
                // ReSharper disable once ObjectCreationAsStatement
                new GameBoard(fen);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return GetFen();
        }

        [NotNull]
        [CLSCompliant(false)]
        public PerftResult Perft(int depth, PerftFlags flags)
        {
            #region Argument Check

            if (depth < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "depth",
                    depth,
                    @"The value cannot be negative.");
            }

            #endregion

            var perftData = new PerftData();
            var includeDivideMap = flags.HasFlag(PerftFlags.IncludeDivideMap);
            var includeExtraCountTypes = flags.HasFlag(PerftFlags.IncludeExtraCountTypes);
            var canUseParallelism = !flags.HasFlag(PerftFlags.DisableParallelism);

            var stopwatch = Stopwatch.StartNew();
            PerftInternal(this, depth, canUseParallelism, perftData, includeDivideMap, includeExtraCountTypes);
            stopwatch.Stop();

            return new PerftResult(
                flags,
                depth,
                stopwatch.Elapsed,
                perftData.NodeCount,
                perftData.DividedMoves,
                includeExtraCountTypes ? perftData.CheckCount : (ulong?)null,
                includeExtraCountTypes ? perftData.CheckmateCount : (ulong?)null);
        }

        [NotNull]
        [CLSCompliant(false)]
        public PerftResult Perft(int depth)
        {
            return Perft(depth, PerftFlags.None);
        }

        public GameBoard MakeMove([NotNull] GameMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            if (!_validMoves.ContainsKey(move))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "The move '{0}' is not valid.", move),
                    "move");
            }

            #endregion

            return new GameBoard(this, move);
        }

        public GameBoard MakeNullMove()
        {
            if (!this.CanMakeNullMove)
            {
                throw new InvalidOperationException(@"The null move is not allowed.");
            }

            return new GameBoard(this, null);
        }

        [NotNull]
        public GameBoard[] GetHistory()
        {
            var boards = new List<GameBoard>(_fullMoveIndex * 2);

            var currentBoard = this;
            while (currentBoard != null)
            {
                boards.Add(currentBoard);
                currentBoard = currentBoard._previousBoard;
            }

            boards.Reverse();

            return boards.ToArray();
        }

        #endregion

        #region IGameBoard Members: Methods

        public string GetFen()
        {
            var pieceDataSnippet = _gameBoardData.GetFenSnippet();
            var activeColorSnippet = _activeColor.GetFenSnippet();
            var castlingOptionsSnippet = _castlingOptions.GetFenSnippet();
            var enPassantCaptureInfoSnippet = _enPassantCaptureInfo.GetFenSnippet();

            var result = string.Join(
                ChessConstants.FenSnippetSeparator,
                pieceDataSnippet,
                activeColorSnippet,
                castlingOptionsSnippet,
                enPassantCaptureInfoSnippet,
                _halfMoveCountBy50MoveRule.ToString(CultureInfo.InvariantCulture),
                _fullMoveIndex.ToString(CultureInfo.InvariantCulture));

            return result;
        }

        public PieceInfo GetPieceInfo(Position position)
        {
            return _gameBoardData.GetPieceInfo(position);
        }

        public Position[] GetPositions(Piece piece)
        {
            return _gameBoardData.GetPositions(piece);
        }

        public Position[] GetPositions(PieceColor color)
        {
            return _gameBoardData.GetPositions(color);
        }

        public bool IsValidMove(GameMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            return _validMoves.ContainsKey(move);
        }

        public bool IsPawnPromotionMove(GameMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            var result = _gameBoardData.IsPawnPromotion(move.From, move.To);
            return result;
        }

        public bool IsCapturingMove(GameMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            if (_gameBoardData.IsEnPassantCapture(move.From, move.To, _enPassantCaptureInfo))
            {
                return true;
            }

            var sourcePieceInfo = GetPieceInfo(move.From);
            var destinationPieceInfo = GetPieceInfo(move.To);

            var result = sourcePieceInfo.Color == _activeColor
                && destinationPieceInfo.Color == _activeColor.Invert();

            return result;
        }

        public CastlingInfo CheckCastlingMove(GameMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            return this.ValidMoves.ContainsKey(move) ? _gameBoardData.CheckCastlingMove(move) : null;
        }

        public Position[] GetAttacks(Position targetPosition, PieceColor attackingColor)
        {
            var bitboard = _gameBoardData.GetAttackers(targetPosition, attackingColor);
            return bitboard.GetPositions();
        }

        public GameMove[] GetValidMovesBySource(Position sourcePosition)
        {
            return this.ValidMoves.Keys.Where(move => move.From == sourcePosition).ToArray();
        }

        public GameMove[] GetValidMovesByDestination(Position destinationPosition)
        {
            return this.ValidMoves.Keys.Where(move => move.To == destinationPosition).ToArray();
        }

        public AutoDrawType GetAutoDrawType()
        {
            return _autoDrawType;
        }

        public PackedGameBoard Pack()
        {
            // ReSharper disable once ConvertIfStatementToNullCoalescingExpression
            if (_packedGameBoard == null)
            {
                _packedGameBoard = new PackedGameBoard(this);
            }

            return _packedGameBoard;
        }

        IGameBoard IGameBoard.MakeMove(GameMove move)
        {
            return MakeMove(move);
        }

        IGameBoard IGameBoard.MakeNullMove()
        {
            return MakeNullMove();
        }

        IGameBoard[] IGameBoard.GetHistory()
        {
            // ReSharper disable once CoVariantArrayConversion
            return GetHistory();
        }

        #endregion

        #region Private Methods

        private static bool IsValidMoveByPinning(
            IDictionary<Position, Bitboard> pinnedPieceMap,
            Position sourcePosition,
            Position destinationPosition)
        {
            Bitboard bitboard;
            var found = pinnedPieceMap.TryGetValue(sourcePosition, out bitboard);
            return !found || ((bitboard & destinationPosition.Bitboard) == destinationPosition.Bitboard);
        }

        private static void AddMove(
            AddMoveData addMoveData,
            Position sourcePosition,
            Position targetPosition,
            bool isCapture,
            Func<GameMove, bool> checkMove)
        {
            var isPawnPromotion = addMoveData.GameBoardData.IsPawnPromotion(sourcePosition, targetPosition);
            var promotionResult = isPawnPromotion ? ChessHelper.DefaultPromotion : PieceType.None;
            var basicMove = new GameMove(sourcePosition, targetPosition, promotionResult);

            if (checkMove != null && !checkMove(basicMove))
            {
                return;
            }

            var moveFlags = GameMoveFlags.None;
            if (isPawnPromotion)
            {
                moveFlags |= GameMoveFlags.IsPawnPromotion;
            }

            if (isCapture)
            {
                moveFlags |= GameMoveFlags.IsCapture;
            }

            var isEnPassantCapture = addMoveData.GameBoardData.IsEnPassantCapture(
                sourcePosition,
                targetPosition,
                addMoveData.EnPassantCaptureInfo);

            if (isEnPassantCapture)
            {
                moveFlags |= GameMoveFlags.IsEnPassantCapture;
            }

            var pieceMoveInfo = new GameMoveInfo(moveFlags);
            addMoveData.ValidMovesReference.Add(basicMove, pieceMoveInfo);

            if (isPawnPromotion)
            {
                ChessHelper.NonDefaultPromotions.Select(basicMove.MakePromotion).DoForEach(
                    move => addMoveData.ValidMovesReference.Add(move, pieceMoveInfo));
            }
        }

        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        private static KingMoveInfo[] GetActiveKingMoves(
            GameBoardData gameBoardData,
            GameBoardData noActiveKingGameBoardData,
            Position activeKingPosition,
            CastlingOptions castlingOptions,
            PieceColor oppositeColor,
            bool isInCheck)
        {
            var result = gameBoardData
                .GetPotentialMovePositions(
                    isInCheck ? CastlingOptions.None : castlingOptions,
                    null,
                    activeKingPosition)
                .Where(position => !noActiveKingGameBoardData.IsUnderAttack(position, oppositeColor))
                .Select(position => new GameMove(activeKingPosition, position))
                .Where(
                    move =>
                        isInCheck
                            || gameBoardData
                                .CheckCastlingMove(move)
                                .Morph(info => !gameBoardData.IsUnderAttack(info.PassedPosition, oppositeColor), true))
                .Select(
                    move =>
                        new KingMoveInfo(
                            move,
                            gameBoardData[move.To] == Piece.None ? GameMoveFlags.None : GameMoveFlags.IsCapture))
                .ToArray();

            return result;
        }

        private static void InitializeValidMovesAndStateWhenNotInCheck(AddMoveData addMoveData)
        {
            PopulatePawnMoves(addMoveData);

            var gameBoardData = addMoveData.GameBoardData;
            var activeColor = addMoveData.ActiveColor;
            var enPassantCaptureInfo = addMoveData.EnPassantCaptureInfo;

            var activePiecesExceptKingAndPawnsBitboard = addMoveData.ActivePiecesExceptKingBitboard
                & ~gameBoardData.GetBitboard(PieceType.Pawn.ToPiece(activeColor));

            var sourcePositions = activePiecesExceptKingAndPawnsBitboard.GetPositions();
            foreach (var sourcePosition in sourcePositions)
            {
                var potentialMovePositions = gameBoardData.GetPotentialMovePositions(
                    addMoveData.CastlingOptions,
                    enPassantCaptureInfo,
                    sourcePosition);

                var filteredDestinationPositions = potentialMovePositions
                    .Where(position => IsValidMoveByPinning(addMoveData.PinnedPieceMap, sourcePosition, position))
                    .ToArray();

                foreach (var destinationPosition in filteredDestinationPositions)
                {
                    var isEnPassantCapture = gameBoardData.IsEnPassantCapture(
                        sourcePosition,
                        destinationPosition,
                        enPassantCaptureInfo);
                    if (isEnPassantCapture)
                    {
                        var temporaryCastlingOptions = addMoveData.CastlingOptions;

                        var move = new GameMove(sourcePosition, destinationPosition);

                        gameBoardData.MakeMove(
                            move,
                            activeColor,
                            enPassantCaptureInfo,
                            ref temporaryCastlingOptions);

                        var isInvalidMove = gameBoardData.IsInCheck(activeColor);
                        gameBoardData.UndoMove();

                        if (isInvalidMove)
                        {
                            continue;
                        }
                    }

                    AddMove(
                        addMoveData,
                        sourcePosition,
                        destinationPosition,
                        isEnPassantCapture || gameBoardData[destinationPosition] != Piece.None,
                        null);
                }
            }
        }

        private static void InitializeValidMovesAndStateWhenInSingleCheck(
            AddMoveData addMoveData,
            Piece activeKing,
            Position activeKingPosition,
            Bitboard checkAttackPositionsBitboard)
        {
            var gameBoardData = addMoveData.GameBoardData;
            var pinnedPieceMap = addMoveData.PinnedPieceMap;

            var checkAttackPosition = checkAttackPositionsBitboard.GetFirstPosition();
            var checkingPieceInfo = gameBoardData.GetPieceInfo(checkAttackPosition);
            var activeKingBitboard = gameBoardData.GetBitboard(activeKing);

            var capturingBitboard = gameBoardData.GetAttackers(checkAttackPosition, addMoveData.ActiveColor)
                & ~activeKingBitboard;

            var capturingSourcePositions =
                capturingBitboard
                    .GetPositions()
                    .Where(position => IsValidMoveByPinning(pinnedPieceMap, position, checkAttackPosition))
                    .ToArray();

            foreach (var capturingSourcePosition in capturingSourcePositions)
            {
                AddMove(addMoveData, capturingSourcePosition, checkAttackPosition, true, null);
            }

            var enPassantCaptureInfo = addMoveData.EnPassantCaptureInfo;
            if (enPassantCaptureInfo != null
                && enPassantCaptureInfo.TargetPiecePosition == checkAttackPosition)
            {
                //// TODO [vmcl] Fast to implement approach (likely non-optimal)

                var activeColorPawn = PieceType.Pawn.ToPiece(addMoveData.ActiveColor);
                var activePawnPositions = gameBoardData.GetPositions(activeColorPawn);
                var capturePosition = enPassantCaptureInfo.CapturePosition;
                foreach (var activePawnPosition in activePawnPositions)
                {
                    var canCapture = gameBoardData
                        .GetPotentialMovePositions(
                            CastlingOptions.None,
                            enPassantCaptureInfo,
                            activePawnPosition)
                        .Contains(capturePosition);

                    if (canCapture && IsValidMoveByPinning(pinnedPieceMap, activePawnPosition, capturePosition))
                    {
                        AddMove(addMoveData, activePawnPosition, capturePosition, true, null);
                    }
                }
            }

            if (!checkingPieceInfo.PieceType.IsSliding())
            {
                return;
            }

            var bridgeKey = new PositionBridgeKey(checkAttackPosition, activeKingPosition);
            var positionBridge = ChessHelper.PositionBridgeMap[bridgeKey];

            if (positionBridge.IsNone)
            {
                return;
            }

            var moves = addMoveData.ActivePiecesExceptKingBitboard
                .GetPositions()
                .SelectMany(
                    sourcePosition => gameBoardData
                        .GetPotentialMovePositions(
                            addMoveData.CastlingOptions,
                            enPassantCaptureInfo,
                            sourcePosition)
                        .Where(
                            targetPosition =>
                                (targetPosition.Bitboard & positionBridge).IsAny
                                    && IsValidMoveByPinning(
                                        pinnedPieceMap,
                                        sourcePosition,
                                        targetPosition))
                        .Select(targetPosition => new GameMove(sourcePosition, targetPosition)))
                .ToArray();

            foreach (var move in moves)
            {
                AddMove(addMoveData, move.From, move.To, false, null);
            }
        }

        private static void PopulatePawnMoves(AddMoveData addMoveData)
        {
            var potentialPawnMoves = new List<GameMoveData>(ValidMoveCapacity);

            var gameBoardData = addMoveData.GameBoardData;
            var enPassantCaptureInfo = addMoveData.EnPassantCaptureInfo;
            var pinnedPieceMap = addMoveData.PinnedPieceMap;

            gameBoardData.GetPawnMoves(
                potentialPawnMoves,
                addMoveData.ActiveColor,
                enPassantCaptureInfo == null ? Bitboard.None : enPassantCaptureInfo.CapturePosition.Bitboard);

            foreach (var pair in potentialPawnMoves)
            {
                var potentialPawnMove = pair.Move;

                var sourcePosition = potentialPawnMove.From;
                var destinationPosition = potentialPawnMove.To;
                if (!IsValidMoveByPinning(pinnedPieceMap, sourcePosition, destinationPosition))
                {
                    continue;
                }

                var isEnPassantCapture = gameBoardData.IsEnPassantCapture(
                    sourcePosition,
                    destinationPosition,
                    enPassantCaptureInfo);
                if (isEnPassantCapture)
                {
                    var temporaryCastlingOptions = addMoveData.CastlingOptions;

                    var move = new GameMove(sourcePosition, destinationPosition);

                    gameBoardData.MakeMove(
                        move,
                        addMoveData.ActiveColor,
                        enPassantCaptureInfo,
                        ref temporaryCastlingOptions);

                    var isInvalidMove = gameBoardData.IsInCheck(addMoveData.ActiveColor);
                    gameBoardData.UndoMove();

                    if (isInvalidMove)
                    {
                        continue;
                    }
                }

                addMoveData.ValidMovesReference.Add(pair.Move, pair.MoveInfo);
            }
        }

        private void Validate(bool forceValidation)
        {
            if (!forceValidation && !_validateAfterMove)
            {
                return;
            }

            _gameBoardData.EnsureConsistency();

            foreach (var king in ChessConstants.BothKings)
            {
                var count = _gameBoardData.GetPositions(king).Length;
                if (count != 1)
                {
                    throw new ChessPlatformException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The number of the '{0}' pieces is {1}. Must be exactly one.",
                            king.GetDescription(),
                            count));
                }
            }

            if (_gameBoardData.IsInCheck(_activeColor.Invert()))
            {
                throw new ChessPlatformException("Inactive king is in check.");
            }

            foreach (var pieceColor in ChessConstants.PieceColors)
            {
                var color = pieceColor;

                var pieceToCountMap = ChessConstants
                    .PieceTypesExceptNone
                    .ToDictionary(
                        Factotum.Identity,
                        item => _gameBoardData.GetPieceCount(item.ToPiece(color)));

                var allCount = pieceToCountMap.Values.Sum();
                var pawnCount = pieceToCountMap[PieceType.Pawn];

                if (pawnCount > ChessConstants.MaxPawnCountPerColor)
                {
                    throw new ChessPlatformException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Too many '{0}' ({1}).",
                            PieceType.Pawn.ToPiece(color).GetDescription(),
                            pawnCount));
                }

                if (allCount > ChessConstants.MaxPieceCountPerColor)
                {
                    throw new ChessPlatformException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Too many pieces of the color {0} ({1}).",
                            color.GetName(),
                            allCount));
                }
            }

            var allPawnsBitboard = _gameBoardData.GetBitboard(PieceType.Pawn.ToPiece(PieceColor.White))
                | _gameBoardData.GetBitboard(PieceType.Pawn.ToPiece(PieceColor.Black));

            if ((allPawnsBitboard & ChessHelper.InvalidPawnPositionsBitboard).IsAny)
            {
                throw new ChessPlatformException("One or more pawn are located at the invalid rank.");
            }

            //// TODO [vmcl] (*) Other verifications
        }

        private void InitializeValidMovesAndState(
            out Dictionary<GameMove, GameMoveInfo> validMoves,
            out GameState state)
        {
            var activeKing = PieceType.King.ToPiece(_activeColor);
            var activeKingPosition = _gameBoardData.GetPositions(activeKing).Single();
            var oppositeColor = _activeColor.Invert();

            var checkAttackPositionsBitboard = _gameBoardData.GetAttackers(activeKingPosition, oppositeColor);
            var isInCheck = checkAttackPositionsBitboard.IsAny;
            var isInDoubleCheck = isInCheck && !checkAttackPositionsBitboard.IsExactlyOneBitSet();

            var pinnedPieceMap = _gameBoardData
                .GetPinnedPieceInfos(activeKingPosition)
                .ToDictionary(item => item.Position, item => item.AllowedMoves);

            var activePiecesExceptKingBitboard = _gameBoardData.GetBitboard(_activeColor)
                & ~_gameBoardData.GetBitboard(activeKing);

            validMoves = new Dictionary<GameMove, GameMoveInfo>(ValidMoveCapacity);

            var addMoveData = new AddMoveData(
                _gameBoardData,
                validMoves,
                _enPassantCaptureInfo,
                _activeColor,
                _castlingOptions,
                pinnedPieceMap,
                activePiecesExceptKingBitboard);

            var noActiveKingPieceData = _gameBoardData.Copy();
            noActiveKingPieceData.SetPiece(activeKingPosition, Piece.None);

            var activeKingMoves = GetActiveKingMoves(
                _gameBoardData,
                noActiveKingPieceData,
                activeKingPosition,
                _castlingOptions,
                oppositeColor,
                isInCheck);

            foreach (var activeKingMove in activeKingMoves)
            {
                validMoves.Add(activeKingMove.Move, new GameMoveInfo(activeKingMove.Flags));
            }

            if (!isInCheck)
            {
                InitializeValidMovesAndStateWhenNotInCheck(addMoveData);

                state = validMoves.Count == 0 ? GameState.Stalemate : GameState.Default;
                return;
            }

            if (!isInDoubleCheck)
            {
                InitializeValidMovesAndStateWhenInSingleCheck(
                    addMoveData,
                    activeKing,
                    activeKingPosition,
                    checkAttackPositionsBitboard);
            }

            state = validMoves.Count == 0
                ? GameState.Checkmate
                : (isInDoubleCheck ? GameState.DoubleCheck : GameState.Check);
        }

        private void InitializeResultString(out string resultString)
        {
            switch (_state)
            {
                case GameState.Default:
                case GameState.Check:
                case GameState.DoubleCheck:
                    resultString = _autoDrawType == AutoDrawType.None ? ResultStrings.Other : ResultStrings.Draw;
                    break;

                case GameState.Checkmate:
                    resultString = _activeColor == PieceColor.White ? ResultStrings.BlackWon : ResultStrings.WhiteWon;
                    break;

                case GameState.Stalemate:
                    resultString = ResultStrings.Draw;
                    break;

                default:
                    throw _state.CreateEnumValueNotSupportedException();
            }
        }

        private void FinishInitialization(
            bool forceValidation,
            out ReadOnlyDictionary<GameMove, GameMoveInfo> validMoves,
            out GameState state,
            out AutoDrawType autoDrawType,
            out string resultString,
            out ReadOnlyDictionary<PackedGameBoard, int> repetitions)
        {
            Validate(forceValidation);

            if (_previousBoard != null && _previousBoard._autoDrawType == AutoDrawType.ThreefoldRepetition)
            {
                autoDrawType = AutoDrawType.ThreefoldRepetition;
                repetitions = null;
            }
            else
            {
                if (_previousBoard != null && _previousBoard._repetitions == null)
                {
                    throw new InvalidOperationException("Internal logic error: repetition map is not assigned.");
                }

                var repetitionMap = _previousBoard == null
                    ? new Dictionary<PackedGameBoard, int>()
                    : new Dictionary<PackedGameBoard, int>(_previousBoard._repetitions);

                var packedGameBoard = this.Pack();
                var thisRepetitionCount = repetitionMap.GetValueOrDefault(packedGameBoard) + 1;
                repetitionMap[packedGameBoard] = thisRepetitionCount;
                repetitions = repetitionMap.AsReadOnly();

                var isThreefoldRepetition = thisRepetitionCount >= ThreefoldCount;
                autoDrawType = isThreefoldRepetition ? AutoDrawType.ThreefoldRepetition : AutoDrawType.None;
            }

            Dictionary<GameMove, GameMoveInfo> validMoveMap;
            InitializeValidMovesAndState(out validMoveMap, out state);
            validMoves = validMoveMap.AsReadOnly();

            if (autoDrawType == AutoDrawType.None && !state.IsGameFinished())
            {
                if (this.FullMoveCountBy50MoveRule >= ChessConstants.FullMoveCountBy50MoveRule)
                {
                    autoDrawType = AutoDrawType.FiftyMoveRule;
                }
                else if (_gameBoardData.IsInsufficientMaterialState())
                {
                    autoDrawType = AutoDrawType.InsufficientMaterial;
                }
            }

            InitializeResultString(out resultString);
        }

        private void SetupDefault(
            out PieceColor activeColor,
            out CastlingOptions castlingOptions,
            out EnPassantCaptureInfo enPassantTarget,
            out int halfMovesBy50MoveRule,
            out int fullMoveIndex)
        {
            _gameBoardData.SetupNewPiece(Piece.WhiteRook, "a1");
            _gameBoardData.SetupNewPiece(Piece.WhiteKnight, "b1");
            _gameBoardData.SetupNewPiece(Piece.WhiteBishop, "c1");
            _gameBoardData.SetupNewPiece(Piece.WhiteQueen, "d1");
            _gameBoardData.SetupNewPiece(Piece.WhiteKing, "e1");
            _gameBoardData.SetupNewPiece(Piece.WhiteBishop, "f1");
            _gameBoardData.SetupNewPiece(Piece.WhiteKnight, "g1");
            _gameBoardData.SetupNewPiece(Piece.WhiteRook, "h1");
            Position.GenerateRank(1).DoForEach(position => _gameBoardData.SetupNewPiece(Piece.WhitePawn, position));

            Position.GenerateRank(6).DoForEach(position => _gameBoardData.SetupNewPiece(Piece.BlackPawn, position));
            _gameBoardData.SetupNewPiece(Piece.BlackRook, "a8");
            _gameBoardData.SetupNewPiece(Piece.BlackKnight, "b8");
            _gameBoardData.SetupNewPiece(Piece.BlackBishop, "c8");
            _gameBoardData.SetupNewPiece(Piece.BlackQueen, "d8");
            _gameBoardData.SetupNewPiece(Piece.BlackKing, "e8");
            _gameBoardData.SetupNewPiece(Piece.BlackBishop, "f8");
            _gameBoardData.SetupNewPiece(Piece.BlackKnight, "g8");
            _gameBoardData.SetupNewPiece(Piece.BlackRook, "h8");

            activeColor = PieceColor.White;
            castlingOptions = CastlingOptions.All;
            enPassantTarget = null;
            halfMovesBy50MoveRule = 0;
            fullMoveIndex = 1;
        }

        private void SetupByFen(
            string fen,
            out PieceColor activeColor,
            out CastlingOptions castlingOptions,
            out EnPassantCaptureInfo enPassantCaptureTarget,
            out int halfMovesBy50MoveRule,
            out int fullMoveIndex)
        {
            const string InvalidFenMessage = "Invalid FEN.";

            var fenSnippets = fen
                .Trim()
                .Split(ChessConstants.FenSnippetSeparator.AsArray(), StringSplitOptions.None);
            if (fenSnippets.Length != ChessConstants.FenSnippetCount)
            {
                throw new ArgumentException(InvalidFenMessage, "fen");
            }

            var pieceDataSnippet = fenSnippets[0];
            _gameBoardData.SetupByFenSnippet(pieceDataSnippet);

            var activeColorSnippet = fenSnippets[1];
            if (!ChessConstants.FenSnippetToColorMap.TryGetValue(activeColorSnippet, out activeColor))
            {
                throw new ArgumentException(InvalidFenMessage, "fen");
            }

            castlingOptions = CastlingOptions.None;
            var castlingOptionsSnippet = fenSnippets[2];
            if (castlingOptionsSnippet != ChessConstants.NoneCastlingOptionsFenSnippet)
            {
                var castlingOptionsSnippetSet = castlingOptionsSnippet.ToHashSet();
                foreach (var optionChar in castlingOptionsSnippetSet)
                {
                    CastlingOptions option;
                    if (!ChessConstants.FenCharCastlingOptionMap.TryGetValue(optionChar, out option))
                    {
                        throw new ArgumentException(InvalidFenMessage, "fen");
                    }

                    castlingOptions |= option;
                }
            }

            enPassantCaptureTarget = null;
            var enPassantCaptureTargetSnippet = fenSnippets[3];
            if (enPassantCaptureTargetSnippet != ChessConstants.NoEnPassantCaptureFenSnippet)
            {
                var capturePosition = Position.TryFromAlgebraic(enPassantCaptureTargetSnippet);
                if (!capturePosition.HasValue)
                {
                    throw new ArgumentException(InvalidFenMessage, "fen");
                }

                var enPassantInfo =
                    ChessConstants.ColorToEnPassantInfoMap.Values.SingleOrDefault(
                        obj => obj.CaptureTargetRank == capturePosition.Value.Rank);

                if (enPassantInfo == null)
                {
                    throw new ArgumentException(InvalidFenMessage, "fen");
                }

                enPassantCaptureTarget = new EnPassantCaptureInfo(
                    capturePosition.Value,
                    new Position(false, capturePosition.Value.File, enPassantInfo.EndRank));
            }

            var halfMovesBy50MoveRuleSnippet = fenSnippets[4];
            if (!ChessHelper.TryParseInt(halfMovesBy50MoveRuleSnippet, out halfMovesBy50MoveRule)
                || halfMovesBy50MoveRule < 0)
            {
                throw new ArgumentException(InvalidFenMessage, "fen");
            }

            var fullMoveIndexSnippet = fenSnippets[5];
            if (!ChessHelper.TryParseInt(fullMoveIndexSnippet, out fullMoveIndex) || fullMoveIndex <= 0)
            {
                throw new ArgumentException(InvalidFenMessage, "fen");
            }
        }

        private static void PerftInternal(
            GameBoard gameBoard,
            int depth,
            bool canUseParallelism,
            PerftData perftData,
            bool includeDivideMap,
            bool includeExtraCountTypes)
        {
            if (depth == 0)
            {
                perftData.NodeCount++;

                if (!includeExtraCountTypes)
                {
                    return;
                }

                switch (gameBoard.State)
                {
                    case GameState.Check:
                    case GameState.DoubleCheck:
                        checked
                        {
                            perftData.CheckCount++;
                        }

                        break;

                    case GameState.Checkmate:
                        checked
                        {
                            perftData.CheckCount++;
                            perftData.CheckmateCount++;
                        }

                        break;
                }

                return;
            }

            var moves = gameBoard.ValidMoves.Keys;
            if (depth == 1 && !includeExtraCountTypes && !includeDivideMap)
            {
                perftData.NodeCount += checked((ulong)moves.Count);
                return;
            }

            if (canUseParallelism)
            {
                var topDatas = moves
                    .AsParallel()
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(
                        move =>
                        {
                            var localData = new PerftData();
                            var newBoard = gameBoard.MakeMove(move);
                            PerftInternal(newBoard, depth - 1, false, localData, false, includeExtraCountTypes);
                            return KeyValuePair.Create(move, localData);
                        })
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                var totalTopDatas = topDatas.Aggregate(new PerftData(), (acc, pair) => acc + pair.Value);
                perftData.Include(totalTopDatas);
                topDatas.DoForEach(pair => perftData.DividedMoves.Add(pair.Key, pair.Value.NodeCount));

                return;
            }

            foreach (var move in moves)
            {
                var previousNodeCount = perftData.NodeCount;

                var newBoard = gameBoard.MakeMove(move);
                PerftInternal(newBoard, depth - 1, false, perftData, false, includeExtraCountTypes);

                if (includeDivideMap)
                {
                    perftData.DividedMoves[move] = perftData.DividedMoves.GetValueOrDefault(move)
                        + checked(perftData.NodeCount - previousNodeCount);
                }
            }
        }

        #endregion

        #region PerftData Class

        private sealed class PerftData
        {
            #region Constructors

            public PerftData()
            {
                this.DividedMoves = new Dictionary<GameMove, ulong>();
            }

            #endregion

            #region Public Properties

            public ulong NodeCount
            {
                get;
                set;
            }

            public ulong CheckCount
            {
                get;
                set;
            }

            public ulong CheckmateCount
            {
                get;
                set;
            }

            public Dictionary<GameMove, ulong> DividedMoves
            {
                get;
                private set;
            }

            #endregion

            #region Operators

            public static PerftData operator +(PerftData left, PerftData right)
            {
                return new PerftData
                {
                    CheckCount = left.CheckCount + right.CheckCount,
                    CheckmateCount = left.CheckmateCount + right.CheckmateCount,
                    NodeCount = left.NodeCount + right.NodeCount
                };
            }

            #endregion

            #region Public Methods

            public void Include(PerftData other)
            {
                #region Argument Check

                if (other == null)
                {
                    throw new ArgumentNullException("other");
                }

                #endregion

                this.CheckCount += other.CheckCount;
                this.CheckmateCount += other.CheckmateCount;
                this.NodeCount += other.NodeCount;
            }

            #endregion
        }

        #endregion

        #region AddMoveData Class

        private sealed class AddMoveData
        {
            #region Constructors

            public AddMoveData(
                [NotNull] GameBoardData gameBoardData,
                [NotNull] IDictionary<GameMove, GameMoveInfo> validMovesReference,
                [CanBeNull] EnPassantCaptureInfo enPassantCaptureInfo,
                PieceColor activeColor,
                CastlingOptions castlingOptions,
                [NotNull] Dictionary<Position, Bitboard> pinnedPieceMap,
                Bitboard activePiecesExceptKingBitboard)
            {
                this.GameBoardData = gameBoardData.EnsureNotNull();
                this.ValidMovesReference = validMovesReference.EnsureNotNull();
                this.EnPassantCaptureInfo = enPassantCaptureInfo;
                this.ActiveColor = activeColor;
                this.CastlingOptions = castlingOptions;
                this.PinnedPieceMap = pinnedPieceMap.EnsureNotNull();
                this.ActivePiecesExceptKingBitboard = activePiecesExceptKingBitboard;
            }

            #endregion

            #region Public Properties

            [NotNull]
            public GameBoardData GameBoardData
            {
                get;
                private set;
            }

            [NotNull]
            public IDictionary<GameMove, GameMoveInfo> ValidMovesReference
            {
                get;
                private set;
            }

            [CanBeNull]
            public EnPassantCaptureInfo EnPassantCaptureInfo
            {
                get;
                private set;
            }

            public PieceColor ActiveColor
            {
                get;
                private set;
            }

            public CastlingOptions CastlingOptions
            {
                get;
                private set;
            }

            [NotNull]
            public Dictionary<Position, Bitboard> PinnedPieceMap
            {
                get;
                private set;
            }

            public Bitboard ActivePiecesExceptKingBitboard
            {
                get;
                private set;
            }

            #endregion
        }

        #endregion

        #region KingMoveInfo Class

        private sealed class KingMoveInfo
        {
            #region Constructors

            public KingMoveInfo([NotNull] GameMove move, GameMoveFlags flags)
            {
                this.Move = move.EnsureNotNull();
                this.Flags = flags;
            }

            #endregion

            #region Public Properties

            public GameMove Move
            {
                get;
                private set;
            }

            public GameMoveFlags Flags
            {
                get;
                private set;
            }

            #endregion
        }

        #endregion
    }
}