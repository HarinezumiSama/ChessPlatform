using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ChessPlatform.Engine;
using ChessPlatform.Logging;
using ChessPlatform.UI.Desktop.Logging;
using log4net;
using log4net.Config;
using Timer = System.Timers.Timer;

namespace ChessPlatform.UI.Desktop
{
    /// <summary>
    ///     Interaction logic for <b>App.xaml</b>.
    /// </summary>
    internal sealed partial class App
    {
        private static readonly ILogger LoggerInstance = new Log4NetLogger(LogManager.GetLogger(typeof(App)));

        private static int _hasTerminatingExceptionOccurred;

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            GlobalContext.Properties[@"AppTitle"] = AppConstants.LoggingFullTitle;

            GlobalContext.Properties[@"LogSubdirectoryAndFileNameOnly"] =
                $@"{AppConstants.LogSubdirectory}{Path.DirectorySeparatorChar}{AppConstants.EntryAssemblyName}";

            XmlConfigurator.Configure();
        }

        internal static void ProcessUnhandledException(Exception exception)
        {
            if (exception is null)
            {
                return;
            }

            var eventArgs = new UnhandledExceptionEventArgs(exception, false);
            OnUnhandledException(AppDomain.CurrentDomain, eventArgs);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            LoggerInstance.Audit($@"{AppConstants.FullTitle} is starting.");
            LoggerInstance.AuditProcessorInformation();
            LoggerInstance.AuditMemoryInformation();
            LoggerInstance.AuditGCSettings();

            base.OnStartup(e);

            ToolTipService.ShowOnDisabledProperty.OverrideMetadata(typeof(Control), new FrameworkPropertyMetadata(true));

            var openingBookProvider = new PolyglotOpeningBookProvider(LoggerInstance);
            Task.Run(openingBookProvider.PrefetchAsync);

            var mainWindow = new GameWindow(LoggerInstance, openingBookProvider);
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LoggerInstance.Audit($@"{AppConstants.FullTitle} is exiting.");
            base.OnExit(e);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            const string DefaultExceptionText = "<Unknown exception>";

            static string CreateErrorDescription(string details, string extraInfo = null)
                => $@"Unhandled exception has occurred (see below).{extraInfo}{Environment.NewLine}The process will be terminated.{
                    Environment.NewLine}{(string.IsNullOrEmpty(details) ? string.Empty : Environment.NewLine)}{details}";

            static string GetFullTypeName(object obj) => obj.GetType().GetFullName();

            static string CreateErrorText(object exceptionObject)
            {
                switch (exceptionObject)
                {
                    case null:
                        return DefaultExceptionText;

                    case Exception outerException
                        when outerException is AggregateException || outerException is TargetInvocationException:
                        {
                            var baseException = outerException.GetBaseException();

                            //// ReSharper disable once ConditionIsAlwaysTrueOrFalse
                            if (baseException is null || ReferenceEquals(baseException, outerException))
                            {
                                return $@"{GetFullTypeName(outerException)}: {outerException.Message}";
                            }

                            return $@"{GetFullTypeName(outerException)}: {GetFullTypeName(baseException)}: {baseException.Message}";
                        }

                    case Exception exception:
                        return $@"{GetFullTypeName(exception)}: {exception.Message}";

                    default:
                        string asString;
                        try
                        {
                            asString = exceptionObject.ToString();
                        }
                        catch (Exception)
                        {
                            asString = DefaultExceptionText;
                        }

                        return $@"{GetFullTypeName(exceptionObject)}: {asString}";
                }
            }

            static void TerminateOwnProcess()
            {
                LoggerInstance.Audit(@"TERMINATING PROCESS due to the unhandled exception.");
                Process.GetCurrentProcess().Kill();
            }

            var exceptionObject = args?.ExceptionObject;

            var logErrorText = exceptionObject is Exception ? string.Empty : DefaultExceptionText;
            var logExtraInfo = $@"{Environment.NewLine}(Arguments: {nameof(sender)} = {sender})";
            var logErrorDescription = $@"[{AppConstants.FullTitle}] {CreateErrorDescription(logErrorText, logExtraInfo)}";
            LoggerInstance.Fatal(logErrorDescription, exceptionObject as Exception);

            if (Interlocked.CompareExchange(ref _hasTerminatingExceptionOccurred, 1, 0) != 0)
            {
                return;
            }

            var uiErrorText = CreateErrorText(exceptionObject);
            var uiErrorDescription = CreateErrorDescription(uiErrorText);
            var dialogTimeout = TimeSpan.FromMinutes(1);

            using var terminationTimer = new Timer(dialogTimeout.TotalMilliseconds)
            {
                Enabled = false,
                AutoReset = false
            };

            terminationTimer.Elapsed += (_, eventArgs) => TerminateOwnProcess();

            void CallGuiExceptionHandler()
            {
                try
                {
                    var window = Current?.MainWindow;

                    //// ReSharper disable once AccessToDisposedClosure
                    terminationTimer.Enabled = false;

                    LoggerInstance.Audit(@"Displaying the unhandled exception dialog.");

                    window.ShowMessageDialog(
                        uiErrorDescription,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        @"Unhandled Exception");

                    LoggerInstance.Audit(@"The unhandled exception dialog has been closed.");
                }
                catch (Exception ex)
                    when (!ex.IsFatal())
                {
                    LoggerInstance.Warn(@"Failed to display the unhandled exception dialog.", ex);
                }
                finally
                {
                    TerminateOwnProcess();
                }
            }

            LoggerInstance.Audit(
                $@"Calling {nameof(System.Windows.Threading.Dispatcher)} to display the unhandled exception dialog (timeout: {
                    dialogTimeout}).");

            terminationTimer.Enabled = true;

            var dispatcher = Current?.Dispatcher;
            if (dispatcher is null || dispatcher.CheckAccess())
            {
                CallGuiExceptionHandler();
                return;
            }

            dispatcher.Invoke(CallGuiExceptionHandler, DispatcherPriority.Send);
        }
    }
}