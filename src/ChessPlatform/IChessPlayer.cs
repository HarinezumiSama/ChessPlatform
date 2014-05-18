using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        Task<PieceMove> GetMove([NotNull] IGameBoard board, CancellationToken cancellationToken);

        #endregion
    }
}