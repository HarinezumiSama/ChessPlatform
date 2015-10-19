using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Omnifactotum.Annotations;

namespace ChessPlatform.GamePlay
{
    public sealed class PrincipalVariationInfo
    {
        #region Constants and Fields

        public static readonly PrincipalVariationInfo Zero = new PrincipalVariationInfo(0);

        private readonly List<GameMove> _movesInternal;

        #endregion

        #region Constructors

        public PrincipalVariationInfo(int value)
        {
            _movesInternal = new List<GameMove>();
            Value = value;
            Moves = _movesInternal.AsReadOnly();
        }

        private PrincipalVariationInfo(
            int value,
            [NotNull] GameMove move,
            [NotNull] ICollection<GameMove> successiveMoves)
            : this(value)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            if (successiveMoves == null)
            {
                throw new ArgumentNullException(nameof(successiveMoves));
            }

            #endregion

            _movesInternal.Add(move);
            _movesInternal.AddRange(successiveMoves);
        }

        private PrincipalVariationInfo(int value, [NotNull] ICollection<GameMove> moves)
            : this(value)
        {
            #region Argument Check

            if (moves == null)
            {
                throw new ArgumentNullException(nameof(moves));
            }

            #endregion

            _movesInternal.AddRange(moves);
        }

        #endregion

        #region Public Properties

        public int Value
        {
            get;
        }

        [NotNull]
        public ReadOnlyCollection<GameMove> Moves
        {
            get;
        }

        [CanBeNull]
        public GameMove FirstMove => _movesInternal.FirstOrDefault();

        #endregion

        #region Operators

        [DebuggerNonUserCode]
        [NotNull]
        public static PrincipalVariationInfo operator -([NotNull] PrincipalVariationInfo principalVariationInfo)
        {
            #region Argument Check

            if (principalVariationInfo == null)
            {
                throw new ArgumentNullException(nameof(principalVariationInfo));
            }

            #endregion

            return new PrincipalVariationInfo(-principalVariationInfo.Value, principalVariationInfo._movesInternal);
        }

        [DebuggerNonUserCode]
        [NotNull]
        public static PrincipalVariationInfo operator |(
            [NotNull] GameMove move,
            [NotNull] PrincipalVariationInfo principalVariationInfo)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            if (principalVariationInfo == null)
            {
                throw new ArgumentNullException(nameof(principalVariationInfo));
            }

            #endregion

            return new PrincipalVariationInfo(
                principalVariationInfo.Value,
                move,
                principalVariationInfo._movesInternal);
        }

        #endregion

        #region Public Methods

        [DebuggerNonUserCode]
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{{ {0} : {1} }}",
                Value,
                _movesInternal.Count == 0 ? "x" : _movesInternal.Select(move => move.ToString()).Join(", "));
        }

        [DebuggerNonUserCode]
        public string ToStandardAlgebraicNotationString([NotNull] GameBoard board)
        {
            #region Argument Check

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            #endregion

            var moveListBuilder = new StringBuilder();
            var currentBoard = board;
            foreach (var move in _movesInternal)
            {
                if (moveListBuilder.Length != 0)
                {
                    moveListBuilder.Append(", ");
                }

                var notation = currentBoard.GetStandardAlgebraicNotationInternal(move, out currentBoard);
                moveListBuilder.Append(notation);
            }

            var result = $@"{{ {Value} : {(moveListBuilder.Length == 0 ? "x" : moveListBuilder.ToString())} }}";
            return result;
        }

        #endregion
    }
}