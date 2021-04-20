using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform.GamePlay
{
    [DebuggerDisplay(@"{ToString(),nq}")]
    public struct EvaluationScore
    {
        public const int ZeroValue = 0;
        public const int MateValue = 1000000000;
        public const int PositiveInfinityValue = checked(MateValue + 1);
        public const int NegativeInfinityValue = checked(-PositiveInfinityValue);

        public static readonly EvaluationScore Zero = new EvaluationScore(ZeroValue);
        public static readonly EvaluationScore Mate = new EvaluationScore(MateValue);
        public static readonly EvaluationScore PositiveInfinity = new EvaluationScore(PositiveInfinityValue);
        public static readonly EvaluationScore NegativeInfinity = new EvaluationScore(NegativeInfinityValue);

        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EvaluationScore(int value)
        {
            Value = value;
        }

        public int Value
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
        }

        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EvaluationScore operator -(EvaluationScore operand)
        {
            return new EvaluationScore(-operand.Value);
        }

        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EvaluationScore operator +(EvaluationScore left, EvaluationScore right)
        {
            return new EvaluationScore(left.Value + right.Value);
        }

        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EvaluationScore operator -(EvaluationScore left, EvaluationScore right)
        {
            return new EvaluationScore(left.Value - right.Value);
        }

        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EvaluationScore CreateCheckmatingScore(int plyDistance)
        {
            if (plyDistance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(plyDistance),
                    plyDistance,
                    @"The value cannot be negative.");
            }

            return new EvaluationScore(MateValue - plyDistance);
        }

        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EvaluationScore CreateGettingCheckmatedScore(int plyDistance)
        {
            if (plyDistance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(plyDistance),
                    plyDistance,
                    @"The value cannot be negative.");
            }

            return new EvaluationScore(-MateValue + plyDistance);
        }

        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EvaluationScore Max(EvaluationScore score1, EvaluationScore score2)
        {
            return score1.Value >= score2.Value ? score1 : score2;
        }

        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EvaluationScore Min(EvaluationScore score1, EvaluationScore score2)
        {
            return score1.Value <= score2.Value ? score1 : score2;
        }

        [DebuggerNonUserCode]
        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }
    }
}