using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Omnifactotum.Annotations;

namespace ChessPlatform.GamePlay
{
    [DebuggerDisplay(@"{ToString(),nq}")]
    public sealed class VariationLine
    {
        public static readonly VariationLine Zero = new VariationLine(EvaluationScore.Zero);

        private readonly List<GameMove> _movesInternal;

        [DebuggerNonUserCode]
        public VariationLine(EvaluationScore value)
            : this(value, null)
        {
            // Nothing to do
        }

        private VariationLine(EvaluationScore value, EvaluationScore? localValue)
        {
            if (value.Value.Abs() > EvaluationScore.MateValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value.Value,
                    $@"The score value is out of the valid range [{-EvaluationScore.MateValue:#,##0} .. {
                        EvaluationScore.MateValue:#,##0}].");
            }

            _movesInternal = new List<GameMove>();
            Value = value;
            LocalValue = localValue;
            Moves = _movesInternal.AsReadOnly();
        }

        private VariationLine(
            EvaluationScore value,
            EvaluationScore? localValue,
            [NotNull] GameMove move,
            [NotNull] ICollection<GameMove> successiveMoves)
            : this(value, localValue)
        {
            if (move is null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            if (successiveMoves is null)
            {
                throw new ArgumentNullException(nameof(successiveMoves));
            }

            _movesInternal.Add(move);
            _movesInternal.AddRange(successiveMoves);
        }

        private VariationLine(
            EvaluationScore value,
            EvaluationScore? localValue,
            [NotNull] ICollection<GameMove> moves)
            : this(value, localValue)
        {
            if (moves is null)
            {
                throw new ArgumentNullException(nameof(moves));
            }

            _movesInternal.AddRange(moves);
        }

        public EvaluationScore Value
        {
            get;
        }

        public EvaluationScore? LocalValue
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

        public string ValueString
        {
            get
            {
                var mateMoveDistance = Value.GetMateMoveDistance();
                return mateMoveDistance.HasValue ? $@"mate {mateMoveDistance.Value}" : $@"cp {Value.Value}";
            }
        }

        public string LocalValueString => LocalValue?.ToString() ?? "null";

        [DebuggerNonUserCode]
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariationLine operator -([NotNull] VariationLine operand)
        {
            if (operand is null)
            {
                throw new ArgumentNullException(nameof(operand));
            }

            return new VariationLine(-operand.Value, -operand.LocalValue, operand._movesInternal);
        }

        [DebuggerNonUserCode]
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariationLine operator |(
            [NotNull] GameMove move,
            [NotNull] VariationLine operand)
        {
            if (move is null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            if (operand is null)
            {
                throw new ArgumentNullException(nameof(operand));
            }

            return new VariationLine(operand.Value, operand.LocalValue, move, operand._movesInternal);
        }

        [DebuggerNonUserCode]
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariationLine operator +([NotNull] VariationLine left, EvaluationScore right)
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            return new VariationLine(left.Value + right, left.LocalValue, left._movesInternal);
        }

        [DebuggerNonUserCode]
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VariationLine operator -([NotNull] VariationLine left, EvaluationScore right)
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            return new VariationLine(left.Value - right, left.LocalValue, left._movesInternal);
        }

        [DebuggerNonUserCode]
        public override string ToString()
        {
            return $@"{{ {ValueString} : L({LocalValueString}) : {
                (_movesInternal.Count == 0 ? "x" : _movesInternal.Select(move => move.ToString()).Join(", "))} }}";
        }

        [DebuggerNonUserCode]
        public string ToStandardAlgebraicNotationString([NotNull] GameBoard board)
        {
            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var movesString = board.GetStandardAlgebraicNotation(_movesInternal);

            var result =
                $@"{{ {ValueString} : L({LocalValueString}) : {(movesString.IsNullOrEmpty() ? "x" : movesString)} }}";

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VariationLine WithLocalValue(EvaluationScore localValue)
        {
            if (LocalValue.HasValue)
            {
                throw new InvalidOperationException(
                    $@"The local value cannot be re-assigned for {ToString()}.");
            }

            return new VariationLine(Value, localValue, _movesInternal);
        }
    }
}