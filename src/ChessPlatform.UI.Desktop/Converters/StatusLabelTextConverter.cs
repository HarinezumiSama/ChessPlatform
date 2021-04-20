using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace ChessPlatform.UI.Desktop.Converters
{
    internal sealed class StatusLabelTextConverter : IValueConverter
    {
        #region Constants and Fields

        public static readonly StatusLabelTextConverter Instance = new StatusLabelTextConverter();

        #endregion

        #region Constructors

        private StatusLabelTextConverter()
        {
            // Nothing to do
        }

        #endregion

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            #region Argument Check

            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            if (!targetType.IsAssignableFrom(typeof(string)))
            {
                throw new ArgumentException($@"Invalid target type ({targetType.GetFullName()}).", nameof(targetType));
            }

            #endregion

            var gameBoard = value as GameBoard;
            if (gameBoard == null)
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

        #endregion
    }
}