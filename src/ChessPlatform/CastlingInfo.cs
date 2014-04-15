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
            #region Argument Check

            if (castlingMove == null)
            {
                throw new ArgumentNullException("castlingMove");
            }

            if (emptySquares == null)
            {
                throw new ArgumentNullException("emptySquares");
            }

            if (Math.Abs(castlingMove.From.X88Value - castlingMove.To.X88Value) != 2)
            {
                throw new ArgumentException("Invalid castling move.", "castlingMove");
            }

            #endregion

            this.CastlingMove = castlingMove;
            this.EmptySquares = emptySquares.AsReadOnly();
            this.PassedPosition = new Position((byte)((castlingMove.From.X88Value + castlingMove.To.X88Value) / 2));
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

        public Position PassedPosition
        {
            get;
            private set;
        }

        #endregion
    }
}