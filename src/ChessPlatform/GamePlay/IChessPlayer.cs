using System;
using System.Threading.Tasks;
using Omnifactotum.Annotations;

namespace ChessPlatform.GamePlay
{
    public interface IChessPlayer : IDisposable
    {
        event EventHandler<ChessPlayerFeedbackEventArgs> FeedbackProvided;

        GameSide Side
        {
            get;
        }

        string Name
        {
            get;
        }

        Task<VariationLine> CreateGetMoveTask([NotNull] GetMoveRequest request);
    }
}