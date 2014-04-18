using System;
using System.Collections.Generic;
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

        private readonly BoardState _boardState = new BoardState();

        #endregion

        #region Constructors

        public MainWindow()
        {
            InitializeComponent();

            InitializeControls();
            DisplayBoardState();
        }

        #endregion

        #region Private Methods

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

        private void DisplayBoardState()
        {
            foreach (var position in ChessHelper.AllPositions)
            {
                var pieceInfo = _boardState.GetPieceInfo(position);
                var ch = UIHelper.PieceToCharMap[pieceInfo.Piece];

                var textBlock = _positionToSquareElementMap[position];
                ResetSquareElementColor(textBlock);
                textBlock.Text = ch.ToString(CultureInfo.InvariantCulture);
            }

            this.StatusLabel.Content = string.Format(
                CultureInfo.InvariantCulture,
                "Move: {0}. Turn: {1}. State: {2}.",
                _boardState.FullMoveIndex,
                _boardState.ActiveColor,
                _boardState.State);
        }

        private void TextBlockSquare_MouseEnter(object sender, MouseEventArgs args)
        {
            var position = GetSquarePosition(args.OriginalSource);
            if (!position.HasValue)
            {
                return;
            }

            var sourceSquareElement = _positionToSquareElementMap[position.Value];

            var moves = _boardState.GetValidMovesBySource(position.Value);
            foreach (var move in moves)
            {
                var squareElement = _positionToSquareElementMap[move.To];
                squareElement.Background = UIHelper.GetSquareBrush(position.Value, SquareMode.ValidMoveTarget);
            }

            if (moves.Length != 0)
            {
                sourceSquareElement.Background = UIHelper.GetSquareBrush(position.Value, SquareMode.ValidMoveSource);
            }
        }

        private void TextBlockSquare_MouseLeave(object sender, MouseEventArgs args)
        {
            var position = GetSquarePosition(args.OriginalSource);
            if (!position.HasValue)
            {
                return;
            }

            DisplayBoardState();
        }

        #endregion
    }
}