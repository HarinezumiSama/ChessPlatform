using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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
            : this(value, null)
        {
            // Nothing to do
        }

        private PrincipalVariationInfo(int value, int? localValue)
        {
            _movesInternal = new List<GameMove>();
            Value = value;
            LocalValue = localValue;
            Moves = _movesInternal.AsReadOnly();
        }

        private PrincipalVariationInfo(
            int value,
            int? localValue,
            [NotNull] GameMove move,
            [NotNull] ICollection<GameMove> successiveMoves)
            : this(value, localValue)
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

        private PrincipalVariationInfo(int value, int? localValue, [NotNull] ICollection<GameMove> moves)
            : this(value, localValue)
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

        public int? LocalValue
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

        public string LocalValueString => LocalValue.ToStringSafelyInvariant("null");

        #endregion

        #region Operators

        [DebuggerNonUserCode]
        [NotNull]
        public static PrincipalVariationInfo operator -([NotNull] PrincipalVariationInfo operand)
        {
            #region Argument Check

            if (operand == null)
            {
                throw new ArgumentNullException(nameof(operand));
            }

            #endregion

            return new PrincipalVariationInfo(
                -operand.Value,
                -operand.LocalValue,
                operand._movesInternal);
        }

        [DebuggerNonUserCode]
        [NotNull]
        public static PrincipalVariationInfo operator |(
            [NotNull] GameMove move,
            [NotNull] PrincipalVariationInfo operand)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            if (operand == null)
            {
                throw new ArgumentNullException(nameof(operand));
            }

            #endregion

            return new PrincipalVariationInfo(
                operand.Value,
                operand.LocalValue,
                move,
                operand._movesInternal);
        }

        #endregion

        #region Public Methods

        [DebuggerNonUserCode]
        public override string ToString()
        {
            return $@"{{ {Value} : L({LocalValueString}) : {
                (_movesInternal.Count == 0 ? "x" : _movesInternal.Select(move => move.ToString()).Join(", "))} }}";
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

            var movesString = board.GetStandardAlgebraicNotation(_movesInternal);

            var result =
                $@"{{ {Value} : L({LocalValueString}) : {(movesString.IsNullOrEmpty() ? "x" : movesString)} }}";

            return result;
        }

        public PrincipalVariationInfo WithLocalValue(int localValue)
        {
            if (LocalValue.HasValue)
            {
                throw new InvalidOperationException(
                    $@"The local value cannot be re-assigned for {ToString()}.");
            }

            return new PrincipalVariationInfo(Value, localValue, _movesInternal);
        }

        #endregion
    }
}