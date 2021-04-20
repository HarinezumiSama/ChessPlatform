using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        /// <summary>
        ///     Initializes a new instance of the <see cref="PlayerChoiceControlViewModel"/> class.
        /// </summary>
        public PlayerChoiceControlViewModel(GameSide playerSide)
        {
            PlayerSide = playerSide;

            var playerControlItemsInternal =
                new[]
                {
                    new ControlItem<IPlayerInfo>(
                        ViewModelHelper.CreateGuiHumanChessPlayerInfo(),
                        $"Human ({Environment.UserName})"),
                    new ControlItem<IPlayerInfo>(
                        new PlayerInfo<EnginePlayer, SmartEnoughPlayerCreationData>(
                            new SmartEnoughPlayerCreationData(),
                            (side, data) =>
                                new EnginePlayer(
                                    side,
                                    new EnginePlayerParameters
                                    {
                                        UseOpeningBook = data.UseOpeningBook,
                                        MaxPlyDepth = data.MaxPlyDepth.EnsureNotNull(),
                                        MaxTimePerMove = data.MaxTimePerMove,
                                        UseMultipleProcessors = data.UseMultipleProcessors,
                                        UseTranspositionTable = data.UseTranspositionTable,
                                        TranspositionTableSizeInMegaBytes =
                                            data.TranspositionTableSizeInMegaBytes.EnsureNotNull()
                                    })),
                        $"Computer ({typeof(EnginePlayer).GetQualifiedName()})")
                };

            PlayerControlItems = CollectionViewSource.GetDefaultView(playerControlItemsInternal);
            PlayerControlItems.CurrentChanged += PlayerTypes_CurrentChanged;

            RaiseSelectedPlayerControlItemChanged();
        }

        public GameSide PlayerSide
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

        private void RaiseSelectedPlayerControlItemChanged()
        {
            RaisePropertyChanged(() => SelectedPlayerControlItem);
        }

        private void PlayerTypes_CurrentChanged(object sender, EventArgs eventArgs)
        {
            RaiseSelectedPlayerControlItemChanged();
        }

        private sealed class ValidPlayerSettingsConstraint : TypedMemberConstraintBase<ControlItem<IPlayerInfo>>
        {
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
                        $@"The player is not selected ({
                            ((PlayerChoiceControlViewModel)memberContext.Container).PlayerSide}).");

                    return;
                }

                var dataValidationResult = value.Value.CreationData?.Validate();
                if (dataValidationResult != null && !dataValidationResult.IsObjectValid)
                {
                    dataValidationResult.Errors.DoForEach(
                        error => AddError(validatorContext, error.Context, error.ErrorMessage));
                }
            }
        }
    }
}