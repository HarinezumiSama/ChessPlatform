using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Omnifactotum.Annotations;

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

            this.ViewModel.Fen = GameBoard.IsValidFen(clipboardText)
                ? clipboardText
                : ChessConstants.DefaultInitialFen;
        }

        #endregion

        #region Public Properties

        [NotNull]
        public IPlayerInfo WhitePlayer
        {
            get
            {
                return this.ViewModel
                    .WhitePlayerViewModel
                    .SelectedPlayerControlItem
                    .EnsureNotNull()
                    .Value
                    .EnsureNotNull();
            }
        }

        [NotNull]
        public IPlayerInfo BlackPlayer
        {
            get
            {
                return this.ViewModel
                    .BlackPlayerViewModel
                    .SelectedPlayerControlItem
                    .EnsureNotNull()
                    .Value
                    .EnsureNotNull();
            }
        }

        #endregion

        #region Private Methods: Event Handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DialogResult = null;
        }

        private void Start_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.ViewModel.IsValid();
        }

        private void Start_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!this.ViewModel.IsValid())
            {
                this.DialogResult = null;
                return;
            }

            this.DialogResult = true;
            Close();
        }

        private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        private void SetDefaultFenButton_Click(object sender, RoutedEventArgs e)
        {
            this.ViewModel.Fen = ChessConstants.DefaultInitialFen;
        }

        private void PasteFenFromClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            this.ViewModel.Fen = Clipboard.GetText();
        }

        #endregion
    }
}