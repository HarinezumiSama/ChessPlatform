using System;
using System.Linq;

namespace ChessPlatform.UI.Desktop.Controls
{
    internal static class ControlItem
    {
        public static ControlItem<T> Create<T>(T value, string text)
        {
            return new ControlItem<T>(value, text);
        }

        public static ControlItem<T> Create<T>(T value)
        {
            return new ControlItem<T>(value);
        }
    }
}