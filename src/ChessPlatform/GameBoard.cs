using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using ChessPlatform.Internal;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public sealed class GameBoard
    {
        private const int ThreefoldCount = 3;
        private const int ValidMoveCapacity = 64;
        private const int PotentialMoveListCapacity = 512;

        [ThreadStatic]
        private static volatile List<GameMoveData> _potentialMoveDatas;

        [ThreadStatic]
        private static volatile List<GameMoveData> _initializeValidMovesAndStateWhenNotInCheckMoveDatas;

        private readonly GameBoardData _gameBoardData;

        private readonly GameSide _activeSide;
        private readonly GameState _state;
        private readonly AutoDrawType _autoDrawType;
        private readonly CastlingOptions _castlingOptions;

        [CanBeNull]
        private readonly EnPassantCaptureInfo _enPassantCaptureInfo;

        private readonly ReadOnlyDictionary<GameMove, GameMoveFlags> _validMoves;
        private readonly int _halfMoveCountBy50MoveRule;
        private readonly int _fullMoveIndex;
        private readonly string _resultString;
        private readonly bool _validateAfterMove;
        private readonly ReadOnlyDictionary<long, int> _repetitions;
        private readonly long _zobristKey;

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
            LastCapturedPiece = Piece.None;

            SetupDefault(
                out _activeSide,
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
                out _repetitions,
                out _zobristKey);
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
            if (string.IsNullOrWhiteSpace(fen))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    nameof(fen));
            }

            _validateAfterMove = validateAfterMove;
            _gameBoardData = new GameBoardData();
            LastCapturedPiece = Piece.None;

            SetupByFen(
                fen,
                out _activeSide,
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
                out _repetitions,
                out _zobristKey);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameBoard"/> class
        ///     using the specified previous state and specified move.
        /// </summary>
        private GameBoard([NotNull] GameBoard previousBoard, [CanBeNull] GameMove move)
        {
            if (previousBoard == null)
            {
                throw new ArgumentNullException(nameof(previousBoard));
            }

            PreviousBoard = previousBoard;
            _validateAfterMove = previousBoard._validateAfterMove;
            _gameBoardData = previousBoard._gameBoardData.Copy();
            _activeSide = previousBoard._activeSide.Invert();
            _castlingOptions = previousBoard._castlingOptions;

            _fullMoveIndex = previousBoard._fullMoveIndex + (move != null && _activeSide == GameSide.White ? 1 : 0);

            if (move == null)
            {
                _enPassantCaptureInfo = previousBoard._enPassantCaptureInfo;
                PreviousMove = previousBoard.PreviousMove;
                LastCapturedPiece = previousBoard.LastCapturedPiece;
                _halfMoveCountBy50MoveRule = previousBoard._halfMoveCountBy50MoveRule;
            }
            else
            {
                _enPassantCaptureInfo = _gameBoardData.GetEnPassantCaptureInfo(move);

                var makeMoveData = _gameBoardData.MakeMove(
                    move,
                    previousBoard._activeSide,
                    previousBoard._enPassantCaptureInfo,
                    ref _castlingOptions);

                PreviousMove = makeMoveData.Move;
                LastCapturedPiece = makeMoveData.CapturedPiece;

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
                out _repetitions,
                out _zobristKey);
        }

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
            get;
        }

        public GameSide ActiveSide
        {
            [DebuggerStepThrough]
            get
            {
                return _activeSide;
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

        public ReadOnlyDictionary<GameMove, GameMoveFlags> ValidMoves
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

        public GameMove PreviousMove
        {
            [DebuggerStepThrough]
            get;
        }

        public Piece LastCapturedPiece
        {
            [DebuggerStepThrough]
            get;
        }

        public bool CanMakeNullMove => _state == GameState.Default;

        public Piece this[Square square]
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _gameBoardData.PiecePosition[square];
            }
        }

        public Bitboard this[Piece piece]
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _gameBoardData.PiecePosition[piece];
            }
        }

        public Bitboard this[GameSide side]
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _gameBoardData.PiecePosition[side];
            }
        }

        public long ZobristKey => _zobristKey;

        internal int HalfMoveCountBy50MoveRule
        {
            [DebuggerStepThrough]
            get
            {
                return _halfMoveCountBy50MoveRule;
            }
        }

        [DebuggerStepThrough]
        public static bool IsValidFen(string fen)
        {
            if (!ChessHelper.IsValidFenFormat(fen))
            {
                return false;
            }

            try
            {
                //// TODO [HarinezumiSama] Create FEN verification which is NOT exception based
                // ReSharper disable once ObjectCreationAsStatement
                new GameBoard(fen);
            }
            catch (Exception ex)
                when (ex is ArgumentException || ex is ChessPlatformException)
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
            if (depth < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(depth),
                    depth,
                    @"The value cannot be negative.");
            }

            var perftData = new PerftData();
            var includeDivideMap = flags.HasFlag(PerftFlags.IncludeDivideMap);
            var includeExtraCountTypes = flags.HasFlag(PerftFlags.IncludeExtraCountTypes);
            var enableParallelism = flags.HasFlag(PerftFlags.EnableParallelism);

            var stopwatch = Stopwatch.StartNew();
            PerftInternal(this, depth, enableParallelism, perftData, includeDivideMap, includeExtraCountTypes);
            stopwatch.Stop();

            return new PerftResult(
                flags,
                depth,
                stopwatch.Elapsed,
                perftData.NodeCount,
                perftData.CaptureCount,
                perftData.EnPassantCaptureCount,
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
            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            if (!_validMoves.ContainsKey(move))
            {
                throw new ArgumentException($@"The move '{move}' is not valid.", nameof(move));
            }

            return new GameBoard(this, move);
        }

        public GameBoard MakeNullMove()
        {
            if (!CanMakeNullMove)
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
                currentBoard = currentBoard.PreviousBoard;
            }

            boards.Reverse();

            return boards.ToArray();
        }

        public string GetFen()
        {
            var pieceDataSnippet = _gameBoardData.GetFenSnippet();
            var activeSideSnippet = _activeSide.GetFenSnippet();
            var castlingOptionsSnippet = _castlingOptions.GetFenSnippet();
            var enPassantCaptureInfoSnippet = _enPassantCaptureInfo.GetFenSnippet();

            var result = string.Join(
                ChessConstants.FenSnippetSeparator,
                pieceDataSnippet,
                activeSideSnippet,
                castlingOptionsSnippet,
                enPassantCaptureInfoSnippet,
                _halfMoveCountBy50MoveRule.ToString(CultureInfo.InvariantCulture),
                _fullMoveIndex.ToString(CultureInfo.InvariantCulture));

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitboard GetBitboard(Piece piece)
        {
            return _gameBoardData.PiecePosition[piece];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitboard GetBitboard(GameSide side)
        {
            return _gameBoardData.PiecePosition[side];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValidMove(GameMove move)
        {
            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            return _validMoves.ContainsKey(move);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPawnPromotionMove(GameMove move)
        {
            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            var result = _gameBoardData.IsPawnPromotion(move.From, move.To);
            return result;
        }

        public bool IsCapturingMove(GameMove move)
        {
            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            if (_gameBoardData.IsEnPassantCapture(move.From, move.To, _enPassantCaptureInfo))
            {
                return true;
            }

            var sourcePiece = this[move.From];
            var destinationPiece = this[move.To];

            var result = sourcePiece.GetSide() == _activeSide
                && destinationPiece.GetSide() == _activeSide.Invert();

            return result;
        }

        public CastlingInfo CheckCastlingMove(GameMove move)
        {
            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            return ValidMoves.ContainsKey(move) ? _gameBoardData.CheckCastlingMove(move) : null;
        }

        public Square[] GetAttacks(Square targetSquare, GameSide attackingSide)
        {
            var bitboard = _gameBoardData.GetAttackers(targetSquare, attackingSide);
            return bitboard.GetSquares();
        }

        public GameMove[] GetValidMovesBySource(Square sourceSquare)
        {
            return ValidMoves.Keys.Where(move => move.From == sourceSquare).ToArray();
        }

        public GameMove[] GetValidMovesByDestination(Square destinationSquare)
        {
            return ValidMoves.Keys.Where(move => move.To == destinationSquare).ToArray();
        }

        public AutoDrawType GetAutoDrawType()
        {
            return _autoDrawType;
        }

        [NotNull]
        public GameMove ParseSanMove([NotNull] string sanMoveText)
        {
            if (string.IsNullOrWhiteSpace(sanMoveText))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    nameof(sanMoveText));
            }

            var match = SanMoveHelper.SanMoveRegex.Match(sanMoveText);
            if (!match.Success)
            {
                throw new InvalidOperationException($@"Invalid SAN move format: '{sanMoveText}'.");
            }

            KeyValuePair<GameMove, GameMoveFlags>[] filteredPairs;

            var moveNotation = match.Groups[SanMoveHelper.MoveNotationGroupName].Value;
            switch (moveNotation)
            {
                case SanMoveHelper.ShortCastlingSymbol:
                    filteredPairs = FindCastlingMoveInternal(true);
                    break;

                case SanMoveHelper.LongCastlingSymbol:
                    filteredPairs = FindCastlingMoveInternal(false);
                    break;

                default:
                    var movedPiece = match.Groups[SanMoveHelper.MovedPieceGroupName].Value;
                    var fromFile = match.Groups[SanMoveHelper.FromFileGroupName].Value;
                    var fromRank = match.Groups[SanMoveHelper.FromRankGroupName].Value;
                    var isCapture =
                        match.Groups[SanMoveHelper.CaptureSignGroupName].Value == SanMoveHelper.CaptureSymbol;
                    var to = match.Groups[SanMoveHelper.ToGroupName].Value;
                    var promotion = match.Groups[SanMoveHelper.PromotionGroupName].Value;
                    var check = match.Groups[SanMoveHelper.CheckGroupName].Value;

                    var sanMove = new SanMove
                    {
                        MovedPiece = movedPiece.IsNullOrEmpty()
                            ? PieceType.Pawn
                            : ChessConstants.FenCharToPieceTypeMap[movedPiece.Single()],
                        FromFile =
                            fromFile.IsNullOrEmpty() ? default(int?) : Square.GetFileFromChar(fromFile.Single()),
                        FromRank =
                            fromRank.IsNullOrEmpty() ? default(int?) : Square.GetRankFromChar(fromRank.Single()),
                        IsCapture = isCapture,
                        To = Square.FromAlgebraic(to),
                        Promotion =
                            promotion.IsNullOrEmpty()
                                ? PieceType.None
                                : ChessConstants.FenCharToPieceTypeMap[promotion.Single()],
                        IsCheck = check == SanMoveHelper.CheckSymbol,
                        IsCheckmate = check == SanMoveHelper.CheckmateSymbol
                    };

                    filteredPairs = ValidMoves
                        .Where(
                            pair =>
                                pair.Key.To == sanMove.To
                                    && this[pair.Key.From].GetPieceType() == sanMove.MovedPiece
                                    && pair.Key.PromotionResult == sanMove.Promotion
                                    && (!sanMove.FromFile.HasValue || pair.Key.From.File == sanMove.FromFile.Value)
                                    && (!sanMove.FromRank.HasValue || pair.Key.From.Rank == sanMove.FromRank.Value)
                                    && pair.Value.IsAnyCapture() == sanMove.IsCapture)
                        .ToArray();

                    break;
            }

            switch (filteredPairs.Length)
            {
                case 0:
                    throw new ChessPlatformException(
                        $@"Invalid SAN move '{sanMoveText}' for the board '{GetFen()}'.");

                case 1:
                    return filteredPairs[0].Key;

                default:
                    throw new ChessPlatformException(
                        $@"Ambiguous SAN move '{sanMoveText}' for the board '{GetFen()}': {filteredPairs.Length
                            } options.");
            }
        }

        public bool IsSamePosition([NotNull] GameBoard otherBoard)
        {
            if (otherBoard == null)
            {
                throw new ArgumentNullException(nameof(otherBoard));
            }

            return ReferenceEquals(this, otherBoard) ||
                (_zobristKey == otherBoard._zobristKey
                    && _castlingOptions == otherBoard._castlingOptions
                    && _activeSide == otherBoard._activeSide
                    && _enPassantCaptureInfo == otherBoard._enPassantCaptureInfo
                    && _gameBoardData.IsSamePosition(otherBoard._gameBoardData));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ClearPotentialMoveDatas()
        {
            if (_potentialMoveDatas == null)
            {
                _potentialMoveDatas = new List<GameMoveData>(PotentialMoveListCapacity);
            }
            else
            {
                _potentialMoveDatas.Clear();
            }
        }

        private static bool IsValidMoveByPinning(
            Bitboard[] pinLimitations,
            Square sourceSquare,
            Square destinationSquare)
        {
            var bitboard = pinLimitations[sourceSquare.SquareIndex];

            var intersection = destinationSquare.Bitboard & bitboard;
            return intersection.IsAny;
        }

        private static void AddMove(
            AddMoveData addMoveData,
            Square sourceSquare,
            Square targetSquare,
            GameMoveFlags moveFlags)
        {
            var isPawnPromotion = moveFlags.IsAnySet(GameMoveFlags.IsPawnPromotion);
            var promotionResult = isPawnPromotion ? ChessHelper.DefaultPromotion : PieceType.None;
            var basicMove = new GameMove(sourceSquare, targetSquare, promotionResult);

            addMoveData.ValidMoves.Add(basicMove, moveFlags);

            if (isPawnPromotion)
            {
                ChessHelper.NonDefaultPromotions.Select(basicMove.MakePromotion).DoForEach(
                    move => addMoveData.ValidMoves.Add(move, moveFlags));
            }
        }

        private static void GenerateKingMoves(
            [NotNull] AddMoveData addMoveData,
            Square activeKingSquare,
            bool isInCheck)
        {
            var gameBoardData = addMoveData.GameBoardData;

            ClearPotentialMoveDatas();

            gameBoardData.GenerateKingMoves(
                _potentialMoveDatas,
                addMoveData.ActiveSide,
                isInCheck ? CastlingOptions.None : addMoveData.CastlingOptions,
                Bitboard.Everything);

            if (_potentialMoveDatas.Count == 0)
            {
                return;
            }

            var noActiveKingGameBoardData = gameBoardData.Copy();
            noActiveKingGameBoardData.PiecePosition.SetPiece(activeKingSquare, Piece.None);

            var oppositeSide = addMoveData.OppositeSide;

            // ReSharper disable once ForCanBeConvertedToForeach - For optimization
            for (var index = 0; index < _potentialMoveDatas.Count; index++)
            {
                var potentialMoveData = _potentialMoveDatas[index];

                if (noActiveKingGameBoardData.IsUnderAttack(potentialMoveData.Move.To, oppositeSide))
                {
                    continue;
                }

                if (!isInCheck && potentialMoveData.MoveFlags.IsKingCastling())
                {
                    var castlingInfo = gameBoardData.CheckCastlingMove(potentialMoveData.Move);
                    if (gameBoardData.IsUnderAttack(castlingInfo.PassedSquare, oppositeSide))
                    {
                        continue;
                    }
                }

                addMoveData.ValidMoves.Add(potentialMoveData.Move, potentialMoveData.MoveFlags);
            }
        }

        private static void InitializeValidMovesAndStateWhenNotInCheck(AddMoveData addMoveData)
        {
            GeneratePawnMoves(addMoveData, GeneratedMoveTypes.All, Bitboard.Everything);

            var gameBoardData = addMoveData.GameBoardData;

            if (_initializeValidMovesAndStateWhenNotInCheckMoveDatas == null)
            {
                _initializeValidMovesAndStateWhenNotInCheckMoveDatas =
                    new List<GameMoveData>(PotentialMoveListCapacity);
            }
            else
            {
                _initializeValidMovesAndStateWhenNotInCheckMoveDatas.Clear();
            }

            var moveDatas = _initializeValidMovesAndStateWhenNotInCheckMoveDatas;

            gameBoardData.GenerateKnightMoves(
                moveDatas,
                addMoveData.ActiveSide,
                GeneratedMoveTypes.All,
                Bitboard.Everything);

            gameBoardData.GenerateQueenMoves(
                moveDatas,
                addMoveData.ActiveSide,
                GeneratedMoveTypes.All);

            gameBoardData.GenerateRookMoves(
                moveDatas,
                addMoveData.ActiveSide,
                GeneratedMoveTypes.All);

            gameBoardData.GenerateBishopMoves(
                moveDatas,
                addMoveData.ActiveSide,
                GeneratedMoveTypes.All);

            foreach (var moveData in moveDatas)
            {
                var move = moveData.Move;
                if (!IsValidMoveByPinning(addMoveData.PinLimitations, move.From, move.To))
                {
                    continue;
                }

                addMoveData.ValidMoves.Add(move, moveData.MoveFlags);
            }
        }

        private static void InitializeValidMovesAndStateWhenInSingleCheck(
            AddMoveData addMoveData,
            Piece activeKing,
            Square activeKingSquare,
            Bitboard checkAttackSquaresBitboard)
        {
            var gameBoardData = addMoveData.GameBoardData;
            var pinLimitations = addMoveData.PinLimitations;

            var checkAttackSquare = checkAttackSquaresBitboard.GetFirstSquare();
            var checkingPiece = gameBoardData.PiecePosition[checkAttackSquare];
            var activeKingBitboard = gameBoardData.PiecePosition[activeKing];

            //// TODO [HarinezumiSama] Generate attacker moves (this will eliminate some extra checks)
            var capturingBitboard = gameBoardData.GetAttackers(checkAttackSquare, addMoveData.ActiveSide)
                & ~activeKingBitboard;

            var currentCapturingBitboard = capturingBitboard;
            while (currentCapturingBitboard.IsAny)
            {
                var squareIndex = Bitboard.PopFirstSquareIndex(ref currentCapturingBitboard);
                var capturingSourceSquare = new Square(squareIndex);

                if (!IsValidMoveByPinning(pinLimitations, capturingSourceSquare, checkAttackSquare))
                {
                    continue;
                }

                var moveFlags = GameMoveFlags.IsRegularCapture;
                if (gameBoardData.IsPawnPromotion(capturingSourceSquare, checkAttackSquare))
                {
                    moveFlags |= GameMoveFlags.IsPawnPromotion;
                }

                if (gameBoardData.IsEnPassantCapture(
                    capturingSourceSquare,
                    checkAttackSquare,
                    addMoveData.EnPassantCaptureInfo))
                {
                    moveFlags |= GameMoveFlags.IsEnPassantCapture;
                    moveFlags &= ~GameMoveFlags.IsRegularCapture;
                }

                AddMove(addMoveData, capturingSourceSquare, checkAttackSquare, moveFlags);
            }

            var enPassantCaptureInfo = addMoveData.EnPassantCaptureInfo;
            if (enPassantCaptureInfo != null && enPassantCaptureInfo.TargetPieceSquare == checkAttackSquare)
            {
                GeneratePawnMoves(
                    addMoveData,
                    GeneratedMoveTypes.Capture,
                    enPassantCaptureInfo.CaptureSquare.Bitboard);
            }

            if (!checkingPiece.GetPieceType().IsSliding())
            {
                return;
            }

            var squareBridgeKey = new SquareBridgeKey(checkAttackSquare, activeKingSquare);
            var squareBridge = ChessHelper.SquareBridgeMap[squareBridgeKey];

            if (squareBridge.IsNone)
            {
                return;
            }

            GeneratePawnMoves(addMoveData, GeneratedMoveTypes.Quiet, squareBridge);

            var moves = addMoveData.ActivePiecesExceptKingAndPawnsBitboard
                .GetSquares()
                .SelectMany(
                    sourceSquare => gameBoardData
                        .GetPotentialMoveSquares(
                            addMoveData.CastlingOptions,
                            enPassantCaptureInfo,
                            sourceSquare)
                        .Where(
                            destinationSquare =>
                                (destinationSquare.Bitboard & squareBridge).IsAny
                                    && IsValidMoveByPinning(
                                        pinLimitations,
                                        sourceSquare,
                                        destinationSquare))
                        .Select(destinationSquare => new GameMove(sourceSquare, destinationSquare)))
                .ToArray();

            foreach (var move in moves)
            {
                AddMove(addMoveData, move.From, move.To, GameMoveFlags.None);
            }
        }

        private static void GeneratePawnMoves(
            AddMoveData addMoveData,
            GeneratedMoveTypes generatedMoveTypes,
            Bitboard target)
        {
            var gameBoardData = addMoveData.GameBoardData;
            var enPassantCaptureInfo = addMoveData.EnPassantCaptureInfo;
            var pinLimitations = addMoveData.PinLimitations;

            ClearPotentialMoveDatas();

            gameBoardData.GeneratePawnMoves(
                _potentialMoveDatas,
                addMoveData.ActiveSide,
                generatedMoveTypes,
                enPassantCaptureInfo?.CaptureSquare.Bitboard ?? Bitboard.None,
                target);

            // ReSharper disable once ForCanBeConvertedToForeach - For optimization
            for (var index = 0; index < _potentialMoveDatas.Count; index++)
            {
                var gameMoveData = _potentialMoveDatas[index];

                var potentialPawnMove = gameMoveData.Move;

                var sourceSquare = potentialPawnMove.From;
                var destinationSquare = potentialPawnMove.To;
                if (!IsValidMoveByPinning(pinLimitations, sourceSquare, destinationSquare))
                {
                    continue;
                }

                var isEnPassantCapture = gameBoardData.IsEnPassantCapture(
                    sourceSquare,
                    destinationSquare,
                    enPassantCaptureInfo);
                if (isEnPassantCapture)
                {
                    var temporaryCastlingOptions = addMoveData.CastlingOptions;

                    var move = new GameMove(sourceSquare, destinationSquare);

                    gameBoardData.MakeMove(
                        move,
                        addMoveData.ActiveSide,
                        enPassantCaptureInfo,
                        ref temporaryCastlingOptions);

                    var isInvalidMove = gameBoardData.IsInCheck(addMoveData.ActiveSide);
                    gameBoardData.UndoMove();

                    if (isInvalidMove)
                    {
                        continue;
                    }
                }

                addMoveData.ValidMoves.Add(gameMoveData.Move, gameMoveData.MoveFlags);
            }
        }

        private static void PerftInternal(
            GameBoard gameBoard,
            int depth,
            bool enableParallelism,
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

            var validMoves = gameBoard.ValidMoves;
            var moves = validMoves.Keys;

            if (depth == 1)
            {
                ulong captureCount = 0;
                ulong enPassantCaptureCount = 0;
                foreach (var validMove in validMoves)
                {
                    if (validMove.Value.IsAnyCapture())
                    {
                        captureCount++;
                    }

                    if (validMove.Value.IsEnPassantCapture())
                    {
                        enPassantCaptureCount++;
                    }
                }

                checked
                {
                    perftData.CaptureCount += captureCount;
                    perftData.EnPassantCaptureCount += enPassantCaptureCount;
                }

                if (!includeDivideMap && !includeExtraCountTypes)
                {
                    checked
                    {
                        perftData.NodeCount += (ulong)moves.Count;
                    }

                    return;
                }
            }

            if (enableParallelism)
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

            GameBoardData gameBoardDataCopy = null;
            if (depth == 1 && includeExtraCountTypes && !includeDivideMap)
            {
                gameBoardDataCopy = gameBoard._gameBoardData.Copy();
            }

            foreach (var move in moves)
            {
                if (gameBoardDataCopy != null)
                {
                    var castlingOptions = gameBoard._castlingOptions;
                    gameBoardDataCopy.MakeMove(
                        move,
                        gameBoard._activeSide,
                        gameBoard._enPassantCaptureInfo,
                        ref castlingOptions);

                    var isInCheck = gameBoardDataCopy.IsInCheck(gameBoard._activeSide.Invert());
                    gameBoardDataCopy.UndoMove();

                    if (!isInCheck)
                    {
                        checked
                        {
                            perftData.NodeCount++;
                        }

                        continue;
                    }
                }

                var previousNodeCount = perftData.NodeCount;

                var newBoard = gameBoard.MakeMove(move);
                PerftInternal(newBoard, depth - 1, false, perftData, false, includeExtraCountTypes);

                if (includeDivideMap)
                {
                    perftData.DividedMoves[move] = checked(perftData.DividedMoves.GetValueOrDefault(move)
                        + perftData.NodeCount - previousNodeCount);
                }
            }
        }

        private static bool ShouldIncludeEnPassantHash(
            [CanBeNull] EnPassantCaptureInfo enPassantCaptureInfo,
            [NotNull] ReadOnlyDictionary<GameMove, GameMoveFlags> validMoves)
        {
            if (enPassantCaptureInfo == null)
            {
                return false;
            }

            var result = validMoves.Any(
                pair => pair.Value.IsEnPassantCapture() && pair.Key.To == enPassantCaptureInfo.CaptureSquare);

            return result;
        }

        private void Validate(bool forceValidation)
        {
            if (!forceValidation && !_validateAfterMove)
            {
                return;
            }

            _gameBoardData.PiecePosition.EnsureConsistency();

            foreach (var king in ChessConstants.BothKings)
            {
                const int ExpectedCount = 1;

                var count = _gameBoardData.PiecePosition[king].GetSquareCount();
                if (count != ExpectedCount)
                {
                    throw new ChessPlatformException(
                        $@"The number of the '{king.GetDescription()}' pieces is {count}. Must be exactly {
                            ExpectedCount}.");
                }
            }

            if (_gameBoardData.IsInCheck(_activeSide.Invert()))
            {
                throw new ChessPlatformException("Inactive king is in check.");
            }

            foreach (var side in ChessConstants.GameSides)
            {
                var pieceToCountMap = ChessConstants
                    .PieceTypesExceptNone
                    .ToDictionary(
                        Factotum.Identity,
                        item => _gameBoardData.PiecePosition[item.ToPiece(side)].GetSquareCount());

                var allCount = pieceToCountMap.Values.Sum();
                var pawnCount = pieceToCountMap[PieceType.Pawn];

                if (pawnCount > ChessConstants.MaxPawnCountPerSide)
                {
                    throw new ChessPlatformException(
                        $@"Too many '{side.ToPiece(PieceType.Pawn).GetDescription()}' ({pawnCount}).");
                }

                if (allCount > ChessConstants.MaxPieceCountPerSide)
                {
                    throw new ChessPlatformException($@"Too many {side.GetName()} side pieces ({allCount}).");
                }
            }

            var allPawnsBitboard = _gameBoardData.PiecePosition[PieceType.Pawn.ToPiece(GameSide.White)]
                | _gameBoardData.PiecePosition[PieceType.Pawn.ToPiece(GameSide.Black)];

            if ((allPawnsBitboard & ChessHelper.InvalidPawnSquaresBitboard).IsAny)
            {
                throw new ChessPlatformException("One or more pawns are located at the invalid rank.");
            }

            if (_castlingOptions != CastlingOptions.None)
            {
                foreach (var castlingInfo in ChessHelper.CastlingOptionToInfoMap.Values)
                {
                    if (!_castlingOptions.IsAnySet(castlingInfo.Option))
                    {
                        continue;
                    }

                    var side = castlingInfo.GameSide;
                    var king = side.ToPiece(PieceType.King);
                    var rook = side.ToPiece(PieceType.Rook);

                    if (_gameBoardData.PiecePosition[castlingInfo.KingMove.From] != king
                        || _gameBoardData.PiecePosition[castlingInfo.RookMove.From] != rook)
                    {
                        throw new ChessPlatformException(
                            $@"Invalid position. {side} cannot castle {castlingInfo.CastlingSide}.");
                    }
                }
            }

            //// TODO [HarinezumiSama] (*) Other verifications
        }

        private void ValidateValidMoves()
        {
            if (!_validateAfterMove)
            {
                return;
            }

            foreach (var pair in ValidMoves)
            {
                var move = pair.Key;
                var expectedIsPawnPromotion = _gameBoardData.IsPawnPromotion(move.From, move.To);
                if (pair.Value.IsPawnPromotion() != expectedIsPawnPromotion)
                {
                    throw new ChessPlatformException(
                        $@"The move '{move}' is inconsistent with respect to its pawn promotion state.");
                }

                var expectedIsRegularCapture = _gameBoardData.PiecePosition[move.To] != Piece.None;
                if (pair.Value.IsRegularCapture() != expectedIsRegularCapture)
                {
                    throw new ChessPlatformException(
                        $@"The move '{move}' is inconsistent with respect to its capture state.");
                }

                var expectedIsEnPassantCapture = _gameBoardData.IsEnPassantCapture(
                    move.From,
                    move.To,
                    _enPassantCaptureInfo);
                if (pair.Value.IsEnPassantCapture() != expectedIsEnPassantCapture)
                {
                    throw new ChessPlatformException(
                        $@"The move '{move}' is inconsistent with respect to its en passant capture state.");
                }

                var expectedIsKingCastling = _gameBoardData.CheckCastlingMove(move) != null;
                if (pair.Value.IsKingCastling() != expectedIsKingCastling)
                {
                    throw new ChessPlatformException(
                        $@"The move '{move}' is inconsistent with respect to its king castling state.");
                }
            }
        }

        private void InitializeValidMovesAndState(
            out Dictionary<GameMove, GameMoveFlags> validMoves,
            out GameState state)
        {
            var activeKing = _activeSide.ToPiece(PieceType.King);
            var activeKingSquare = _gameBoardData.PiecePosition[activeKing].GetFirstSquare();
            var oppositeSide = _activeSide.Invert();

            var attackersBitboard = _gameBoardData.GetAttackers(activeKingSquare, oppositeSide);
            var isInCheck = attackersBitboard.IsAny;
            var isInDoubleCheck = isInCheck && !attackersBitboard.IsExactlyOneSquare();

            var pinLimitations = _gameBoardData.GetPinLimitations(activeKingSquare.SquareIndex, oppositeSide);

            var activePiecesExceptKingAndPawnsBitboard = _gameBoardData.PiecePosition[_activeSide]
                & ~_gameBoardData.PiecePosition[activeKing]
                & ~_gameBoardData.PiecePosition[_activeSide.ToPiece(PieceType.Pawn)];

            validMoves = new Dictionary<GameMove, GameMoveFlags>(ValidMoveCapacity);

            var addMoveData = new AddMoveData(
                _gameBoardData,
                validMoves,
                _enPassantCaptureInfo,
                _activeSide,
                _castlingOptions,
                pinLimitations,
                activePiecesExceptKingAndPawnsBitboard);

            GenerateKingMoves(addMoveData, activeKingSquare, isInCheck);

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
                    activeKingSquare,
                    attackersBitboard);
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
                    resultString = _activeSide == GameSide.White ? ResultStrings.BlackWon : ResultStrings.WhiteWon;
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
            out ReadOnlyDictionary<GameMove, GameMoveFlags> validMoves,
            out GameState state,
            out AutoDrawType autoDrawType,
            out string resultString,
            out ReadOnlyDictionary<long, int> repetitions,
            out long zobristKey)
        {
            Validate(forceValidation);

            Dictionary<GameMove, GameMoveFlags> validMoveMap;
            InitializeValidMovesAndState(out validMoveMap, out state);
            validMoves = validMoveMap.AsReadOnly();

            zobristKey = _gameBoardData.PiecePosition.ZobristKey
                ^ ZobristHashHelper.GetCastlingHash(_castlingOptions)
                ^ (ShouldIncludeEnPassantHash(_enPassantCaptureInfo, validMoves)
                    ? ZobristHashHelper.GetEnPassantHash(
                        _enPassantCaptureInfo,
                        GetBitboard(_activeSide.ToPiece(PieceType.Pawn)))
                    : 0L)
                ^ ZobristHashHelper.GetTurnHash(_activeSide);

            if (PreviousBoard != null && PreviousBoard._autoDrawType == AutoDrawType.ThreefoldRepetition)
            {
                autoDrawType = AutoDrawType.ThreefoldRepetition;
                repetitions = null;
            }
            else
            {
                if (PreviousBoard != null && PreviousBoard._repetitions == null)
                {
                    throw new InvalidOperationException("Internal logic error: repetition map is not assigned.");
                }

                var repetitionMap = PreviousBoard == null
                    ? new Dictionary<long, int>()
                    : new Dictionary<long, int>(PreviousBoard._repetitions);

                var thisRepetitionCount = repetitionMap.GetValueOrDefault(zobristKey) + 1;
                repetitionMap[zobristKey] = thisRepetitionCount;
                repetitions = repetitionMap.AsReadOnly();

                var isThreefoldRepetition = false;
                if (thisRepetitionCount >= ThreefoldCount)
                {
                    var exactRepetitionCount = GetHistory().Where(IsSamePosition).Count();
                    isThreefoldRepetition = exactRepetitionCount >= ThreefoldCount;
                }

                autoDrawType = isThreefoldRepetition ? AutoDrawType.ThreefoldRepetition : AutoDrawType.None;
            }

            if (autoDrawType == AutoDrawType.None && !state.IsGameFinished())
            {
                if (FullMoveCountBy50MoveRule >= ChessConstants.FullMoveCountBy50MoveRule)
                {
                    autoDrawType = AutoDrawType.FiftyMoveRule;
                }
                else if (_gameBoardData.IsInsufficientMaterialState())
                {
                    autoDrawType = AutoDrawType.InsufficientMaterial;
                }
            }

            InitializeResultString(out resultString);

            ValidateValidMoves();
        }

        private void SetupDefault(
            out GameSide activeSide,
            out CastlingOptions castlingOptions,
            out EnPassantCaptureInfo enPassantTarget,
            out int halfMovesBy50MoveRule,
            out int fullMoveIndex)
        {
            _gameBoardData.PiecePosition.SetupNewPiece("a1", Piece.WhiteRook);
            _gameBoardData.PiecePosition.SetupNewPiece("b1", Piece.WhiteKnight);
            _gameBoardData.PiecePosition.SetupNewPiece("c1", Piece.WhiteBishop);
            _gameBoardData.PiecePosition.SetupNewPiece("d1", Piece.WhiteQueen);
            _gameBoardData.PiecePosition.SetupNewPiece("e1", Piece.WhiteKing);
            _gameBoardData.PiecePosition.SetupNewPiece("f1", Piece.WhiteBishop);
            _gameBoardData.PiecePosition.SetupNewPiece("g1", Piece.WhiteKnight);
            _gameBoardData.PiecePosition.SetupNewPiece("h1", Piece.WhiteRook);
            Square.GenerateRank(1)
                .DoForEach(square => _gameBoardData.PiecePosition.SetupNewPiece(square, Piece.WhitePawn));

            Square.GenerateRank(6)
                .DoForEach(square => _gameBoardData.PiecePosition.SetupNewPiece(square, Piece.BlackPawn));
            _gameBoardData.PiecePosition.SetupNewPiece("a8", Piece.BlackRook);
            _gameBoardData.PiecePosition.SetupNewPiece("b8", Piece.BlackKnight);
            _gameBoardData.PiecePosition.SetupNewPiece("c8", Piece.BlackBishop);
            _gameBoardData.PiecePosition.SetupNewPiece("d8", Piece.BlackQueen);
            _gameBoardData.PiecePosition.SetupNewPiece("e8", Piece.BlackKing);
            _gameBoardData.PiecePosition.SetupNewPiece("f8", Piece.BlackBishop);
            _gameBoardData.PiecePosition.SetupNewPiece("g8", Piece.BlackKnight);
            _gameBoardData.PiecePosition.SetupNewPiece("h8", Piece.BlackRook);

            activeSide = GameSide.White;
            castlingOptions = CastlingOptions.All;
            enPassantTarget = null;
            halfMovesBy50MoveRule = 0;
            fullMoveIndex = 1;
        }

        private void SetupByFen(
            string fen,
            out GameSide activeSide,
            out CastlingOptions castlingOptions,
            out EnPassantCaptureInfo enPassantCaptureInfo,
            out int halfMovesBy50MoveRule,
            out int fullMoveIndex)
        {
            const string InvalidFenMessage = "Invalid FEN.";

            var fenSnippets = fen
                .Trim()
                .Split(ChessConstants.FenSnippetSeparator.AsArray(), StringSplitOptions.None);
            if (fenSnippets.Length != ChessConstants.FenSnippetCount)
            {
                throw new ArgumentException(InvalidFenMessage, nameof(fen));
            }

            var pieceDataSnippet = fenSnippets[0];
            _gameBoardData.PiecePosition.SetupByFenSnippet(pieceDataSnippet);

            var activeSideSnippet = fenSnippets[1];
            if (!ChessConstants.FenSnippetToGameSideMap.TryGetValue(activeSideSnippet, out activeSide))
            {
                throw new ArgumentException(InvalidFenMessage, nameof(fen));
            }

            castlingOptions = CastlingOptions.None;
            var castlingOptionsSnippet = fenSnippets[2];
            if (castlingOptionsSnippet != ChessConstants.NoneCastlingOptionsFenSnippet)
            {
                var castlingOptionsSnippetSet = OmnifactotumCollectionExtensions.ToHashSet(castlingOptionsSnippet);
                foreach (var optionChar in castlingOptionsSnippetSet)
                {
                    CastlingOptions option;
                    if (!ChessConstants.FenCharCastlingOptionMap.TryGetValue(optionChar, out option))
                    {
                        throw new ArgumentException(InvalidFenMessage, nameof(fen));
                    }

                    castlingOptions |= option;
                }
            }

            enPassantCaptureInfo = null;
            var enPassantCaptureTargetSnippet = fenSnippets[3];
            if (enPassantCaptureTargetSnippet != ChessConstants.NoEnPassantCaptureFenSnippet)
            {
                var captureSquare = Square.TryFromAlgebraic(enPassantCaptureTargetSnippet);
                if (!captureSquare.HasValue)
                {
                    throw new ArgumentException(InvalidFenMessage, nameof(fen));
                }

                var enPassantInfo =
                    ChessConstants.GameSideToDoublePushInfoMap.Values.SingleOrDefault(
                        obj => obj.CaptureTargetRank == captureSquare.Value.Rank);

                if (enPassantInfo == null)
                {
                    throw new ArgumentException(InvalidFenMessage, nameof(fen));
                }

                enPassantCaptureInfo = new EnPassantCaptureInfo(
                    captureSquare.Value,
                    new Square(captureSquare.Value.File, enPassantInfo.EndRank));
            }

            var halfMovesBy50MoveRuleSnippet = fenSnippets[4];
            if (!ChessHelper.TryParseInt(halfMovesBy50MoveRuleSnippet, out halfMovesBy50MoveRule)
                || halfMovesBy50MoveRule < 0)
            {
                throw new ArgumentException(InvalidFenMessage, nameof(fen));
            }

            var fullMoveIndexSnippet = fenSnippets[5];
            if (!ChessHelper.TryParseInt(fullMoveIndexSnippet, out fullMoveIndex) || fullMoveIndex <= 0)
            {
                throw new ArgumentException(InvalidFenMessage, nameof(fen));
            }
        }

        [NotNull]
        private KeyValuePair<GameMove, GameMoveFlags>[] FindCastlingMoveInternal(bool kingSide)
        {
            var castlingOptions = kingSide ? CastlingOptions.KingSideMask : CastlingOptions.QueenSideMask;

            var result = ValidMoves
                .Where(
                    pair => pair.Value.IsKingCastling() && (CheckCastlingMove(pair.Key).Option & castlingOptions) != 0)
                .ToArray();

            return result;
        }

        private sealed class PerftData
        {
            public PerftData()
            {
                DividedMoves = new Dictionary<GameMove, ulong>();
            }

            public ulong NodeCount
            {
                get;
                set;
            }

            public ulong CaptureCount
            {
                get;
                set;
            }

            public ulong EnPassantCaptureCount
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
            }

            public static PerftData operator +(PerftData left, PerftData right)
            {
                return new PerftData
                {
                    CaptureCount = left.CaptureCount + right.CaptureCount,
                    CheckCount = left.CheckCount + right.CheckCount,
                    CheckmateCount = left.CheckmateCount + right.CheckmateCount,
                    EnPassantCaptureCount = left.EnPassantCaptureCount + right.EnPassantCaptureCount,
                    NodeCount = left.NodeCount + right.NodeCount
                };
            }

            public void Include(PerftData other)
            {
                if (other == null)
                {
                    throw new ArgumentNullException(nameof(other));
                }

                CaptureCount += other.CaptureCount;
                CheckCount += other.CheckCount;
                CheckmateCount += other.CheckmateCount;
                EnPassantCaptureCount += other.EnPassantCaptureCount;
                NodeCount += other.NodeCount;
            }
        }

        private sealed class AddMoveData
        {
            public AddMoveData(
                [NotNull] GameBoardData gameBoardData,
                [NotNull] IDictionary<GameMove, GameMoveFlags> validMovesReference,
                [CanBeNull] EnPassantCaptureInfo enPassantCaptureInfo,
                GameSide activeSide,
                CastlingOptions castlingOptions,
                [NotNull] Bitboard[] pinLimitations,
                Bitboard activePiecesExceptKingAndPawnsBitboard)
            {
                GameBoardData = gameBoardData.EnsureNotNull();
                ValidMoves = validMovesReference.EnsureNotNull();
                EnPassantCaptureInfo = enPassantCaptureInfo;
                ActiveSide = activeSide;
                OppositeSide = activeSide.Invert();
                CastlingOptions = castlingOptions;
                PinLimitations = pinLimitations.EnsureNotNull();
                ActivePiecesExceptKingAndPawnsBitboard = activePiecesExceptKingAndPawnsBitboard;
            }

            [NotNull]
            public GameBoardData GameBoardData
            {
                get;
            }

            [NotNull]
            public IDictionary<GameMove, GameMoveFlags> ValidMoves
            {
                get;
            }

            [CanBeNull]
            public EnPassantCaptureInfo EnPassantCaptureInfo
            {
                get;
            }

            public GameSide ActiveSide
            {
                get;
            }

            public GameSide OppositeSide
            {
                get;
            }

            public CastlingOptions CastlingOptions
            {
                get;
            }

            [NotNull]
            public Bitboard[] PinLimitations
            {
                get;
            }

            public Bitboard ActivePiecesExceptKingAndPawnsBitboard
            {
                get;
            }
        }

        internal struct SanMove
        {
            public PieceType MovedPiece
            {
                get;
                set;
            }

            public int? FromFile
            {
                get;
                set;
            }

            public int? FromRank
            {
                get;
                set;
            }

            public bool IsCapture
            {
                get;
                set;
            }

            public Square To
            {
                get;
                set;
            }

            public PieceType Promotion
            {
                get;
                set;
            }

            public bool IsCheck
            {
                get;
                set;
            }

            public bool IsCheckmate
            {
                get;
                set;
            }
        }
    }
}