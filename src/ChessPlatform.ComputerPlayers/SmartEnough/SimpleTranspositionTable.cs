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

        private readonly Dictionary<InternalKey, AlphaBetaScore> _scoreMap;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="SimpleTranspositionTable"/> class.
        /// </summary>
        internal SimpleTranspositionTable(int maximumItemCount)
        {
            #region Argument Check

            if (maximumItemCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "maximumItemCount",
                    maximumItemCount,
                    @"The value cannot be negative.");
            }

            #endregion

            this.MaximumItemCount = maximumItemCount;
            _scoreMap = new Dictionary<InternalKey, AlphaBetaScore>(maximumItemCount);

            if (this.MaximumItemCount <= 0)
            {
                Trace.TraceWarning(
                    "[{0}] The transposition table is DISABLED.",
                    MethodBase.GetCurrentMethod().GetQualifiedName());
            }
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

        public AlphaBetaScore GetScore([NotNull] IGameBoard board, int plyDistance)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException("board");
            }

            #endregion

            this.TotalRequestCount++;

            var key = GetKey(board, plyDistance);

            AlphaBetaScore result;
            if (!_scoreMap.TryGetValue(key, out result))
            {
                return null;
            }

            this.HitCount++;
            return result;
        }

        public void SaveScore([NotNull] IGameBoard board, int plyDistance, [NotNull] AlphaBetaScore score)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException("board");
            }

            if (score == null)
            {
                throw new ArgumentNullException("score");
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

        private static InternalKey GetKey([NotNull] IGameBoard board, int plyDepth)
        {
            var packedGameBoard = board.Pack();
            return new InternalKey(packedGameBoard, plyDepth);
        }

        #endregion

        #region InternalKey Class

        private sealed class InternalKey : IEquatable<InternalKey>
        {
            #region Constants and Fields

            private readonly PackedGameBoard _packedGameBoard;
            private readonly int _plyDepth;
            private readonly int _hashCode;

            #endregion

            #region Constructors

            internal InternalKey(PackedGameBoard packedGameBoard, int plyDepth)
            {
                _packedGameBoard = packedGameBoard.EnsureNotNull();
                _plyDepth = plyDepth;

                _hashCode = _packedGameBoard.CombineHashCodes(_plyDepth);
            }

            #endregion

            #region Public Methods

            public override bool Equals(object obj)
            {
                return Equals(obj as InternalKey);
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }

            #endregion

            #region IEquatable<InternalKey> Members

            public bool Equals(InternalKey other)
            {
                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                if (ReferenceEquals(other, null))
                {
                    return false;
                }

                return _hashCode == other._hashCode
                    && _plyDepth == other._plyDepth
                    && PackedGameBoard.Equals(_packedGameBoard, other._packedGameBoard);
            }

            #endregion
        }

        #endregion
    }
}