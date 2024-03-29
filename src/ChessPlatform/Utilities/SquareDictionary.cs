﻿using System.Collections.Generic;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform.Utilities
{
    public sealed class SquareDictionary<TValue> : FixedSizeDictionary<Square, TValue, SquareDeterminant>
    {
        public SquareDictionary()
        {
            // Nothing to do
        }

        public SquareDictionary([NotNull] IDictionary<Square, TValue> dictionary)
            : base(dictionary)
        {
            // Nothing to do
        }

        //// ReSharper disable once SuggestBaseTypeForParameter
        public SquareDictionary([NotNull] SquareDictionary<TValue> dictionary)
            : base(dictionary)
        {
            // Nothing to do
        }
    }
}