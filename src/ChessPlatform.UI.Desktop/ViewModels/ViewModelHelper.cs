namespace ChessPlatform.UI.Desktop.ViewModels
{
    internal static class ViewModelHelper
    {
        public static IPlayerInfo CreateGuiHumanChessPlayerInfo()
        {
            return new PlayerInfo<GuiHumanChessPlayer, GuiHumanChessPlayerCreationData>(
                null,
                (side, data) => new GuiHumanChessPlayer(side));
        }
    }
}