using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ChessPlatform.GamePlay;
using Omnifactotum;
using Omnifactotum.Annotations;

//// ReSharper disable SuggestBaseTypeForParameter - For optimization
//// ReSharper disable LoopCanBeConvertedToQuery - Using simpler loops for speed optimization
//// ReSharper disable ForCanBeConvertedToForeach - Using simpler loops for speed optimization

namespace ChessPlatform.Engine
{
    internal sealed class MoveHistoryStatistics
    {
        #region Constants and Fields

        public const int MinDepth = 1;
        public const int MaxDepth = CommonEngineConstants.MaxPlyDepthUpperLimit;

        public static readonly ValueRange<int> DepthRange = ValueRange.Create(MinDepth, MaxDepth);

        private static readonly int PieceCount = ChessConstants.Pieces.Max(item => (int)item) + 1;

        private readonly object _syncLock;
        private readonly KillerMoveData[] _killerMoveDatas;
        private readonly int[] _historyTable;

        #endregion

        #region Constructors

        public MoveHistoryStatistics()
        {
            _syncLock = new object();

            _killerMoveDatas = new KillerMoveData[MaxDepth];
            _historyTable = new int[PieceCount * ChessConstants.SquareCount];
        }

        #endregion

        #region Public Methods

        public void RecordCutOffMove(
            [NotNull] GameBoard board,
            [NotNull] GameMove move,
            int plyDistance,
            int remainingDepth,
            [CanBeNull] ICollection<GameMove> previousQueitMoves)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            if (plyDistance < MinDepth || plyDistance > MaxDepth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(plyDistance),
                    plyDistance,
                    $@"The value is out of the valid range ({MinDepth} .. {MaxDepth}).");
            }

            #endregion

            if (board.State.IsAnyCheck() || remainingDepth <= 0)
            {
                return;
            }

            GameMoveInfo moveInfo;
            if (!board.ValidMoves.TryGetValue(move, out moveInfo) || !LocalHelper.IsQuietMove(moveInfo))
            {
                return;
            }

            var moveBonus = remainingDepth.Sqr();

            lock (_syncLock)
            {
                _killerMoveDatas[plyDistance - 1].RecordKiller(move);

                UpdateHistoryTableUnsafe(board, move, moveBonus);

                if (previousQueitMoves == null || previousQueitMoves.Count == 0)
                {
                    return;
                }

                foreach (var previousQueitMove in previousQueitMoves)
                {
                    UpdateHistoryTableUnsafe(board, previousQueitMove, -moveBonus);
                }
            }
        }

        public void AddKillerMoves(
            int plyDistance,
            [NotNull] Dictionary<GameMove, GameMoveInfo> remainingMoves,
            [NotNull] List<OrderedMove> resultList)
        {
            if (!DepthRange.Contains(plyDistance))
            {
                return;
            }

            KillerMoveData killerMoveData;
            lock (_syncLock)
            {
                killerMoveData = _killerMoveDatas[plyDistance - 1];
            }

            AddSingleKillerMove(killerMoveData.Primary, remainingMoves, resultList);
            AddSingleKillerMove(killerMoveData.Secondary, remainingMoves, resultList);
        }

        public int GetHistoryValue([NotNull] GameBoard board, [NotNull] GameMove move)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            #endregion

            var piece = board[move.From];
            var to = move.To;
            var historyTableOffset = GetHistoryTableOffset(piece, to);

            lock (_syncLock)
            {
                return _historyTable[historyTableOffset];
            }
        }

        public string GetAllKillersInfoString()
        {
            string result;
            lock (_syncLock)
            {
                result = _killerMoveDatas
                    .Select(
                        (data, i) =>
                            data.Primary == null
                                ? null
                                : $@"  #{i + 1:D2} {{ {data.Primary}, {data.Secondary.ToStringSafely("<none>")} }}")
                    .Where(s => !s.IsNullOrEmpty())
                    .Join(Environment.NewLine);
            }

            if (result.IsNullOrWhiteSpace())
            {
                result = "  (none)";
            }

            return result;
        }

        public string GetAllHistoryInfoString()
        {
            var entries = new List<Tuple<Piece, Position, int>>(_historyTable.Length);
            lock (_syncLock)
            {
                foreach (var piece in ChessConstants.PiecesExceptNone)
                {
                    foreach (var position in ChessHelper.AllPositions)
                    {
                        var historyTableOffset = GetHistoryTableOffset(piece, position);
                        var value = _historyTable[historyTableOffset];
                        if (value > 0)
                        {
                            entries.Add(Tuple.Create(piece, position, value));
                        }
                    }
                }
            }

            var result = entries
                .OrderByDescending(t => t.Item3)
                .ThenBy(t => t.Item2.SquareIndex)
                .ThenBy(t => (int)t.Item1)
                .Select(t => $@"  {t.Item1.GetDescription()} : {t.Item2} = {t.Item3}")
                .Join(Environment.NewLine);

            if (result.IsNullOrWhiteSpace())
            {
                result = "  (none)";
            }

            return result;
        }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetHistoryTableOffset(Piece piece, Position position)
        {
            return ((int)piece) * ChessConstants.SquareCount + position.SquareIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddSingleKillerMove(
            [CanBeNull] GameMove killerMove,
            [NotNull] Dictionary<GameMove, GameMoveInfo> remainingMoves,
            [NotNull] List<OrderedMove> resultList)
        {
            GameMoveInfo moveInfo;
            if (killerMove == null || !remainingMoves.TryGetValue(killerMove, out moveInfo)
                || !LocalHelper.IsQuietMove(moveInfo))
            {
                return;
            }

            var orderedMove = new OrderedMove(killerMove, moveInfo);
            resultList.Add(orderedMove);
            remainingMoves.Remove(killerMove);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateHistoryTableUnsafe([NotNull] GameBoard board, [NotNull] GameMove move, int bonus)
        {
            var historyTableOffset = GetHistoryTableOffset(board[move.From], move.To);
            _historyTable[historyTableOffset] += bonus;
        }

        #endregion
    }
}