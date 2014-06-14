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

        private readonly List<PieceMove> _movesInternal;

        #endregion

        #region Constructors

        public AlphaBetaScore(int score)
        {
            _movesInternal = new List<PieceMove>();
            this.Score = score;
            this.Moves = _movesInternal.AsReadOnly();
        }

        private AlphaBetaScore(int score, [NotNull] PieceMove move, [NotNull] ICollection<PieceMove> successiveMoves)
            : this(score)
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

        private AlphaBetaScore(int score, ICollection<PieceMove> moves)
            : this(score)
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

        public int Score
        {
            get;
            private set;
        }

        [NotNull]
        public ReadOnlyCollection<PieceMove> Moves
        {
            get;
            private set;
        }

        #endregion

        #region Operators

        [NotNull]
        public static AlphaBetaScore operator -(AlphaBetaScore alphaBetaScore)
        {
            #region Argument Check

            if (alphaBetaScore == null)
            {
                throw new ArgumentNullException("alphaBetaScore");
            }

            #endregion

            return new AlphaBetaScore(-alphaBetaScore.Score, alphaBetaScore._movesInternal);
        }

        [NotNull]
        public static AlphaBetaScore operator +(PieceMove move, AlphaBetaScore alphaBetaScore)
        {
            #region Argument Check

            if (alphaBetaScore == null)
            {
                throw new ArgumentNullException("alphaBetaScore");
            }

            #endregion

            return new AlphaBetaScore(alphaBetaScore.Score, move, alphaBetaScore._movesInternal);
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{{ {0} : {1} }}",
                this.Score,
                this.Moves.Count == 0 ? "x" : this.Moves.Select(move => move.ToString()).Join(", "));
        }

        #endregion
    }
}