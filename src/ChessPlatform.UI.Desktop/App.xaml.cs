using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace ChessPlatform.UI.Desktop
{
    /// <summary>
    ///     Interaction logic for <b>App.xaml</b>.
    /// </summary>
    public partial class App
    {
        #region Constants and Fields

        public static readonly string Title = string.Format(
            CultureInfo.InvariantCulture,
            "Chess Platform UI for Desktop {0}",
            ChessHelper.GetPlatformVersion(true));

        #endregion

        #region Protected Methods

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var window = Current.Morph(obj => obj.MainWindow);

            var text = string.Format(
                CultureInfo.InvariantCulture,
                "Unhandled exception has occurred (see below).{0}"
                    + "The process will be terminated.{0}"
                    + "{0}"
                    + "{1}",
                Environment.NewLine,
                args.ExceptionObject.ToStringSafely("<Unknown exception>"));

            window.ShowErrorDialog(text, "Unhandled exception");

            Process.GetCurrentProcess().Kill();
        }

        #endregion
    }
}