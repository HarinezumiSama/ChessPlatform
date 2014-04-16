using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public sealed class BoardState
    {
        #region Constants and Fields

        private readonly Piece[] _pieces;
        private readonly Dictionary<Piece, HashSet<byte>> _pieceOffsetMap;

        private readonly PieceColor _activeColor;
        private readonly GameState _state;
        private readonly CastlingOptions _castlingOptions;
        private readonly Position? _enPassantCaptureTarget;
        private readonly ReadOnlySet<PieceMove> _validMoves;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="BoardState"/> class.
        /// </summary>
        internal BoardState()
        {
            CreatePieces(out _pieces, out _pieceOffsetMap);
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

            CreatePieces(out _pieces, out _pieceOffsetMap);

            //// TODO [vmcl] Initialize board with the specified FEN

            //// _activeColor = ...
            //// _castlingOptions = ...
            ////_enPassantCaptureTarget = ...

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

            _pieces = previousState._pieces.Copy();
            _pieceOffsetMap = ChessHelper.CopyPieceOffsetMap(previousState._pieceOffsetMap);
            _activeColor = previousState._activeColor.Invert();
            _castlingOptions = previousState.CastlingOptions;

            _enPassantCaptureTarget = ChessHelper.GetEnPassantCaptureTarget(_pieces, move);

            var movingPiece = SetPiece(move.From, Piece.None);

            movingPiece.EnsureDefined();

            var color = movingPiece.GetColor();
            if (movingPiece == Piece.None || !color.HasValue)
            {
                throw new ChessPlatformException("The move starting position contains no piece.");
            }

            var removed = _pieceOffsetMap.GetValueOrCreate(movingPiece).Remove(move.From.X88Value);
            if (!removed)
            {
                throw new ChessPlatformException("Inconsistent state of the piece offset map.");
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
                            "The promoted piece type is not specified or is invalid for promoted {0}.",
                            movingPiece.GetDescription()));
                }

                movingPiece = promotedPieceType.Value.ToPiece(pieceColor);
            }

            var capturedPiece = SetPiece(move.To, movingPiece);
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

                var capturedRemoved = _pieceOffsetMap.GetValueOrCreate(capturedPiece).Remove(move.To.X88Value);
                if (!capturedRemoved)
                {
                    throw new ChessPlatformException("Inconsistent state of the piece offset map.");
                }
            }

            var added = _pieceOffsetMap.GetValueOrCreate(movingPiece).Add(move.To.X88Value);
            if (!added)
            {
                throw new ChessPlatformException("Inconsistent state of the piece offset map.");
            }

            //// TODO [vmcl] Consider castling move!

            if (CastlingOptions != CastlingOptions.None)
            {
                //// TODO [vmcl] Adjust castling options
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

        public Position? EnPassantCaptureTarget
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

            ChessHelper.GetFenSnippet(_pieces, resultBuilder);

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
            return ChessHelper.GetPiece(_pieces, position);
        }

        public Position[] GetPiecePositions(Piece piece)
        {
            #region Argument Check

            piece.EnsureDefined();

            if (piece == Piece.None || piece.GetPieceType() == PieceType.None)
            {
                throw new ArgumentException("Invalid piece.", "piece");
            }

            #endregion

            var offsets = _pieceOffsetMap.GetValueOrDefault(piece);
            if (offsets == null)
            {
                return new Position[0];
            }

            var result = offsets.Select(item => new Position(item)).ToArray();
            return result;
        }

        public BoardState MakeMove([NotNull] PieceMove move, [CanBeNull] PieceType? promotedPieceType)
        {
            return new BoardState(this, move, promotedPieceType);
        }

        public Position[] GetAttacks(Position targetPosition, PieceColor attackingColor)
        {
            return ChessHelper.GetAttacks(_pieces, targetPosition, attackingColor);
        }

        #endregion

        #region Private Methods

        private static void CreatePieces(out Piece[] pieces, out Dictionary<Piece, HashSet<byte>> pieceOffsetMap)
        {
            Trace.Assert(ChessConstants.X88Length == 128, "Invalid 0x88 length.");

            pieces = new Piece[ChessConstants.X88Length];
            pieceOffsetMap = new Dictionary<Piece, HashSet<byte>>();
        }

        private void Validate()
        {
            foreach (var king in ChessConstants.BothKings)
            {
                var count = _pieceOffsetMap.GetValueOrCreate(king).Count;
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

            if (ChessHelper.IsInCheck(_pieces, _pieceOffsetMap, _activeColor.Invert()))
            {
                throw new ChessPlatformException("Inactive king is under check.");
            }

            foreach (var pieceColor in ChessConstants.PieceColors)
            {
                var color = pieceColor;
                var pairs = _pieceOffsetMap
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
            var isInCheck = ChessHelper.IsInCheck(_pieces, _pieceOffsetMap, _activeColor);
            var oppositeColor = _activeColor.Invert();

            var activePieceOffsets = _pieceOffsetMap
                .Where(pair => pair.Key.GetColor() == _activeColor && pair.Value.Count != 0)
                .SelectMany(pair => pair.Value)
                .ToArray();

            var validMoveSet = new HashSet<PieceMove>();
            var moveHelper = new MoveHelper(_pieces, _pieceOffsetMap);
            foreach (var offset in activePieceOffsets)
            {
                var sourcePosition = new Position(offset);

                var potentialMovePositions = ChessHelper.GetPotentialMovePositions(
                    _pieces,
                    _castlingOptions,
                    _enPassantCaptureTarget,
                    sourcePosition);

                foreach (var destinationPosition in potentialMovePositions)
                {
                    var move = new PieceMove(sourcePosition, destinationPosition);

                    var castlingMove = ChessHelper.CheckCastlingMove(_pieces, move);
                    if (castlingMove != null)
                    {
                        if (isInCheck
                            || ChessHelper.IsUnderAttack(_pieces, castlingMove.PassedPosition, oppositeColor))
                        {
                            continue;
                        }
                    }

                    moveHelper.MakeMove(move);

                    if (!ChessHelper.IsInCheck(moveHelper.Pieces, moveHelper.PieceOffsetMap, _activeColor))
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
            out Position? enPassantTarget)
        {
            SetupNewPiece(PieceType.Rook, PieceColor.White, "a1");
            SetupNewPiece(PieceType.Knight, PieceColor.White, "b1");
            SetupNewPiece(PieceType.Bishop, PieceColor.White, "c1");
            SetupNewPiece(PieceType.Queen, PieceColor.White, "d1");
            SetupNewPiece(PieceType.King, PieceColor.White, "e1");
            SetupNewPiece(PieceType.Bishop, PieceColor.White, "f1");
            SetupNewPiece(PieceType.Knight, PieceColor.White, "g1");
            SetupNewPiece(PieceType.Rook, PieceColor.White, "h1");
            Position.GenerateRank(1).DoForEach(position => SetupNewPiece(PieceType.Pawn, PieceColor.White, position));

            Position.GenerateRank(6).DoForEach(position => SetupNewPiece(PieceType.Pawn, PieceColor.Black, position));
            SetupNewPiece(PieceType.Rook, PieceColor.Black, "a8");
            SetupNewPiece(PieceType.Knight, PieceColor.Black, "b8");
            SetupNewPiece(PieceType.Bishop, PieceColor.Black, "c8");
            SetupNewPiece(PieceType.Queen, PieceColor.Black, "d8");
            SetupNewPiece(PieceType.King, PieceColor.Black, "e8");
            SetupNewPiece(PieceType.Bishop, PieceColor.Black, "f8");
            SetupNewPiece(PieceType.Knight, PieceColor.Black, "g8");
            SetupNewPiece(PieceType.Rook, PieceColor.Black, "h8");

            activeColor = PieceColor.White;
            castlingOptions = CastlingOptions.All;
            enPassantTarget = null;
        }

        private void SetupNewPiece(PieceType pieceType, PieceColor color, Position position)
        {
            var offset = position.X88Value;
            var existingPiece = _pieces[offset];
            if (existingPiece != Piece.None)
            {
                throw new ChessPlatformException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The board square '{0}' is already occupied by '{1}'.",
                        position,
                        existingPiece));
            }

            var piece = pieceType.ToPiece(color);
            _pieces[offset] = piece;

            var added = _pieceOffsetMap.GetValueOrCreate(piece).Add(offset);
            if (!added)
            {
                throw new ChessPlatformException("Inconsistent state of the piece offset map.");
            }
        }

        private Piece SetPiece(Position position, Piece piece)
        {
            return ChessHelper.SetPiece(_pieces, position, piece);
        }

        #endregion
    }
}