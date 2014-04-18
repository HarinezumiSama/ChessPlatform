using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Omnifactotum;

namespace ChessPlatform.UI.Desktop
{
    internal static class UIHelper
    {
        #region Constants and Fields

        public static readonly ReadOnlyDictionary<Piece, char> PieceToCharMap =
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

        public static readonly ReadOnlyDictionary<SquareMode, SquareColors> SquareColorMap =
            new ReadOnlyDictionary<SquareMode, SquareColors>(
                new Dictionary<SquareMode, SquareColors>
                {
                    { SquareMode.Default, new SquareColors(Colors.DarkGray, Colors.WhiteSmoke) },
                    { SquareMode.ValidMoveSource, new SquareColors(Colors.Orange, Colors.Yellow) },
                    { SquareMode.ValidMoveTarget, new SquareColors(Colors.DarkCyan, Colors.LightSeaGreen) }
                });

        #endregion

        #region Public Methods

        public static Brush GetSquareBrush(Position position, SquareMode squareMode)
        {
            var color = GetSquareColor(position, squareMode);
            return new SolidColorBrush(color);
        }

        #endregion

        #region Private Methods

        private static Color GetSquareColor(Position position, SquareMode squareMode)
        {
            var squareColors = SquareColorMap[squareMode];
            var isDark = (position.File + position.Rank) % 2 == 0;
            return squareColors[isDark];
        }

        #endregion
    }
}