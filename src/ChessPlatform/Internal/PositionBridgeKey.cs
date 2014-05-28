using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform.Internal
{
    internal struct PositionBridgeKey : IEquatable<PositionBridgeKey>
    {
        #region Constants and Fields

        private readonly Position _first;
        private readonly Position _second;
        private readonly int _hashCode;

        #endregion

        #region Constructors

        internal PositionBridgeKey(Position first, Position second)
        {
            #region Argument Check

            if (first == second)
            {
                throw new ArgumentException("Positions cannot be equal.");
            }

            #endregion

            if (first.X88Value > second.X88Value)
            {
                Factotum.Exchange(ref first, ref second);
            }

            _first = first;
            _second = second;

            _hashCode = (_first.X88Value << 8) | second.X88Value;
        }

        #endregion

        #region Public Properties

        public Position First
        {
            [DebuggerStepThrough]
            get
            {
                return _first;
            }
        }

        public Position Second
        {
            [DebuggerStepThrough]
            get
            {
                return _second;
            }
        }

        #endregion

        #region Public Methods

        public override bool Equals(object obj)
        {
            return obj is PositionBridgeKey && Equals((PositionBridgeKey)obj);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{{{0}:{1}}}", _first, _second);
        }

        #endregion

        #region IEquatable<PositionBridgeKey> Members

        public bool Equals(PositionBridgeKey other)
        {
            return other._first == _first && other._second == _second;
        }

        #endregion
    }
}