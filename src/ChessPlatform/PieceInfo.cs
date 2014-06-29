using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace ChessPlatform
{
    public struct PieceInfo
    {
        #region Constants and Fields

        private readonly Piece _piece;
        private readonly PieceType _pieceType;
        private readonly PieceColor? _color;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PieceInfo"/> class.
        /// </summary>
        public PieceInfo(Piece piece)
        {
            _piece = piece;
            _pieceType = piece.GetPieceType();
            _color = piece.GetColor();
        }

        #endregion

        #region Public Properties

        public Piece Piece
        {
            [DebuggerStepThrough]
            get
            {
                return _piece;
            }
        }

        public PieceType PieceType
        {
            [DebuggerStepThrough]
            get
            {
                return _pieceType;
            }
        }

        public PieceColor? Color
        {
            [DebuggerStepThrough]
            get
            {
                return _color;
            }
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