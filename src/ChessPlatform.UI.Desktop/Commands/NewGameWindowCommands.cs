using System;
using System.Linq;
using System.Windows.Input;

namespace ChessPlatform.UI.Desktop.Commands
{
    internal static class NewGameWindowCommands
    {
        #region Constants and Fields

        public static readonly RoutedUICommand Start = new RoutedUICommand(
            "Start",
            "Start",
            typeof(NewGameWindowCommands));

        public static readonly RoutedUICommand Cancel = new RoutedUICommand(
            "Cancel",
            "Cancel",
            typeof(NewGameWindowCommands));

        #endregion
    }
}