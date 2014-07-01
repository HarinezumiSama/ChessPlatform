using System;
using System.Linq;
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

        string Name
        {
            get;
        }

        #endregion

        #region Methods

        Task<PieceMove> GetMove([NotNull] GetMoveRequest request);

        #endregion
    }
}