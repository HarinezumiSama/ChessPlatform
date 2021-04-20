using System;
using System.Collections.Generic;
using System.Linq;
using ChessPlatform.GamePlay;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    internal sealed class VariationLineCache
    {
        private readonly Dictionary<GameMove, VariationLine> _cache;

        public VariationLineCache([NotNull] GameBoard board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            _cache = board.ValidMoves.ToDictionary(pair => pair.Key, pair => (VariationLine)null);
        }

        public VariationLine this[[NotNull] GameMove move]
        {
            get => _cache[move];

            set
            {
                if (move == null)
                {
                    throw new ArgumentNullException(nameof(move));
                }

                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                var variationLine = _cache[move];
                if (variationLine != null)
                {
                    throw new InvalidOperationException($@"Unable to overwrite the variation line of move {move}.");
                }

                _cache[move] = value;
            }
        }

        public IOrderedEnumerable<KeyValuePair<GameMove, VariationLine>> GetOrderedByScore()
        {
            return _cache
                .OrderByDescending(pair => pair.Value.EnsureNotNull().Value.Value)
                .ThenByDescending(pair => pair.Value.LocalValue?.Value)
                .ThenBy(pair => pair.Key.GetHashCode());
        }
    }
}