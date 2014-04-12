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
        private readonly PieceColor _activeColor;
        private readonly PieceColor? _colorInCheck;
        private readonly bool _isStalemate;
        private readonly PieceColor? _checkmatingColor;
        private readonly CastlingOptions _castlingOptions;
        private readonly ReadOnlySet<PieceMove> _validMoves;

        //// TODO [vmcl] Castling options for both sides

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="BoardState"/> class.
        /// </summary>
        internal BoardState()
        {
            _pieces = CreatePieces();
            _activeColor = SetupDefault();
            _castlingOptions = CastlingOptions.All;

            Validate();

            _colorInCheck = GetColorInCheck();
            _validMoves = GetValidMoves();
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

            _pieces = CreatePieces();

            //// TODO [vmcl] Initialize according to the specified FEN
            ////_castlingOptions=...

            Validate();

            _colorInCheck = GetColorInCheck();
            _validMoves = GetValidMoves();

            throw new NotImplementedException();
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
            _activeColor = previousState._activeColor.Invert();
            _castlingOptions = previousState._castlingOptions;

            var movingPiece = SetPiece(move.From, Piece.None);

            #region Argument Check

            movingPiece.EnsureDefined();

            var color = movingPiece.GetColor();

            if (movingPiece == Piece.None || !color.HasValue)
            {
                throw new ArgumentException("The move starting position contains no piece.", "move");
            }

            var pieceColor = color.Value;

            if (pieceColor != previousState._activeColor)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The move starting position specifies piece of invalid color (expected: {0}).",
                        previousState._activeColor),
                    "move");
            }

            var pieceType = movingPiece.GetPieceType();
            if (pieceType == PieceType.Pawn
                && (pieceColor == PieceColor.White && move.To.Rank == ChessConstants.WhitePawnPromotionRank
                    || pieceColor == PieceColor.Black && move.To.Rank == ChessConstants.BlackPawnPromotionRank))
            {
                #region Argument Check

                if (!promotedPieceType.HasValue)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The promoted piece type is not specified for promoted {0}.",
                            movingPiece.GetDescription()),
                        "promotedPieceType");
                }

                #endregion

                movingPiece = promotedPieceType.Value.ToPiece(pieceColor);
            }

            #endregion

            SetPiece(move.To, movingPiece);

            //// TODO [vmcl] Adjust castling options
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

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return GetFen();
        }

        public string GetFen()
        {
            var resultBuilder = new StringBuilder((ChessConstants.FileCount + 1) * ChessConstants.RankCount + 20);

            var emptySquareCount = new ValueContainer<int>(0);
            Action writeEmptyCount =
                () =>
                {
                    if (emptySquareCount.Value > 0)
                    {
                        resultBuilder.Append(emptySquareCount.Value);
                        emptySquareCount.Value = 0;
                    }
                };

            for (var rank = ChessConstants.RankCount - 1; rank >= 0; rank--)
            {
                if (rank < ChessConstants.RankCount - 1)
                {
                    resultBuilder.Append('/');
                }

                for (var file = 0; file < ChessConstants.FileCount; file++)
                {
                    var piece = GetPiece(new Position((byte)file, (byte)rank));
                    if (piece == Piece.None)
                    {
                        emptySquareCount.Value++;
                        continue;
                    }

                    writeEmptyCount();
                    var fenChar = piece.GetFenChar();
                    resultBuilder.Append(fenChar);
                }

                writeEmptyCount();
            }

            //// TODO [vmcl] Consider actual: (a) castling availability; (b) en passant target; (c) half move clock; (d) full move number
            resultBuilder.AppendFormat(
                CultureInfo.InvariantCulture,
                " {0} - - 0 1",
                _activeColor == PieceColor.White ? 'w' : 'b');

            return resultBuilder.ToString();
        }

        #endregion

        #region Internal Methods

        internal BoardState MakeMove([NotNull] PieceMove move, [CanBeNull] PieceType? promotedPieceType)
        {
            return new BoardState(this, move, promotedPieceType);
        }

        #endregion

        #region Private Methods

        private static Piece[] CreatePieces()
        {
            Trace.Assert(ChessConstants.X88Length == 128, "Invalid 0x88 length.");

            return new Piece[ChessConstants.X88Length];
        }

        private void Validate()
        {
            //// TODO [vmcl] (1) Count kings, (2) no kings near each other, (3) no 2 kings under check, (4) no more than 16 pieces of each color, (5) etc.
        }

        private PieceColor SetupDefault()
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

            return PieceColor.White;
        }

        private void SetupNewPiece(PieceType pieceType, PieceColor color, Position position)
        {
            var offset = position.X88Value;
            var existingPiece = _pieces[offset];
            if (existingPiece != Piece.None)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The board square '{0}' is already occupied by '{1}'.",
                        position,
                        existingPiece));
            }

            var piece = pieceType.ToPiece(color);
            _pieces[offset] = piece;
        }

        private Piece SetPiece(Position position, Piece piece)
        {
            var offset = position.X88Value;
            var oldPiece = _pieces[offset];
            _pieces[offset] = piece;

            return oldPiece;
        }

        private Piece GetPiece(Position position)
        {
            var offset = position.X88Value;
            return _pieces[offset];
        }

        private PieceColor? GetColorInCheck()
        {
            throw new NotImplementedException();
        }

        private ReadOnlySet<PieceMove> GetValidMoves()
        {
            GetPotentialMoves("a1").ToString();

            throw new NotImplementedException();
        }

        private PieceMove[] GetPotentialMoves(Position position)
        {
            var piece = GetPiece(position);
            var pieceType = piece.GetPieceType();
            var color = piece.GetColor();

            Position[] positions;
            switch (pieceType)
            {
                case PieceType.King:
                    var kingOffsets = new byte[]
                    {
                        0xFF,
                        0x01,
                        0xF0,
                        0x10,
                        0x1F,
                        0xF1,
                        0x11,
                        0xEF,
                        (byte)(_castlingOptions.IsAnySet(CastlingOptions.WhiteKingSide) ? 0x02 : 0),
                        (byte)(_castlingOptions.IsAnySet(CastlingOptions.WhiteQueenSide) ? 0xFE : 0)
                    };

                    positions = position.GetValidPositions(kingOffsets);
                    break;

                default:
                    throw pieceType.CreateEnumValueNotImplementedException();
            }

            var result = positions.Select(item => new PieceMove(position, item)).ToArray();
            return result;
        }

        #endregion
    }
}