using System;
using System.Diagnostics;
using System.Linq;

namespace ChessPlatform
{
    public struct PieceInfo
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PieceInfo"/> class.
        /// </summary>
        public PieceInfo(Piece piece)
        {
            Piece = piece;
            PieceType = piece.GetPieceType();
            Side = piece.GetSide();
        }

        #endregion

        #region Public Properties

        public Piece Piece
        {
            [DebuggerStepThrough]
            get;
        }

        public PieceType PieceType
        {
            [DebuggerStepThrough]
            get;
        }

        public GameSide? Side
        {
            [DebuggerStepThrough]
            get;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return $@"{{{Piece.GetName()}, {PieceType.GetName()}, {Side?.ToString() ?? "null"}}}";
        }

        #endregion
    }
}