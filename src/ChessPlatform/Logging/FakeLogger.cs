#nullable enable

using System;

namespace ChessPlatform.Logging
{
    public sealed class FakeLogger : ILogger
    {
        public static readonly ILogger Instance = new FakeLogger();

        private FakeLogger()
        {
            // Nothing to do
        }

        /// <inheritdoc />
        public void Write(LogEntryType entryType, object message, Exception? exception)
        {
            // Nothing to do
        }

        /// <inheritdoc />
        public void Write(LogEntryType entryType, Func<object> createMessage, Exception? exception)
        {
            // Nothing to do
        }
    }
}