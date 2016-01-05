using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ChessPlatform.Engine;

namespace ChessPlatform.UI.Desktop
{
    /// <summary>
    ///     Interaction logic for <b>App.xaml</b>.
    /// </summary>
    public partial class App
    {
        #region Constants and Fields

        public static readonly string Title = $@"Chess Platform UI for Desktop {ChessHelper.PlatformVersion}";

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
            var errorDetails = args?.ExceptionObject?.ToString() ?? "<Unknown exception>";

            var text =
                $@"Unhandled exception has occurred (see below).{Environment.NewLine}The process will be terminated.{
                    Environment.NewLine}{Environment.NewLine}{errorDetails}";

            Trace.TraceError(text);

            var window = Current?.MainWindow;
            window.ShowErrorDialog(text, $@"Unhandled Exception — {Title}");

            Process.GetCurrentProcess().Kill();
        }

        private static void InitializeOpeningBooksInBackground()
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var task = new Task(
                () =>
                {
                    ////PolyglotOpeningBook.Performance.EnsureNotNull();
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