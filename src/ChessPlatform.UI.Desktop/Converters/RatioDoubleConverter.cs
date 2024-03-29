﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace ChessPlatform.UI.Desktop.Converters
{
    internal sealed class RatioDoubleConverter : IValueConverter
    {
        public RatioDoubleConverter(double ratio)
        {
            Ratio = ratio;
        }

        public RatioDoubleConverter()
            : this(1d)
        {
            // Nothing to do
        }

        public double Ratio
        {
            get;
            set;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(double) || !(value is double doubleValue))
            {
                throw new ArgumentException(@"Invalid argument(s).");
            }

            return doubleValue * Ratio;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(double) || !(value is double doubleValue))
            {
                throw new ArgumentException(@"Invalid argument(s).");
            }

            if (Ratio.IsZero())
            {
                throw new InvalidOperationException(@"Unable to convert back since the ratio is zero.");
            }

            return doubleValue / Ratio;
        }
    }
}