using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop.Converters
{
    internal sealed class DebugConverter : IValueConverter
    {
        private readonly string _asString;

        public DebugConverter()
            : this(null)
        {
            // Nothing to do
        }

        public DebugConverter([CanBeNull] string name)
        {
            Name = name;

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat(GetType().GetQualifiedName());
            if (!Name.IsNullOrWhiteSpace())
            {
                stringBuilder.AppendFormat(CultureInfo.InvariantCulture, " : {0}", Name);
            }

            _asString = stringBuilder.ToString();
        }

        public string Name
        {
            get;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public override string ToString()
        {
            return _asString;
        }
    }
}