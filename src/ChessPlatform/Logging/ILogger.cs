#nullable enable

using System;
using Omnifactotum.Annotations;

namespace ChessPlatform.Logging
{
    public interface ILogger
    {
        void Write(LogEntryType entryType, object message, [CanBeNull] Exception? exception);

        void Write(LogEntryType entryType, Func<object> createMessage, [CanBeNull] Exception? exception);
    }
}