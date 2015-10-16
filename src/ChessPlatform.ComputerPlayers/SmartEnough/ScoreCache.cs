using System;
using System.Collections.Generic;
using System.Linq;
using ChessPlatform.GamePlay;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    internal sealed class ScoreCache
    {
        #region Constants and Fields

        private readonly Dictionary<GameMove, PrincipalVariationInfo> _cache;

        #endregion

        #region Constants and Fields

        public ScoreCache([NotNull] GameBoard board)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            #endregion

            _cache = board.ValidMoves.ToDictionary(pair => pair.Key, pair => (PrincipalVariationInfo)null);
        }

        #endregion

        #region Public Properties

        public PrincipalVariationInfo this[[NotNull] GameMove move]
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

                var oldScore = _cache[move];
                if (oldScore != null)
                {
                    throw new InvalidOperationException($@"Unable to overwrite the score for move {move}.");
                }

                _cache[move] = value;
            }
        }

        #endregion

        #region Public Methods

        public IOrderedEnumerable<KeyValuePair<GameMove, PrincipalVariationInfo>> OrderMovesByScore()
        {
            return _cache
                .OrderByDescending(pair => pair.Value.EnsureNotNull().Value)
                .ThenBy(pair => pair.Key.GetHashCode());
        }

        #endregion
    }
}