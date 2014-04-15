using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ChessPlatform
{
    public sealed class CastlingInfo
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="CastlingInfo"/> class.
        /// </summary>
        internal CastlingInfo(PieceMove castlingMove, params Position[] emptySquares)
        {
            this.CastlingMove = castlingMove.EnsureNotNull();
            this.EmptySquares = emptySquares.EnsureNotNull().AsReadOnly();
        }

        #endregion

        #region Public Properties

        public PieceMove CastlingMove
        {
            get;
            private set;
        }

        public ReadOnlyCollection<Position> EmptySquares
        {
            get;
            private set;
        }

        #endregion
    }
}