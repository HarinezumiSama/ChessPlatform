using System;
using System.Globalization;
using System.Linq;

namespace ChessPlatform
{
    public sealed class PieceMove : IEquatable<PieceMove>
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PieceMove"/> class.
        /// </summary>
        public PieceMove(Position from, Position to)
        {
            #region Argument Check

            if (from == to)
            {
                throw new ArgumentException("The source and destination positions must be different.");
            }

            #endregion

            this.From = from;
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

        #region Public Methods

        public override bool Equals(object obj)
        {
            return Equals(obj as PieceMove);
        }

        public override int GetHashCode()
        {
            return this.From.CombineHashCodes(this.To);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}-{1}", this.From, this.To);
        }

        #endregion

        #region IEquatable<PieceMove> Members

        public bool Equals(PieceMove other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other.From == this.From && other.To == this.To;
        }

        #endregion
    }
}