using System;
using System.Diagnostics;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public readonly struct GameMoveData
    {
        internal GameMoveData([NotNull] GameMove move, GameMoveFlags moveFlags)
        {
            Move = move ?? throw new ArgumentNullException(nameof(move));
            MoveFlags = moveFlags;
        }

        public GameMove Move
        {
            [DebuggerStepThrough]
            get;
        }

        public GameMoveFlags MoveFlags
        {
            [DebuggerStepThrough]
            get;
        }

        public override string ToString() => $@"{Move} : {MoveFlags}";
    }
}