using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Omnifactotum;

namespace ChessPlatform.UI.Desktop.Converters
{
    public sealed class KeyGestureConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(string))
            {
                return value;
            }

            var collection = value as IEnumerable;
            if (collection != null)
            {
                return GetStringRepresentation(collection);
            }

            var routedCommand = value as RoutedCommand;
            if (routedCommand != null)
            {
                return GetStringRepresentation(routedCommand.InputGestures);
            }

            var menuItem = value as MenuItem;
            if (menuItem != null)
            {
                routedCommand = menuItem.Command as RoutedCommand;
                if (routedCommand != null)
                {
                    return GetStringRepresentation(routedCommand.InputGestures);
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region Private Methods

        private static object GetStringRepresentation(IEnumerable collection)
        {
            var result = collection
                .OfType<KeyGesture>()
                .Select(item => item.GetDisplayStringForCulture(CultureInfo.InvariantCulture))
                .OrderBy(item => item.Length)
                .ThenBy(Factotum.Identity)
                .Join(" | ");

            return result;
        }

        #endregion
    }
}