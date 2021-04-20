using System;
using System.Diagnostics;
using Omnifactotum;

namespace ChessPlatform.Internal
{
    internal readonly struct SquareBridgeKey : IEquatable<SquareBridgeKey>
    {
        private readonly int _hashCode;

        internal SquareBridgeKey(Square first, Square second)
        {
            if (first == second)
            {
                throw new ArgumentException("Squares cannot be the same.");
            }

            if (first.SquareIndex > second.SquareIndex)
            {
                Factotum.Exchange(ref first, ref second);
            }

            First = first;
            Second = second;

            _hashCode = (First.SquareIndex << 8) | second.SquareIndex;
        }

        public Square First
        {
            [DebuggerStepThrough]
            get;
        }

        public Square Second
        {
            [DebuggerStepThrough]
            get;
        }

        public override bool Equals(object obj)
        {
            return obj is SquareBridgeKey squareBridgeKey && Equals(squareBridgeKey);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override string ToString()
        {
            return $@"{{{First}:{Second}}}";
        }

        public bool Equals(SquareBridgeKey other)
        {
            return other.First == First && other.Second == Second;
        }
    }
}