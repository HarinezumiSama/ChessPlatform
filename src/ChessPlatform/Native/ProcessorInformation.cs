namespace ChessPlatform.Native
{
    public sealed class ProcessorInformation
    {
        internal ProcessorInformation(int totalCoreCount, int totalLogicalProcessorCount, int maxClockSpeedMhz)
        {
            TotalCoreCount = totalCoreCount;
            TotalLogicalProcessorCount = totalLogicalProcessorCount;
            MaxClockSpeedMhz = maxClockSpeedMhz;
        }

        public int TotalCoreCount { get; }

        public int TotalLogicalProcessorCount { get; }

        public int MaxClockSpeedMhz { get; }

        public override string ToString()
            => $@"{nameof(TotalCoreCount)} = {TotalCoreCount:N0}, {
                nameof(TotalLogicalProcessorCount)} = {TotalLogicalProcessorCount:N0}, {
                nameof(MaxClockSpeedMhz)} = {MaxClockSpeedMhz:N0}";
    }
}