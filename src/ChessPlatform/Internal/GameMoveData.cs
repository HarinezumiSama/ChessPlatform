using System;
using System.Diagnostics;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.Internal
{
    internal struct GameMoveData
    {
        #region Constructors

        internal GameMoveData([NotNull] GameMove move, GameMoveInfo moveInfo)
        {
            Move = move;
            MoveInfo = moveInfo;
        }

        #endregion

        #region Public Properties

        public GameMove Move
        {
            [DebuggerStepThrough]
            get;
        }

        public GameMoveInfo MoveInfo
        {
            [DebuggerStepThrough]
            get;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return $@"{Move} : {MoveInfo}";
        }

        #endregion
    }
}