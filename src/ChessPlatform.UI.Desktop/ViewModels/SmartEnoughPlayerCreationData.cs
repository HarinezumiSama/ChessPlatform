using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using ChessPlatform.Engine;
using ChessPlatform.GamePlay;
using Omnifactotum;
using Omnifactotum.Validation;
using Omnifactotum.Validation.Constraints;

namespace ChessPlatform.UI.Desktop.ViewModels
{
    internal sealed class SmartEnoughPlayerCreationData : PlayerCreationData
    {
        #region Constants and Fields

        private static readonly ValueRange<int> MaxPlyDepthRange =
            ValueRange.Create(
                CommonEngineConstants.MaxPlyDepthLowerLimit,
                CommonEngineConstants.MaxPlyDepthUpperLimit);

        private static readonly ValueRange<TimeSpan> MaxTimePerMoveRange = ValueRange.Create(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromDays(7));

        private int? _maxPlyDepth;
        private bool _useOpeningBook;
        private TimeSpan? _maxTimePerMove;
        private bool _useMultipleProcessors;
        private bool _useTranspositionTable;
        private int? _transpositionTableSizeInMegaBytes;

        #endregion

        #region Constructors

        public SmartEnoughPlayerCreationData()
        {
            MaxPlyDepth = CommonEngineConstants.MaxPlyDepthUpperLimit;
            UseOpeningBook = true;
            MaxTimePerMove = TimeSpan.FromSeconds(15);
            UseMultipleProcessors = true;
            UseTranspositionTable = true;
            TranspositionTableSizeInMegaBytes = 1024;
        }

        #endregion

        #region Public Properties

        [DisplayName(@"1. Max Time per Move")]
        [MemberConstraint(typeof(MaxTimePerMoveConstraint))]
        public TimeSpan? MaxTimePerMove
        {
            [DebuggerStepThrough]
            get
            {
                return _maxTimePerMove;
            }

            set
            {
                if (value == _maxTimePerMove)
                {
                    return;
                }

                _maxTimePerMove = value;
                RaisePropertyChanged(() => MaxTimePerMove);
            }
        }

        [DisplayName(@"2. Max Ply Depth")]
        [MemberConstraint(typeof(MaxPlyDepthConstraint))]
        public int? MaxPlyDepth
        {
            [DebuggerStepThrough]
            get
            {
                return _maxPlyDepth;
            }

            set
            {
                if (value == _maxPlyDepth)
                {
                    return;
                }

                _maxPlyDepth = value;
                RaisePropertyChanged(() => MaxPlyDepth);
            }
        }

        [DisplayName(@"3. Use Opening Book")]
        public bool UseOpeningBook
        {
            [DebuggerStepThrough]
            get
            {
                return _useOpeningBook;
            }

            set
            {
                if (value == _useOpeningBook)
                {
                    return;
                }

                _useOpeningBook = value;
                RaisePropertyChanged(() => UseOpeningBook);
            }
        }

        [DisplayName(@"4. Use Multiple CPUs")]
        public bool UseMultipleProcessors
        {
            [DebuggerStepThrough]
            get
            {
                return _useMultipleProcessors;
            }

            set
            {
                if (value == _useMultipleProcessors)
                {
                    return;
                }

                _useMultipleProcessors = value;
                RaisePropertyChanged(() => UseMultipleProcessors);
            }
        }

        [DisplayName(@"5. Use Transposition Table")]
        public bool UseTranspositionTable
        {
            [DebuggerStepThrough]
            get
            {
                return _useTranspositionTable;
            }

            set
            {
                if (value == _useTranspositionTable)
                {
                    return;
                }

                _useTranspositionTable = value;
                RaisePropertyChanged(() => UseTranspositionTable);
            }
        }

        [DisplayName(@"6. TT Size in MB (if used)")]
        [MemberConstraint(typeof(TranspositionTableSizeConstraint))]
        public int? TranspositionTableSizeInMegaBytes
        {
            [DebuggerStepThrough]
            get
            {
                return _transpositionTableSizeInMegaBytes;
            }

            set
            {
                if (value == _transpositionTableSizeInMegaBytes)
                {
                    return;
                }

                _transpositionTableSizeInMegaBytes = value;
                RaisePropertyChanged(() => TranspositionTableSizeInMegaBytes);
            }
        }

        #endregion

        #region MaxPlyDepthConstraint Class

        private sealed class MaxPlyDepthConstraint : TypedMemberConstraintBase<int?>
        {
            protected override void ValidateTypedValue(
                ObjectValidatorContext validatorContext,
                MemberConstraintValidationContext memberContext,
                int? value)
            {
                if (!value.HasValue)
                {
                    AddError(validatorContext, memberContext, "The value is not specified.");
                    return;
                }

                if (!MaxPlyDepthRange.Contains(value.Value))
                {
                    AddError(
                        validatorContext,
                        memberContext,
                        $@"The specified max ply depth ({value}) is out of the valid range {MaxPlyDepthRange}.");
                }
            }
        }

        #endregion

        #region MaxTimePerMoveConstraint Class

        private sealed class MaxTimePerMoveConstraint : TypedMemberConstraintBase<TimeSpan?>
        {
            protected override void ValidateTypedValue(
                ObjectValidatorContext validatorContext,
                MemberConstraintValidationContext memberContext,
                TimeSpan? value)
            {
                if (value.HasValue && !MaxTimePerMoveRange.Contains(value.Value))
                {
                    AddError(
                        validatorContext,
                        memberContext,
                        $@"The value {value} is out of the valid range {MaxTimePerMoveRange}.");
                }
            }
        }

        #endregion

        #region TranspositionTableSizeConstraint Class

        private sealed class TranspositionTableSizeConstraint : TypedMemberConstraintBase<int?>
        {
            protected override void ValidateTypedValue(
                ObjectValidatorContext validatorContext,
                MemberConstraintValidationContext memberContext,
                int? value)
            {
                if (!value.HasValue)
                {
                    AddError(validatorContext, memberContext, "The value is not specified.");
                    return;
                }

                if (!TranspositionTableHelper.SizeInMegaBytesRange.Contains(value.Value))
                {
                    AddError(
                        validatorContext,
                        memberContext,
                        $@"The value is out of the valid range ({TranspositionTableHelper.SizeInMegaBytesRange.Lower
                            } .. {
                            TranspositionTableHelper.SizeInMegaBytesRange.Upper}).");
                }
            }
        }

        #endregion
    }
}