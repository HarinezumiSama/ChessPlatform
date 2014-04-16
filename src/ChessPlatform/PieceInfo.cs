using System;
using System.Globalization;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform
{
    public struct PieceInfo
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PieceInfo"/> class.
        /// </summary>
        public PieceInfo(Piece piece)
            : this()
        {
            #region Argument Check

            piece.EnsureDefined();

            #endregion

            this.Piece = piece;
            this.PieceType = piece.GetPieceType();
            this.Color = piece.GetColor();
        }

        #endregion

        #region Public Properties

        public Piece Piece
        {
            get;
            private set;
        }

        public PieceType PieceType
        {
            get;
            private set;
        }

        public PieceColor? Color
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{{{0}, {1}, {2}}}",
                this.Piece.GetName(),
                this.PieceType.GetName(),
                this.Color.ToStringSafely("null"));
        }

        #endregion
    }
}