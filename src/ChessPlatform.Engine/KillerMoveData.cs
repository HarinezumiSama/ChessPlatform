﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    [DebuggerDisplay("[{GetType().Name,nq}] Primary = {Primary}, Secondary = {Secondary}")]
    internal struct KillerMoveData
    {
        [CanBeNull]
        public GameMove Primary
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;

            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set;
        }

        [CanBeNull]
        public GameMove Secondary
        {
            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;

            [DebuggerNonUserCode]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecordKiller([NotNull] GameMove killerMove)
        {
            if (killerMove is null)
            {
                throw new ArgumentNullException(nameof(killerMove));
            }

            //// ReSharper disable once InvertIf
            if (Primary != killerMove)
            {
                Secondary = Primary;
                Primary = killerMove;
            }
        }
    }
}