#nullable enable

using System.Runtime.InteropServices;

namespace ChessPlatform.Native
{
    internal static class NativeMethods
    {
        private const string Kernel32Library = @"kernel32.dll";

        [DllImport(Kernel32Library, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPhysicallyInstalledSystemMemory(out ulong totalMemoryInKilobytes);

        [DllImport(Kernel32Library, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref NativeTypes.MEMORYSTATUSEX lpBuffer);
    }
}