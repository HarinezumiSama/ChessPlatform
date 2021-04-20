using ChessPlatform.GamePlay;
using ChessPlatform.UI.Desktop.ViewModels;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop
{
    internal interface IPlayerInfo
    {
        [CanBeNull]
        PlayerCreationData CreationData
        {
            get;
        }

        [NotNull]
        IChessPlayer CreatePlayer(GameSide side);
    }
}