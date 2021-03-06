﻿using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace ChessPlatform.UI.Desktop.Converters
{
    internal sealed class RatioDoubleConverter : IValueConverter
    {
        #region Constructors

        public RatioDoubleConverter(double ratio)
        {
            Ratio = ratio;
        }

        public RatioDoubleConverter()
            : this(1d)
        {
            // Nothing to do
        }

        #endregion

        #region Public Properties

        public double Ratio
        {
            get;
            set;
        }

        #endregion

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double) || targetType != typeof(double))
            {
                throw new ArgumentException("Invalid argument(s).");
            }

            return ((double)value) * Ratio;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double) || targetType != typeof(double))
            {
                throw new ArgumentException("Invalid argument(s).");
            }

            if (Ratio.IsZero())
            {
                throw new InvalidOperationException("Unable to convert back since the ratio is zero.");
            }

            return ((double)value) / Ratio;
        }

        #endregion
    }
}