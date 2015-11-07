using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
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

        #region Internal Methods

        internal static void ProcessUnhandledException(Exception exception)
        {
            if (exception == null)
            {
                return;
            }

            var eventArgs = new UnhandledExceptionEventArgs(exception, false);
            OnUnhandledException(AppDomain.CurrentDomain, eventArgs);
        }

        #endregion

        #region Protected Methods

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            var message = $@"STARTING :: {Title}";

            var titleFrameTop = new string('=', message.Length);
            var titleFrameBottom = new string('-', message.Length);

            Trace.WriteLine(string.Empty);
            Trace.WriteLine($@"*-{titleFrameTop}-*");
            Trace.WriteLine($@"| {message} |");
            Trace.WriteLine($@"*-{titleFrameBottom}-*");
            Trace.WriteLine(string.Empty);

            InitializeOpeningBooksInBackground();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            var message = $@"EXITING :: {Title}";

            var titleFrameTop = new string('-', message.Length);
            var titleFrameBottom = new string('=', message.Length);

            Trace.WriteLine(string.Empty);
            Trace.WriteLine($@"*-{titleFrameTop}-*");
            Trace.WriteLine($@"| {message} |");
            Trace.WriteLine($@"*-{titleFrameBottom}-*");
            Trace.WriteLine(string.Empty);

            base.OnExit(e);
        }

        #endregion

        #region Private Methods

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Action action = () => OnUnhandledExceptionInternal(args);

            var dispatcher = Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
                return;
            }

            dispatcher.Invoke(action, DispatcherPriority.Send);
        }

        private static void OnUnhandledExceptionInternal(UnhandledExceptionEventArgs args)
        {
            var window = Current?.MainWindow;

            var text = string.Format(
                "Unhandled exception has occurred (see below).{0}The process will be terminated.{0}{0}{1}",
                Environment.NewLine,
                args.ExceptionObject.ToStringSafely("<Unknown exception>"));

            Trace.TraceError(text);

            window?.ShowErrorDialog(text, $"Unhandled Exception — {Title}");

            Process.GetCurrentProcess().Kill();
        }

        private static void InitializeOpeningBooksInBackground()
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var task = new Task(
                () =>
                {
                    OpeningBook.InitializeDefault();
                    PolyglotOpeningBook.Performance.EnsureNotNull();
                    PolyglotOpeningBook.Varied.EnsureNotNull();
                });

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