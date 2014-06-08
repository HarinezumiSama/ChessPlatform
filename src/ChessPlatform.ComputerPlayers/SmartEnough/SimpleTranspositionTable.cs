using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    internal sealed class SimpleTranspositionTable
    {
        #region Constants and Fields

        private readonly Dictionary<Tuple<PackedGameBoard, int>, int> _scoreMap;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="SimpleTranspositionTable"/> class.
        /// </summary>
        internal SimpleTranspositionTable(int maximumItemCount)
        {
            #region Argument Check

            if (maximumItemCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "maximumItemCount",
                    maximumItemCount,
                    @"The value must be positive.");
            }

            #endregion

            this.MaximumItemCount = maximumItemCount;
            _scoreMap = new Dictionary<Tuple<PackedGameBoard, int>, int>(maximumItemCount);
        }

        #endregion

        #region Public Properties

        public int MaximumItemCount
        {
            get;
            private set;
        }

        public ulong HitCount
        {
            get;
            private set;
        }

        public ulong TotalRequestCount
        {
            get;
            private set;
        }

        public int ItemCount
        {
            [DebuggerNonUserCode]
            get
            {
                return _scoreMap.Count;
            }
        }

        #endregion

        #region Public Methods

        public int? GetScore([NotNull] IGameBoard board, int plyDistance)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException("board");
            }

            #endregion

            this.TotalRequestCount++;

            var key = GetKey(board, plyDistance);

            int result;
            if (!_scoreMap.TryGetValue(key, out result))
            {
                return null;
            }

            this.HitCount++;
            return result;
        }

        public void SaveScore([NotNull] IGameBoard board, int plyDistance, int score)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException("board");
            }

            #endregion

            if (_scoreMap.Count >= this.MaximumItemCount)
            {
                return;
            }

            var key = GetKey(board, plyDistance);
            _scoreMap.Add(key, score);

            if (_scoreMap.Count >= this.MaximumItemCount)
            {
                Trace.TraceInformation(
                    "[{0}] Maximum item count has been reached ({1}).",
                    MethodBase.GetCurrentMethod().GetQualifiedName(),
                    this.MaximumItemCount);
            }
        }

        #endregion

        #region Private Methods

        private static Tuple<PackedGameBoard, int> GetKey([NotNull] IGameBoard board, int plyDepth)
        {
            var packedGameBoard = board.Pack();
            return Tuple.Create(packedGameBoard, plyDepth);
        }

        #endregion
    }
}