using System;
using System.Linq;

namespace ChessPlatform.Pieces
{
    [BaseFenChar('P')]
    public sealed class Pawn : Piece
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Pawn"/> class.
        /// </summary>
        internal Pawn()
        {
            // Nothing to do
        }

        #endregion
    }
}