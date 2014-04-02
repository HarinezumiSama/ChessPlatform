using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using ChessPlatform.Pieces;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public sealed class Board
    {
        #region Constants and Fields

        private readonly Piece[] _pieces;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Board"/> class.
        /// </summary>
        public Board()
        {
            Trace.Assert(ChessConstants.X88Length == 128, "Invalid 0x88 length.");

            _pieces = new Piece[ChessConstants.X88Length];
            this.ActiveColor = PieceColor.White;
        }

        #endregion

        #region Public Properties

        public PieceColor ActiveColor
        {
            get;
            private set;
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

            var emptySquareCount = 0;
            Action writeEmptyCount = () =>
            {
                if (emptySquareCount > 0)
                {
                    resultBuilder.Append(emptySquareCount);
                    emptySquareCount = 0;
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
                    var piece = GetPiece(new Position(file, rank));
                    if (piece == null)
                    {
                        emptySquareCount++;
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
                this.ActiveColor == PieceColor.White ? 'w' : 'b');

            return resultBuilder.ToString();
        }

        public void Setup(string fen)
        {
            throw new NotImplementedException();

            ////Validate();
        }

        public void SetupDefault()
        {
            Clear();

            SetupNewPiece<Rook>(PieceColor.White, "a1");
            SetupNewPiece<Knight>(PieceColor.White, "b1");
            SetupNewPiece<Bishop>(PieceColor.White, "c1");
            SetupNewPiece<Queen>(PieceColor.White, "d1");
            SetupNewPiece<King>(PieceColor.White, "e1");
            SetupNewPiece<Bishop>(PieceColor.White, "f1");
            SetupNewPiece<Knight>(PieceColor.White, "g1");
            SetupNewPiece<Rook>(PieceColor.White, "h1");
            Position.GenerateRank(1).DoForEach(position => SetupNewPiece<Pawn>(PieceColor.White, position));

            Position.GenerateRank(6).DoForEach(position => SetupNewPiece<Pawn>(PieceColor.Black, position));
            SetupNewPiece<Rook>(PieceColor.Black, "a8");
            SetupNewPiece<Knight>(PieceColor.Black, "b8");
            SetupNewPiece<Bishop>(PieceColor.Black, "c8");
            SetupNewPiece<Queen>(PieceColor.Black, "d8");
            SetupNewPiece<King>(PieceColor.Black, "e8");
            SetupNewPiece<Bishop>(PieceColor.Black, "f8");
            SetupNewPiece<Knight>(PieceColor.Black, "g8");
            SetupNewPiece<Rook>(PieceColor.Black, "h8");

            this.ActiveColor = PieceColor.White;

            Validate();
        }

        public void Validate()
        {
            //// TODO [vmcl] Count kings and so on
        }

        #endregion

        #region Private Methods

        private static int GetOffset([NotNull] Position position)
        {
            #region Argument Check

            if (position == null)
            {
                throw new ArgumentNullException("position");
            }

            #endregion

            return (position.Rank << 4) + position.File;
        }

        private void SetupNewPiece<TPiece>(PieceColor color, [NotNull] Position position)
            where TPiece : Piece
        {
            #region Argument Check

            if (position == null)
            {
                throw new ArgumentNullException("position");
            }

            #endregion

            var offset = GetOffset(position);
            var existingPiece = _pieces[offset];
            if (existingPiece != null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The board square '{0}' is already occupied by '{1}'.",
                        position,
                        existingPiece));
            }

            var piece = Piece.CreatePiece<TPiece>(color, position);
            _pieces[offset] = piece;
        }

        private void SetPiece([NotNull] Position position, [CanBeNull] Piece piece)
        {
            #region Argument Check

            if (position == null)
            {
                throw new ArgumentNullException("position");
            }

            #endregion

            var offset = GetOffset(position);

            var oldPiece = _pieces[offset];
            if (oldPiece != null)
            {
                oldPiece.Position = null;
            }

            _pieces[offset] = piece;

            if (piece != null)
            {
                piece.Position = position;
            }
        }

        private Piece GetPiece([NotNull] Position position)
        {
            #region Argument Check

            if (position == null)
            {
                throw new ArgumentNullException("position");
            }

            #endregion

            var offset = GetOffset(position);
            return _pieces[offset];
        }

        private void Clear()
        {
            for (var rank = 0; rank < ChessConstants.RankCount; rank++)
            {
                for (var file = 0; file < ChessConstants.FileCount; file++)
                {
                    SetPiece(new Position(file, rank), null);
                }
            }
        }

        #endregion
    }
}