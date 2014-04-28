using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public sealed class BoardState
    {
        #region Constants and Fields

        private readonly PieceData _pieceData;

        private readonly PieceColor _activeColor;
        private readonly GameState _state;
        private readonly CastlingOptions _castlingOptions;
        private readonly EnPassantCaptureInfo _enPassantCaptureInfo;
        private readonly ReadOnlySet<PieceMove> _validMoves;
        private readonly int _halfMovesBy50MoveRule;
        private readonly int _fullMoveIndex;
        private readonly PieceMove _previousMove;
        private readonly string _resultString;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="BoardState"/> class.
        /// </summary>
        public BoardState()
        {
            _pieceData = new PieceData();

            SetupDefault(
                out _activeColor,
                out _castlingOptions,
                out _enPassantCaptureInfo,
                out _halfMovesBy50MoveRule,
                out _fullMoveIndex);

            PostInitialize(out _validMoves, out _state);
            InitializeResultString(out _resultString);
            Validate();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BoardState"/> class.
        /// </summary>
        public BoardState([NotNull] string fen)
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

            SetupByFen(
                fen,
                out _activeColor,
                out _castlingOptions,
                out _enPassantCaptureInfo,
                out _halfMovesBy50MoveRule,
                out _fullMoveIndex);

            PostInitialize(out _validMoves, out _state);
            InitializeResultString(out _resultString);
            Validate();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BoardState"/> class.
        /// </summary>
        private BoardState([NotNull] BoardState previousState, [NotNull] PieceMove move)
        {
            #region Argument Check

            if (previousState == null)
            {
                throw new ArgumentNullException("previousState");
            }

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            if (!previousState._validMoves.Contains(move))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "The move '{0}' is not valid.", move),
                    "move");
            }

            #endregion

            _pieceData = previousState._pieceData.Copy();
            _activeColor = previousState._activeColor.Invert();
            _castlingOptions = previousState.CastlingOptions;

            _fullMoveIndex = previousState._fullMoveIndex + (_activeColor == PieceColor.White ? 1 : 0);
            _enPassantCaptureInfo = _pieceData.GetEnPassantCaptureInfo(move);

            var makeMoveData = _pieceData.MakeMove(
                move,
                previousState._activeColor,
                previousState._enPassantCaptureInfo,
                ref _castlingOptions);

            _previousMove = makeMoveData.Move;

            _halfMovesBy50MoveRule = makeMoveData.ShouldKeepCountingBy50MoveRule
                ? previousState._halfMovesBy50MoveRule + 1
                : 0;

            PostInitialize(out _validMoves, out _state);
            InitializeResultString(out _resultString);
            Validate();
        }

        #endregion

        #region Public Properties

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

        public ReadOnlySet<PieceMove> ValidMoves
        {
            [DebuggerStepThrough]
            get
            {
                return _validMoves;
            }
        }

        public int HalfMovesBy50MoveRule
        {
            [DebuggerStepThrough]
            get
            {
                return _halfMovesBy50MoveRule;
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

        public string ResultString
        {
            [DebuggerStepThrough]
            get
            {
                return _resultString;
            }
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return GetFen();
        }

        [CLSCompliant(false)]
        public PerftResult Perft(int depth)
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

            var stopwatch = Stopwatch.StartNew();
            PerftInternal(this, depth, perftData);
            stopwatch.Stop();

            return new PerftResult(depth, stopwatch.Elapsed, perftData.NodeCount);
        }

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
                _halfMovesBy50MoveRule.ToString(CultureInfo.InvariantCulture),
                _fullMoveIndex.ToString(CultureInfo.InvariantCulture));

            return result;
        }

        public Piece GetPiece(Position position)
        {
            return _pieceData.GetPiece(position);
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

            return this.ValidMoves.Contains(move);
        }

        public bool IsPawnPromotion(PieceMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            return this.ValidMoves.Contains(move) && _pieceData.IsPawnPromotion(move);
        }

        public CastlingInfo CheckCastlingMove(PieceMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            return this.ValidMoves.Contains(move) ? _pieceData.CheckCastlingMove(move) : null;
        }

        public BoardState MakeMove([NotNull] PieceMove move)
        {
            return new BoardState(this, move);
        }

        public Position[] GetAttacks(Position targetPosition, PieceColor attackingColor)
        {
            return _pieceData.GetAttacks(targetPosition, attackingColor);
        }

        public PieceMove[] GetValidMovesBySource(Position sourcePosition)
        {
            return this.ValidMoves.Where(move => move.From == sourcePosition).ToArray();
        }

        public PieceMove[] GetValidMovesByDestination(Position destinationPosition)
        {
            return this.ValidMoves.Where(move => move.To == destinationPosition).ToArray();
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

            //// TODO [vmcl] (*) No non-promoted pawns at their last rank, (*) etc.
        }

        private void PostInitialize(out ReadOnlySet<PieceMove> validMoves, out GameState state)
        {
            //// TODO [vmcl] Implement DoubleCheck verification
            var isInCheck = _pieceData.IsInCheck(_activeColor);
            var oppositeColor = _activeColor.Invert();

            var isInsufficientMaterialState = _pieceData.IsInsufficientMaterialState();
            if (isInsufficientMaterialState)
            {
                state = GameState.ForcedDrawInsufficientMaterial;
                validMoves = new HashSet<PieceMove>().AsReadOnly();
                return;
            }

            var activePiecePositions = ChessConstants
                .PieceTypes
                .Where(item => item != PieceType.None)
                .SelectMany(item => _pieceData.GetPiecePositions(item.ToPiece(_activeColor)))
                .ToArray();

            var validMoveSet = new HashSet<PieceMove>();
            var pieceDataCopy = _pieceData.Copy();
            foreach (var sourcePosition in activePiecePositions)
            {
                var potentialMovePositions = _pieceData.GetPotentialMovePositions(
                    _castlingOptions,
                    _enPassantCaptureInfo,
                    sourcePosition);

                foreach (var destinationPosition in potentialMovePositions)
                {
                    var isPawnPromotion = _pieceData.IsPawnPromotion(sourcePosition, destinationPosition);

                    var promotionResult = isPawnPromotion ? ChessHelper.DefaultPromotion : PieceType.None;
                    var basicMove = new PieceMove(sourcePosition, destinationPosition, promotionResult);

                    var castlingMove = _pieceData.CheckCastlingMove(basicMove);
                    if (castlingMove != null)
                    {
                        if (isInCheck || _pieceData.IsUnderAttack(castlingMove.PassedPosition, oppositeColor))
                        {
                            continue;
                        }
                    }

                    var castlingOptionsCopy = _castlingOptions;
                    pieceDataCopy.MakeMove(
                        basicMove,
                        _activeColor,
                        _enPassantCaptureInfo,
                        ref castlingOptionsCopy);

                    if (!pieceDataCopy.IsInCheck(_activeColor))
                    {
                        validMoveSet.Add(basicMove);

                        if (isPawnPromotion)
                        {
                            ChessHelper.NonDefaultPromotions.DoForEach(
                                item => validMoveSet.Add(basicMove.MakePromotion(item)));
                        }
                    }

                    pieceDataCopy.UndoMove();
                }
            }

            validMoves = validMoveSet.AsReadOnly();

            state = validMoves.Count == 0
                ? (isInCheck ? GameState.Checkmate : GameState.Stalemate)
                : (isInCheck ? GameState.Check : GameState.Default);
        }

        private void InitializeResultString(out string resultString)
        {
            switch (_state)
            {
                case GameState.Default:
                case GameState.Check:
                    resultString = ResultStrings.Other;
                    break;

                case GameState.Checkmate:
                    resultString = _activeColor == PieceColor.White ? ResultStrings.BlackWon : ResultStrings.WhiteWon;
                    break;

                case GameState.ForcedDrawInsufficientMaterial:
                case GameState.Stalemate:
                    resultString = ResultStrings.Draw;
                    break;

                default:
                    throw _state.CreateEnumValueNotSupportedException();
            }
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
                    new Position(capturePosition.Value.File, enPassantInfo.StartRank));
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

        private static void PerftInternal(BoardState boardState, int depth, PerftData perftData)
        {
            if (depth == 0)
            {
                perftData.NodeCount++;
                return;
            }

            var moves = boardState.ValidMoves;
            if (depth == 1)
            {
                perftData.NodeCount += checked((ulong)moves.Count);
                return;
            }

            foreach (var move in moves)
            {
                var newBoardState = boardState.MakeMove(move);
                PerftInternal(newBoardState, depth - 1, perftData);
            }
        }

        #endregion

        #region PerftData Class

        private sealed class PerftData
        {
            #region Public Properties

            public ulong NodeCount
            {
                get;
                set;
            }

            #endregion
        }

        #endregion
    }
}