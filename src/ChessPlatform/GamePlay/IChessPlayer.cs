﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Omnifactotum.Annotations;

namespace ChessPlatform.GamePlay
{
    public interface IChessPlayer
    {
        #region Properties

        PieceColor Color
        {
            get;
        }

        string Name
        {
            get;
        }

        #endregion

        #region Methods

        Task<GameMove> CreateGetMoveTask([NotNull] GetMoveRequest request);

        #endregion
    }
}