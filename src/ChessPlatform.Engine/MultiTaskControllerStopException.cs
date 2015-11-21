using System;
using System.Linq;
using System.Runtime.Serialization;

namespace ChessPlatform.Engine
{
    [Serializable]
    internal sealed class MultiTaskControllerStopException : Exception
    {
        #region Constructors

        public MultiTaskControllerStopException()
        {
            // Nothing to do
        }

        private MultiTaskControllerStopException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // Nothing to do
        }

        #endregion
    }
}