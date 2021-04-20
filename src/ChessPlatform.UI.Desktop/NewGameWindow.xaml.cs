using System;
using System.Collections.Generic;
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
        public NewGameWindow()
        {
            InitializeComponent();

            Title = $@"New Game – {App.Title}";

            var clipboardText = Clipboard.GetText();

            ViewModel.Fen = GameBoard.IsValidFen(clipboardText)
                ? clipboardText
                : ChessConstants.DefaultInitialFen;
        }

        [NotNull]
        public IPlayerInfo WhitePlayer
            => ViewModel.WhitePlayerViewModel.SelectedPlayerControlItem.EnsureNotNull().Value.EnsureNotNull();

        [NotNull]
        public IPlayerInfo BlackPlayer
            => ViewModel.BlackPlayerViewModel.SelectedPlayerControlItem.EnsureNotNull().Value.EnsureNotNull();

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
    }
}