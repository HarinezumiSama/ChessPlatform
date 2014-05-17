using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ChessPlatform.UI.Desktop.Converters;
using ChessPlatform.UI.Desktop.ViewModels;
using Omnifactotum;

namespace ChessPlatform.UI.Desktop
{
    /// <summary>
    ///     Interaction logic for <b>GameWindow.xaml</b>.
    /// </summary>
    internal sealed partial class GameWindow
    {
        #region Constants and Fields

        private static readonly GridLength StarGridLength = new GridLength(1d, GridUnitType.Star);

        private bool _canExecuteCopyFenToClipboard = true;
        private Popup _promotionPopup;

        #endregion

        #region Constructors

        public GameWindow()
        {
            InitializeComponent();

            InitializeControls();
            this.ViewModel.SubscribeToChangeOf(() => this.ViewModel.CurrentGameBoard, this.OnCurrentGameBoardChanged);
        }

        #endregion

        #region Private Methods: General

        private static Position? GetSquarePosition(object element)
        {
            var source = element as FrameworkElement;
            return source == null || !(source.Tag is Position) ? null : (Position)source.Tag;
        }

        private void StartNewGame(bool confirm)
        {
            if (confirm)
            {
                var answer = this.ShowYesNoDialog("Do you want to start a new game?");
                if (answer != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            this.ViewModel.StartNewGame();
        }

        private void StartNewGameFromFenFromClipboard(bool confirm)
        {
            var fen = Clipboard.GetText();
            if (fen.IsNullOrWhiteSpace())
            {
                this.ShowWarningDialog("No valid FEN in the clipboard.");
                return;
            }

            if (confirm)
            {
                var answer = this.ShowYesNoDialog(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Do you want to start a new game using the following FEN?{0}"
                            + "{0}"
                            + "{1}",
                        Environment.NewLine,
                        fen));

                if (answer != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            try
            {
                this.ViewModel.StartNewGameFromFen(fen);
            }
            catch (ArgumentException)
            {
                this.ShowErrorDialog(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Invalid FEN:{0}{1}",
                        Environment.NewLine,
                        fen));
            }
        }

        private void UndoLastMove(bool confirm)
        {
            if (!this.ViewModel.CanUndoLastMove())
            {
                return;
            }

            if (confirm)
            {
                var answer = this.ShowYesNoDialog(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Do you want to undo the last move ({0})?",
                        this.ViewModel.CurrentGameBoard.PreviousMove));

                if (answer != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            this.ViewModel.UndoLastMove();
        }

        private void InitializeControls()
        {
            Enumerable
                .Range(0, ChessConstants.RankCount)
                .DoForEach(i => this.BoardGrid.RowDefinitions.Add(new RowDefinition { Height = StarGridLength }));

            Enumerable
                .Range(0, ChessConstants.FileCount)
                .DoForEach(i => this.BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = StarGridLength }));

            foreach (var position in ChessHelper.AllPositions)
            {
                var textBlock = new TextBlock
                {
                    Margin = new Thickness(),
                    Tag = position,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    DataContext = this.ViewModel.SquareViewModels[position]
                };

                textBlock.SetBinding(
                    TextBlock.BackgroundProperty,
                    new Binding(Factotum.For<BoardSquareViewModel>.GetPropertyName(obj => obj.Background)));

                textBlock.SetBinding(
                    TextBlock.ForegroundProperty,
                    new Binding(Factotum.For<BoardSquareViewModel>.GetPropertyName(obj => obj.Foreground)));

                textBlock.SetBinding(
                    TextBlock.TextProperty,
                    new Binding(Factotum.For<BoardSquareViewModel>.GetPropertyName(obj => obj.Text)));

                textBlock.MouseEnter += this.TextBlockSquare_MouseEnter;
                textBlock.MouseLeave += this.TextBlockSquare_MouseLeave;
                textBlock.MouseLeftButtonUp += this.TextBlockSquare_MouseLeftButtonUp;

                textBlock.SetValue(Grid.RowProperty, ChessConstants.RankRange.Upper - position.Rank);
                textBlock.SetValue(Grid.ColumnProperty, (int)position.File);

                this.BoardGrid.Children.Add(textBlock);
            }

            this.StatusLabel.SetBinding(
                ContentProperty,
                new Binding(Factotum.For<GameWindowViewModel>.GetPropertyName(obj => obj.CurrentGameBoard))
                {
                    Converter = StatusLabelTextConverter.Instance
                });

            InitializePromotionControls();
        }

        private void InitializePromotionControls()
        {
            this.PromotionContainerGrid.Visibility = Visibility.Collapsed;

            var promotionGrid = new Grid { Background = Brushes.Transparent };

            Enumerable
                .Range(0, ChessConstants.ValidPromotions.Count)
                .DoForEach(i => promotionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = StarGridLength }));

            var viewbox = new Viewbox
            {
                Child = new Border
                {
                    BorderThickness = new Thickness(1d),
                    BorderBrush = SystemColors.HighlightBrush,
                    Child = promotionGrid
                },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.Both
            };

            var popupContent = new Grid
            {
                Background = Brushes.Transparent,
                Children = { viewbox }
            };

            popupContent.SetBinding(
                HeightProperty,
                new Binding
                {
                    Source = this.BoardViewbox,
                    Path = new PropertyPath(ActualHeightProperty.Name),
                    Mode = BindingMode.OneWay,
                    Converter = new RatioDoubleConverter(0.2d)
                });

            _promotionPopup = new Popup
            {
                IsOpen = false,
                StaysOpen = false,
                AllowsTransparency = true,
                Placement = PlacementMode.Center,
                PlacementTarget = this.BoardViewbox,
                HorizontalOffset = 0,
                VerticalOffset = 0,
                PopupAnimation = PopupAnimation.None,
                Focusable = true,
                Opacity = 0d,
                Child = popupContent,
            };

            _promotionPopup.PreviewKeyDown += this.PromotionPopup_PreviewKeyDown;
            _promotionPopup.Opened += this.PromotionPopup_Opened;
            _promotionPopup.Closed += this.PromotionPopup_Closed;

            this.MainGrid.Children.Add(_promotionPopup);

            var validPromotions = ChessConstants.ValidPromotions.ToArray();
            for (var index = 0; index < validPromotions.Length; index++)
            {
                var promotion = validPromotions[index];

                var textBlock = new TextBlock
                {
                    Margin = new Thickness(),
                    Tag = promotion,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Foreground = new SolidColorBrush(Colors.Maroon),
                    Background = index % 2 == 0 ? Brushes.DeepSkyBlue : Brushes.LightBlue,
                    Text = UIHelper.PieceToCharMap[promotion].ToString(CultureInfo.InvariantCulture)
                };

                var textBlockBorder = new Border
                {
                    Child = textBlock,
                    BorderBrush = SystemColors.HighlightBrush,
                    BorderThickness = new Thickness(2d)
                };

                textBlockBorder.SetValue(Grid.RowProperty, 0);
                textBlockBorder.SetValue(Grid.ColumnProperty, index);

                textBlock.MouseLeftButtonUp +=
                    (sender, e) =>
                    {
                        _promotionPopup.Tag = promotion;
                        _promotionPopup.IsOpen = false;
                    };

                textBlock.MouseEnter += (sender, e) => textBlockBorder.BorderBrush = SystemColors.ActiveBorderBrush;
                textBlock.MouseLeave += (sender, e) => textBlockBorder.BorderBrush = SystemColors.HighlightBrush;

                promotionGrid.Children.Add(textBlockBorder);
            }
        }

        private void MakeMoveInternal(PieceMove move)
        {
            this.ViewModel.MakeMove(move);
        }

        private void MakeMove(PieceMove move)
        {
            var isPawnPromotion = this.ViewModel.CurrentGameBoard.IsPawnPromotion(move);
            if (isPawnPromotion)
            {
                move = move.MakePromotion(ChessHelper.DefaultPromotion);
            }

            if (!this.ViewModel.CurrentGameBoard.IsValidMove(move))
            {
                this.ViewModel.ResetSelectionMode();
                return;
            }

            if (isPawnPromotion)
            {
                QueryPawnPromotion(move);
                return;
            }

            MakeMoveInternal(move);
        }

        private void QueryPawnPromotion(PieceMove move)
        {
            this.PromotionContainerGrid.Width = this.BoardGrid.ActualWidth;
            this.PromotionContainerGrid.Height = this.BoardGrid.ActualHeight;
            this.PromotionContainerGrid.Visibility = Visibility.Visible;

            var promotionPopupClosed = new ValueContainer<EventHandler>();

            promotionPopupClosed.Value =
                (sender, args) =>
                {
                    _promotionPopup.Closed -= promotionPopupClosed.Value;

                    var promotion = _promotionPopup.Tag as PieceType?;
                    if (promotion.HasValue && promotion.Value != PieceType.None)
                    {
                        var promotionMove = move.MakePromotion(promotion.Value);
                        MakeMoveInternal(promotionMove);
                    }
                };

            _promotionPopup.Closed += promotionPopupClosed.Value;

            _promotionPopup.Tag = PieceType.None;
            _promotionPopup.IsOpen = true;
        }

        private void ClosePromotion(bool cancel)
        {
            this.PromotionContainerGrid.Visibility = Visibility.Collapsed;

            if (cancel)
            {
                _promotionPopup.Tag = PieceType.None;
            }

            _promotionPopup.IsOpen = false;
        }

        #endregion

        #region Private Methods: Event Handlers

        private void OnCurrentGameBoardChanged(object sender, EventArgs e)
        {
            var popupControl = this.BoardViewbox;

            var currentGameBoard = this.ViewModel.CurrentGameBoard;
            switch (currentGameBoard.State)
            {
                case GameState.Check:
                    popupControl.ShowInfoPopup("Check!");
                    break;

                case GameState.DoubleCheck:
                    popupControl.ShowInfoPopup("Double Check!");
                    break;

                case GameState.Checkmate:
                    popupControl.ShowInfoPopup(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Checkmate! {0}",
                            currentGameBoard.ResultString));
                    break;

                case GameState.Stalemate:
                    popupControl.ShowInfoPopup("Stalemate. Draw.");
                    break;
            }
        }

        private void TextBlockSquare_MouseEnter(object sender, MouseEventArgs args)
        {
            var position = GetSquarePosition(args.OriginalSource);
            if (!position.HasValue)
            {
                return;
            }

            if (this.ViewModel.SelectionMode == GameWindowSelectionMode.None)
            {
                this.ViewModel.CurrentTargetPosition = null;
                this.ViewModel.SetValidMovesOnlySelectionMode(position.Value);
                return;
            }

            this.ViewModel.CurrentTargetPosition = position.Value;
        }

        private void TextBlockSquare_MouseLeave(object sender, MouseEventArgs args)
        {
            if (this.ViewModel.SelectionMode == GameWindowSelectionMode.DisplayValidMovesOnly)
            {
                this.ViewModel.ResetSelectionMode();
            }
        }

        private void TextBlockSquare_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var position = GetSquarePosition(e.OriginalSource);
            if (!position.HasValue)
            {
                return;
            }

            var movingPiecePosition = this.ViewModel.CurrentSourcePosition;
            if (this.ViewModel.SelectionMode != GameWindowSelectionMode.MovingPieceSelected
                || !movingPiecePosition.HasValue)
            {
                this.ViewModel.SetMovingPieceSelectionMode(position.Value);
                return;
            }

            if (movingPiecePosition.Value == position.Value)
            {
                this.ViewModel.ResetSelectionMode();
                return;
            }

            var move = new PieceMove(movingPiecePosition.Value, position.Value);
            MakeMove(move);
        }

        private void Exit_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void NewGame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StartNewGame(true);
        }

        private void UndoLastMove_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            UndoLastMove(true);
        }

        private void UndoLastMove_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.ViewModel.CanUndoLastMove();
        }

        private void CopyFenToClipboard_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var fen = this.ViewModel.CurrentGameBoard.GetFen();
            Clipboard.SetText(fen);

            this.MainGrid.ShowInfoPopup(
                "FEN has been copied to the clipboard.",
                popupOpened: () => _canExecuteCopyFenToClipboard = false,
                popupClosed: () => _canExecuteCopyFenToClipboard = true);
        }

        private void CopyFenToClipboard_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _canExecuteCopyFenToClipboard;
        }

        private void NewGameFromFenFromClipboard_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StartNewGameFromFenFromClipboard(true);
        }

        private void PromotionPopup_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ClosePromotion(true);
            }
        }

        private void PromotionPopup_Opened(object sender, EventArgs e)
        {
            _promotionPopup.Focus();
        }

        private void PromotionPopup_Closed(object sender, EventArgs e)
        {
            ClosePromotion(false);
        }

        #endregion
    }
}