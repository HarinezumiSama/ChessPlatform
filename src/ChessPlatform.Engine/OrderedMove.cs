using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    internal struct OrderedMove
    {
        #region Constructors

        public OrderedMove([NotNull] GameMove move, GameMoveInfo moveInfo)
        {
            Move = move;
            MoveInfo = moveInfo;
        }

        #endregion

        #region Public Properties

        [NotNull]
        public GameMove Move
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        public GameMoveInfo MoveInfo
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return $@"{{ {Move} {MoveInfo.Flags} }}";
        }

        #endregion
    }
}