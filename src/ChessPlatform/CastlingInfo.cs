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
        internal CastlingInfo(
            CastlingOptions option,
            GameMove kingMove,
            GameMove rookMove,
            params Position[] emptySquares)
        {
            #region Argument Check

            if (!option.IsAnySet(CastlingOptions.WhiteMask) && !option.IsAnySet(CastlingOptions.BlackMask))
            {
                throw new ArgumentException("Invalid castling option.", "option");
            }

            if (kingMove == null)
            {
                throw new ArgumentNullException("kingMove");
            }

            if (rookMove == null)
            {
                throw new ArgumentNullException("rookMove");
            }

            if (emptySquares == null)
            {
                throw new ArgumentNullException("emptySquares");
            }

            if (Math.Abs(kingMove.From.X88Value - kingMove.To.X88Value) != 2)
            {
                throw new ArgumentException("Invalid castling move.", "kingMove");
            }

            #endregion

            this.Option = option;
            this.KingMove = kingMove;
            this.RookMove = rookMove;
            this.EmptySquares = emptySquares.AsReadOnly();
            this.PassedPosition = new Position((byte)((kingMove.From.X88Value + kingMove.To.X88Value) / 2));
        }

        #endregion

        #region Public Properties

        public CastlingOptions Option
        {
            get;
            private set;
        }

        public GameMove KingMove
        {
            get;
            private set;
        }

        public GameMove RookMove
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