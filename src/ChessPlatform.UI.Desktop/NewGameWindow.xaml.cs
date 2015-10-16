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

            Title = string.Format(CultureInfo.InvariantCulture, "New Game – {0}", App.Title);

            var clipboardText = Clipboard.GetText();

            ViewModel.Fen = GameBoard.IsValidFen(clipboardText)
                ? clipboardText
                : ChessConstants.DefaultInitialFen;
        }

        #endregion

        #region Public Properties

        [NotNull]
        public IPlayerInfo WhitePlayer
            => ViewModel.WhitePlayerViewModel.SelectedPlayerControlItem.EnsureNotNull().Value.EnsureNotNull();

        [NotNull]
        public IPlayerInfo BlackPlayer
            => ViewModel.BlackPlayerViewModel.SelectedPlayerControlItem.EnsureNotNull().Value.EnsureNotNull();

        #endregion

        #region Private Methods: Event Handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DialogResult = null;
        }

        private void Start_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ViewModel.IsValid();
        }

        private void Start_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!ViewModel.IsValid())
            {
                DialogResult = null;
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SetDefaultFenButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Fen = ChessConstants.DefaultInitialFen;
        }

        private void PasteFenFromClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Fen = Clipboard.GetText();
        }

        #endregion
    }
}