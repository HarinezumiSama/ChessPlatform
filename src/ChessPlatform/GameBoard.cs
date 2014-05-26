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

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameBoard"/> class.
        /// </summary>
        public GameBoard()
        {
            _pieceData = new PieceData();
            _lastCapturedPiece = Piece.None;

            SetupDefault(
                out _activeColor,
                out _castlingOptions,
                out _enPassantCaptureInfo,
                out _halfMoveCountBy50MoveRule,
                out _fullMoveIndex);

            FinishInitialization(out _validMoves, out _state, out _resultString);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameBoard"/> class.
        /// </summary>
        public GameBoard([NotNull] string fen)
        {
            #region Argument Check

            if (string.IsNullOrWhiteSpace(fen))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    "fen");
            }

            #endregion

            _pieceData = new PieceData();
            _lastCapturedPiece = Piece.None;

            SetupByFen(
                fen,
                out _activeColor,
                out _castlingOptions,
                out _enPassantCaptureInfo,
                out _halfMoveCountBy50MoveRule,
                out _fullMoveIndex);

            FinishInitialization(out _validMoves, out _state, out _resultString);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameBoard"/> class
        ///     using the specified previous state and specified move.
        /// </summary>
        private GameBoard([NotNull] GameBoard previous, [NotNull] PieceMove move)
        {
            #region Argument Check

            if (previous == null)
            {
                throw new ArgumentNullException("previous");
            }

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            if (!previous._validMoves.ContainsKey(move))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "The move '{0}' is not valid.", move),
                    "move");
            }

            #endregion

            _pieceData = previous._pieceData.Copy();
            _activeColor = previous._activeColor.Invert();
            _castlingOptions = previous._castlingOptions;

            _fullMoveIndex = previous._fullMoveIndex + (_activeColor == PieceColor.White ? 1 : 0);
            _enPassantCaptureInfo = _pieceData.GetEnPassantCaptureInfo(move);

            var makeMoveData = _pieceData.MakeMove(
                move,
                previous._activeColor,
                previous._enPassantCaptureInfo,
                ref _castlingOptions);

            _previousMove = makeMoveData.Move;
            _lastCapturedPiece = makeMoveData.CapturedPiece;

            _halfMoveCountBy50MoveRule = makeMoveData.ShouldKeepCountingBy50MoveRule
                ? previous._halfMoveCountBy50MoveRule + 1
                : 0;

            FinishInitialization(out _validMoves, out _state, out _resultString);
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

        #endregion

        #region IGameBoard Members

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

        public override string ToString()
        {
            return GetFen();
        }

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

            var stopwatch = Stopwatch.StartNew();
            PerftInternal(this, depth, true, perftData, includeDivideMap, includeExtraCountTypes);
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

        [CLSCompliant(false)]
        public PerftResult Perft(int depth)
        {
            return Perft(depth, PerftFlags.None);
        }

        public GameBoard MakeMove([NotNull] PieceMove move)
        {
            return new GameBoard(this, move);
        }

        #endregion

        #region IGameBoard Members

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

        public Position[] GetPiecePositions(Piece piece)
        {
            return _pieceData.GetPiecePositions(piece);
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

            //// TODO [vmcl] Check threefold repetition

            if (this.FullMoveCountBy50MoveRule >= ChessConstants.FullMoveCountBy50MoveRule)
            {
                return AutoDrawType.FiftyMoveRule;
            }

            return AutoDrawType.None;
        }

        public PackedGameBoard Pack()
        {
            return new PackedGameBoard(this);
        }

        IGameBoard IGameBoard.MakeMove(PieceMove move)
        {
            return MakeMove(move);
        }

        #endregion

        #region Private Methods

        private void Validate()
        {
            _pieceData.EnsureConsistency();

            foreach (var king in ChessConstants.BothKings)
            {
                var count = _pieceData.GetPiecePositions(king).Length;
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
                    .PieceTypes
                    .Where(item => item != PieceType.None)
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
            var activeKingPosition = _pieceData.GetPiecePositions(activeKing).Single();
            var oppositeColor = _activeColor.Invert();

            var checkAttackPositions = _pieceData.GetAttackingPositions(activeKingPosition, oppositeColor);
            var isInCheck = checkAttackPositions.Length != 0;
            var isInDoubleCheck = checkAttackPositions.Length > 1;

            var pinnedPieceMap = _pieceData
                .GetPinnedPieceInfos(activeKingPosition)
                .ToDictionary(item => item.Position, item => item.AllowedMoves);

            Func<Position, Position, bool> isValidMoveByPinning =
                (sourcePosition, targetPosition) =>
                {
                    Bitboard bitboard;
                    var found = pinnedPieceMap.TryGetValue(sourcePosition, out bitboard);
                    return !found || ((bitboard & targetPosition.Bitboard) == targetPosition.Bitboard);
                };

            validMoves = new Dictionary<PieceMove, PieceMoveInfo>();
            var validMovesReference = validMoves;

            Action<Position, Position, bool, Func<PieceMove, bool>> addBasicMove =
                (sourcePosition, targetPosition, isCapturing, checkMove) =>
                {
                    var isPawnPromotion = _pieceData.IsPawnPromotion(sourcePosition, targetPosition);
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

                    if (isCapturing)
                    {
                        moveFlags |= PieceMoveFlags.IsCapture;
                    }

                    var pieceMoveInfo = new PieceMoveInfo(moveFlags);
                    validMovesReference.Add(basicMove, pieceMoveInfo);

                    if (isPawnPromotion)
                    {
                        ChessHelper.NonDefaultPromotions.Select(basicMove.MakePromotion).DoForEach(
                            move => validMovesReference.Add(move, pieceMoveInfo));
                    }
                };

            var activePieceNoKingPositions = Lazy.Create(
                () => ChessConstants
                    .PieceTypes
                    .Where(item => item != PieceType.None && item != PieceType.King)
                    .SelectMany(item => _pieceData.GetPiecePositions(item.ToPiece(_activeColor)))
                    .ToArray());

            var noActiveKingPieceData = _pieceData.Copy();
            noActiveKingPieceData.SetPiece(activeKingPosition, Piece.None);

            var activeKingMoves = _pieceData
                .GetPotentialMovePositions(
                    isInCheck ? CastlingOptions.None : _castlingOptions,
                    null,
                    activeKingPosition)
                .Where(position => !noActiveKingPieceData.IsUnderAttack(position, oppositeColor))
                .Select(position => new PieceMove(activeKingPosition, position))
                .Where(
                    move =>
                        isInCheck
                            || _pieceData
                                .CheckCastlingMove(move)
                                .Morph(info => !_pieceData.IsUnderAttack(info.PassedPosition, oppositeColor), true))
                .Select(
                    move =>
                        new
                        {
                            Move = move,
                            Flags = _pieceData[move.To] == Piece.None ? PieceMoveFlags.None : PieceMoveFlags.IsCapture
                        })
                .ToArray();

            foreach (var activeKingMove in activeKingMoves)
            {
                validMovesReference.Add(activeKingMove.Move, new PieceMoveInfo(activeKingMove.Flags));
            }

            if (isInCheck)
            {
                if (!isInDoubleCheck)
                {
                    var checkAttackPosition = checkAttackPositions.Single();
                    var checkingPieceInfo = _pieceData.GetPieceInfo(checkAttackPosition);

                    var capturingSourcePositions = _pieceData
                        .GetAttackingPositions(checkAttackPosition, _activeColor)
                        .Where(
                            position =>
                                _pieceData[position] != activeKing
                                    && isValidMoveByPinning(position, checkAttackPosition))
                        .ToArray();

                    capturingSourcePositions.DoForEach(
                        sourcePosition => addBasicMove(sourcePosition, checkAttackPosition, true, null));

                    if (_enPassantCaptureInfo != null
                        && _enPassantCaptureInfo.TargetPiecePosition == checkAttackPosition)
                    {
                        //// TODO [vmcl] Fast to implement approach (likely non-optimal)

                        var activeColorPawn = PieceType.Pawn.ToPiece(_activeColor);
                        var activePawnPositions = _pieceData.GetPiecePositions(activeColorPawn);
                        var capturePosition = _enPassantCaptureInfo.CapturePosition;
                        foreach (var activePawnPosition in activePawnPositions)
                        {
                            var canCapture = _pieceData
                                .GetPotentialMovePositions(
                                    CastlingOptions.None,
                                    _enPassantCaptureInfo,
                                    activePawnPosition)
                                .Contains(capturePosition);

                            if (canCapture && isValidMoveByPinning(activePawnPosition, capturePosition))
                            {
                                addBasicMove(activePawnPosition, capturePosition, true, null);
                            }
                        }
                    }

                    if (checkingPieceInfo.PieceType.IsSliding())
                    {
                        var bridgeKey = new PositionBridgeKey(checkAttackPosition, activeKingPosition);
                        var positionBridge = ChessHelper.PositionBridgeMap[bridgeKey];

                        if (!positionBridge.IsZero())
                        {
                            var moves = activePieceNoKingPositions.Value
                                .SelectMany(
                                    sourcePosition => _pieceData
                                        .GetPotentialMovePositions(
                                            _castlingOptions,
                                            _enPassantCaptureInfo,
                                            sourcePosition)
                                        .Where(
                                            targetPosition =>
                                                !(targetPosition.Bitboard & positionBridge).IsZero()
                                                    && isValidMoveByPinning(sourcePosition, targetPosition))
                                        .Select(targetPosition => new PieceMove(sourcePosition, targetPosition)))
                                .ToArray();

                            foreach (var move in moves)
                            {
                                addBasicMove(move.From, move.To, false, null);
                            }
                        }
                    }
                }

                state = validMoves.Count == 0
                    ? GameState.Checkmate
                    : (isInDoubleCheck ? GameState.DoubleCheck : GameState.Check);

                return;
            }

            foreach (var sourcePosition in activePieceNoKingPositions.Value)
            {
                var potentialMovePositions = _pieceData.GetPotentialMovePositions(
                    _castlingOptions,
                    _enPassantCaptureInfo,
                    sourcePosition);

                var filteredDestinationPositions = potentialMovePositions
                    .Where(position => isValidMoveByPinning(sourcePosition, position))
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

                    addBasicMove(
                        sourcePosition,
                        destinationPosition,
                        isEnPassantCapture || _pieceData[destinationPosition] != Piece.None,
                        null);
                }
            }

            state = validMoves.Count == 0 ? GameState.Stalemate : GameState.Default;
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
            out ReadOnlyDictionary<PieceMove, PieceMoveInfo> validMoves,
            out GameState state,
            out string resultString)
        {
            Validate();

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
            bool isTopDepth,
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

            if (isTopDepth)
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
    }
}