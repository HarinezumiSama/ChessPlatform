﻿using System;
using System.Runtime.Serialization;

namespace ChessPlatform.GamePlay
{
    [Serializable]
    public sealed class MoveNowRequestedException : Exception
    {
        internal MoveNowRequestedException()
        {
            // Nothing to do
        }

        private MoveNowRequestedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // Nothing to do
        }
    }
}