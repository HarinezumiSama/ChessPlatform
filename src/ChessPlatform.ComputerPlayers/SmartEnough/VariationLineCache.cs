using System;
using System.Collections.Generic;
using System.Linq;
using ChessPlatform.GamePlay;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    internal sealed class VariationLineCache
    {
        #region Constants and Fields

        private readonly Dictionary<GameMove, VariationLine> _cache;

        #endregion

        #region Constants and Fields

        public VariationLineCache([NotNull] GameBoard board)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            #endregion

            _cache = board.ValidMoves.ToDictionary(pair => pair.Key, pair => (VariationLine)null);
        }

        #endregion

        #region Public Properties

        public VariationLine this[[NotNull] GameMove move]
        {
            get
            {
                return _cache[move];
            }

            set
            {
                #region Argument Check

                if (move == null)
                {
                    throw new ArgumentNullException(nameof(move));
                }

                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                #endregion

                var variationLine = _cache[move];
                if (variationLine != null)
                {
                    throw new InvalidOperationException($@"Unable to overwrite the variation line of move {move}.");
                }

                _cache[move] = value;
            }
        }

        #endregion

        #region Public Methods

        public IOrderedEnumerable<KeyValuePair<GameMove, VariationLine>> GetOrderedByScore()
        {
            return _cache
                .OrderByDescending(pair => pair.Value.EnsureNotNull().Value.Value)
                .ThenByDescending(pair => pair.Value.LocalValue?.Value)
                .ThenBy(pair => pair.Key.GetHashCode());
        }

        #endregion
    }
}