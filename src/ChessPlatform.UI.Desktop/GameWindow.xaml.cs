using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using ChessPlatform.GamePlay;
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

            Title = App.Title;

            InitializeControls(false);
            InitializePromotionControls();

            ViewModel.SubscribeToChangeOf(() => ViewModel.CurrentGameBoard, OnCurrentGameBoardChanged);
            ViewModel.SubscribeToChangeOf(() => ViewModel.IsReversedView, OnIsReversedViewChanged);
            ViewModel.SubscribeToChangeOf(() => ViewModel.IsComputerPlayerActive, OnIsComputerPlayerActiveChanged);

            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
        }

        #endregion

        #region Protected Methods

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            ViewModel.Play();
        }

        #endregion

        #region Private Methods: General

        private static Square? GetSquare(object element)
        {
            var source = element as FrameworkElement;
            return source?.Tag as Square?;
        }

        private void StartNewGame()
        {
            var newGameWindow = new NewGameWindow { Owner = this };
            if (!newGameWindow.ShowDialog().GetValueOrDefault())
            {
                return;
            }

            ViewModel.InitializeNewGame(
                newGameWindow.ViewModel.Fen,
                newGameWindow.WhitePlayer,
                newGameWindow.BlackPlayer);

            ViewModel.Play();
        }

        private void UndoLastMove(bool confirm)
        {
            if (!ViewModel.CanUndoLastMove())
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

            ViewModel.UndoLastMove();
        }

        private void InitializeControls(bool reversedView)
        {
            const double CoordinateSymbolSizeRatio = 0.5d;

            BoardGrid.Children.OfType<FrameworkElement>().DoForEach(obj => obj.DataContext = null);
            BoardGrid.ClearGrid();

            RankSymbolGrid.ClearGrid();
            FileSymbolGrid.ClearGrid();

            Enumerable
                .Range(0, ChessConstants.RankCount)
                .DoForEach(i => BoardGrid.RowDefinitions.Add(new RowDefinition { Height = StarGridLength }));

            Enumerable
                .Range(0, ChessConstants.FileCount)
                .DoForEach(i => BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = StarGridLength }));

            foreach (var square in ChessHelper.AllSquares)
            {
                var row = reversedView
                    ? ChessConstants.RankRange.Lower + square.Rank
                    : ChessConstants.RankRange.Upper - square.Rank;

                var column = reversedView
                    ? ChessConstants.FileRange.Upper - square.File
                    : ChessConstants.FileRange.Lower + square.File;

                var label = new Label
                {
                    Margin = new Thickness(),
                    Padding = new Thickness(),
                    Tag = square,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontSize = 12,
                    FontFamily = new FontFamily("Merida"),
                    Width = 16,
                    Height = 16
                };

                label.SetBinding(BackgroundProperty, new Binding(nameof(BoardSquareViewModel.Background)));
                label.SetBinding(ForegroundProperty, new Binding(nameof(BoardSquareViewModel.Foreground)));
                label.SetBinding(ContentProperty, new Binding(nameof(BoardSquareViewModel.Text)));

                label.MouseEnter += BoardSquare_MouseEnter;
                label.MouseLeave += BoardSquare_MouseLeave;
                label.MouseLeftButtonUp += BoardSquare_MouseLeftButtonUp;

                var border = new Border
                {
                    Child = label,
                    Margin = new Thickness(),
                    Padding = new Thickness(),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    DataContext = ViewModel.SquareViewModels[square],
                    BorderThickness = new Thickness(1)
                };

                border.SetValue(Grid.RowProperty, row);
                border.SetValue(Grid.ColumnProperty, column);

                border.SetBinding(Border.BorderBrushProperty, new Binding(nameof(BoardSquareViewModel.BorderBrush)));
                border.SetBinding(Border.BackgroundProperty, new Binding(nameof(BoardSquareViewModel.BorderBrush)));

                BoardGrid.Children.Add(border);
            }

            Enumerable
                .Range(0, ChessConstants.RankCount)
                .DoForEach(i => RankSymbolGrid.RowDefinitions.Add(new RowDefinition { Height = StarGridLength }));

            for (var rank = 0; rank < ChessConstants.RankCount; rank++)
            {
                var textBlock = new TextBlock
                {
                    Margin = new Thickness(1),
                    LayoutTransform = new ScaleTransform(CoordinateSymbolSizeRatio, CoordinateSymbolSizeRatio),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Text = (rank + 1).ToString(CultureInfo.InvariantCulture),
                    Foreground = Brushes.CadetBlue
                };

                var row = reversedView
                    ? ChessConstants.RankRange.Lower + rank
                    : ChessConstants.RankRange.Upper - rank;

                textBlock.SetValue(Grid.RowProperty, row);
                textBlock.SetValue(Grid.ColumnProperty, 0);

                RankSymbolGrid.Children.Add(textBlock);
            }

            Enumerable
                .Range(0, ChessConstants.FileCount)
                .DoForEach(
                    i => FileSymbolGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = StarGridLength }));

            for (var file = 0; file < ChessConstants.FileCount; file++)
            {
                var textBlock = new TextBlock
                {
                    Margin = new Thickness(1),
                    LayoutTransform = new ScaleTransform(CoordinateSymbolSizeRatio, CoordinateSymbolSizeRatio),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Text = ((char)('a' + file)).ToString(CultureInfo.InvariantCulture),
                    Foreground = Brushes.CadetBlue
                };

                var column = reversedView
                    ? ChessConstants.FileRange.Upper - file
                    : ChessConstants.FileRange.Lower + file;

                textBlock.SetValue(Grid.RowProperty, 0);
                textBlock.SetValue(Grid.ColumnProperty, column);

                FileSymbolGrid.Children.Add(textBlock);
            }

            StatusLabel.SetBinding(
                ContentProperty,
                new Binding(nameof(GameWindowViewModel.CurrentGameBoard))
                {
                    Converter = StatusLabelTextConverter.Instance
                });
        }

        private void InitializePromotionControls()
        {
            PromotionContainerGrid.Visibility = Visibility.Collapsed;

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
                    Source = BoardViewbox,
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
                PlacementTarget = BoardGridBorder,
                HorizontalOffset = 0,
                VerticalOffset = 0,
                PopupAnimation = PopupAnimation.None,
                Focusable = true,
                Opacity = 0d,
                Child = popupContent,
            };

            _promotionPopup.PreviewKeyDown += PromotionPopup_PreviewKeyDown;
            _promotionPopup.Opened += PromotionPopup_Opened;
            _promotionPopup.Closed += PromotionPopup_Closed;

            MainGrid.Children.Add(_promotionPopup);

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
                    TextAlignment = TextAlignment.Center,
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

        private void MakeMoveInternal(GameMove move)
        {
            ViewModel.MakeMove(move);
        }

        private void MakeMove(GameMove move)
        {
            var currentGameBoard = ViewModel.CurrentGameBoard;
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
                ViewModel.ResetSelectionMode();
                return;
            }

            if (isPawnPromotion)
            {
                QueryPawnPromotion(move);
                return;
            }

            MakeMoveInternal(move);
        }

        private void QueryPawnPromotion(GameMove move)
        {
            PromotionContainerGrid.Width = BoardGrid.ActualWidth;
            PromotionContainerGrid.Height = BoardGrid.ActualHeight;
            PromotionContainerGrid.Visibility = Visibility.Visible;

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
            PromotionContainerGrid.Visibility = Visibility.Collapsed;

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
            var popupControl = BoardGridBorder;

            var currentGameBoard = ViewModel.CurrentGameBoard;
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
                    popupControl.ShowInfoPopup($@"Checkmate! {currentGameBoard.ResultString}");
                    break;

                case GameState.Stalemate:
                    popupControl.ShowInfoPopup("Stalemate. Draw.");
                    break;

                default:
                    switch (ViewModel.GameManagerResult)
                    {
                        case GameResult.Draw:
                            string drawType;
                            switch (ViewModel.GameManagerAutoDrawType)
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

                            popupControl.ShowInfoPopup($@"Draw: {drawType}.");

                            break;
                    }

                    break;
            }
        }

        private void OnIsReversedViewChanged(object sender, EventArgs e)
        {
            InitializeControls(ViewModel.IsReversedView);
        }

        private void OnIsComputerPlayerActiveChanged(object sender, EventArgs e)
        {
            var isComputerPlayerActive = ViewModel.IsComputerPlayerActive;
            if (isComputerPlayerActive && !ViewModel.GameManagerResult.HasValue)
            {
                TaskbarItemInfoInstance.ProgressValue = 0.5d;
                TaskbarItemInfoInstance.ProgressState = TaskbarItemProgressState.Paused;
            }
            else
            {
                TaskbarItemInfoInstance.ProgressValue = 0;
                TaskbarItemInfoInstance.ProgressState = TaskbarItemProgressState.None;
            }
        }

        private void BoardSquare_MouseEnter(object sender, MouseEventArgs args)
        {
            var square = GetSquare(args.OriginalSource);
            if (!square.HasValue)
            {
                return;
            }

            switch (ViewModel.SelectionMode)
            {
                case GameWindowSelectionMode.None:
                    return;

                case GameWindowSelectionMode.Default:
                    ViewModel.CurrentTargetSquare = null;
                    ViewModel.SetValidMovesOnlySelectionMode(square.Value);
                    return;
            }

            ViewModel.CurrentTargetSquare = square.Value;
        }

        private void BoardSquare_MouseLeave(object sender, MouseEventArgs args)
        {
            switch (ViewModel.SelectionMode)
            {
                case GameWindowSelectionMode.None:
                    return;

                case GameWindowSelectionMode.DisplayValidMovesOnly:
                    ViewModel.ResetSelectionMode();
                    break;
            }
        }

        private void BoardSquare_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var square = GetSquare(e.Source);
            if (!square.HasValue)
            {
                return;
            }

            if (ViewModel.SelectionMode == GameWindowSelectionMode.None)
            {
                return;
            }

            var movingPieceSquare = ViewModel.CurrentSourceSquare;
            if (ViewModel.SelectionMode != GameWindowSelectionMode.MovingPieceSelected
                || !movingPieceSquare.HasValue)
            {
                ViewModel.SetMovingPieceSelectionMode(square.Value);
                return;
            }

            if (movingPieceSquare.Value == square.Value)
            {
                ViewModel.ResetSelectionMode();
                return;
            }

            var move = new GameMove(movingPieceSquare.Value, square.Value);
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
            e.CanExecute = ViewModel.CanUndoLastMove();
        }

        private void CopyFenToClipboard_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var currentGameBoard = ViewModel.CurrentGameBoard;
            if (currentGameBoard == null)
            {
                return;
            }

            var fen = currentGameBoard.GetFen();
            Clipboard.SetText(fen);

            MainGrid.ShowInfoPopup(
                "FEN has been copied to the clipboard.",
                popupOpened: () => _canExecuteCopyFenToClipboard = false,
                popupClosed: () => _canExecuteCopyFenToClipboard = true);
        }

        private void CopyFenToClipboard_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var currentGameBoard = ViewModel.CurrentGameBoard;
            if (currentGameBoard == null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = _canExecuteCopyFenToClipboard;
        }

        private void CopyHistoryToClipboard_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var moveHistory = ViewModel.MoveHistory;
            Clipboard.SetText(moveHistory);

            MainGrid.ShowInfoPopup(
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
            ViewModel.IsReversedView = !ViewModel.IsReversedView;
        }

        private void ShowPlayerFeedback_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.ShouldShowPlayerFeedback = !ViewModel.ShouldShowPlayerFeedback;
        }

        private void ShowPlayersTimers_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.ShouldShowPlayersTimers = !ViewModel.ShouldShowPlayersTimers;
        }

        private void RequestMoveNow_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.RequestMoveNow();
        }

        private void RequestMoveNow_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ViewModel.CanRequestMoveNow;
        }

        #endregion
    }
}