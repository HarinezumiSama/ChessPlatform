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
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Invalid target type ({0}).",
                        targetType.GetFullName()),
                    nameof(targetType));
            }

            #endregion

            var gameBoard = value as GameBoard;
            if (gameBoard == null)
            {
                return string.Empty;
            }

            var result = string.Format(
                culture,
                "Move: {0}. Turn: {1}. State: {2}. Valid moves: {3}. Result: {4}. Auto draw: {5}",
                gameBoard.FullMoveIndex,
                gameBoard.ActiveColor,
                gameBoard.State,
                gameBoard.ValidMoves.Count,
                gameBoard.ResultString,
                gameBoard.GetAutoDrawType());

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}