using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace ChessPlatform
{
    internal struct AttackCacheKey : IEquatable<AttackCacheKey>
    {
        #region Constants and Fields

        private readonly Position _position;
        private readonly Piece _piece;
        private readonly int _hashCode;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="AttackCacheKey"/> class.
        /// </summary>
        public AttackCacheKey(Position position, Piece piece)
        {
            #region Argument Check

            if (piece == Piece.None)
            {
                throw new ArgumentException("Cannot be empty square.", "piece");
            }

            #endregion

            _position = position;
            _piece = piece;

            _hashCode = _position.CombineHashCodes(_piece);
        }

        #endregion

        #region Public Properties

        public Position Position
        {
            [DebuggerStepThrough]
            get
            {
                return _position;
            }
        }

        public Piece Piece
        {
            [DebuggerStepThrough]
            get
            {
                return _piece;
            }
        }

        #endregion

        #region Operators

        public static bool operator ==(AttackCacheKey left, AttackCacheKey right)
        {
            return EqualityComparer<AttackCacheKey>.Default.Equals(left, right);
        }

        public static bool operator !=(AttackCacheKey left, AttackCacheKey right)
        {
            return !(left == right);
        }

        #endregion

        #region Public Methods

        public override bool Equals(object obj)
        {
            return obj is AttackCacheKey && Equals((AttackCacheKey)obj);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} @ {1}", _piece.GetDescription(), _position);
        }

        #endregion

        #region IEquatable<AttackCacheKey> Members

        public bool Equals(AttackCacheKey other)
        {
            return other._piece == _piece && other._position == _position;
        }

        #endregion
    }
}