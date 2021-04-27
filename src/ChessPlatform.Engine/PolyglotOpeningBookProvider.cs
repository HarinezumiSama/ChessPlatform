using System;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ChessPlatform.Engine.Properties;
using ChessPlatform.Logging;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    public sealed class PolyglotOpeningBookProvider : IOpeningBookProvider
    {
        private readonly ILogger _logger;
        private readonly Lazy<PolyglotOpeningBook> _performanceInstance;
        private readonly Lazy<PolyglotOpeningBook> _variedInstance;

        public PolyglotOpeningBookProvider([NotNull] ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _performanceInstance = Lazy.Create(
                () => InitializeBook(() => Resources.OpeningBook_Performance_Polyglot),
                LazyThreadSafetyMode.ExecutionAndPublication);

            _variedInstance = Lazy.Create(
                () => InitializeBook(() => Resources.OpeningBook_Varied_Polyglot),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <inheritdoc />
        public IOpeningBook PerformanceOpeningBook => _performanceInstance.Value;

        /// <inheritdoc />
        public IOpeningBook VariedOpeningBook => _variedInstance.Value;

        /// <inheritdoc />
        public async Task PrefetchAsync()
        {
            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            _logger.Info(@"Initializing the opening book(s).");
            try
            {
                await Task.WhenAll(
                    Task.Run(() => _variedInstance.Value.EnsureNotNull()),
                    Task.Run(() => _performanceInstance.Value.EnsureNotNull()));
            }
            catch (Exception ex)
                when (!ex.IsFatal())
            {
                _logger.Error($@"[{currentMethodName}] Error initializing the the opening book(s).", ex);
                return;
            }

            _logger.Info(@"Initializing the opening book(s) has been completed successfully.");
        }

        private PolyglotOpeningBook InitializeBook([NotNull] Expression<Func<byte[]>> streamDataGetter)
        {
            PolyglotOpeningBook openingBook;

            var currentMethodName = MethodBase.GetCurrentMethod().GetQualifiedName();

            var bookName = Factotum.GetPropertyName(streamDataGetter);
            var data = streamDataGetter.Compile().Invoke();

            _logger.Audit($"[{currentMethodName}] Initializing the opening book {bookName.ToUIString()}...");

            var stopwatch = Stopwatch.StartNew();
            using (var stream = new MemoryStream(data))
            {
                openingBook = new PolyglotOpeningBook(stream);
            }

            stopwatch.Stop();

            _logger.Audit($@"[{currentMethodName}] The opening book {bookName.ToUIString()} has been initialized in {stopwatch.Elapsed}.");

            return openingBook;
        }
    }
}