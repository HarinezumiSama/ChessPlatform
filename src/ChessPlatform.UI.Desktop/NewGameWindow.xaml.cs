using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace ChessPlatform.UI.Desktop
{
    /// <summary>
    ///     Interaction logic for <b>NewGameWindow.xaml</b>.
    /// </summary>
    internal partial class NewGameWindow
    {
        #region Constructors

        public NewGameWindow()
        {
            InitializeComponent();

            this.Title = string.Format(CultureInfo.InvariantCulture, "New Game – {0}", App.Title);

            var clipboardText = Clipboard.GetText();

            this.FenTextBox.Text = GameBoard.IsValidFen(clipboardText)
                ? clipboardText
                : ChessConstants.DefaultInitialFen;
        }

        #endregion

        #region Public Properties

        public PlayerInfo WhitePlayer
        {
            get
            {
                return this.WhitePlayerChoiceControl.ViewModel.SelectedPlayerControlItem.EnsureNotNull().Value;
            }
        }

        public PlayerInfo BlackPlayer
        {
            get
            {
                return this.BlackPlayerChoiceControl.ViewModel.SelectedPlayerControlItem.EnsureNotNull().Value;
            }
        }

        public string InitialFen
        {
            get
            {
                return this.FenTextBox.Text.TrimSafely();
            }
        }

        #endregion

        #region Private Methods: Event Handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DialogResult = null;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (!GameBoard.IsValidFen(this.InitialFen))
            {
                this.ShowErrorDialog("The specified FEN has invalid format or represents an invalid board position.");
                return;
            }

            this.DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        private void DefaultButton_Click(object sender, RoutedEventArgs e)
        {
            this.FenTextBox.Text = ChessConstants.DefaultInitialFen;
        }

        private void FromClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            this.FenTextBox.Text = Clipboard.GetText();
        }

        #endregion
    }
}