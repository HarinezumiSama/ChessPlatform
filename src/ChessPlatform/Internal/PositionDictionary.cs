using System;
using System.Collections.Generic;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform.Internal
{
    internal sealed class PositionDictionary<TValue> : FixedSizeDictionary<Position, TValue, PositionDeterminant>
    {
        #region Constructors

        public PositionDictionary()
        {
            // Nothing to do
        }

        public PositionDictionary(IEnumerable<KeyValuePair<Position, TValue>> dictionary)
            : base((IDictionary<Position, TValue>)dictionary)
        {
            // Nothing to do
        }

        public PositionDictionary(PositionDictionary<TValue> dictionary)
            : base(dictionary)
        {
            // Nothing to do
        }

        #endregion
    }
}