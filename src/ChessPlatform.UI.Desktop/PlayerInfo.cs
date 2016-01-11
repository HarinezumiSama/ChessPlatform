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
        #region Constants and Fields

        private readonly Func<GameSide, TCreationData, TPlayer> _createPlayer;

        #endregion

        #region Constructors

        public PlayerInfo(
            [CanBeNull] TCreationData initialCreationData,
            [NotNull] Func<GameSide, TCreationData, TPlayer> createPlayer)
        {
            #region Argument Check

            if (createPlayer == null)
            {
                throw new ArgumentNullException(nameof(createPlayer));
            }

            #endregion

            CreationData = initialCreationData;
            _createPlayer = createPlayer;
        }

        #endregion

        #region Public Properties

        [CanBeNull]
        public TCreationData CreationData
        {
            get;
        }

        #endregion

        #region Public Methods

        [NotNull]
        public TPlayer CreatePlayer(GameSide side)
        {
            return _createPlayer(side, CreationData).EnsureNotNull();
        }

        #endregion

        #region IPlayerInfo Members

        PlayerCreationData IPlayerInfo.CreationData => CreationData;

        IChessPlayer IPlayerInfo.CreatePlayer(GameSide side)
        {
            return CreatePlayer(side);
        }

        #endregion
    }
}