using System;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers
{
    public interface IOpeningBook
    {
        #region Methods

        [NotNull]
        GameMove[] FindPossibleMoves([NotNull] GameBoard board);

        #endregion
    }
}