using System;
using System.Globalization;
using System.Windows.Data;

namespace ChessPlatform.UI.Desktop.Converters
{
    internal sealed class StatusLabelTextConverter : IValueConverter
    {
        public static readonly StatusLabelTextConverter Instance = new StatusLabelTextConverter();

        private StatusLabelTextConverter()
        {
            // Nothing to do
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType is null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            if (!targetType.IsAssignableFrom(typeof(string)))
            {
                throw new ArgumentException($@"Invalid target type ({targetType.GetFullName()}).", nameof(targetType));
            }

            var gameBoard = value as GameBoard;
            if (gameBoard is null)
            {
                return string.Empty;
            }

            var result =
                $@"Move: {gameBoard.FullMoveIndex}. Side to move: {gameBoard.ActiveSide}. State: {gameBoard.State
                    }. Valid moves: {gameBoard.ValidMoves.Count}. Result: {gameBoard.ResultString}. Auto draw: {
                    gameBoard.GetAutoDrawType()}";

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}