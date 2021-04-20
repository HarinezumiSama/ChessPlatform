using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ChessPlatform.Engine
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TranspositionTableBucket
    {
        private TranspositionTableEntry _entry1;
#pragma warning disable 169
#pragma warning disable 414
        private readonly short _padding1;
#pragma warning restore 414
#pragma warning restore 169

        private TranspositionTableEntry _entry2;
#pragma warning disable 169
#pragma warning disable 414
        private readonly short _padding2;
#pragma warning restore 414
#pragma warning restore 169

        //// ReSharper disable once ConvertToAutoProperty - entry fields are made aligned
        public TranspositionTableEntry Entry1
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _entry1;
            }

            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _entry1 = value;
            }
        }

        //// ReSharper disable once ConvertToAutoProperty - entry fields are made aligned
        public TranspositionTableEntry Entry2
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _entry2;
            }

            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _entry2 = value;
            }
        }
    }
}