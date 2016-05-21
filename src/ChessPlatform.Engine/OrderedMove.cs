using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    //// TODO [vmcl] Re-use GameMoveData instead of OrderedMove - ?

    internal struct OrderedMove
    {
        #region Constructors

        public OrderedMove([NotNull] GameMove move, GameMoveFlags moveFlags)
        {
            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            Move = move;
            MoveFlags = moveFlags;
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

        public GameMoveFlags MoveFlags
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return $@"{{ {Move} {MoveFlags} }}";
        }

        #endregion
    }
}