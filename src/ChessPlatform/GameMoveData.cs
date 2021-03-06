﻿using System;
using System.Diagnostics;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public struct GameMoveData
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

        public override string ToString() => $@"{Move} : {MoveFlags}";

        #endregion
    }
}