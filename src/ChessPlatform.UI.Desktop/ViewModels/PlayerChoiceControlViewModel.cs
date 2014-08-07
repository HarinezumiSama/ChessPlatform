using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using ChessPlatform.ComputerPlayers.SmartEnough;
using ChessPlatform.UI.Desktop.Controls;

namespace ChessPlatform.UI.Desktop.ViewModels
{
    internal sealed class PlayerChoiceControlViewModel : ViewModelBase
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PlayerChoiceControlViewModel"/> class.
        /// </summary>
        public PlayerChoiceControlViewModel()
        {
            var playerTypesInternal =
                new[]
                {
                    ControlItem.Create((PlayerInfo)null, "Human Player"),
                    ControlItem.Create(
                        new PlayerInfo(color => new SmartEnoughPlayer(color, 100, true, TimeSpan.FromSeconds(15))),
                        "Computer Player")
                };

            this.PlayerControlItems = CollectionViewSource.GetDefaultView(playerTypesInternal);
            this.PlayerControlItems.CurrentChanged += this.PlayerTypes_CurrentChanged;
        }

        #endregion

        #region Public Properties

        public ICollectionView PlayerControlItems
        {
            get;
            private set;
        }

        public ControlItem<PlayerInfo> SelectedPlayerControlItem
        {
            get
            {
                return (ControlItem<PlayerInfo>)this.PlayerControlItems.CurrentItem;
            }
        }

        #endregion

        #region Private Methods

        private void PlayerTypes_CurrentChanged(object sender, EventArgs eventArgs)
        {
            RaisePropertyChanged(() => this.SelectedPlayerControlItem);
        }

        #endregion
    }
}