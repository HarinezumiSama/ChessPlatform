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
        #region Constants and Fields

        private const int ThreefoldCount = 3;
        private const int ValidMoveCapacity = 64;
        private const int PotentialMoveListCapacity = 512;

        [ThreadStatic]
        private static volatile List<GameMoveData> _potentialMoveDatas;

        [ThreadStatic]
        private static volatile List<GameMoveData> _initializeValidMovesAndStateWhenNotInCheckMoveDatas;

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
                    nameof(fen));
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
                throw new ArgumentNullException(nameof(previousBoard));
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

        public bool CanMakeNullMove => !_state.IsAnyCheck();

        public Piece this[Position position] => _gameBoardData[position];

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
                    nameof(depth),
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
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            if (!_validMoves.ContainsKey(move))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "The move '{0}' is not valid.", move),
                    nameof(move));
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

        public Bitboard GetBitboard(Piece piece)
        {
            return _gameBoardData.GetBitboard(piece);
        }

        public Bitboard GetBitboard(PieceColor color)
        {
            return _gameBoardData.GetBitboard(color);
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
                throw new ArgumentNullException(nameof(move));
            }

            #endregion

            return _validMoves.ContainsKey(move);
        }

        public bool IsPawnPromotionMove(GameMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
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
                throw new ArgumentNullException(nameof(move));
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
                throw new ArgumentNullException(nameof(move));
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

        #endregion

        #region Private Methods

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
            Position sourcePosition,
            Position destinationPosition)
        {
            var bitboard = pinLimitations[sourcePosition.SquareIndex];

            var intersection = destinationPosition.Bitboard & bitboard;
            return intersection.IsAny;
        }

        private static void AddMove(
            AddMoveData addMoveData,
            Position sourcePosition,
            Position targetPosition,
            GameMoveFlags moveFlags)
        {
            var isPawnPromotion = moveFlags.IsAnySet(GameMoveFlags.IsPawnPromotion);
            var promotionResult = isPawnPromotion ? ChessHelper.DefaultPromotion : PieceType.None;
            var basicMove = new GameMove(sourcePosition, targetPosition, promotionResult);

            var pieceMoveInfo = new GameMoveInfo(moveFlags);
            addMoveData.ValidMoves.Add(basicMove, pieceMoveInfo);

            if (isPawnPromotion)
            {
                ChessHelper.NonDefaultPromotions.Select(basicMove.MakePromotion).DoForEach(
                    move => addMoveData.ValidMoves.Add(move, pieceMoveInfo));
            }
        }

        private static void GenerateKingMoves(
            [NotNull] AddMoveData addMoveData,
            Position activeKingPosition,
            bool isInCheck)
        {
            var gameBoardData = addMoveData.GameBoardData;

            ClearPotentialMoveDatas();

            gameBoardData.GenerateKingMoves(
                _potentialMoveDatas,
                addMoveData.ActiveColor,
                isInCheck ? CastlingOptions.None : addMoveData.CastlingOptions,
                Bitboard.Everything);

            if (_potentialMoveDatas.Count == 0)
            {
                return;
            }

            var noActiveKingGameBoardData = gameBoardData.Copy();
            noActiveKingGameBoardData.SetPiece(activeKingPosition, Piece.None);

            var oppositeColor = addMoveData.OppositeColor;

            // ReSharper disable once ForCanBeConvertedToForeach - For optimization
            for (var index = 0; index < _potentialMoveDatas.Count; index++)
            {
                var potentialMoveData = _potentialMoveDatas[index];

                if (noActiveKingGameBoardData.IsUnderAttack(potentialMoveData.Move.To, oppositeColor))
                {
                    continue;
                }

                if (!isInCheck && potentialMoveData.MoveInfo.IsKingCastling)
                {
                    var castlingInfo = gameBoardData.CheckCastlingMove(potentialMoveData.Move);
                    if (gameBoardData.IsUnderAttack(castlingInfo.PassedPosition, oppositeColor))
                    {
                        continue;
                    }
                }

                addMoveData.ValidMoves.Add(potentialMoveData.Move, potentialMoveData.MoveInfo);
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
                addMoveData.ActiveColor,
                GeneratedMoveTypes.All,
                Bitboard.Everything);

            gameBoardData.GenerateQueenMoves(
                moveDatas,
                addMoveData.ActiveColor,
                GeneratedMoveTypes.All);

            gameBoardData.GenerateRookMoves(
                moveDatas,
                addMoveData.ActiveColor,
                GeneratedMoveTypes.All);

            gameBoardData.GenerateBishopMoves(
                moveDatas,
                addMoveData.ActiveColor,
                GeneratedMoveTypes.All);

            foreach (var moveData in moveDatas)
            {
                var move = moveData.Move;
                if (!IsValidMoveByPinning(addMoveData.PinLimitations, move.From, move.To))
                {
                    continue;
                }

                addMoveData.ValidMoves.Add(move, moveData.MoveInfo);
            }
        }

        private static void InitializeValidMovesAndStateWhenInSingleCheck(
            AddMoveData addMoveData,
            Piece activeKing,
            Position activeKingPosition,
            Bitboard checkAttackPositionsBitboard)
        {
            var gameBoardData = addMoveData.GameBoardData;
            var pinLimitations = addMoveData.PinLimitations;

            var checkAttackPosition = checkAttackPositionsBitboard.GetFirstPosition();
            var checkingPieceInfo = gameBoardData.GetPieceInfo(checkAttackPosition);
            var activeKingBitboard = gameBoardData.GetBitboard(activeKing);

            //// TODO [vmcl] Generate attacker moves (this will eliminate some extra checks)
            var capturingBitboard = gameBoardData.GetAttackers(checkAttackPosition, addMoveData.ActiveColor)
                & ~activeKingBitboard;

            var currentCapturingBitboard = capturingBitboard;
            while (currentCapturingBitboard.IsAny)
            {
                var squareIndex = Bitboard.PopFirstBitSetIndex(ref currentCapturingBitboard);
                var capturingSourcePosition = Position.FromSquareIndex(squareIndex);

                if (!IsValidMoveByPinning(pinLimitations, capturingSourcePosition, checkAttackPosition))
                {
                    continue;
                }

                var moveFlags = GameMoveFlags.IsCapture;
                if (gameBoardData.IsPawnPromotion(capturingSourcePosition, checkAttackPosition))
                {
                    moveFlags |= GameMoveFlags.IsPawnPromotion;
                }

                if (gameBoardData.IsEnPassantCapture(
                    capturingSourcePosition,
                    checkAttackPosition,
                    addMoveData.EnPassantCaptureInfo))
                {
                    moveFlags |= GameMoveFlags.IsEnPassantCapture;
                    moveFlags &= ~GameMoveFlags.IsCapture;
                }

                AddMove(addMoveData, capturingSourcePosition, checkAttackPosition, moveFlags);
            }

            var enPassantCaptureInfo = addMoveData.EnPassantCaptureInfo;
            if (enPassantCaptureInfo != null && enPassantCaptureInfo.TargetPiecePosition == checkAttackPosition)
            {
                GeneratePawnMoves(
                    addMoveData,
                    GeneratedMoveTypes.Capture,
                    enPassantCaptureInfo.CapturePosition.Bitboard);
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

            GeneratePawnMoves(addMoveData, GeneratedMoveTypes.Quiet, positionBridge);

            var moves = addMoveData.ActivePiecesExceptKingAndPawnsBitboard
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
                                        pinLimitations,
                                        sourcePosition,
                                        targetPosition))
                        .Select(targetPosition => new GameMove(sourcePosition, targetPosition)))
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
                addMoveData.ActiveColor,
                generatedMoveTypes,
                enPassantCaptureInfo?.CapturePosition.Bitboard ?? Bitboard.None,
                target);

            // ReSharper disable once ForCanBeConvertedToForeach - For optimization
            for (var index = 0; index < _potentialMoveDatas.Count; index++)
            {
                var gameMoveData = _potentialMoveDatas[index];

                var potentialPawnMove = gameMoveData.Move;

                var sourcePosition = potentialPawnMove.From;
                var destinationPosition = potentialPawnMove.To;
                if (!IsValidMoveByPinning(pinLimitations, sourcePosition, destinationPosition))
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

                addMoveData.ValidMoves.Add(gameMoveData.Move, gameMoveData.MoveInfo);
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

            var validMoves = gameBoard.ValidMoves;
            var moves = validMoves.Keys;

            if (depth == 1)
            {
                ulong captureCount = 0;
                ulong enPassantCaptureCount = 0;
                foreach (var validMove in validMoves)
                {
                    if (validMove.Value.IsAnyCapture)
                    {
                        captureCount++;
                    }

                    if (validMove.Value.IsEnPassantCapture)
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
                        gameBoard._activeColor,
                        gameBoard._enPassantCaptureInfo,
                        ref castlingOptions);

                    var isInCheck = gameBoardDataCopy.IsInCheck(gameBoard._activeColor.Invert());
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

        private void Validate(bool forceValidation)
        {
            if (!forceValidation && !_validateAfterMove)
            {
                return;
            }

            _gameBoardData.EnsureConsistency();

            foreach (var king in ChessConstants.BothKings)
            {
                var count = _gameBoardData.GetBitboard(king).GetBitSetCount();
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

        private void ValidateValidMoves()
        {
            if (!_validateAfterMove)
            {
                return;
            }

            foreach (var pair in this.ValidMoves)
            {
                var move = pair.Key;
                var expectedIsPawnPromotion = _gameBoardData.IsPawnPromotion(move.From, move.To);
                if (pair.Value.IsPawnPromotion != expectedIsPawnPromotion)
                {
                    throw new ChessPlatformException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The move '{0}' is inconsistent with respect to its pawn promotion state.",
                            move));
                }

                var expectedIsCapture = _gameBoardData[move.To] != Piece.None;
                if (pair.Value.IsCapture != expectedIsCapture)
                {
                    throw new ChessPlatformException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The move '{0}' is inconsistent with respect to its capture state.",
                            move));
                }

                var expectedIsEnPassantCapture = _gameBoardData.IsEnPassantCapture(
                    move.From,
                    move.To,
                    _enPassantCaptureInfo);
                if (pair.Value.IsEnPassantCapture != expectedIsEnPassantCapture)
                {
                    throw new ChessPlatformException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The move '{0}' is inconsistent with respect to its en passant capture state.",
                            move));
                }

                var expectedIsKingCastling = _gameBoardData.CheckCastlingMove(move) != null;
                if (pair.Value.IsKingCastling != expectedIsKingCastling)
                {
                    throw new ChessPlatformException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The move '{0}' is inconsistent with respect to its king castling state.",
                            move));
                }
            }
        }

        private void InitializeValidMovesAndState(
            out Dictionary<GameMove, GameMoveInfo> validMoves,
            out GameState state)
        {
            var activeKing = PieceType.King.ToPiece(_activeColor);
            var activeKingPosition = _gameBoardData.GetBitboard(activeKing).GetFirstPosition();
            var oppositeColor = _activeColor.Invert();

            var checkAttackPositionsBitboard = _gameBoardData.GetAttackers(activeKingPosition, oppositeColor);
            var isInCheck = checkAttackPositionsBitboard.IsAny;
            var isInDoubleCheck = isInCheck && !checkAttackPositionsBitboard.IsExactlyOneBitSet();

            var pinLimitations = _gameBoardData.GetPinLimitations(activeKingPosition.SquareIndex, oppositeColor);

            var activePiecesExceptKingAndPawnsBitboard = _gameBoardData.GetBitboard(_activeColor)
                & ~_gameBoardData.GetBitboard(activeKing)
                & ~_gameBoardData.GetBitboard(PieceType.Pawn.ToPiece(_activeColor));

            validMoves = new Dictionary<GameMove, GameMoveInfo>(ValidMoveCapacity);

            var addMoveData = new AddMoveData(
                _gameBoardData,
                validMoves,
                _enPassantCaptureInfo,
                _activeColor,
                _castlingOptions,
                pinLimitations,
                activePiecesExceptKingAndPawnsBitboard);

            GenerateKingMoves(addMoveData, activeKingPosition, isInCheck);

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

            ValidateValidMoves();
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
                throw new ArgumentException(InvalidFenMessage, nameof(fen));
            }

            var pieceDataSnippet = fenSnippets[0];
            _gameBoardData.SetupByFenSnippet(pieceDataSnippet);

            var activeColorSnippet = fenSnippets[1];
            if (!ChessConstants.FenSnippetToColorMap.TryGetValue(activeColorSnippet, out activeColor))
            {
                throw new ArgumentException(InvalidFenMessage, nameof(fen));
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
                        throw new ArgumentException(InvalidFenMessage, nameof(fen));
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
                    throw new ArgumentException(InvalidFenMessage, nameof(fen));
                }

                var enPassantInfo =
                    ChessConstants.ColorToEnPassantInfoMap.Values.SingleOrDefault(
                        obj => obj.CaptureTargetRank == capturePosition.Value.Rank);

                if (enPassantInfo == null)
                {
                    throw new ArgumentException(InvalidFenMessage, nameof(fen));
                }

                enPassantCaptureTarget = new EnPassantCaptureInfo(
                    capturePosition.Value,
                    new Position(false, capturePosition.Value.File, enPassantInfo.EndRank));
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

            #endregion

            #region Operators

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

            #endregion

            #region Public Methods

            public void Include(PerftData other)
            {
                #region Argument Check

                if (other == null)
                {
                    throw new ArgumentNullException(nameof(other));
                }

                #endregion

                this.CaptureCount += other.CaptureCount;
                this.CheckCount += other.CheckCount;
                this.CheckmateCount += other.CheckmateCount;
                this.EnPassantCaptureCount += other.EnPassantCaptureCount;
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
                [NotNull] Bitboard[] pinLimitations,
                Bitboard activePiecesExceptKingAndPawnsBitboard)
            {
                this.GameBoardData = gameBoardData.EnsureNotNull();
                this.ValidMoves = validMovesReference.EnsureNotNull();
                this.EnPassantCaptureInfo = enPassantCaptureInfo;
                this.ActiveColor = activeColor;
                this.OppositeColor = activeColor.Invert();
                this.CastlingOptions = castlingOptions;
                this.PinLimitations = pinLimitations.EnsureNotNull();
                this.ActivePiecesExceptKingAndPawnsBitboard = activePiecesExceptKingAndPawnsBitboard;
            }

            #endregion

            #region Public Properties

            [NotNull]
            public GameBoardData GameBoardData
            {
                get;
            }

            [NotNull]
            public IDictionary<GameMove, GameMoveInfo> ValidMoves
            {
                get;
            }

            [CanBeNull]
            public EnPassantCaptureInfo EnPassantCaptureInfo
            {
                get;
            }

            public PieceColor ActiveColor
            {
                get;
            }

            public PieceColor OppositeColor
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

            #endregion
        }

        #endregion
    }
}