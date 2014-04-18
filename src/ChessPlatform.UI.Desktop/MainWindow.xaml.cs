using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Omnifactotum;

namespace ChessPlatform.UI.Desktop
{
    /// <summary>
    ///     Interaction logic for <b>MainWindow.xaml</b>.
    /// </summary>
    public sealed partial class MainWindow
    {
        #region Constants and Fields

        private static readonly ReadOnlyDictionary<Piece, char> PieceToCharMap =
            new ReadOnlyDictionary<Piece, char>(
                new Dictionary<Piece, char>
                {
                    { Piece.None, '\x2001' },
                    { Piece.WhiteKing, '\x2654' },
                    { Piece.WhiteQueen, '\x2655' },
                    { Piece.WhiteRook, '\x2656' },
                    { Piece.WhiteBishop, '\x2657' },
                    { Piece.WhiteKnight, '\x2658' },
                    { Piece.WhitePawn, '\x2659' },
                    { Piece.BlackKing, '\x265A' },
                    { Piece.BlackQueen, '\x265B' },
                    { Piece.BlackRook, '\x265C' },
                    { Piece.BlackBishop, '\x265D' },
                    { Piece.BlackKnight, '\x265E' },
                    { Piece.BlackPawn, '\x265F' }
                });

        private readonly Dictionary<Position, TextBlock> _positionToLabelMap = new Dictionary<Position, TextBlock>();
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
                var color = (position.File + position.Rank) % 2 == 0 ? Colors.DarkGray : Colors.WhiteSmoke;

                var textBlock = new TextBlock
                {
                    Margin = new Thickness(),
                    Tag = position,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = new SolidColorBrush(color)
                };

                _positionToLabelMap.Add(position, textBlock);

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
                var ch = PieceToCharMap[pieceInfo.Piece];

                var textBlock = _positionToLabelMap[position];
                textBlock.Text = ch.ToString(CultureInfo.InvariantCulture);
            }
        }

        #endregion
    }
}