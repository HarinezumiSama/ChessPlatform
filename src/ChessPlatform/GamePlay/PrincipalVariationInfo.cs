using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Omnifactotum.Annotations;

namespace ChessPlatform.GamePlay
{
    public sealed class PrincipalVariationInfo
    {
        #region Constants and Fields

        public static readonly PrincipalVariationInfo Zero = new PrincipalVariationInfo(EvaluationScore.Zero);

        private readonly List<GameMove> _movesInternal;

        #endregion

        #region Constructors

        [DebuggerNonUserCode]
        public PrincipalVariationInfo(EvaluationScore value)
            : this(value, null)
        {
            // Nothing to do
        }

        private PrincipalVariationInfo(EvaluationScore value, EvaluationScore? localValue)
        {
            _movesInternal = new List<GameMove>();
            Value = value;
            LocalValue = localValue;
            Moves = _movesInternal.AsReadOnly();
        }

        private PrincipalVariationInfo(
            EvaluationScore value,
            EvaluationScore? localValue,
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

        private PrincipalVariationInfo(
            EvaluationScore value,
            EvaluationScore? localValue,
            [NotNull] ICollection<GameMove> moves)
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

        #endregion

        #region Operators

        [DebuggerNonUserCode]
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrincipalVariationInfo operator -([NotNull] PrincipalVariationInfo operand)
        {
            #region Argument Check

            if (operand == null)
            {
                throw new ArgumentNullException(nameof(operand));
            }

            #endregion

            return new PrincipalVariationInfo(-operand.Value, -operand.LocalValue, operand._movesInternal);
        }

        [DebuggerNonUserCode]
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            return new PrincipalVariationInfo(operand.Value, operand.LocalValue, move, operand._movesInternal);
        }

        [DebuggerNonUserCode]
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrincipalVariationInfo operator +([NotNull] PrincipalVariationInfo left, EvaluationScore right)
        {
            #region Argument Check

            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            #endregion

            return new PrincipalVariationInfo(left.Value + right, left.LocalValue, left._movesInternal);
        }

        [DebuggerNonUserCode]
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrincipalVariationInfo operator -([NotNull] PrincipalVariationInfo left, EvaluationScore right)
        {
            #region Argument Check

            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            #endregion

            return new PrincipalVariationInfo(left.Value - right, left.LocalValue, left._movesInternal);
        }

        #endregion

        #region Public Methods

        [DebuggerNonUserCode]
        public override string ToString()
        {
            return $@"{{ {ValueString} : L({LocalValueString}) : {
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
                $@"{{ {ValueString} : L({LocalValueString}) : {(movesString.IsNullOrEmpty() ? "x" : movesString)} }}";

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PrincipalVariationInfo WithLocalValue(EvaluationScore localValue)
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