using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using ChessPlatform.ComputerPlayers.SmartEnough;
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
            this.PlayerColor = playerColor;

            var playerTypesInternal =
                new[]
                {
                    new ControlItem<IPlayerInfo>(
                        ViewModelHelper.CreateGuiHumanChessPlayerInfo(),
                        string.Format(CultureInfo.InvariantCulture, "Human ({0})", Environment.UserName)),
                    new ControlItem<IPlayerInfo>(
                        new PlayerInfo<SmartEnoughPlayer, SmartEnoughPlayerCreationData>(
                            new SmartEnoughPlayerCreationData(),
                            (color, data) =>
                                new SmartEnoughPlayer(
                                    color,
                                    data.UseOpeningBook,
                                    data.MaxPlyDepth.EnsureNotNull(),
                                    data.MaxTimePerMove)),
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Computer ({0})",
                            typeof(SmartEnoughPlayer).GetQualifiedName()))
                };

            this.PlayerControlItems = CollectionViewSource.GetDefaultView(playerTypesInternal);
            this.PlayerControlItems.CurrentChanged += this.PlayerTypes_CurrentChanged;

            RaiseSelectedPlayerControlItemChanged();
        }

        #endregion

        #region Public Properties

        public PieceColor PlayerColor
        {
            get;
            private set;
        }

        public ICollectionView PlayerControlItems
        {
            get;
            private set;
        }

        [MemberConstraint(typeof(ValidPlayerSettingsConstraint))]
        public ControlItem<IPlayerInfo> SelectedPlayerControlItem
        {
            get
            {
                return (ControlItem<IPlayerInfo>)this.PlayerControlItems.CurrentItem;
            }
        }

        #endregion

        #region Private Methods

        private void RaiseSelectedPlayerControlItemChanged()
        {
            RaisePropertyChanged(() => this.SelectedPlayerControlItem);
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
                if (value == null || value.Value == null)
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

                var smartEnoughPlayerCreationData = value.Value.CreationData as SmartEnoughPlayerCreationData;
                if (smartEnoughPlayerCreationData != null)
                {
                    var creationDataValidationResult = smartEnoughPlayerCreationData.Validate();
                    if (!creationDataValidationResult.IsObjectValid)
                    {
                        creationDataValidationResult.Errors.DoForEach(
                            error => AddError(validatorContext, error.Context, error.ErrorMessage));
                    }
                }
            }

            #endregion
        }

        #endregion
    }
}