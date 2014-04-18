using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
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

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="BoardState"/> class.
        /// </summary>
        internal BoardState()
        {
            _pieceData = new PieceData();
            _fullMoveIndex = 1;
            SetupDefault(out _activeColor, out _castlingOptions, out _enPassantCaptureInfo);
            PostInitialize(out _validMoves, out _state);
            Validate();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BoardState"/> class.
        /// </summary>
        internal BoardState([NotNull] string fen)
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
            SetupByFen(fen, out _activeColor, out _castlingOptions, out _enPassantCaptureInfo);
            PostInitialize(out _validMoves, out _state);
            Validate();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BoardState"/> class.
        /// </summary>
        private BoardState(
            [NotNull] BoardState previousState,
            [NotNull] PieceMove move,
            [CanBeNull] PieceType? promotedPieceType)
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
                _enPassantCaptureInfo,
                promotedPieceType,
                ref _castlingOptions);

            _halfMovesBy50MoveRule = makeMoveData.ShouldKeepCountingBy50MoveRule
                ? previousState._halfMovesBy50MoveRule + 1
                : 0;

            PostInitialize(out _validMoves, out _state);
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

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return GetFen();
        }

        public string GetFen()
        {
            var resultBuilder = new StringBuilder((ChessConstants.FileCount + 1) * ChessConstants.RankCount + 20);

            _pieceData.GetFenSnippet(resultBuilder);

            //// TODO [vmcl] Consider actual: (*) half move clock; (*) full move number
            resultBuilder.AppendFormat(
                CultureInfo.InvariantCulture,
                " {0} {1} {2} {3} {4}",
                _activeColor.GetFenSnippet(),
                _castlingOptions.GetFenSnippet(),
                _enPassantCaptureInfo == null ? "-" : _enPassantCaptureInfo.CapturePosition.ToString(),
                _halfMovesBy50MoveRule,
                _fullMoveIndex);

            return resultBuilder.ToString();
        }

        public Piece GetPiece(Position position)
        {
            return _pieceData.GetPiece(position);
        }

        public Position[] GetPiecePositions(Piece piece)
        {
            return _pieceData.GetPiecePositions(piece);
        }

        public BoardState MakeMove([NotNull] PieceMove move, [CanBeNull] PieceType? promotedPieceType)
        {
            return new BoardState(this, move, promotedPieceType);
        }

        public Position[] GetAttacks(Position targetPosition, PieceColor attackingColor)
        {
            return _pieceData.GetAttacks(targetPosition, attackingColor);
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
                            "The number of the '{0}' piece is {1}. Must be exactly one.",
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
            var isInCheck = _pieceData.IsInCheck(_activeColor);
            var oppositeColor = _activeColor.Invert();

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
                    var move = new PieceMove(sourcePosition, destinationPosition);

                    var castlingMove = _pieceData.CheckCastlingMove(move);
                    if (castlingMove != null)
                    {
                        if (isInCheck || _pieceData.IsUnderAttack(castlingMove.PassedPosition, oppositeColor))
                        {
                            continue;
                        }
                    }

                    var castlingOptions = CastlingOptions.All;
                    pieceDataCopy.MakeMove(
                        move,
                        _activeColor,
                        _enPassantCaptureInfo,
                        PieceType.Queen,
                        ref castlingOptions);

                    if (!pieceDataCopy.IsInCheck(_activeColor))
                    {
                        validMoveSet.Add(move);
                    }

                    pieceDataCopy.UndoMove();
                }
            }

            validMoves = validMoveSet.AsReadOnly();

            state = validMoves.Count == 0
                ? (isInCheck ? GameState.Checkmate : GameState.Stalemate)
                : (isInCheck ? GameState.Check : GameState.Default);
        }

        private void SetupDefault(
            out PieceColor activeColor,
            out CastlingOptions castlingOptions,
            out EnPassantCaptureInfo enPassantTarget)
        {
            _pieceData.SetupNewPiece(PieceType.Rook, PieceColor.White, "a1");
            _pieceData.SetupNewPiece(PieceType.Knight, PieceColor.White, "b1");
            _pieceData.SetupNewPiece(PieceType.Bishop, PieceColor.White, "c1");
            _pieceData.SetupNewPiece(PieceType.Queen, PieceColor.White, "d1");
            _pieceData.SetupNewPiece(PieceType.King, PieceColor.White, "e1");
            _pieceData.SetupNewPiece(PieceType.Bishop, PieceColor.White, "f1");
            _pieceData.SetupNewPiece(PieceType.Knight, PieceColor.White, "g1");
            _pieceData.SetupNewPiece(PieceType.Rook, PieceColor.White, "h1");
            Position.GenerateRank(1)
                .DoForEach(position => _pieceData.SetupNewPiece(PieceType.Pawn, PieceColor.White, position));

            Position.GenerateRank(6)
                .DoForEach(position => _pieceData.SetupNewPiece(PieceType.Pawn, PieceColor.Black, position));
            _pieceData.SetupNewPiece(PieceType.Rook, PieceColor.Black, "a8");
            _pieceData.SetupNewPiece(PieceType.Knight, PieceColor.Black, "b8");
            _pieceData.SetupNewPiece(PieceType.Bishop, PieceColor.Black, "c8");
            _pieceData.SetupNewPiece(PieceType.Queen, PieceColor.Black, "d8");
            _pieceData.SetupNewPiece(PieceType.King, PieceColor.Black, "e8");
            _pieceData.SetupNewPiece(PieceType.Bishop, PieceColor.Black, "f8");
            _pieceData.SetupNewPiece(PieceType.Knight, PieceColor.Black, "g8");
            _pieceData.SetupNewPiece(PieceType.Rook, PieceColor.Black, "h8");

            activeColor = PieceColor.White;
            castlingOptions = CastlingOptions.All;
            enPassantTarget = null;
        }

        private void SetupByFen(
            string inputFen,
            out PieceColor activeColor,
            out CastlingOptions castlingOptions,
            out EnPassantCaptureInfo enPassantCaptureTarget)
        {
            var fen = inputFen.Trim();
            Trace.TraceInformation("[{0}] '{1}'", MethodBase.GetCurrentMethod().GetQualifiedName(), fen);

            //// TODO [vmcl] Parse FEN
            throw new NotImplementedException();
        }

        #endregion
    }
}