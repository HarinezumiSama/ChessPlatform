using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using ChessPlatform.Engine;
using ChessPlatform.UI.Desktop.Controls;
using Omnifactotum.Validation;
using Omnifactotum.Validation.Constraints;

namespace ChessPlatform.UI.Desktop.ViewModels
{
    internal sealed class PlayerChoiceControlViewModel : ViewModelBase
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PlayerChoiceControlViewModel"/> class.
        /// </summary>
        public PlayerChoiceControlViewModel(PieceColor playerColor)
        {
            PlayerColor = playerColor;

            var playerTypesInternal =
                new[]
                {
                    new ControlItem<IPlayerInfo>(
                        ViewModelHelper.CreateGuiHumanChessPlayerInfo(),
                        string.Format(CultureInfo.InvariantCulture, "Human ({0})", Environment.UserName)),
                    new ControlItem<IPlayerInfo>(
                        new PlayerInfo<EnginePlayer, SmartEnoughPlayerCreationData>(
                            new SmartEnoughPlayerCreationData(),
                            (color, data) =>
                                new EnginePlayer(
                                    color,
                                    new EnginePlayerParameters
                                    {
                                        UseOpeningBook = data.UseOpeningBook,
                                        MaxPlyDepth = data.MaxPlyDepth.EnsureNotNull(),
                                        MaxTimePerMove = data.MaxTimePerMove,
                                        UseMultipleProcessors = data.UseMultipleProcessors,
                                        UseTranspositionTable = data.UseTranspositionTable,
                                        TranspositionTableSizeInMegaBytes = data.TranspositionTableSizeInMegaBytes
                                    })),
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Computer ({0})",
                            typeof(EnginePlayer).GetQualifiedName()))
                };

            PlayerControlItems = CollectionViewSource.GetDefaultView(playerTypesInternal);
            PlayerControlItems.CurrentChanged += PlayerTypes_CurrentChanged;

            RaiseSelectedPlayerControlItemChanged();
        }

        #endregion

        #region Public Properties

        public PieceColor PlayerColor
        {
            get;
        }

        public ICollectionView PlayerControlItems
        {
            get;
        }

        [MemberConstraint(typeof(ValidPlayerSettingsConstraint))]
        public ControlItem<IPlayerInfo> SelectedPlayerControlItem
            => (ControlItem<IPlayerInfo>)PlayerControlItems.CurrentItem;

        #endregion

        #region Private Methods

        private void RaiseSelectedPlayerControlItemChanged()
        {
            RaisePropertyChanged(() => SelectedPlayerControlItem);
        }

        private void PlayerTypes_CurrentChanged(object sender, EventArgs eventArgs)
        {
            RaiseSelectedPlayerControlItemChanged();
        }

        #endregion

        #region ValidPlayerSettingsConstraint Class

        private sealed class ValidPlayerSettingsConstraint : TypedMemberConstraintBase<ControlItem<IPlayerInfo>>
        {
            #region Protected Methods

            protected override void ValidateTypedValue(
                ObjectValidatorContext validatorContext,
                MemberConstraintValidationContext memberContext,
                ControlItem<IPlayerInfo> value)
            {
                if (value?.Value == null)
                {
                    AddError(
                        validatorContext,
                        memberContext,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The player is not selected ({0}).",
                            ((PlayerChoiceControlViewModel)memberContext.Container).PlayerColor));

                    return;
                }

                var dataValidationResult = value.Value.CreationData?.Validate();
                if (dataValidationResult != null && !dataValidationResult.IsObjectValid)
                {
                    dataValidationResult.Errors.DoForEach(
                        error => AddError(validatorContext, error.Context, error.ErrorMessage));
                }
            }

            #endregion
        }

        #endregion
    }
}