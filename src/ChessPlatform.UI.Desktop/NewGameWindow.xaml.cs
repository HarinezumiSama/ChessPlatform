using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ChessPlatform.UI.Desktop
{
    /// <summary>
    ///     Interaction logic for <b>NewGameWindow.xaml</b>.
    /// </summary>
    public partial class NewGameWindow
    {
        #region Constructors

        public NewGameWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Public Properties

        public Type WhitePlayerType
        {
            get
            {
                return this.WhitePlayerChoiceControl.ViewModel.SelectedPlayerType;
            }
        }

        public Type BlackPlayerType
        {
            get
            {
                return this.BlackPlayerChoiceControl.ViewModel.SelectedPlayerType;
            }
        }

        #endregion

        #region Private Methods: Event Handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DialogResult = null;
        }

        private void OKButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.WhitePlayerType == null || this.BlackPlayerType == null)
            {
                return;
            }

            this.DialogResult = true;
            Close();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        #endregion
    }
}