using System;
using System.Collections.Generic;
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
        public static readonly string KingSymbol = "\x265A";

        public static readonly ReadOnlyDictionary<PieceType, string> PieceToSymbolMap =
            new ReadOnlyDictionary<PieceType, string>(
                new Dictionary<PieceType, string>
                {
                    { PieceType.None, "\x2001" },
                    { PieceType.King, KingSymbol },
                    { PieceType.Queen, "\x265B" },
                    { PieceType.Rook, "\x265C" },
                    { PieceType.Bishop, "\x265D" },
                    { PieceType.Knight, "\x265E" },
                    { PieceType.Pawn, "\x265F" }
                });

        public static readonly Brush WhitePieceBrush = Brushes.DarkKhaki;
        public static readonly Brush BlackPieceBrush = Brushes.Black;

        private static readonly ReadOnlyDictionary<SquareMode, SquareColors> SquareColorMap =
            new ReadOnlyDictionary<SquareMode, SquareColors>(
                new Dictionary<SquareMode, SquareColors>
                {
                    { SquareMode.Default, new SquareColors(Colors.DimGray, Colors.WhiteSmoke) },
                    { SquareMode.ValidMoveSource, new SquareColors(Colors.Brown, Colors.Yellow) },
                    { SquareMode.ValidMoveTarget, new SquareColors(Colors.Green, Colors.LightGreen) },
                    { SquareMode.CurrentMoveSource, new SquareColors(Colors.Navy, Colors.Blue) },
                    { SquareMode.CurrentMoveTarget, new SquareColors(Colors.DarkRed, Colors.Red) }
                });

        private static readonly string EntryAssemblyName =
            (Assembly.GetEntryAssembly() ?? typeof(UIHelper).Assembly).GetName().Name;

        public static Brush GetSquareBrush(Square square, SquareMode squareMode)
        {
            var color = GetSquareColor(square, squareMode);
            return new SolidColorBrush(color);
        }

        public static Brush GetPieceBrush(GameSide side)
        {
            return side == GameSide.White ? WhitePieceBrush : BlackPieceBrush;
        }

        public static Brush GetPieceBrush(GameSide? side)
        {
            return side.HasValue ? GetPieceBrush(side.Value) : Brushes.Transparent;
        }

        public static MessageBoxResult ShowMessageDialog(
            [CanBeNull] this Window owner,
            string text,
            MessageBoxButton button,
            MessageBoxImage icon,
            string caption = null)
        {
            var window = owner ?? Application.Current?.MainWindow;
            var title = window?.Title ?? EntryAssemblyName;

            var fullCaption = caption.IsNullOrWhiteSpace() ? title : $@"{caption} – {title}";

            var result = window == null
                ? MessageBox.Show(text, fullCaption, button, icon)
                : MessageBox.Show(window, text, fullCaption, button, icon);

            return result;
        }

        public static void ShowErrorDialog([CanBeNull] this Window owner, string text, string caption = null)
        {
            ShowMessageDialog(owner, text, MessageBoxButton.OK, MessageBoxImage.Error, caption);
        }

        public static void ShowWarningDialog([CanBeNull] this Window owner, string text, string caption = null)
        {
            ShowMessageDialog(owner, text, MessageBoxButton.OK, MessageBoxImage.Warning, caption);
        }

        public static MessageBoxResult ShowYesNoDialog(
            [CanBeNull] this Window owner,
            string text,
            string caption = null)
        {
            return ShowMessageDialog(owner, text, MessageBoxButton.YesNo, MessageBoxImage.Question, caption);
        }

        public static void ShowInfoPopup(
            [NotNull] this UIElement popupElement,
            [NotNull] string text,
            Point? popupPoint = null,
            Action popupOpened = null,
            Action popupClosed = null)
        {
            if (popupElement == null)
            {
                throw new ArgumentNullException(nameof(popupElement));
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException(
                    @"The value can be neither empty or whitespace-only string nor null.",
                    nameof(text));
            }

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
                HorizontalOffset = popupPoint?.X ?? 0,
                VerticalOffset = popupPoint?.Y ?? 0,
                PopupAnimation = PopupAnimation.None,
                Focusable = false,
                Opacity = 0d,
                Child = popupContent
            };

            popup.MouseUp += (sender, e) => popup.IsOpen = false;

            var fadeInAnimation = new DoubleAnimation(0d, 1d, new Duration(TimeSpan.FromSeconds(0.5d)));
            Storyboard.SetTarget(fadeInAnimation, popupContent);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(UIElement.OpacityProperty));

            var fadeOutAnimation = new DoubleAnimation(1d, 0d, new Duration(TimeSpan.FromSeconds(0.2d)))
            {
                BeginTime = TimeSpan.FromSeconds(0.75d)
            };

            Storyboard.SetTarget(fadeOutAnimation, popupContent);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(UIElement.OpacityProperty));

            var storyboard = new Storyboard { Children = { fadeInAnimation, fadeOutAnimation } };
            storyboard.Completed += (sender, args) => popup.IsOpen = false;

            popup.Opened +=
                (sender, e) =>
                {
                    popupOpened?.Invoke();
                    popup.BeginStoryboard(storyboard);
                };

            popup.Closed += (sender, args) => popupClosed?.Invoke();

            // Must be the final statement
            popup.IsOpen = true;
        }

        public static void ClearGrid([NotNull] this Grid grid)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            grid.Children.Clear();
            grid.ColumnDefinitions.Clear();
            grid.RowDefinitions.Clear();
        }

        private static Color GetSquareColor(Square square, SquareMode squareMode)
        {
            var squareColors = SquareColorMap[squareMode];
            var isDarkSquare = square.IsDark();
            return squareColors[isDarkSquare];
        }
    }
}