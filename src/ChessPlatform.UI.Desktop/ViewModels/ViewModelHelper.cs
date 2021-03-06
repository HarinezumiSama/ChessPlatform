﻿using System;
using System.Linq;

namespace ChessPlatform.UI.Desktop.ViewModels
{
    internal static class ViewModelHelper
    {
        #region Public Methods

        public static IPlayerInfo CreateGuiHumanChessPlayerInfo()
        {
            return new PlayerInfo<GuiHumanChessPlayer, GuiHumanChessPlayerCreationData>(
                null,
                (side, data) => new GuiHumanChessPlayer(side));
        }

        #endregion
    }
}