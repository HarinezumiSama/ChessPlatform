﻿#nullable enable

using System.Runtime.InteropServices;

namespace ChessPlatform.Native
{
    internal static class NativeTypes
    {
        //// ReSharper disable InconsistentNaming :: WinAPI
        //// ReSharper disable IdentifierTypo

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MEMORYSTATUSEX
        {
            /// <summary>
            ///     Size of the structure, in bytes. You must set this member before calling GlobalMemoryStatusEx.
            /// </summary>
            public uint dwLength;

            /// <summary>
            ///     Number between 0 and 100 that specifies the approximate percentage of physical memory that is in use
            ///     (0 indicates no memory use and 100 indicates full memory use).
            /// </summary>
            public uint dwMemoryLoad;

            /// <summary>
            ///     Total size of physical memory, in bytes.
            /// </summary>
            public ulong ullTotalPhys;

            /// <summary>
            ///     Size of physical memory available, in bytes.
            /// </summary>
            public ulong ullAvailPhys;

            /// <summary>
            ///     Size of the committed memory limit, in bytes. This is physical memory plus the size of the page
            ///     file, minus a small overhead.
            /// </summary>
            public ulong ullTotalPageFile;

            /// <summary>
            ///     Size of available memory to commit, in bytes. The limit is ullTotalPageFile.
            /// </summary>
            public ulong ullAvailPageFile;

            /// <summary>
            ///     Total size of the user mode portion of the virtual address space of the calling process, in bytes.
            /// </summary>
            public ulong ullTotalVirtual;

            /// <summary>
            ///     Size of unreserved and uncommitted memory in the user mode portion of the virtual address space of
            ///     the calling process, in bytes.
            /// </summary>
            public ulong ullAvailVirtual;

            /// <summary>
            ///     Size of unreserved and uncommitted memory in the extended portion of the virtual address space of
            ///     the calling process, in bytes.
            /// </summary>
            public ulong ullAvailExtendedVirtual;

            /// <summary>
            ///     Creates a new initialized instance of the <see cref="MEMORYSTATUSEX"/> structure.
            /// </summary>
            public static MEMORYSTATUSEX Create() => new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)) };
        }

        //// ReSharper restore IdentifierTypo
        //// ReSharper restore InconsistentNaming
    }
}