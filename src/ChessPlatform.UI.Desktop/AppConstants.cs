#nullable enable

using System;
using System.IO;
using System.Reflection;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop
{
    public static class AppConstants
    {
        public static readonly string LogSubdirectory = @".log";

        [NotNull]
        public static readonly string EntryAssemblyName = Internal.EntryAssembly.GetName().Name;

        [NotNull]
        public static readonly string Product =
            Internal.EntryAssembly.GetSingleCustomAttribute<AssemblyProductAttribute>(false).Product;

        [NotNull]
        public static readonly string Version =
            Internal.EntryAssembly.GetSingleOrDefaultCustomAttribute<AssemblyInformationalVersionAttribute>(false)?.InformationalVersion
                ?? Internal.EntryAssembly.GetName().Version.ToString();

        [NotNull]
        public static readonly string Title = Product;

        [NotNull]
        public static readonly string FullTitle = $@"{Product} {Version}";

        [NotNull]
        public static readonly string LoggingFullTitle = $@"{Product} ({Internal.ExecutableFileName}) {Version}";

        [NotNull]
        public static readonly string ExecutableDirectory = Path.GetDirectoryName(Internal.ExecutableFilePath).EnsureNotNull()!;

        private static class Internal
        {
            [NotNull]
            public static readonly Assembly EntryAssembly = (Assembly.GetEntryAssembly() ?? typeof(Internal).Assembly).EnsureNotNull();

            [NotNull]
            public static readonly string ExecutableFilePath = EntryAssembly.GetLocalPath().EnsureNotNull();

            [NotNull]
            public static readonly string ExecutableFileName = Path.GetFileName(ExecutableFilePath).EnsureNotNull();
        }
    }
}