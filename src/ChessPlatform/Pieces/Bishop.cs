using System;
using System.Linq;

namespace ChessPlatform.Pieces
{
    public sealed class Bishop : Piece
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Bishop"/> class.
        /// </summary>
        internal Bishop()
        {
            // Nothing to do
        }

        #endregion

        #region Protected Properties

        protected override char BaseFenChar
        {
            get
            {
                return 'B';
            }
        }

        #endregion
    }
}