using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessPlatform.Engine
{
    internal struct TranspositionTableBucket
    {
        #region Constants and Fields

        private TranspositionTableEntry _entry;

#pragma warning disable 169
#pragma warning disable 414
        private readonly long _padding1;
        private readonly long _padding2;
        private readonly long _padding3;
#pragma warning restore 414
#pragma warning restore 169

        #endregion

        #region Public Properties

        //// ReSharper disable once ConvertToAutoProperty
        public TranspositionTableEntry Entry
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _entry;
            }

            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _entry = value;
            }
        }

        #endregion
    }
}