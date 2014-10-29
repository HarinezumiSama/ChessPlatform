using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    internal sealed class BoardCache
    {
        #region Constants and Fields

        private readonly Dictionary<BoardCacheKey, GameBoard> _boards;

        #endregion

        #region Constructors

        internal BoardCache(int maximumItemCount)
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
            _boards = new Dictionary<BoardCacheKey, GameBoard>(maximumItemCount);
        }

        #endregion

        #region Public Properties

        public int MaximumItemCount
        {
            get;
            private set;
        }

        public int ItemCount
        {
            [DebuggerStepThrough]
            get
            {
                return _boards.Count;
            }
        }

        public int HitCount
        {
            get;
            private set;
        }

        public int TotalRequestCount
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public GameBoard MakeMove([NotNull] GameBoard board, [NotNull] GameMove move)
        {
            var result = MakeMoveInternal(board, move);
            return result;
        }

        public GameBoard MakeNullMove([NotNull] GameBoard board)
        {
            var result = MakeMoveInternal(board, null);
            return result;
        }

        #endregion

        #region Private Methods

        private GameBoard MakeMoveInternal([NotNull] GameBoard board, [CanBeNull] GameMove move)
        {
            var key = new BoardCacheKey(board, move);

            this.TotalRequestCount++;

            GameBoard result;
            if (_boards.TryGetValue(key, out result))
            {
                this.HitCount++;
                return result;
            }

            result = move == null ? board.MakeNullMove() : board.MakeMove(move);

            if (_boards.Count >= this.MaximumItemCount)
            {
                return result;
            }

            _boards.Add(key, result);
            if (_boards.Count >= this.MaximumItemCount)
            {
                Trace.TraceInformation(
                    "[{0}] Maximum item count has been reached ({1}).",
                    MethodBase.GetCurrentMethod().GetQualifiedName(),
                    this.MaximumItemCount);
            }

            return result;
        }

        #endregion
    }
}