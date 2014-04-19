using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace ChessPlatform.UI.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        #region Protected Methods

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var window = Current.Morph(obj => obj.MainWindow);

            MessageBox.Show(
                window,
                args.ExceptionObject.ToStringSafely("<Unknown exception>"),
                "Unhandled error",
                MessageBoxButton.OK);

            Process.GetCurrentProcess().Kill();
        }

        #endregion
    }
}