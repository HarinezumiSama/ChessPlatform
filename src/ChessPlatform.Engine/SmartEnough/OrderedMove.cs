using System;
using System.Diagnostics;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine.SmartEnough
{
    [DebuggerDisplay("Move = {Move}, MoveInfo = {MoveInfo}, IsPvMove = {IsPvMove}")]
    internal struct OrderedMove
    {
        #region Constructors

        public OrderedMove([NotNull] GameMove move, GameMoveInfo moveInfo, bool isPvMove)
        {
            Move = move;
            MoveInfo = moveInfo;
            IsPvMove = isPvMove;
        }

        #endregion

        #region Public Properties

        [NotNull]
        public GameMove Move
        {
            get;
        }

        public GameMoveInfo MoveInfo
        {
            get;
        }

        public bool IsPvMove
        {
            get;
        }

        #endregion
    }
}