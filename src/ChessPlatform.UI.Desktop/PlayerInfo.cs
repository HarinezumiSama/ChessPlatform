using System;
using System.Linq;
using ChessPlatform.GamePlay;
using ChessPlatform.UI.Desktop.ViewModels;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop
{
    internal sealed class PlayerInfo<TPlayer, TCreationData> : IPlayerInfo
        where TPlayer : class, IChessPlayer
        where TCreationData : PlayerCreationData
    {
        private readonly Func<GameSide, TCreationData, TPlayer> _createPlayer;

        public PlayerInfo(
            [CanBeNull] TCreationData initialCreationData,
            [NotNull] Func<GameSide, TCreationData, TPlayer> createPlayer)
        {
            if (createPlayer == null)
            {
                throw new ArgumentNullException(nameof(createPlayer));
            }

            CreationData = initialCreationData;
            _createPlayer = createPlayer;
        }

        [CanBeNull]
        public TCreationData CreationData
        {
            get;
        }

        [NotNull]
        public TPlayer CreatePlayer(GameSide side)
        {
            return _createPlayer(side, CreationData).EnsureNotNull();
        }

        PlayerCreationData IPlayerInfo.CreationData => CreationData;

        IChessPlayer IPlayerInfo.CreatePlayer(GameSide side)
        {
            return CreatePlayer(side);
        }
    }
}