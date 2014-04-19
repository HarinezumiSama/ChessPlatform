using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessPlatform.UI.Desktop
{
    /// <summary>
    ///     Interaction logic for <b>MainWindow.xaml</b>.
    /// </summary>
    public sealed partial class MainWindow
    {
        #region Constants and Fields

        private readonly Dictionary<Position, TextBlock> _positionToSquareElementMap =
            new Dictionary<Position, TextBlock>();

        private readonly Stack<BoardState> _previosBoardStates = new Stack<BoardState>();
        private BoardState _currentBoardState = new BoardState();

        private Position? _movingPiecePosition;

        private bool _canExecuteCopyFenToClipboard = true;

        #endregion

        #region Constructors

        public MainWindow()
        {
            InitializeComponent();

            InitializeControls();
            RedrawBoardState();
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

            RedrawBoardState();
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

            RedrawBoardState();
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
            RedrawBoardState();
        }

        private void InitializeControls()
        {
            var starGridLength = new GridLength(1d, GridUnitType.Star);

            Enumerable
                .Range(0, ChessConstants.RankCount)
                .DoForEach(i => this.BoardGrid.RowDefinitions.Add(new RowDefinition { Height = starGridLength }));

            Enumerable
                .Range(0, ChessConstants.FileCount)
                .DoForEach(i => this.BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = starGridLength }));

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

                var viewbox = new Viewbox
                {
                    Child = textBlock,
                    Margin = new Thickness(),
                    Tag = position,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Stretch = Stretch.Fill,
                    StretchDirection = StretchDirection.Both
                };

                viewbox.SetValue(Grid.RowProperty, ChessConstants.RankRange.Upper - position.Rank);
                viewbox.SetValue(Grid.ColumnProperty, (int)position.File);

                this.BoardGrid.Children.Add(viewbox);
            }
        }

        private void RedrawBoardState()
        {
            _movingPiecePosition = null;

            foreach (var position in ChessHelper.AllPositions)
            {
                var pieceInfo = _currentBoardState.GetPieceInfo(position);
                var ch = UIHelper.PieceToCharMap[pieceInfo.Piece];

                var textBlock = _positionToSquareElementMap[position];
                ResetSquareElementColor(textBlock);
                textBlock.Text = ch.ToString(CultureInfo.InvariantCulture);
            }

            this.StatusLabel.Content = string.Format(
                CultureInfo.InvariantCulture,
                "Move: {0}. Turn: {1}. State: {2}. Valid moves: {3}.",
                _currentBoardState.FullMoveIndex,
                _currentBoardState.ActiveColor,
                _currentBoardState.State,
                _currentBoardState.ValidMoves.Count);
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
                RedrawBoardState();
                return;
            }

            DisplayValidMoves(_movingPiecePosition.Value);

            var squareElement = _positionToSquareElementMap[_movingPiecePosition.Value];

            squareElement.Background = UIHelper.GetSquareBrush(
                _movingPiecePosition.Value,
                SquareMode.CurrentMoveSource);
        }

        private void MakeMove(PieceMove move)
        {
            if (!_currentBoardState.IsValidMove(move))
            {
                SetMovingPiecePosition(null);
                return;
            }

            PieceType? promotedPieceType = null;
            if (_currentBoardState.IsPawnPromotion(move))
            {
                var pieceType = QueryPawnPromotion(_currentBoardState.ActiveColor);
                if (pieceType == PieceType.None)
                {
                    SetMovingPiecePosition(null);
                    return;
                }

                promotedPieceType = pieceType;
            }

            var newBoardState = _currentBoardState.MakeMove(move, promotedPieceType).EnsureNotNull();

            _previosBoardStates.Push(_currentBoardState);
            _currentBoardState = newBoardState;

            RedrawBoardState();
        }

        private PieceType QueryPawnPromotion(PieceColor activeColor)
        {
            throw new NotImplementedException();
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
                RedrawBoardState();
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

        #endregion
    }
}