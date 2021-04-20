using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace ChessPlatform.UI.Desktop.Converters
{
    public sealed class BooleanToVisibilityConverter : BooleanToValueConverter<Visibility>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BooleanToVisibilityConverter"/> class.
        /// </summary>
        public BooleanToVisibilityConverter()
        {
            TrueValue = Visibility.Visible;
            FalseValue = Visibility.Collapsed;
        }
    }
}