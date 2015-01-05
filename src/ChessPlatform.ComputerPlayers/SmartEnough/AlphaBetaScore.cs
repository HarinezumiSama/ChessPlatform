using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.ComputerPlayers.SmartEnough
{
    internal sealed class AlphaBetaScore
    {
        #region Constants and Fields

        private readonly List<GameMove> _movesInternal;

        #endregion

        #region Constructors

        public AlphaBetaScore(int value)
        {
            _movesInternal = new List<GameMove>();
            this.Value = value;
            this.Moves = _movesInternal.AsReadOnly();
        }

        private AlphaBetaScore(int value, [NotNull] GameMove move, [NotNull] ICollection<GameMove> successiveMoves)
            : this(value)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            if (successiveMoves == null)
            {
                throw new ArgumentNullException("successiveMoves");
            }

            if (successiveMoves.Any(item => item == null))
            {
                throw new ArgumentException(@"The collection contains a null element.", "successiveMoves");
            }

            #endregion

            _movesInternal.Add(move);
            _movesInternal.AddRange(successiveMoves);
        }

        private AlphaBetaScore(int value, ICollection<GameMove> moves)
            : this(value)
        {
            #region Argument Check

            if (moves.Any(item => item == null))
            {
                throw new ArgumentException(@"The collection contains a null element.", "moves");
            }

            #endregion

            _movesInternal.AddRange(moves);
        }

        #endregion

        #region Public Properties

        public int Value
        {
            get;
            private set;
        }

        [NotNull]
        public ReadOnlyCollection<GameMove> Moves
        {
            get;
            private set;
        }

        #endregion

        #region Operators

        [DebuggerNonUserCode]
        [NotNull]
        public static AlphaBetaScore operator -(AlphaBetaScore alphaBetaScore)
        {
            #region Argument Check

            if (alphaBetaScore == null)
            {
                throw new ArgumentNullException("alphaBetaScore");
            }

            #endregion

            return new AlphaBetaScore(-alphaBetaScore.Value, alphaBetaScore._movesInternal);
        }

        [DebuggerNonUserCode]
        [NotNull]
        public static AlphaBetaScore operator |(GameMove move, AlphaBetaScore alphaBetaScore)
        {
            #region Argument Check

            if (alphaBetaScore == null)
            {
                throw new ArgumentNullException("alphaBetaScore");
            }

            #endregion

            return new AlphaBetaScore(alphaBetaScore.Value, move, alphaBetaScore._movesInternal);
        }

        #endregion

        #region Public Methods

        [DebuggerNonUserCode]
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{{ {0} : {1} }}",
                this.Value,
                this.Moves.Count == 0 ? "x" : this.Moves.Select(move => move.ToString()).Join(", "));
        }

        #endregion
    }
}