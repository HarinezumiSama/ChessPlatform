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

        private readonly PieceData _pieceData;

        private readonly PieceColor _activeColor;
        private readonly GameState _state;
        private readonly CastlingOptions _castlingOptions;
        private readonly EnPassantCaptureInfo _enPassantCaptureInfo;
        private readonly ReadOnlyDictionary<PieceMove, PieceMoveInfo> _validMoves;
        private readonly int _halfMoveCountBy50MoveRule;
        private readonly int _fullMoveIndex;
        private readonly PieceMove _previousMove;
        private readonly Piece _lastCapturedPiece;
        private readonly string _resultString;
        private readonly bool _validateAfterMove;
        private readonly GameBoard _previousBoard;
        private readonly ReadOnlyDictionary<PackedGameBoard, int> _repetitions;
        private readonly bool _isThreefoldRepetition;

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
            _pieceData = new PieceData();
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
                out _resultString,
                out _repetitions,
                out _isThreefoldRepetition);
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
            _pieceData = new PieceData();
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
                out _resultString,
                out _repetitions,
                out _isThreefoldRepetition);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameBoard"/> class
        ///     using the specified previous state and specified move.
        /// </summary>
        private GameBoard([NotNull] GameBoard previousBoard, [CanBeNull] PieceMove move)
        {
            #region Argument Check

            if (previousBoard == null)
            {
                throw new ArgumentNullException("previousBoard");
            }

            #endregion

            _previousBoard = previousBoard;
            _validateAfterMove = previousBoard._validateAfterMove;
            _pieceData = previousBoard._pieceData.Copy();
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
                _enPassantCaptureInfo = _pieceData.GetEnPassantCaptureInfo(move);

                var makeMoveData = _pieceData.MakeMove(
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
                out _resultString,
                out _repetitions,
                out _isThreefoldRepetition);
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

        public ReadOnlyDictionary<PieceMove, PieceMoveInfo> ValidMoves
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

        public PieceMove PreviousMove
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
                return _pieceData[position];
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

        public GameBoard MakeMove([NotNull] PieceMove move)
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
            var pieceDataSnippet = _pieceData.GetFenSnippet();
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
            return _pieceData.GetPieceInfo(position);
        }

        public Position[] GetPositions(Piece piece)
        {
            return _pieceData.GetPositions(piece);
        }

        public Position[] GetPositions(PieceColor color)
        {
            return _pieceData.GetPositions(color);
        }

        public bool IsValidMove(PieceMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            return _validMoves.ContainsKey(move);
        }

        public bool IsPawnPromotionMove(PieceMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            var pieceMoveInfo = _validMoves.GetValueOrDefault(move);
            if (pieceMoveInfo != null)
            {
                return pieceMoveInfo.IsPawnPromotion;
            }

            var result = _pieceData.IsPawnPromotion(move.From, move.To);
            return result;
        }

        public bool IsCapturingMove(PieceMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            var pieceMoveInfo = _validMoves.GetValueOrDefault(move);
            if (pieceMoveInfo != null)
            {
                return pieceMoveInfo.IsCapture;
            }

            if (_pieceData.IsEnPassantCapture(move.From, move.To, _enPassantCaptureInfo))
            {
                return true;
            }

            var sourcePieceInfo = GetPieceInfo(move.From);
            var destinationPieceInfo = GetPieceInfo(move.To);

            var result = sourcePieceInfo.Color == _activeColor
                && destinationPieceInfo.Color == _activeColor.Invert();

            return result;
        }

        public CastlingInfo CheckCastlingMove(PieceMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            return this.ValidMoves.ContainsKey(move) ? _pieceData.CheckCastlingMove(move) : null;
        }

        public Position[] GetAttacks(Position targetPosition, PieceColor attackingColor)
        {
            return _pieceData.GetAttackingPositions(targetPosition, attackingColor);
        }

        public PieceMove[] GetValidMovesBySource(Position sourcePosition)
        {
            return this.ValidMoves.Keys.Where(move => move.From == sourcePosition).ToArray();
        }

        public PieceMove[] GetValidMovesByDestination(Position destinationPosition)
        {
            return this.ValidMoves.Keys.Where(move => move.To == destinationPosition).ToArray();
        }

        public AutoDrawType GetAutoDrawType()
        {
            var isInsufficientMaterialState = _pieceData.IsInsufficientMaterialState();
            if (isInsufficientMaterialState)
            {
                return AutoDrawType.InsufficientMaterial;
            }

            if (_isThreefoldRepetition)
            {
                return AutoDrawType.ThreefoldRepetition;
            }

            if (this.FullMoveCountBy50MoveRule >= ChessConstants.FullMoveCountBy50MoveRule)
            {
                return AutoDrawType.FiftyMoveRule;
            }

            return AutoDrawType.None;
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

        IGameBoard IGameBoard.MakeMove(PieceMove move)
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
            Position targetPosition)
        {
            Bitboard bitboard;
            var found = pinnedPieceMap.TryGetValue(sourcePosition, out bitboard);
            return !found || ((bitboard & targetPosition.Bitboard) == targetPosition.Bitboard);
        }

        private static Position[] GetActivePieceExceptKingPositions(PieceData pieceData, PieceColor activeColor)
        {
            var entireColorBitboard = pieceData.GetBitboard(activeColor);
            var kingBitboard = pieceData.GetBitboard(PieceType.King.ToPiece(activeColor));

            var bitboard = entireColorBitboard & ~kingBitboard;
            return bitboard.GetPositions();
        }

        private static void AddMove(
            AddMoveData addMoveData,
            Position sourcePosition,
            Position targetPosition,
            bool isCapture,
            Func<PieceMove, bool> checkMove)
        {
            var isPawnPromotion = addMoveData.PieceData.IsPawnPromotion(sourcePosition, targetPosition);
            var promotionResult = isPawnPromotion ? ChessHelper.DefaultPromotion : PieceType.None;
            var basicMove = new PieceMove(sourcePosition, targetPosition, promotionResult);

            if (checkMove != null && !checkMove(basicMove))
            {
                return;
            }

            var moveFlags = PieceMoveFlags.None;
            if (isPawnPromotion)
            {
                moveFlags |= PieceMoveFlags.IsPawnPromotion;
            }

            if (isCapture)
            {
                moveFlags |= PieceMoveFlags.IsCapture;
            }

            var isEnPassantCapture = addMoveData.PieceData.IsEnPassantCapture(
                sourcePosition,
                targetPosition,
                addMoveData.EnPassantCaptureInfo);

            if (isEnPassantCapture)
            {
                moveFlags |= PieceMoveFlags.IsEnPassantCapture;
            }

            var pieceMoveInfo = new PieceMoveInfo(moveFlags);
            addMoveData.ValidMovesReference.Add(basicMove, pieceMoveInfo);

            if (isPawnPromotion)
            {
                ChessHelper.NonDefaultPromotions.Select(basicMove.MakePromotion).DoForEach(
                    move => addMoveData.ValidMovesReference.Add(move, pieceMoveInfo));
            }
        }

        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        private static KingMoveInfo[] GetActiveKingMoves(
            PieceData pieceData,
            PieceData noActiveKingPieceData,
            Position activeKingPosition,
            CastlingOptions castlingOptions,
            PieceColor oppositeColor,
            bool isInCheck)
        {
            var result = pieceData
                .GetPotentialMovePositions(
                    isInCheck ? CastlingOptions.None : castlingOptions,
                    null,
                    activeKingPosition)
                .Where(position => !noActiveKingPieceData.IsUnderAttack(position, oppositeColor))
                .Select(position => new PieceMove(activeKingPosition, position))
                .Where(
                    move =>
                        isInCheck
                            || pieceData
                                .CheckCastlingMove(move)
                                .Morph(info => !pieceData.IsUnderAttack(info.PassedPosition, oppositeColor), true))
                .Select(
                    move =>
                        new KingMoveInfo(
                            move,
                            pieceData[move.To] == Piece.None ? PieceMoveFlags.None : PieceMoveFlags.IsCapture))
                .ToArray();

            return result;
        }

        private void Validate(bool forceValidation)
        {
            if (!forceValidation && !_validateAfterMove)
            {
                return;
            }

            _pieceData.EnsureConsistency();

            foreach (var king in ChessConstants.BothKings)
            {
                var count = _pieceData.GetPositions(king).Length;
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

            if (_pieceData.IsInCheck(_activeColor.Invert()))
            {
                throw new ChessPlatformException("Inactive king is under check.");
            }

            foreach (var pieceColor in ChessConstants.PieceColors)
            {
                var color = pieceColor;

                var pieceToCountMap = ChessConstants
                    .PieceTypesExceptNone
                    .ToDictionary(
                        Factotum.Identity,
                        item => _pieceData.GetPieceCount(item.ToPiece(color)));

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

            //// TODO [vmcl] (*) No pawns at invalid ranks, (*) etc.
        }

        private void InitializeValidMovesAndState(
            out Dictionary<PieceMove, PieceMoveInfo> validMoves,
            out GameState state)
        {
            var activeKing = PieceType.King.ToPiece(_activeColor);
            var activeKingPosition = _pieceData.GetPositions(activeKing).Single();
            var oppositeColor = _activeColor.Invert();

            var checkAttackPositions = _pieceData.GetAttackingPositions(activeKingPosition, oppositeColor);
            var isInCheck = checkAttackPositions.Length != 0;
            var isInDoubleCheck = checkAttackPositions.Length > 1;

            var pinnedPieceMap = _pieceData
                .GetPinnedPieceInfos(activeKingPosition)
                .ToDictionary(item => item.Position, item => item.AllowedMoves);

            validMoves = new Dictionary<PieceMove, PieceMoveInfo>();
            var addMoveData = new AddMoveData(validMoves, _pieceData, _enPassantCaptureInfo);

            var activePieceExceptKingPositions = Lazy.Create(
                () => GetActivePieceExceptKingPositions(_pieceData, _activeColor));

            var noActiveKingPieceData = _pieceData.Copy();
            noActiveKingPieceData.SetPiece(activeKingPosition, Piece.None);

            var activeKingMoves = GetActiveKingMoves(
                _pieceData,
                noActiveKingPieceData,
                activeKingPosition,
                _castlingOptions,
                oppositeColor,
                isInCheck);

            foreach (var activeKingMove in activeKingMoves)
            {
                validMoves.Add(activeKingMove.Move, new PieceMoveInfo(activeKingMove.Flags));
            }

            if (isInCheck)
            {
                if (!isInDoubleCheck)
                {
                    InitializeValidMovesAndStateWhenInSingleCheck(
                        _pieceData,
                        _activeColor,
                        _enPassantCaptureInfo,
                        _castlingOptions,
                        addMoveData,
                        activeKing,
                        activeKingPosition,
                        checkAttackPositions,
                        pinnedPieceMap,
                        activePieceExceptKingPositions);
                }

                state = validMoves.Count == 0
                    ? GameState.Checkmate
                    : (isInDoubleCheck ? GameState.DoubleCheck : GameState.Check);

                return;
            }

            foreach (var sourcePosition in activePieceExceptKingPositions.Value)
            {
                var potentialMovePositions = _pieceData.GetPotentialMovePositions(
                    _castlingOptions,
                    _enPassantCaptureInfo,
                    sourcePosition);

                var filteredDestinationPositions = potentialMovePositions
                    .Where(position => IsValidMoveByPinning(pinnedPieceMap, sourcePosition, position))
                    .ToArray();

                foreach (var destinationPosition in filteredDestinationPositions)
                {
                    var isEnPassantCapture = _pieceData.IsEnPassantCapture(
                        sourcePosition,
                        destinationPosition,
                        _enPassantCaptureInfo);
                    if (isEnPassantCapture)
                    {
                        var temporaryCastlingOptions = _castlingOptions;

                        var move = new PieceMove(sourcePosition, destinationPosition);

                        _pieceData.MakeMove(
                            move,
                            _activeColor,
                            _enPassantCaptureInfo,
                            ref temporaryCastlingOptions);

                        var isInvalidMove = _pieceData.IsInCheck(_activeColor);
                        _pieceData.UndoMove();

                        if (isInvalidMove)
                        {
                            continue;
                        }
                    }

                    AddMove(
                        addMoveData,
                        sourcePosition,
                        destinationPosition,
                        isEnPassantCapture || _pieceData[destinationPosition] != Piece.None,
                        null);
                }
            }

            state = validMoves.Count == 0 ? GameState.Stalemate : GameState.Default;
        }

        private static void InitializeValidMovesAndStateWhenInSingleCheck(
            PieceData pieceData,
            PieceColor activeColor,
            EnPassantCaptureInfo enPassantCaptureInfo,
            CastlingOptions castlingOptions,
            AddMoveData addMoveData,
            Piece activeKing,
            Position activeKingPosition,
            IEnumerable<Position> checkAttackPositions,
            IDictionary<Position, Bitboard> pinnedPieceMap,
            Lazy<Position[]> activePieceExceptKingPositions)
        {
            var checkAttackPosition = checkAttackPositions.Single();
            var checkingPieceInfo = pieceData.GetPieceInfo(checkAttackPosition);

            var capturingSourcePositions = pieceData
                .GetAttackingPositions(checkAttackPosition, activeColor)
                .Where(
                    position =>
                        pieceData[position] != activeKing
                            && IsValidMoveByPinning(pinnedPieceMap, position, checkAttackPosition))
                .ToArray();

            capturingSourcePositions.DoForEach(
                sourcePosition => AddMove(addMoveData, sourcePosition, checkAttackPosition, true, null));

            if (enPassantCaptureInfo != null
                && enPassantCaptureInfo.TargetPiecePosition == checkAttackPosition)
            {
                //// TODO [vmcl] Fast to implement approach (likely non-optimal)

                var activeColorPawn = PieceType.Pawn.ToPiece(activeColor);
                var activePawnPositions = pieceData.GetPositions(activeColorPawn);
                var capturePosition = enPassantCaptureInfo.CapturePosition;
                foreach (var activePawnPosition in activePawnPositions)
                {
                    var canCapture = pieceData
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

            if (positionBridge.IsZero())
            {
                return;
            }

            var moves = activePieceExceptKingPositions.Value
                .SelectMany(
                    sourcePosition => pieceData
                        .GetPotentialMovePositions(
                            castlingOptions,
                            enPassantCaptureInfo,
                            sourcePosition)
                        .Where(
                            targetPosition =>
                                !(targetPosition.Bitboard & positionBridge).IsZero()
                                    && IsValidMoveByPinning(
                                        pinnedPieceMap,
                                        sourcePosition,
                                        targetPosition))
                        .Select(targetPosition => new PieceMove(sourcePosition, targetPosition)))
                .ToArray();

            foreach (var move in moves)
            {
                AddMove(addMoveData, move.From, move.To, false, null);
            }
        }

        private void InitializeResultString(out string resultString)
        {
            switch (_state)
            {
                case GameState.Default:
                case GameState.Check:
                case GameState.DoubleCheck:
                    resultString = ResultStrings.Other;
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
            out ReadOnlyDictionary<PieceMove, PieceMoveInfo> validMoves,
            out GameState state,
            out string resultString,
            out ReadOnlyDictionary<PackedGameBoard, int> repetitions,
            out bool isThreefoldRepetition)
        {
            Validate(forceValidation);

            if (_previousBoard != null && _previousBoard._isThreefoldRepetition)
            {
                isThreefoldRepetition = true;
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

                isThreefoldRepetition = thisRepetitionCount >= ThreefoldCount;
            }

            Dictionary<PieceMove, PieceMoveInfo> validMoveMap;
            InitializeValidMovesAndState(out validMoveMap, out state);
            validMoves = validMoveMap.AsReadOnly();

            InitializeResultString(out resultString);
        }

        private void SetupDefault(
            out PieceColor activeColor,
            out CastlingOptions castlingOptions,
            out EnPassantCaptureInfo enPassantTarget,
            out int halfMovesBy50MoveRule,
            out int fullMoveIndex)
        {
            _pieceData.SetupNewPiece(Piece.WhiteRook, "a1");
            _pieceData.SetupNewPiece(Piece.WhiteKnight, "b1");
            _pieceData.SetupNewPiece(Piece.WhiteBishop, "c1");
            _pieceData.SetupNewPiece(Piece.WhiteQueen, "d1");
            _pieceData.SetupNewPiece(Piece.WhiteKing, "e1");
            _pieceData.SetupNewPiece(Piece.WhiteBishop, "f1");
            _pieceData.SetupNewPiece(Piece.WhiteKnight, "g1");
            _pieceData.SetupNewPiece(Piece.WhiteRook, "h1");
            Position.GenerateRank(1).DoForEach(position => _pieceData.SetupNewPiece(Piece.WhitePawn, position));

            Position.GenerateRank(6).DoForEach(position => _pieceData.SetupNewPiece(Piece.BlackPawn, position));
            _pieceData.SetupNewPiece(Piece.BlackRook, "a8");
            _pieceData.SetupNewPiece(Piece.BlackKnight, "b8");
            _pieceData.SetupNewPiece(Piece.BlackBishop, "c8");
            _pieceData.SetupNewPiece(Piece.BlackQueen, "d8");
            _pieceData.SetupNewPiece(Piece.BlackKing, "e8");
            _pieceData.SetupNewPiece(Piece.BlackBishop, "f8");
            _pieceData.SetupNewPiece(Piece.BlackKnight, "g8");
            _pieceData.SetupNewPiece(Piece.BlackRook, "h8");

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
            _pieceData.SetupByFenSnippet(pieceDataSnippet);

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
                this.DividedMoves = new Dictionary<PieceMove, ulong>();
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

            public Dictionary<PieceMove, ulong> DividedMoves
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
                [NotNull] IDictionary<PieceMove, PieceMoveInfo> validMovesReference,
                [NotNull] PieceData pieceData,
                [CanBeNull] EnPassantCaptureInfo enPassantCaptureInfo)
            {
                this.ValidMovesReference = validMovesReference.EnsureNotNull();
                this.PieceData = pieceData.EnsureNotNull();
                this.EnPassantCaptureInfo = enPassantCaptureInfo;
            }

            #endregion

            #region Public Properties

            [NotNull]
            public IDictionary<PieceMove, PieceMoveInfo> ValidMovesReference
            {
                get;
                private set;
            }

            [NotNull]
            public PieceData PieceData
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

            #endregion
        }

        #endregion

        #region KingMoveInfo Class

        private sealed class KingMoveInfo
        {
            #region Constructors

            public KingMoveInfo([NotNull] PieceMove move, PieceMoveFlags flags)
            {
                this.Move = move.EnsureNotNull();
                this.Flags = flags;
            }

            #endregion

            #region Public Properties

            public PieceMove Move
            {
                get;
                private set;
            }

            public PieceMoveFlags Flags
            {
                get;
                private set;
            }

            #endregion
        }

        #endregion
    }
}