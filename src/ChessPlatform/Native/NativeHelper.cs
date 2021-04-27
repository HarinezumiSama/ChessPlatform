#nullable enable

using System;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using static ChessPlatform.Native.NativeTypes;

namespace ChessPlatform.Native
{
    public static class NativeHelper
    {
        private const long KiBiByte = 1L << 10;

        public static long GetPhysicallyInstalledSystemMemory()
        {
            if (NativeMethods.GetPhysicallyInstalledSystemMemory(out var totalMemoryInKilobytes))
            {
                return checked(Convert.ToInt64(totalMemoryInKilobytes) * KiBiByte);
            }

            var memory = MEMORYSTATUSEX.Create();
            if (NativeMethods.GlobalMemoryStatusEx(ref memory))
            {
                return Convert.ToInt64(memory.ullTotalPhys);
            }

            throw new InvalidOperationException(
                @"Failed to determine the size of the physically installed system memory.",
                new Win32Exception(Marshal.GetLastWin32Error()));
        }

        public static ProcessorInformation GetProcessorInformation()
        {
            var scope = new ManagementScope(@"root\cimv2");

            var query = new SelectQuery(
                "Win32_Processor",
                null,
                new[]
                {
                    nameof(WmiProcessorWrapper.NumberOfCores),
                    nameof(WmiProcessorWrapper.NumberOfLogicalProcessors),
                    nameof(WmiProcessorWrapper.MaxClockSpeed)
                });

            using var searcher = new ManagementObjectSearcher(scope, query)
            {
                Options = new EnumerationOptions { EnsureLocatable = true }
            };

            using var objectCollection = searcher.Get();

            var processors = objectCollection
                .Cast<ManagementBaseObject>()
                .Select(
                    obj => new WmiProcessorWrapper
                    {
                        NumberOfCores = GetInt32PropertyValue(obj, nameof(WmiProcessorWrapper.NumberOfCores)),
                        NumberOfLogicalProcessors = GetInt32PropertyValue(obj, nameof(WmiProcessorWrapper.NumberOfLogicalProcessors)),
                        MaxClockSpeed = GetInt32PropertyValue(obj, nameof(WmiProcessorWrapper.MaxClockSpeed))
                    })
                .ToArray();

            //// ReSharper disable ArgumentsStyleOther
            return new ProcessorInformation(
                totalCoreCount: processors.Sum(processor => Convert.ToInt32(processor.NumberOfCores)),
                totalLogicalProcessorCount: processors.Sum(processor => Convert.ToInt32(processor.NumberOfLogicalProcessors)),
                maxClockSpeedMhz: processors.Length == 0 ? 0 : processors.First().MaxClockSpeed);
            //// ReSharper restore ArgumentsStyleOther

            static int GetInt32PropertyValue(ManagementBaseObject obj, string propertyName)
                => Convert.ToInt32(obj.Properties[propertyName].Value);
        }

        private struct WmiProcessorWrapper
        {
            public int NumberOfCores;
            public int NumberOfLogicalProcessors;
            public int MaxClockSpeed;
        }
    }
}