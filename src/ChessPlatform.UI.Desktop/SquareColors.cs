using System.Diagnostics;
using System.Windows.Media;

namespace ChessPlatform.UI.Desktop
{
    internal struct SquareColors
    {
        internal SquareColors(Color darkSquareColor, Color lightSquareColor)
            : this()
        {
            DarkSquareColor = darkSquareColor;
            LightSquareColor = lightSquareColor;
        }

        public Color this[bool isDark]
        {
            [DebuggerNonUserCode]
            get => isDark ? DarkSquareColor : LightSquareColor;
        }

        public Color DarkSquareColor
        {
            get;
        }

        public Color LightSquareColor
        {
            get;
        }
    }
}