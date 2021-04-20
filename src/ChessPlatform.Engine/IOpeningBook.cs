using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    public interface IOpeningBook
    {
        [NotNull]
        OpeningGameMove[] FindPossibleMoves([NotNull] GameBoard board);
    }
}