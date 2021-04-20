using System;
using System.Diagnostics;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public struct GameMoveData
    {
        internal GameMoveData([NotNull] GameMove move, GameMoveFlags moveFlags)
        {
            if (move is null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            Move = move;
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