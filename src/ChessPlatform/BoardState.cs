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
        private readonly EnPassantCaptureInfo _enPassantCaptureTarget;
        private readonly ReadOnlySet<PieceMove> _validMoves;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="BoardState"/> class.
        /// </summary>
        internal BoardState()
        {
            _pieceData = new PieceData();
            SetupDefault(out _activeColor, out _castlingOptions, out _enPassantCaptureTarget);
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
            SetupByFen(fen, out _activeColor, out _castlingOptions, out _enPassantCaptureTarget);
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

            #endregion

            _pieceData = previousState._pieceData.Copy();
            _activeColor = previousState._activeColor.Invert();
            _castlingOptions = previousState.CastlingOptions;

            _enPassantCaptureTarget = _pieceData.GetEnPassantCaptureTarget(move);

            var castlingInfo = _pieceData.CheckCastlingMove(move);
            if (castlingInfo != null)
            {
                if (!_castlingOptions.IsAllSet(castlingInfo.Option))
                {
                    throw new ChessPlatformException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The castling {{{0}}} ({1}) is not allowed.",
                            move,
                            castlingInfo.Option.GetName()));
                }
            }

            var movingPiece = _pieceData.SetPiece(move.From, Piece.None);

            movingPiece.EnsureDefined();

            var color = movingPiece.GetColor();
            if (movingPiece == Piece.None || !color.HasValue)
            {
                throw new ChessPlatformException("The move starting position contains no piece.");
            }

            var removed = _pieceData.PieceOffsetMap.GetValueOrCreate(movingPiece).Remove(move.From.X88Value);
            if (!removed)
            {
                throw ChessPlatformException.CreateInconsistentStateError();
            }

            var pieceColor = color.Value;

            if (pieceColor != previousState._activeColor)
            {
                throw new ChessPlatformException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The move starting position specifies piece of invalid color (expected: {0}).",
                        previousState._activeColor));
            }

            var pieceType = movingPiece.GetPieceType();
            if (pieceType == PieceType.Pawn
                && (pieceColor == PieceColor.White && move.To.Rank == ChessConstants.WhitePawnPromotionRank
                    || pieceColor == PieceColor.Black && move.To.Rank == ChessConstants.BlackPawnPromotionRank))
            {
                if (!promotedPieceType.HasValue || !ChessConstants.ValidPromotions.Contains(promotedPieceType.Value))
                {
                    throw new ChessPlatformException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The promoted piece type is not specified or is invalid for the promoted piece {0}.",
                            movingPiece.GetDescription()));
                }

                movingPiece = promotedPieceType.Value.ToPiece(pieceColor);
            }

            var capturedPiece = _pieceData.SetPiece(move.To, movingPiece);
            if (capturedPiece != Piece.None)
            {
                if (capturedPiece.GetColor() == pieceColor)
                {
                    throw new ChessPlatformException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The move destination position performs a capture of the same color ({0}).",
                            pieceColor));
                }

                var capturedRemoved =
                    _pieceData.PieceOffsetMap.GetValueOrCreate(capturedPiece).Remove(move.To.X88Value);
                if (!capturedRemoved)
                {
                    throw ChessPlatformException.CreateInconsistentStateError();
                }
            }

            var added = _pieceData.PieceOffsetMap.GetValueOrCreate(movingPiece).Add(move.To.X88Value);
            if (!added)
            {
                throw ChessPlatformException.CreateInconsistentStateError();
            }

            if (castlingInfo != null)
            {
                _castlingOptions &= ~castlingInfo.Option;
                //// TODO [vmcl] Move Rook
                throw new NotImplementedException();
            }

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

        public EnPassantCaptureInfo EnPassantCaptureTarget
        {
            [DebuggerStepThrough]
            get
            {
                return _enPassantCaptureTarget;
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
                " {0} {1} {2} 0 1",
                _activeColor.GetFenSnippet(),
                _castlingOptions.GetFenSnippet(),
                _enPassantCaptureTarget.ToStringSafely("-"));

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
            foreach (var king in ChessConstants.BothKings)
            {
                var count = _pieceData.PieceOffsetMap.GetValueOrCreate(king).Count;
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
                var pairs = _pieceData.PieceOffsetMap
                    .Where(pair => pair.Key.GetColor() == color)
                    .Select(pair => KeyValuePair.Create(pair.Key.GetPieceType(), pair.Value.Count))
                    .ToArray();

                var counts = pairs
                    .Aggregate(
                        new { AllCount = 0, PawnCount = 0 },
                        (accumulator, pair) => new
                        {
                            AllCount = accumulator.AllCount + pair.Value,
                            PawnCount = accumulator.PawnCount + (pair.Key == PieceType.Pawn ? pair.Value : 0)
                        });

                if (counts.PawnCount > ChessConstants.MaxPawnCountPerColor)
                {
                    throw new ChessPlatformException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Too many '{0}' ({1}).",
                            PieceType.Pawn.ToPiece(color).GetDescription(),
                            counts.PawnCount));
                }

                if (counts.AllCount > ChessConstants.MaxPieceCountPerColor)
                {
                    throw new ChessPlatformException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Too many pieces of the color {0} ({1}).",
                            color.GetName(),
                            counts.PawnCount));
                }
            }

            //// TODO [vmcl] (*) No non-promoted pawns at their last rank, (*) etc.
        }

        private void PostInitialize(out ReadOnlySet<PieceMove> validMoves, out GameState state)
        {
            var isInCheck = _pieceData.IsInCheck(_activeColor);
            var oppositeColor = _activeColor.Invert();

            var activePieceOffsets = _pieceData.PieceOffsetMap
                .Where(pair => pair.Key.GetColor() == _activeColor && pair.Value.Count != 0)
                .SelectMany(pair => pair.Value)
                .ToArray();

            var validMoveSet = new HashSet<PieceMove>();
            var moveHelper = new MoveHelper(_pieceData);
            foreach (var offset in activePieceOffsets)
            {
                var sourcePosition = new Position(offset);

                var potentialMovePositions = ChessHelper.GetPotentialMovePositions(
                    _pieceData.Pieces,
                    _castlingOptions,
                    _enPassantCaptureTarget,
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

                    moveHelper.MakeMove(move);

                    if (!moveHelper.PieceData.IsInCheck(_activeColor))
                    {
                        validMoveSet.Add(move);
                    }

                    moveHelper.UndoMove();
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