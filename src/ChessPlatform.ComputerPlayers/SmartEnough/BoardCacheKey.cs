using System;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    internal sealed class BoardCacheKey : IEquatable<BoardCacheKey>
    {
        #region Constants and Fields

        private readonly PackedGameBoard _packedGameBoard;
        private readonly PieceMove _move;
        private readonly int _hashCode;

        #endregion

        #region Constructors

        internal BoardCacheKey([NotNull] IGameBoard board, [CanBeNull] PieceMove move)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException("board");
            }

            #endregion

            _packedGameBoard = board.Pack();
            _move = move;
            _hashCode = _packedGameBoard.GetHashCode() ^ (_move == null ? 0 : _move.GetHashCode());
        }

        #endregion

        #region Public Methods

        public override bool Equals(object obj)
        {
            //return obj is BoardCacheKey && Equals((BoardCacheKey)obj);
            return Equals(obj as BoardCacheKey);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        #endregion

        #region IEquatable<BoardCacheKey> Members

        public bool Equals(BoardCacheKey other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other._hashCode == _hashCode
                && other._packedGameBoard == _packedGameBoard
                && other._move == _move;
        }

        #endregion
    }
}