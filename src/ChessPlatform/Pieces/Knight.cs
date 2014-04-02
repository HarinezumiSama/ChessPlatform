using System;
using System.Linq;

namespace ChessPlatform.Pieces
{
    public sealed class Knight : Piece
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Knight"/> class.
        /// </summary>
        internal Knight()
        {
            // Nothing to do
        }

        #endregion

        #region Protected Properties

        protected override char BaseFenChar
        {
            get
            {
                return 'N';
            }
        }

        #endregion


    }
}