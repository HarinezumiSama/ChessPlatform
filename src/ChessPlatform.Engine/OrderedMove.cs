using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    //// TODO [HarinezumiSama] Re-use GameMoveData instead of OrderedMove - ?

    internal readonly struct OrderedMove
    {
        public OrderedMove([NotNull] GameMove move, GameMoveFlags moveFlags)
        {
            Move = move ?? throw new ArgumentNullException(nameof(move));
            MoveFlags = moveFlags;
        }

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

        public override string ToString()
        {
            return $@"{{ {Move} {MoveFlags} }}";
        }
    }
}