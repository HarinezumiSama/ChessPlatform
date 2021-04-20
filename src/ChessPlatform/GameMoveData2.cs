using System.Diagnostics;

namespace ChessPlatform
{
    public readonly struct GameMoveData2
    {
        internal GameMoveData2(GameMove2 move, GameMoveFlags moveFlags)
        {
            Move = move;
            MoveFlags = moveFlags;
        }

        public GameMove2 Move
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