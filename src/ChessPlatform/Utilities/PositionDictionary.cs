using System;
using System.Collections.Generic;
using System.Linq;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform.Utilities
{
    public sealed class PositionDictionary<TValue> : FixedSizeDictionary<Position, TValue, PositionDeterminant>
    {
        #region Constructors

        public PositionDictionary()
        {
            // Nothing to do
        }

        public PositionDictionary([NotNull] IDictionary<Position, TValue> dictionary)
            : base(dictionary)
        {
            // Nothing to do
        }

        public PositionDictionary([NotNull] PositionDictionary<TValue> dictionary)
            : base(dictionary)
        {
            // Nothing to do
        }

        #endregion
    }
}