using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;

namespace ChessPlatform.UI.Desktop
{
    internal struct SquareColors
    {
        #region Constructors

        internal SquareColors(Color darkSquareColor, Color lightSquareColor)
            : this()
        {
            this.DarkSquareColor = darkSquareColor;
            this.LightSquareColor = lightSquareColor;
        }

        #endregion

        #region Public Properties

        public Color this[bool isDark]
        {
            [DebuggerNonUserCode]
            get
            {
                return isDark ? this.DarkSquareColor : this.LightSquareColor;
            }
        }

        public Color DarkSquareColor
        {
            get;
            private set;
        }

        public Color LightSquareColor
        {
            get;
            private set;
        }

        #endregion
    }
}