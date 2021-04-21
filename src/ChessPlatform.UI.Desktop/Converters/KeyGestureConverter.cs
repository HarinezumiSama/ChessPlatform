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
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(string))
            {
                return value;
            }

            switch (value)
            {
                case IEnumerable collection:
                    return GetStringRepresentation(collection);

                case RoutedCommand routedCommand:
                    return GetStringRepresentation(routedCommand.InputGestures);

                case MenuItem { Command: RoutedCommand menuItemRoutedCommand }:
                    return GetStringRepresentation(menuItemRoutedCommand.InputGestures);

                default:
                    return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static object GetStringRepresentation(IEnumerable collection)
        {
            var result = collection
                .OfType<KeyGesture>()
                .Select(item => item.GetDisplayStringForCulture(CultureInfo.InvariantCulture).AvoidNull())
                .OrderBy(item => item.Length)
                .ThenBy(Factotum.Identity)
                .Join(" | ");

            return result;
        }
    }
}