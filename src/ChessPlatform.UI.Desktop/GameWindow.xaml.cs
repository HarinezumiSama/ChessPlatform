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

        private bool _canExecuteCopyFenToClipboard;
        private bool _canExecuteCopyHistoryToClipboard;
        private Popup _promotionPopup;

        #endregion

        #region Constructors

        public GameWindow()
        {
            InitializeComponent();

            _canExecuteCopyFenToClipboard = true;
            _canExecuteCopyHistoryToClipboard = true;

            this.Title = App.Title;

            InitializeControls(false);
            InitializePromotionControls();

            this.ViewModel.SubscribeToChangeOf(() => this.ViewModel.CurrentGameBoard, this.OnCurrentGameBoardChanged);
            this.ViewModel.SubscribeToChangeOf(() => this.ViewModel.IsReversedView, this.OnIsReversedViewChanged);
        }

        #endregion

        #region Protected Methods

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            this.ViewModel.Play();
        }

        #endregion

        #region Private Methods: General

        private static Position? GetSquarePosition(object element)
        {
            var source = element as FrameworkElement;
            return source == null || !(source.Tag is Position) ? null : (Position)source.Tag;
        }

        private void StartNewGame()
        {
            var newGameWindow = new NewGameWindow { Owner = this };
            if (!newGameWindow.ShowDialog().GetValueOrDefault())
            {
                return;
            }

            this.ViewModel.InitializeNewGame(
                newGameWindow.InitialFen,
                newGameWindow.WhitePlayer,
                newGameWindow.BlackPlayer);

            this.ViewModel.Play();
        }

        private void UndoLastMove(bool confirm)
        {
            if (!this.ViewModel.CanUndoLastMove())
            {
                return;
            }

            if (confirm)
            {
                var answer = this.ShowYesNoDialog("Do you want to undo the last move?");
                if (answer != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            this.ViewModel.UndoLastMove();
        }

        private void InitializeControls(bool reversedView)
        {
            this.BoardGrid.Children.OfType<FrameworkElement>().DoForEach(obj => obj.DataContext = null);
            this.BoardGrid.ClearGrid();

            this.RankSymbolGrid.ClearGrid();
            this.FileSymbolGrid.ClearGrid();

            Enumerable
                .Range(0, ChessConstants.RankCount)
                .DoForEach(i => this.BoardGrid.RowDefinitions.Add(new RowDefinition { Height = StarGridLength }));

            Enumerable
                .Range(0, ChessConstants.FileCount)
                .DoForEach(i => this.BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = StarGridLength }));

            foreach (var position in ChessHelper.AllPositions)
            {
                var row = reversedView
                    ? ChessConstants.RankRange.Lower + position.Rank
                    : ChessConstants.RankRange.Upper - position.Rank;

                var column = reversedView
                    ? ChessConstants.FileRange.Upper - position.File
                    : ChessConstants.FileRange.Lower + position.File;

                var textBlock = new TextBlock
                {
                    Margin = new Thickness(),
                    Tag = position,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
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

                var border = new Border
                {
                    Child = textBlock,
                    Margin = new Thickness(),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    DataContext = this.ViewModel.SquareViewModels[position],
                    BorderThickness = new Thickness(1)
                };

                border.SetValue(Grid.RowProperty, row);
                border.SetValue(Grid.ColumnProperty, column);

                border.SetBinding(
                    Border.BorderBrushProperty,
                    new Binding(Factotum.For<BoardSquareViewModel>.GetPropertyName(obj => obj.BorderBrush)));

                this.BoardGrid.Children.Add(border);
            }

            Enumerable
                .Range(0, ChessConstants.RankCount)
                .DoForEach(i => this.RankSymbolGrid.RowDefinitions.Add(new RowDefinition { Height = StarGridLength }));

            for (var rank = 0; rank < ChessConstants.RankCount; rank++)
            {
                var textBlock = new TextBlock
                {
                    Margin = new Thickness(1),
                    LayoutTransform = new ScaleTransform(0.25d, 0.25d),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = (rank + 1).ToString(CultureInfo.InvariantCulture),
                    Foreground = Brushes.CadetBlue
                };

                var row = reversedView
                    ? ChessConstants.RankRange.Lower + rank
                    : ChessConstants.RankRange.Upper - rank;

                textBlock.SetValue(Grid.RowProperty, row);
                textBlock.SetValue(Grid.ColumnProperty, 0);

                this.RankSymbolGrid.Children.Add(textBlock);
            }

            Enumerable
                .Range(0, ChessConstants.FileCount)
                .DoForEach(
                    i => this.FileSymbolGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = StarGridLength }));

            for (var file = 0; file < ChessConstants.FileCount; file++)
            {
                var textBlock = new TextBlock
                {
                    Margin = new Thickness(1),
                    LayoutTransform = new ScaleTransform(0.25d, 0.25d),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = ((char)('A' + file)).ToString(CultureInfo.InvariantCulture),
                    Foreground = Brushes.CadetBlue
                };

                var column = reversedView
                    ? ChessConstants.FileRange.Upper - file
                    : ChessConstants.FileRange.Lower + file;

                textBlock.SetValue(Grid.RowProperty, 0);
                textBlock.SetValue(Grid.ColumnProperty, column);

                this.FileSymbolGrid.Children.Add(textBlock);
            }

            this.StatusLabel.SetBinding(
                ContentProperty,
                new Binding(Factotum.For<GameWindowViewModel>.GetPropertyName(obj => obj.CurrentGameBoard))
                {
                    Converter = StatusLabelTextConverter.Instance
                });
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
                    Source = this.BoardGridBorder,
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
                PlacementTarget = this.BoardGridBorder,
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
                    Text = UIHelper.PieceToSymbolMap[promotion]
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
            var currentGameBoard = this.ViewModel.CurrentGameBoard;
            if (currentGameBoard == null)
            {
                return;
            }

            var isPawnPromotion = currentGameBoard.IsPawnPromotionMove(move);
            if (isPawnPromotion)
            {
                move = move.MakePromotion(ChessHelper.DefaultPromotion);
            }

            if (!currentGameBoard.IsValidMove(move))
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
            var popupControl = this.BoardGridBorder;

            var currentGameBoard = this.ViewModel.CurrentGameBoard;
            if (currentGameBoard == null)
            {
                return;
            }

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

                default:
                    switch (this.ViewModel.GameManagerResult)
                    {
                        case GameResult.Draw:
                            string drawType;
                            switch (this.ViewModel.GameManagerAutoDrawType)
                            {
                                case AutoDrawType.InsufficientMaterial:
                                    drawType = "insufficient material";
                                    break;

                                case AutoDrawType.ThreefoldRepetition:
                                    drawType = "threefold repetition";
                                    break;

                                case AutoDrawType.FiftyMoveRule:
                                    drawType = "50-move rule";
                                    break;

                                default:
                                    drawType = "?";
                                    break;
                            }

                            popupControl.ShowInfoPopup(
                                string.Format(CultureInfo.InvariantCulture, "Draw: {0}.", drawType));

                            break;
                    }

                    break;
            }
        }

        private void OnIsReversedViewChanged(object sender, EventArgs e)
        {
            InitializeControls(this.ViewModel.IsReversedView);
        }

        private void TextBlockSquare_MouseEnter(object sender, MouseEventArgs args)
        {
            var position = GetSquarePosition(args.OriginalSource);
            if (!position.HasValue)
            {
                return;
            }

            switch (this.ViewModel.SelectionMode)
            {
                case GameWindowSelectionMode.None:
                    return;

                case GameWindowSelectionMode.Default:
                    this.ViewModel.CurrentTargetPosition = null;
                    this.ViewModel.SetValidMovesOnlySelectionMode(position.Value);
                    return;
            }

            this.ViewModel.CurrentTargetPosition = position.Value;
        }

        private void TextBlockSquare_MouseLeave(object sender, MouseEventArgs args)
        {
            switch (this.ViewModel.SelectionMode)
            {
                case GameWindowSelectionMode.None:
                    return;

                case GameWindowSelectionMode.DisplayValidMovesOnly:
                    this.ViewModel.ResetSelectionMode();
                    break;
            }
        }

        private void TextBlockSquare_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var position = GetSquarePosition(e.OriginalSource);
            if (!position.HasValue)
            {
                return;
            }

            if (this.ViewModel.SelectionMode == GameWindowSelectionMode.None)
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
            StartNewGame();
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
            var currentGameBoard = this.ViewModel.CurrentGameBoard;
            if (currentGameBoard == null)
            {
                return;
            }

            var fen = currentGameBoard.GetFen();
            Clipboard.SetText(fen);

            this.MainGrid.ShowInfoPopup(
                "FEN has been copied to the clipboard.",
                popupOpened: () => _canExecuteCopyFenToClipboard = false,
                popupClosed: () => _canExecuteCopyFenToClipboard = true);
        }

        private void CopyFenToClipboard_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var currentGameBoard = this.ViewModel.CurrentGameBoard;
            if (currentGameBoard == null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = _canExecuteCopyFenToClipboard;
        }

        private void CopyHistoryToClipboard_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var moveHistory = this.ViewModel.MoveHistory;
            Clipboard.SetText(moveHistory);

            this.MainGrid.ShowInfoPopup(
                "History has been copied to the clipboard.",
                popupOpened: () => _canExecuteCopyHistoryToClipboard = false,
                popupClosed: () => _canExecuteCopyHistoryToClipboard = true);
        }

        private void CopyHistoryToClipboard_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _canExecuteCopyHistoryToClipboard;
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

        private void ReversedBoardView_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.ViewModel.IsReversedView = !this.ViewModel.IsReversedView;
        }

        #endregion
    }
}