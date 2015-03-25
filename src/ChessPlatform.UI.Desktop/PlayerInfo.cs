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

        private readonly Func<PieceColor, TCreationData, TPlayer> _createPlayer;

        #endregion

        #region Constructors

        public PlayerInfo(
            [CanBeNull] TCreationData initialCreationData,
            [NotNull] Func<PieceColor, TCreationData, TPlayer> createPlayer)
        {
            #region Argument Check

            if (createPlayer == null)
            {
                throw new ArgumentNullException("createPlayer");
            }

            #endregion

            this.CreationData = initialCreationData;
            _createPlayer = createPlayer;
        }

        #endregion

        #region Public Properties

        [CanBeNull]
        public TCreationData CreationData
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        [NotNull]
        public TPlayer CreatePlayer(PieceColor color)
        {
            return _createPlayer(color, this.CreationData).EnsureNotNull();
        }

        #endregion

        #region IPlayerInfo Members

        PlayerCreationData IPlayerInfo.CreationData
        {
            get
            {
                return this.CreationData;
            }
        }

        IChessPlayer IPlayerInfo.CreatePlayer(PieceColor color)
        {
            return CreatePlayer(color);
        }

        #endregion
    }
}