using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop.Converters
{
    internal sealed class DebugConverter : IValueConverter
    {
        #region Constants and Fields

        private readonly string _asString;

        #endregion

        #region Constructors

        public DebugConverter()
            : this(null)
        {
            // Nothing to do
        }

        public DebugConverter([CanBeNull] string name)
        {
            this.Name = name;

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat(GetType().GetQualifiedName());
            if (!this.Name.IsNullOrWhiteSpace())
            {
                stringBuilder.AppendFormat(CultureInfo.InvariantCulture, " : {0}", this.Name);
            }

            _asString = stringBuilder.ToString();
        }

        #endregion

        #region Public Properties

        public string Name
        {
            get;
            private set;
        }

        #endregion

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return _asString;
        }

        #endregion
    }
}