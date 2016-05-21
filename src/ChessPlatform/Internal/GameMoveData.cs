using System;
using System.Diagnostics;
using Omnifactotum.Annotations;

namespace ChessPlatform.Internal
{
    internal struct GameMoveData
    {
        #region Constructors

        internal GameMoveData([NotNull] GameMove move, GameMoveFlags moveFlags)
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

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return $@"{Move} : {MoveFlags}";
        }

        #endregion
    }
}