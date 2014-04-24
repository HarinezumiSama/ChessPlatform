﻿using System;
using System.Linq;
using System.Windows.Input;

namespace ChessPlatform.UI.Desktop.Commands
{
    internal static class MainWindowCommands
    {
        #region Constants and Fields

        public static readonly RoutedUICommand NewGame = new RoutedUICommand(
            "_New Game...",
            "NewGame",
            typeof(MainWindowCommands),
            new InputGestureCollection { new KeyGesture(Key.N, ModifierKeys.Control) });

        public static readonly RoutedUICommand NewGameFromFenFromClipboard = new RoutedUICommand(
            "New Game from _FEN from Clipboard...",
            "NewGameFromFenFromClipboard",
            typeof(MainWindowCommands),
            new InputGestureCollection { new KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Shift) });

        public static readonly RoutedUICommand Exit = new RoutedUICommand(
            "E_xit",
            "Exit",
            typeof(MainWindowCommands),
            new InputGestureCollection { new KeyGesture(Key.Q, ModifierKeys.Control) });

        public static readonly RoutedUICommand UndoLastMove = new RoutedUICommand(
            "_Undo Last Move...",
            "UndoLastMove",
            typeof(MainWindowCommands),
            new InputGestureCollection { new KeyGesture(Key.Z, ModifierKeys.Control) });

        public static readonly RoutedUICommand CopyFenToClipboard = new RoutedUICommand(
            "_Copy FEN to Clipboard",
            "CopyFenToClipboard",
            typeof(MainWindowCommands),
            new InputGestureCollection { new KeyGesture(Key.C, ModifierKeys.Control) });

        #endregion
    }
}