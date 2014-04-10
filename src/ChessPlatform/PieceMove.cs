using System;
using System.Linq;

namespace ChessPlatform
{
    public sealed class PieceMove
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PieceMove"/> class.
        /// </summary>
        internal PieceMove(Position from, Position to)
        {
            #region Argument Check

            if (from == null)
            {
                throw new ArgumentNullException("from");
            }

            if (to == null)
            {
                throw new ArgumentNullException("to");
            }

            #endregion

            this.From = @from;
            this.To = to;
        }

        #endregion

        #region Public Properties

        public Position From
        {
            get;
            private set;
        }

        public Position To
        {
            get;
            private set;
        }

        #endregion
    }
}