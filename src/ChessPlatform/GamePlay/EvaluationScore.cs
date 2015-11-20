using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform.GamePlay
{
    public struct EvaluationScore
    {
        #region Constants and Fields

        public const int ZeroValue = 0;
        public const int MateValue = 1000000000;
        public const int PositiveInfinityValue = checked(MateValue + 1);
        public const int NegativeInfinityValue = checked(-PositiveInfinityValue);

        public static readonly EvaluationScore Zero = new EvaluationScore(ZeroValue);
        public static readonly EvaluationScore Mate = new EvaluationScore(MateValue);
        public static readonly EvaluationScore PositiveInfinity = new EvaluationScore(PositiveInfinityValue);
        public static readonly EvaluationScore NegativeInfinity = new EvaluationScore(NegativeInfinityValue);

        #endregion

        #region Constructors

        public EvaluationScore(int value)
        {
            Value = value;
        }

        #endregion

        #region Public Properties

        public int Value
        {
            [DebuggerStepThrough]
            get;
        }

        #endregion

        #region Operators

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

        #endregion

        #region Public Methods

        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EvaluationScore CreateCheckmatingScore(int plyDistance)
        {
            #region Argument Check

            if (plyDistance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(plyDistance),
                    plyDistance,
                    @"The value cannot be negative.");
            }

            #endregion

            return new EvaluationScore(MateValue - plyDistance);
        }

        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EvaluationScore CreateGettingCheckmatedScore(int plyDistance)
        {
            #region Argument Check

            if (plyDistance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(plyDistance),
                    plyDistance,
                    @"The value cannot be negative.");
            }

            #endregion

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

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}