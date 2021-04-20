using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace ChessPlatform.UI.Desktop.Converters
{
    public class BooleanToValueConverter<T> : IValueConverter
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BooleanToValueConverter{T}"/> class.
        /// </summary>
        public BooleanToValueConverter()
        {
            TrueValue = default(T);
            FalseValue = default(T);
        }

        public T TrueValue { get; set; }

        public T FalseValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool boolValue))
            {
                throw new ArgumentException($@"The value must be {nameof(Boolean)}.", nameof(value));
            }

            return boolValue ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is T castValue))
            {
                throw new ArgumentException($@"The value must be {typeof(T).Name}.", nameof(value));
            }

            return EqualityComparer<T>.Default.Equals(castValue, TrueValue);
        }
    }
}