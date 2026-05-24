namespace AppInstaller.Services
{
    /// <summary>
    /// Параметры установщика обновлений.
    /// </summary>
    public sealed class InstallerOptions
    {
        public string? ReleaseApiUrl { get; init; }
        public string? DownloadUrl { get; init; }
        public string AssetExtension { get; init; } = ".zip";
        public string TargetDirectory { get; init; } = AppContext.BaseDirectory;
        public int? MainProcessId { get; init; }
        public string AppExecutable { get; init; } = "MainApp.exe";

        /// <summary>
        /// Разбирает аргументы командной строки установщика.
        /// </summary>
        public static InstallerOptions Parse(string[] args)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--", StringComparison.Ordinal))
                {
                    continue;
                }

                var key = args[i][2..];
                var value = i + 1 < args.Length ? args[i + 1] : string.Empty;
                map[key] = value;
                i++;
            }

            return new InstallerOptions
            {
                ReleaseApiUrl = map.GetValueOrDefault("release-api"),
                DownloadUrl = map.GetValueOrDefault("download-url"),
                AssetExtension = map.GetValueOrDefault("asset-extension") ?? ".zip",
                TargetDirectory = map.GetValueOrDefault("target") ?? AppContext.BaseDirectory,
                MainProcessId = int.TryParse(map.GetValueOrDefault("main-pid"), out var pid) ? pid : null,
                AppExecutable = map.GetValueOrDefault("app") ?? "MainApp.exe"
            };
        }
    }
}
