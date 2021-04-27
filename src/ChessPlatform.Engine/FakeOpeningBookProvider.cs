using System;
using System.Threading.Tasks;

namespace ChessPlatform.Engine
{
    public sealed class FakeOpeningBookProvider : IOpeningBookProvider
    {
        public static readonly IOpeningBookProvider Instance = new FakeOpeningBookProvider();

        private FakeOpeningBookProvider()
        {
            // Nothing to do
        }

        /// <inheritdoc />
        public IOpeningBook PerformanceOpeningBook => throw new NotSupportedException();

        /// <inheritdoc />
        public IOpeningBook VariedOpeningBook => throw new NotSupportedException();

        /// <inheritdoc />
        public Task PrefetchAsync() => Task.CompletedTask;
    }
}