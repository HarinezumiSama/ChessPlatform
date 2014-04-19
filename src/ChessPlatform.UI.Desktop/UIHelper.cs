using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop
{
    internal static class UIHelper
    {
        #region Constants and Fields

        public static readonly ReadOnlyDictionary<Piece, char> PieceToCharMap =
            new ReadOnlyDictionary<Piece, char>(
                new Dictionary<Piece, char>
                {
                    { Piece.None, '\x2001' },
                    { Piece.WhiteKing, '\x2654' },
                    { Piece.WhiteQueen, '\x2655' },
                    { Piece.WhiteRook, '\x2656' },
                    { Piece.WhiteBishop, '\x2657' },
                    { Piece.WhiteKnight, '\x2658' },
                    { Piece.WhitePawn, '\x2659' },
                    { Piece.BlackKing, '\x265A' },
                    { Piece.BlackQueen, '\x265B' },
                    { Piece.BlackRook, '\x265C' },
                    { Piece.BlackBishop, '\x265D' },
                    { Piece.BlackKnight, '\x265E' },
                    { Piece.BlackPawn, '\x265F' }
                });

        public static readonly ReadOnlyDictionary<SquareMode, SquareColors> SquareColorMap =
            new ReadOnlyDictionary<SquareMode, SquareColors>(
                new Dictionary<SquareMode, SquareColors>
                {
                    { SquareMode.Default, new SquareColors(Colors.DimGray, Colors.WhiteSmoke) },
                    { SquareMode.ValidMoveSource, new SquareColors(Colors.SandyBrown, Colors.Yellow) },
                    { SquareMode.ValidMoveTarget, new SquareColors(Colors.DeepPink, Colors.Pink) },
                    { SquareMode.CurrentMoveSource, new SquareColors(Colors.DarkBlue, Colors.SkyBlue) },
                    { SquareMode.CurrentMoveTarget, new SquareColors(Colors.DarkViolet, Colors.Violet) }
                });

        private static readonly string EntryAssemblyName =
            (Assembly.GetEntryAssembly() ?? typeof(UIHelper).Assembly).GetName().Name;

        #endregion

        #region Public Methods

        public static Brush GetSquareBrush(Position position, SquareMode squareMode)
        {
            var color = GetSquareColor(position, squareMode);
            return new SolidColorBrush(color);
        }

        public static MessageBoxResult ShowMessageDialog(
            [CanBeNull] this Window owner,
            string text,
            MessageBoxButton button,
            MessageBoxImage icon,
            string caption = null)
        {
            var window = owner ?? Application.Current.Morph(obj => obj.MainWindow);
            var title = window.Morph(obj => obj.Title, EntryAssemblyName);

            var fullCaption = caption.IsNullOrWhiteSpace()
                ? title
                : string.Format(CultureInfo.InvariantCulture, "{0} – {1}", caption, title);

            var result = MessageBox.Show(window, text, fullCaption, button, icon);
            return result;
        }

        public static void ShowErrorDialog([CanBeNull] this Window owner, string text, string caption = null)
        {
            ShowMessageDialog(owner, text, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void ShowWarningDialog([CanBeNull] this Window owner, string text, string caption = null)
        {
            ShowMessageDialog(owner, text, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static MessageBoxResult ShowYesNoDialog(
            [CanBeNull] this Window owner,
            string text,
            string caption = null)
        {
            return ShowMessageDialog(owner, text, MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        public static void ShowInfoPopup(
            this UIElement popupElement,
            string text,
            Point? popupPoint = null,
            Action popupOpened = null,
            Action popupClosed = null)
        {
            #region Argument Check

            if (popupElement == null)
            {
                throw new ArgumentNullException("popupElement");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException(
                    @"The value can be neither empty or whitespace-only string nor null.",
                    "text");
            }

            #endregion

            var popupTextBlock = new TextBlock
            {
                Text = text,
                Background = SystemColors.HighlightBrush,
                Foreground = SystemColors.HighlightTextBrush,
                FontSize = 20,
                Padding = new Thickness(5d)
            };

            var popupContent = new Grid
            {
                Background = SystemColors.ControlBrush,
                Children =
                {
                    new Border
                    {
                        BorderThickness = new Thickness(2d),
                        BorderBrush = SystemColors.ActiveBorderBrush,
                        Child = popupTextBlock
                    }
                }
            };

            var popup = new Popup
            {
                IsOpen = false,
                StaysOpen = false,
                AllowsTransparency = true,
                Placement = popupPoint.HasValue ? PlacementMode.Relative : PlacementMode.Center,
                PlacementTarget = popupElement,
                HorizontalOffset = popupPoint.HasValue ? popupPoint.Value.X : 0,
                VerticalOffset = popupPoint.HasValue ? popupPoint.Value.Y : 0,
                PopupAnimation = PopupAnimation.None,
                Focusable = false,
                Opacity = 0d,
                Child = popupContent
            };

            popup.MouseUp += (sender, e) => popup.IsOpen = false;

            var fadeInAnimation = new DoubleAnimation(0d, 1d, new Duration(TimeSpan.FromSeconds(0.5d)));
            Storyboard.SetTarget(fadeInAnimation, popupContent);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(UIElement.OpacityProperty));

            var fadeOutAnimation = new DoubleAnimation(1d, 0d, new Duration(TimeSpan.FromSeconds(0.5d)))
            {
                BeginTime = TimeSpan.FromSeconds(1.5d)
            };

            Storyboard.SetTarget(fadeOutAnimation, popupContent);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(UIElement.OpacityProperty));

            var storyboard = new Storyboard { Children = { fadeInAnimation, fadeOutAnimation } };
            storyboard.Completed += (sender, args) => popup.IsOpen = false;

            popup.Opened +=
                (sender, e) =>
                {
                    if (popupOpened != null)
                    {
                        popupOpened();
                    }

                    popup.BeginStoryboard(storyboard);
                };

            popup.Closed +=
                (sender, args) =>
                {
                    if (popupClosed != null)
                    {
                        popupClosed();
                    }
                };

            // Must be the final statement
            popup.IsOpen = true;
        }

        #endregion

        #region Private Methods

        private static Color GetSquareColor(Position position, SquareMode squareMode)
        {
            var squareColors = SquareColorMap[squareMode];
            var isDark = (position.File + position.Rank) % 2 == 0;
            return squareColors[isDark];
        }

        #endregion
    }
}