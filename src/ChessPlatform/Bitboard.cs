﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace ChessPlatform
{
    public struct Bitboard : IEquatable<Bitboard>
    {
        #region Constants and Fields

        public static readonly Bitboard Zero = new Bitboard(0L);

        private readonly long _value;

        #endregion

        #region Constructors

        public Bitboard(long value)
        {
            _value = value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Bitboard"/> class.
        /// </summary>
        public Bitboard(IEnumerable<Position> positions)
        {
            #region Argument Check

            if (positions == null)
            {
                throw new ArgumentNullException("positions");
            }

            #endregion

            this = ChessHelper.GetBitboard(positions);
        }

        #endregion

        #region Public Properties

        public long Value
        {
            [DebuggerStepThrough]
            get
            {
                return _value;
            }
        }

        #endregion

        #region Operators

        public static bool operator ==(Bitboard left, Bitboard right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Bitboard left, Bitboard right)
        {
            return !(left == right);
        }

        public static implicit operator Bitboard(long value)
        {
            return new Bitboard(value);
        }

        public static Bitboard operator ~(Bitboard obj)
        {
            return new Bitboard(~obj._value);
        }

        public static Bitboard operator &(Bitboard left, Bitboard right)
        {
            return new Bitboard(left._value & right._value);
        }

        public static Bitboard operator |(Bitboard left, Bitboard right)
        {
            return new Bitboard(left._value | right._value);
        }

        #endregion

        #region Public Methods

        public static bool Equals(Bitboard left, Bitboard right)
        {
            return left._value == right._value;
        }

        public override bool Equals(object obj)
        {
            return obj is Bitboard && Equals((Bitboard)obj);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{{ {0:X16} : {1} }}",
                _value,
                _value == 0
                    ? "<none>"
                    : _value
                        .FindAllBitsSet()
                        .Select(item => Position.FromBitboardBitIndex(item).ToString())
                        .Join(", "));
        }

        public bool IsZero()
        {
            return _value == 0;
        }

        #endregion

        #region IEquatable<Bitboard> Members

        public bool Equals(Bitboard other)
        {
            return Equals(other, this);
        }

        #endregion
    }
}