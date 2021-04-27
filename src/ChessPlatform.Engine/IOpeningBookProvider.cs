using System.Threading.Tasks;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    public interface IOpeningBookProvider
    {
        [NotNull]
        IOpeningBook PerformanceOpeningBook { get; }

        [NotNull]
        IOpeningBook VariedOpeningBook { get; }

        Task PrefetchAsync();
    }
}