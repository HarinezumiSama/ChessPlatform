#nullable enable

using System;
using System.Runtime;
using ChessPlatform.Native;
using Omnifactotum.Annotations;

namespace ChessPlatform.Logging
{
    public static partial class LoggerExtensions
    {
        private const long KiBiByte = 1L << 10;
        private const long MeBiByte = KiBiByte * KiBiByte;
        private const long GiBiByte = MeBiByte * KiBiByte;

        private static readonly (long Value, string Notation)[] FormatBytesOrderedDivisors =
        {
            (GiBiByte, @"GB"),
            (MeBiByte, @"MB"),
            (KiBiByte, @"KB")
        };

        //// ReSharper disable once InconsistentNaming
        public static void AuditGCSettings([NotNull] this ILogger logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Audit(
                $@"GC settings: {nameof(GCSettings.IsServerGC)} = {GCSettings.IsServerGC}, {
                    nameof(GCSettings.LatencyMode)} = {GCSettings.LatencyMode}");
        }

        public static void AuditMemoryInformation([NotNull] this ILogger logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var physicallyInstalledSystemMemory = NativeHelper.GetPhysicallyInstalledSystemMemory();
            logger.Audit($@"RAM: {FormatBytes(physicallyInstalledSystemMemory)}");
        }

        public static void AuditProcessorInformation([NotNull] this ILogger logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var processorInformation = NativeHelper.GetProcessorInformation();

            logger.Audit(
                $@"Processor: total cores = {processorInformation.TotalCoreCount}, total logical processors = {
                    processorInformation.TotalLogicalProcessorCount}, maximum clock speed = {
                    processorInformation.MaxClockSpeedMhz} MHz");
        }

        private static string FormatBytes(long value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, @"The value cannot be negative.");
            }

            //// ReSharper disable once ForCanBeConvertedToForeach
            for (var index = 0; index < FormatBytesOrderedDivisors.Length; index++)
            {
                var (divisor, notation) = FormatBytesOrderedDivisors[index];

                //// ReSharper disable once InvertIf
                if (value >= divisor)
                {
                    var truncated = Math.Truncate((decimal)value / divisor * 10) / 10;
                    return $@"{truncated:N1} {notation} ({value:N0} B)";
                }
            }

            return $@"{value:N0} B";
        }
    }
}