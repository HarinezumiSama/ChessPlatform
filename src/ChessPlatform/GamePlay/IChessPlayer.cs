using System;
using System.Linq;
using System.Threading.Tasks;
using Omnifactotum.Annotations;

namespace ChessPlatform.GamePlay
{
    public interface IChessPlayer : IDisposable
    {
        #region Events

        event EventHandler<ChessPlayerFeedbackEventArgs> FeedbackProvided;

        #endregion

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

        Task<VariationLine> CreateGetMoveTask([NotNull] GetMoveRequest request);

        #endregion
    }
}