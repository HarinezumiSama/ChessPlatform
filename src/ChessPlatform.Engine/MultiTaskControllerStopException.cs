using System;
using System.Runtime.Serialization;

namespace ChessPlatform.Engine
{
    [Serializable]
    internal sealed class MultiTaskControllerStopException : Exception
    {
        public MultiTaskControllerStopException()
        {
            // Nothing to do
        }

        private MultiTaskControllerStopException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // Nothing to do
        }
    }
}