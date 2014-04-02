using System;
using System.Linq;

namespace ChessPlatform.Pieces
{
    public sealed class Rook : Piece
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Rook"/> class.
        /// </summary>
        internal Rook()
        {
            // Nothing to do
        }

        #endregion

        #region Protected Properties

        protected override char BaseFenChar
        {
            get
            {
                return 'R';
            }
        }

        #endregion
    }
}