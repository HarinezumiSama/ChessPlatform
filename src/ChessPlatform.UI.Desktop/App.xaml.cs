using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using ChessPlatform.ComputerPlayers;

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

            InitializeDefaultOpeningBookInBackground();
        }

        #endregion

        #region Private Methods

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

        private static void InitializeDefaultOpeningBookInBackground()
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var task = new Task(OpeningBook.InitializeDefault);

            task.ContinueWith(
                t =>
                    Trace.TraceError(
                        "[{0}] Error initializing the default opening book: {1}",
                        currentMethodName,
                        t.Exception),
                TaskContinuationOptions.OnlyOnFaulted);

            task.Start();
        }

        #endregion
    }
}