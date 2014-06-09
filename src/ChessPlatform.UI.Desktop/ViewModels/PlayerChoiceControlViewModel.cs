using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using ChessPlatform.ComputerPlayers.SmartEnough;

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
                    typeof(GuiHumanChessPlayer),
                    typeof(SmartEnoughPlayer)
                };

            this.PlayerTypes = CollectionViewSource.GetDefaultView(playerTypesInternal);
            this.PlayerTypes.CurrentChanged += PlayerTypes_CurrentChanged;
        }

        #endregion

        #region Public Properties

        public ICollectionView PlayerTypes
        {
            get;
            private set;
        }

        public Type SelectedPlayerType
        {
            get
            {
                return this.PlayerTypes.CurrentItem as Type;
            }
        }

        #endregion

        #region Private Methods

        private void PlayerTypes_CurrentChanged(object sender, EventArgs eventArgs)
        {
            RaisePropertyChanged(() => this.SelectedPlayerType);
        }

        #endregion
    }
}