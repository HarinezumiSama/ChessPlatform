using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public sealed class GameBoardChangedEventArgs : EventArgs
    {
        #region Constructors

        internal GameBoardChangedEventArgs([NotNull] ICollection<GameBoard> gameBoards)
        {
            #region Argument Check

            if (gameBoards == null)
            {
                throw new ArgumentNullException("gameBoards");
            }

            if (gameBoards.Count == 0)
            {
                throw new ArgumentException("The history cannot be empty.", "gameBoards");
            }

            if (gameBoards.Any(item => item == null))
            {
                throw new ArgumentException(@"The collection contains a null element.", "gameBoards");
            }

            #endregion

            this.GameBoards = gameBoards.ToArray().AsReadOnly();
        }

        #endregion

        #region Public Properties

        public ReadOnlyCollection<GameBoard> GameBoards
        {
            get;
            private set;
        }

        #endregion
    }
}