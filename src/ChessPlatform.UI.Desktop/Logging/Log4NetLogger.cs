#nullable enable

using System;
using ChessPlatform.Logging;
using log4net;
using log4net.Util;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop.Logging
{
    public sealed class Log4NetLogger : ILogger
    {
        private readonly ILog _log;

        public Log4NetLogger([NotNull] ILog log) => _log = log ?? throw new ArgumentNullException(nameof(log));

        /// <inheritdoc />
        public void Write(LogEntryType entryType, object message, Exception? exception)
        {
            switch (entryType)
            {
                case LogEntryType.Verbose:
                    _log.Debug(message, exception);
                    break;

                case LogEntryType.Information:
                case LogEntryType.Audit:
                    _log.Info(message, exception);
                    break;

                case LogEntryType.Warning:
                    _log.Warn(message, exception);
                    break;

                case LogEntryType.Error:
                    _log.Error(message, exception);
                    break;

                case LogEntryType.Fatal:
                    _log.Fatal(message, exception);
                    break;

                default:
                    _log.Info(message, exception);
                    break;
            }
        }

        /// <inheritdoc />
        public void Write(LogEntryType entryType, Func<object> createMessage, Exception? exception)
        {
            switch (entryType)
            {
                case LogEntryType.Verbose:
                    _log.DebugExt(createMessage, exception);
                    break;

                case LogEntryType.Information:
                case LogEntryType.Audit:
                    _log.InfoExt(createMessage, exception);
                    break;

                case LogEntryType.Warning:
                    _log.WarnExt(createMessage, exception);
                    break;

                case LogEntryType.Error:
                    _log.ErrorExt(createMessage, exception);
                    break;

                case LogEntryType.Fatal:
                    _log.FatalExt(createMessage, exception);
                    break;

                default:
                    _log.InfoExt(createMessage, exception);
                    break;
            }
        }
    }
}