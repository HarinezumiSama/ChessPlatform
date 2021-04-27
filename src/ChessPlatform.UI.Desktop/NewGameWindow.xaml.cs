using System;
using System.Windows;
using System.Windows.Input;
using ChessPlatform.Engine;
using ChessPlatform.Logging;
using ChessPlatform.UI.Desktop.ViewModels;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop
{
    /// <summary>
    ///     Interaction logic for <b>NewGameWindow.xaml</b>.
    /// </summary>
    internal partial class NewGameWindow
    {
        public NewGameWindow([NotNull] ILogger logger, [NotNull] IOpeningBookProvider openingBookProvider)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (openingBookProvider is null)
            {
                throw new ArgumentNullException(nameof(openingBookProvider));
            }

            ViewModel = new NewGameWindowViewModel(logger, openingBookProvider);
            DataContext = ViewModel;

            Title = $@"New Game – {AppConstants.FullTitle}";

            InitializeComponent();

            var clipboardText = Clipboard.GetText();

            ViewModel.Fen = GameBoard.IsValidFen(clipboardText)
                ? clipboardText
                : ChessConstants.DefaultInitialFen;
        }

        public NewGameWindow()
            : this(FakeLogger.Instance, FakeOpeningBookProvider.Instance)
        {
            // Nothing to do
        }

        [NotNull]
        public NewGameWindowViewModel ViewModel { get; }

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