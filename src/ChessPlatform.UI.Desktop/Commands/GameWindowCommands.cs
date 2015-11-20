using System;
using System.Linq;
using System.Windows.Input;

namespace ChessPlatform.UI.Desktop.Commands
{
    internal static class GameWindowCommands
    {
        #region Constants and Fields

        public static readonly RoutedUICommand NewGame = new RoutedUICommand(
            "_New Game...",
            nameof(NewGame),
            typeof(GameWindowCommands),
            new InputGestureCollection { new KeyGesture(Key.N, ModifierKeys.Control), new KeyGesture(Key.F2) });

        public static readonly RoutedUICommand Exit = new RoutedUICommand(
            "E_xit",
            nameof(Exit),
            typeof(GameWindowCommands),
            new InputGestureCollection { new KeyGesture(Key.Q, ModifierKeys.Control) });

        public static readonly RoutedUICommand UndoLastMove = new RoutedUICommand(
            "_Undo Last Move...",
            nameof(UndoLastMove),
            typeof(GameWindowCommands),
            new InputGestureCollection
            {
                new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Alt),
                new KeyGesture(Key.Z, ModifierKeys.Control)
            });

        public static readonly RoutedUICommand CopyFenToClipboard = new RoutedUICommand(
            "Copy _FEN to Clipboard",
            nameof(CopyFenToClipboard),
            typeof(GameWindowCommands),
            new InputGestureCollection { new KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Alt) });

        public static readonly RoutedUICommand CopyHistoryToClipboard = new RoutedUICommand(
            "Copy _PGN to Clipboard",
            nameof(CopyHistoryToClipboard),
            typeof(GameWindowCommands),
            new InputGestureCollection { new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Alt) });

        public static readonly RoutedUICommand ReversedBoardView = new RoutedUICommand(
            "_Reversed Board View",
            nameof(ReversedBoardView),
            typeof(GameWindowCommands),
            new InputGestureCollection { new KeyGesture(Key.R, ModifierKeys.Control) });

        public static readonly RoutedUICommand ShowPlayerFeedback = new RoutedUICommand(
            "Show Player _Feedback",
            nameof(ShowPlayerFeedback),
            typeof(GameWindowCommands),
            new InputGestureCollection { new KeyGesture(Key.F, ModifierKeys.Control) });

        public static readonly RoutedUICommand RequestMoveNow = new RoutedUICommand(
            "_Move Now",
            nameof(RequestMoveNow),
            typeof(GameWindowCommands),
            new InputGestureCollection { new KeyGesture(Key.M, ModifierKeys.Control), new KeyGesture(Key.F5) });

        #endregion
    }
}