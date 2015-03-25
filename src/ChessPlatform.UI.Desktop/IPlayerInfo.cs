using ChessPlatform.GamePlay;
using ChessPlatform.UI.Desktop.ViewModels;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop
{
    internal interface IPlayerInfo
    {
        #region Properties

        [CanBeNull]
        PlayerCreationData CreationData
        {
            get;
        }

        #endregion

        #region Methods

        [NotNull]
        IChessPlayer CreatePlayer(PieceColor color);

        #endregion
    }
}