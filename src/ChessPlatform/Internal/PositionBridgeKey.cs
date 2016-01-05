using System;
using System.Diagnostics;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform.Internal
{
    internal struct PositionBridgeKey : IEquatable<PositionBridgeKey>
    {
        #region Constants and Fields

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

            First = first;
            Second = second;

            _hashCode = (First.X88Value << 8) | second.X88Value;
        }

        #endregion

        #region Public Properties

        public Position First
        {
            [DebuggerStepThrough]
            get;
        }

        public Position Second
        {
            [DebuggerStepThrough]
            get;
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
            return $@"{{{First}:{Second}}}";
        }

        #endregion

        #region IEquatable<PositionBridgeKey> Members

        public bool Equals(PositionBridgeKey other)
        {
            return other.First == First && other.Second == Second;
        }

        #endregion
    }
}