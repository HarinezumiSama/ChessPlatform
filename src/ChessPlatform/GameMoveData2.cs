using System.Diagnostics;

namespace ChessPlatform
{
    public struct GameMoveData2
    {
        #region Constructors

        internal GameMoveData2(GameMove2 move, GameMoveFlags moveFlags)
        {
            Move = move;
            MoveFlags = moveFlags;
        }

        #endregion

        #region Public Properties

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

        #endregion

        #region Public Methods

        public override string ToString() => $@"{Move} : {MoveFlags}";

        #endregion
    }
}