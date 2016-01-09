using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ChessPlatform.GamePlay;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TranspositionTableEntry
    {
        #region Constants and Fields

        private readonly ushort _bestMoveEncoded;
        private readonly int _scoreValue;
        private readonly int _localScoreValue;

        #endregion

        #region Constructors

        public TranspositionTableEntry(
            long key,
            [CanBeNull] GameMove bestMove,
            EvaluationScore score,
            EvaluationScore localScore,
            ScoreBound bound,
            int depth)
        {
            Key = key;

            _bestMoveEncoded = (ushort)(bestMove == null
                ? 0
                : (bestMove.From.SquareIndex & 0x3F) << 9
                    | (bestMove.To.SquareIndex & 0x3F) << 3
                    | (int)bestMove.PromotionResult & 0x7);

            _scoreValue = score.Value;
            _localScoreValue = localScore.Value;
            Bound = bound;
            Depth = depth;
            Version = 0;
        }

        #endregion

        #region Public Properties

        public long Key
        {
            get;
        }

        [CanBeNull]
        public GameMove BestMove
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _bestMoveEncoded == 0
                    ? null
                    : new GameMove(
                        new Position((_bestMoveEncoded >> 9) & 0x3F),
                        new Position((_bestMoveEncoded >> 3) & 0x3F),
                        (PieceType)(_bestMoveEncoded & 0x7));
            }
        }

        public EvaluationScore Score => new EvaluationScore(_scoreValue);

        public EvaluationScore LocalScore => new EvaluationScore(_localScoreValue);

        public ScoreBound Bound
        {
            get;
            private set;
        }

        public int Depth
        {
            get;
            private set;
        }

        public uint Version
        {
            get;
            internal set;
        }

        #endregion
    }
}