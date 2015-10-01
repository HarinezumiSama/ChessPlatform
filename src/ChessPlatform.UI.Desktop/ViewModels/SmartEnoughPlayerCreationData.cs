using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using ChessPlatform.ComputerPlayers.SmartEnough;
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
                SmartEnoughPlayerConstants.MaxPlyDepthLowerLimit,
                SmartEnoughPlayerConstants.MaxPlyDepthUpperLimit);

        private static readonly ValueRange<TimeSpan> MaxTimePerMoveRange = ValueRange.Create(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromDays(7));

        private int? _maxPlyDepth;
        private bool _useOpeningBook;
        private TimeSpan? _maxTimePerMove;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="SmartEnoughPlayerCreationData"/> class.
        /// </summary>
        public SmartEnoughPlayerCreationData()
        {
            this.MaxPlyDepth = SmartEnoughPlayerConstants.MaxPlyDepthUpperLimit;
            this.UseOpeningBook = true;
            this.MaxTimePerMove = TimeSpan.FromSeconds(15);
        }

        #endregion

        #region Public Properties

        [DisplayName(@"Max Ply Depth")]
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
                RaisePropertyChanged(() => this.MaxPlyDepth);
            }
        }

        [DisplayName(@"Use Opening Book")]
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
                RaisePropertyChanged(() => this.UseOpeningBook);
            }
        }

        [DisplayName(@"Max Time per Move")]
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
                RaisePropertyChanged(() => this.MaxTimePerMove);
            }
        }

        #endregion

        #region MaxPlyDepthConstraint Class

        private sealed class MaxPlyDepthConstraint : TypedMemberConstraintBase<int?>
        {
            #region Protected Methods

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
                        string.Format(
                            CultureInfo.InvariantCulture,
                            @"The specified max ply depth ({0}) is out of the valid range ({1} .. {2}).",
                            value,
                            MaxPlyDepthRange.Lower,
                            MaxPlyDepthRange.Upper));
                }
            }

            #endregion
        }

        #endregion

        #region MaxTimePerMoveConstraint Class

        private sealed class MaxTimePerMoveConstraint : TypedMemberConstraintBase<TimeSpan?>
        {
            #region Protected Methods

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
                        string.Format(
                            CultureInfo.InvariantCulture,
                            @"The value {0} is out of the valid range ({1} .. {2}).",
                            value,
                            MaxTimePerMoveRange.Lower,
                            MaxTimePerMoveRange.Upper));
                }
            }

            #endregion
        }

        #endregion
    }
}