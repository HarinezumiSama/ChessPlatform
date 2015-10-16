using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    internal sealed class BestMoveInfo
    {
        #region Constructors

        internal BestMoveInfo(ICollection<GameMove> principalVariationMoves)
        {
            #region Argument Check

            if (principalVariationMoves == null)
            {
                throw new ArgumentNullException(nameof(principalVariationMoves));
            }

            if (principalVariationMoves.Any(item => item == null))
            {
                throw new ArgumentException(@"The collection contains a null element.", nameof(principalVariationMoves));
            }

            if (principalVariationMoves.Count == 0)
            {
                throw new ArgumentException(
                    @"Principal variation must have at least one move.",
                    nameof(principalVariationMoves));
            }

            #endregion

            this.PrincipalVariationMoves = principalVariationMoves.ToArray().AsReadOnly();
            this.BestMove = principalVariationMoves.First();
        }

        #endregion

        #region Public Properties

        public GameMove BestMove
        {
            get;
            private set;
        }

        public ReadOnlyCollection<GameMove> PrincipalVariationMoves
        {
            get;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{{ {0} }}",
                this.PrincipalVariationMoves.Select(move => move.ToString()).Join(", "));
        }

        #endregion
    }
}