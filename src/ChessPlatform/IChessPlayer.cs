using System;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public interface IChessPlayer
    {
        #region Properties

        PieceColor Color
        {
            get;
        }

        #endregion

        #region Methods

        PieceMove GetMove([NotNull] IGameBoard board);

        #endregion
    }
}