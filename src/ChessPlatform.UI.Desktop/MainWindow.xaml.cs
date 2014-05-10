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
using Omnifactotum;

namespace ChessPlatform.UI.Desktop
{
    /// <summary>
    ///     Interaction logic for <b>MainWindow.xaml</b>.
    /// </summary>
    public sealed partial class MainWindow
    {
        #region Constants and Fields

        private static readonly GridLength StarGridLength = new GridLength(1d, GridUnitType.Star);

        private readonly Dictionary<Position, TextBlock> _positionToSquareElementMap =
            new Dictionary<Position, TextBlock>();

        private readonly Stack<BoardState> _previosBoardStates = new Stack<BoardState>();
        private BoardState _currentBoardState = new BoardState();

        private Position? _movingPiecePosition;

        private bool _canExecuteCopyFenToClipboard = true;
        private Popup _promotionPopup;

        #endregion

        #region Constructors

        public MainWindow()
        {
            InitializeComponent();

            InitializeControls();
            RedrawBoardState(false);
        }

        #endregion

        #region Private Methods: General

        private static Position? GetSquarePosition(object element)
        {
            var source = element as FrameworkElement;
            return source == null || !(source.Tag is Position) ? null : (Position)source.Tag;
        }

        private static void ResetSquareElementColor(TextBlock squareElement)
        {
            var position = GetSquarePosition(squareElement);
            if (!position.HasValue)
            {
                throw new ArgumentException(@"Invalid square element.", "squareElement");
            }

            squareElement.Background = UIHelper.GetSquareBrush(position.Value, SquareMode.Default);
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

            _currentBoardState = new BoardState();
            _previosBoardStates.Clear();

            RedrawBoardState(true);
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

            BoardState boardState;
            try
            {
                boardState = new BoardState(fen);
            }
            catch (ArgumentException)
            {
                this.ShowErrorDialog(
                    string.Format(CultureInfo.InvariantCulture, "Invalid FEN:{0}{1}", Environment.NewLine, fen));

                return;
            }

            _currentBoardState = boardState;
            _previosBoardStates.Clear();

            RedrawBoardState(true);
        }

        private void UndoLastMove(bool confirm)
        {
            if (_previosBoardStates.Count == 0)
            {
                return;
            }

            if (confirm)
            {
                var answer = this.ShowYesNoDialog(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Do you want to undo the last move ({0})?",
                        _currentBoardState.PreviousMove));

                if (answer != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            _currentBoardState = _previosBoardStates.Pop();
            RedrawBoardState(true);
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
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                ResetSquareElementColor(textBlock);

                textBlock.MouseEnter += this.TextBlockSquare_MouseEnter;
                textBlock.MouseLeave += this.TextBlockSquare_MouseLeave;
                textBlock.MouseLeftButtonDown += this.TextBlockSquare_MouseLeftButtonDown;
                textBlock.MouseLeftButtonUp += this.TextBlockSquare_MouseLeftButtonUp;

                _positionToSquareElementMap.Add(position, textBlock);

                textBlock.SetValue(Grid.RowProperty, ChessConstants.RankRange.Upper - position.Rank);
                textBlock.SetValue(Grid.ColumnProperty, (int)position.File);

                this.BoardGrid.Children.Add(textBlock);
            }

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

        private void RedrawBoardState(bool showStatePopup)
        {
            _movingPiecePosition = null;

            foreach (var position in ChessHelper.AllPositions)
            {
                var pieceInfo = _currentBoardState.GetPieceInfo(position);
                var ch = UIHelper.PieceToCharMap[pieceInfo.PieceType];

                var textBlock = _positionToSquareElementMap[position];
                ResetSquareElementColor(textBlock);
                textBlock.Text = ch.ToString(CultureInfo.InvariantCulture);
                textBlock.Foreground = pieceInfo.Color == PieceColor.White ? Brushes.DarkKhaki : Brushes.Black;
            }

            this.StatusLabel.Content = string.Format(
                CultureInfo.InvariantCulture,
                "Move: {0}. Turn: {1}. State: {2}. Valid moves: {3}. Result: {4}",
                _currentBoardState.FullMoveIndex,
                _currentBoardState.ActiveColor,
                _currentBoardState.State,
                _currentBoardState.ValidMoves.Count,
                _currentBoardState.ResultString);

            if (showStatePopup)
            {
                switch (_currentBoardState.State)
                {
                    case GameState.Check:
                        this.MainGrid.ShowInfoPopup("Check!");
                        break;

                    case GameState.DoubleCheck:
                        this.MainGrid.ShowInfoPopup("Double Check!");
                        break;

                    case GameState.Checkmate:
                        this.MainGrid.ShowInfoPopup(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Checkmate! {0}",
                                _currentBoardState.ResultString));
                        break;

                    case GameState.Stalemate:
                        this.MainGrid.ShowInfoPopup("Stalemate. Draw.");
                        break;

                    case GameState.ForcedDrawInsufficientMaterial:
                        this.MainGrid.ShowInfoPopup("Draw (insufficient material).");
                        break;
                }
            }
        }

        private void DisplayValidMoves(Position position)
        {
            var sourceSquareElement = _positionToSquareElementMap[position];

            var moves = _currentBoardState.GetValidMovesBySource(position);
            foreach (var move in moves)
            {
                var squareElement = _positionToSquareElementMap[move.To];
                squareElement.Background = UIHelper.GetSquareBrush(move.To, SquareMode.ValidMoveTarget);
            }

            if (moves.Length != 0)
            {
                sourceSquareElement.Background = UIHelper.GetSquareBrush(position, SquareMode.ValidMoveSource);
            }
        }

        private void SetMovingPiecePosition(Position? movingPiecePosition)
        {
            ////Trace.TraceInformation(
            ////    "*** [{0}] {1}{2}{3}{2}{2}",
            ////    MethodBase.GetCurrentMethod().Name,
            ////    movingPiecePosition.ToStringSafely("None"),
            ////    Environment.NewLine,
            ////    new StackTrace());

            _movingPiecePosition = movingPiecePosition;

            if (!_movingPiecePosition.HasValue
                || _currentBoardState.GetPiece(_movingPiecePosition.Value) == Piece.None
                || _currentBoardState.GetValidMovesBySource(_movingPiecePosition.Value).Length == 0)
            {
                RedrawBoardState(false);
                return;
            }

            DisplayValidMoves(_movingPiecePosition.Value);

            var squareElement = _positionToSquareElementMap[_movingPiecePosition.Value];

            squareElement.Background = UIHelper.GetSquareBrush(
                _movingPiecePosition.Value,
                SquareMode.CurrentMoveSource);
        }

        private void MakeMoveInternal(PieceMove move)
        {
            var newBoardState = _currentBoardState.MakeMove(move).EnsureNotNull();

            _previosBoardStates.Push(_currentBoardState);
            _currentBoardState = newBoardState;

            RedrawBoardState(true);
        }

        private void MakeMove(PieceMove move)
        {
            if (_currentBoardState.IsPawnPromotion(move))
            {
                move = move.MakePromotion(ChessHelper.DefaultPromotion);
            }

            if (!_currentBoardState.IsValidMove(move))
            {
                SetMovingPiecePosition(null);
                return;
            }

            if (_currentBoardState.IsPawnPromotion(move))
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
            SetMovingPiecePosition(null);
        }

        #endregion

        #region Private Methods: Event Handlers

        private void TextBlockSquare_MouseEnter(object sender, MouseEventArgs args)
        {
            var position = GetSquarePosition(args.OriginalSource);
            if (!position.HasValue)
            {
                return;
            }

            if (!_movingPiecePosition.HasValue)
            {
                DisplayValidMoves(position.Value);
                return;
            }

            if (_movingPiecePosition.Value == position.Value)
            {
                return;
            }

            var move = new PieceMove(_movingPiecePosition.Value, position.Value);
            if (_currentBoardState.IsValidMove(move))
            {
                var squareElement = _positionToSquareElementMap[position.Value];
                squareElement.Background = UIHelper.GetSquareBrush(position.Value, SquareMode.CurrentMoveTarget);
            }
        }

        private void TextBlockSquare_MouseLeave(object sender, MouseEventArgs args)
        {
            var position = GetSquarePosition(args.OriginalSource);
            if (!position.HasValue)
            {
                return;
            }

            if (!_movingPiecePosition.HasValue)
            {
                RedrawBoardState(false);
                return;
            }

            SetMovingPiecePosition(_movingPiecePosition.Value);
        }

        private void TextBlockSquare_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ////var position = GetSquarePosition(e.OriginalSource);
            ////if (!position.HasValue)
            ////{
            ////    return;
            ////}

            ////Trace.TraceInformation(position.Value.ToString());
        }

        private void TextBlockSquare_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var position = GetSquarePosition(e.OriginalSource);
            if (!position.HasValue)
            {
                return;
            }

            if (!_movingPiecePosition.HasValue)
            {
                SetMovingPiecePosition(position);
                return;
            }

            if (_movingPiecePosition.Value == position.Value)
            {
                SetMovingPiecePosition(null);
                return;
            }

            var move = new PieceMove(_movingPiecePosition.Value, position.Value);
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
            e.CanExecute = _previosBoardStates.Count != 0;
        }

        private void CopyFenToClipboard_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var fen = _currentBoardState.GetFen();
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