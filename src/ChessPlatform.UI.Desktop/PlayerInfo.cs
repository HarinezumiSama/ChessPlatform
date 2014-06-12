using System;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop
{
    internal sealed class PlayerInfo
    {
        #region Constructors

        public PlayerInfo([NotNull] Func<PieceColor, IChessPlayer> playerFactory)
        {
            #region Argument Check

            if (playerFactory == null)
            {
                throw new ArgumentNullException("playerFactory");
            }

            #endregion

            this.PlayerFactory = playerFactory;
        }

        #endregion

        #region Public Properties

        public Func<PieceColor, IChessPlayer> PlayerFactory
        {
            get;
            private set;
        }

        #endregion
    }
}