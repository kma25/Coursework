using System.Text;

namespace MainApp.Models
{
    /// <summary>
    /// Настройки основного приложения из App.config и YAML-файлов.
    /// </summary>
    public sealed class AppSettings
    {
        /// <summary>
        /// Адрес сервера PostgreSQL.
        /// </summary>
        public string DatabaseHost { get; init; } = "localhost";

        /// <summary>
        /// Порт PostgreSQL.
        /// </summary>
        public int DatabasePort { get; init; } = 5432;

        /// <summary>
        /// Имя базы данных.
        /// </summary>
        public string DatabaseName { get; init; } = "Coursework tester";

        /// <summary>
        /// Учетная запись для стартовой авторизации и регистрации.
        /// </summary>
        public string AuthUsername { get; init; } = "app_auth";

        /// <summary>
        /// Пароль учетной записи app_auth.
        /// </summary>
        public string AuthPassword { get; init; } = "change_me";

        /// <summary>
        /// Версия приложения.
        /// </summary>
        public string Version { get; init; } = "1.0.0";

        /// <summary>
        /// Путь к YAML-файлу настроек обновления.
        /// </summary>
        public string UpdateConfigPath { get; init; } = "update.yml";

        /// <summary>
        /// Нужно ли автоматически проверять GitHub Releases при запуске приложения.
        /// </summary>
        public bool CheckUpdatesOnStartup { get; init; }

        /// <summary>
        /// Путь к YAML-файлу watcher.
        /// </summary>
        public string WatcherConfigPath { get; init; } = "watcher.yml";

        /// <summary>
        /// Путь к исполняемому файлу watcher.
        /// </summary>
        public string WatcherExecutablePath { get; init; } = "SystemWatcher.exe";

        /// <summary>
        /// Нужно ли запускать watcher автоматически вместе с основным приложением.
        /// </summary>
        public bool AutoStartWatcher { get; init; } = true;

        /// <summary>
        /// Формирует строку подключения для указанной пары Username/Password.
        /// </summary>
        public string BuildConnectionString(string username, string password)
        {
            if (string.Equals(username, "postgres", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Рабочее подключение под postgres запрещено.");
            }

            var builder = new StringBuilder();
            builder.Append("Host=").Append(DatabaseHost).Append(';');
            builder.Append("Port=").Append(DatabasePort).Append(';');
            builder.Append("Database=").Append(DatabaseName).Append(';');
            builder.Append("Username=").Append(username).Append(';');
            builder.Append("Password=").Append(password).Append(';');
            return builder.ToString();
        }

        /// <summary>
        /// Создает рабочую строку подключения роли из фрагмента Username/Password.
        /// </summary>
        public string BuildRoleConnectionString(string roleCredentialFragment)
        {
            var credentials = ParseRoleCredentials(roleCredentialFragment);

            // База специально не приходит из БД: так роль не может подменить Host/Port/Database,
            // а приложение всегда берет эти значения из App.config.
            return BuildConnectionString(credentials.Username, credentials.Password);
        }

        /// <summary>
        /// Извлекает Username и Password из строки, которую вернула функция БД.
        /// </summary>
        public static (string Username, string Password) ParseRoleCredentials(string value)
        {
            var parts = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var part in parts)
            {
                var index = part.IndexOf('=');
                if (index <= 0)
                {
                    continue;
                }

                map[part[..index].Trim()] = part[(index + 1)..].Trim();
            }

            if (!map.TryGetValue("Username", out var username) || !map.TryGetValue("Password", out var password))
            {
                throw new FormatException("Строка роли должна содержать только Username и Password.");
            }

            return (username, password);
        }
    }

    /// <summary>
    /// Настройки проверки обновлений.
    /// </summary>
    public sealed class UpdateSettings
    {
        /// <summary>
        /// Владелец GitHub-репозитория, в котором публикуются релизы.
        /// </summary>
        public string UpdateOwner { get; init; } = "example";

        /// <summary>
        /// Имя GitHub-репозитория с релизами приложения.
        /// </summary>
        public string UpdateRepo { get; init; } = "it-support";

        /// <summary>
        /// Расширение файла сборки, который должен быть прикреплен к релизу.
        /// </summary>
        public string UpdateAssetExtension { get; init; } = ".zip";

        /// <summary>
        /// Максимальное время ожидания ответа GitHub API.
        /// </summary>
        public int UpdateHttpTimeoutSeconds { get; init; } = 15;
    }

    /// <summary>
    /// Информация о релизе, найденном через HTTP API.
    /// </summary>
    /// <param name="HasUpdate">Показывает, является ли найденный релиз новее текущей версии и готов ли он к установке.</param>
    /// <param name="TagName">Git-тег релиза, например v1.0.1.</param>
    /// <param name="Name">Человекочитаемое название релиза.</param>
    /// <param name="Body">Описание релиза из GitHub Releases.</param>
    /// <param name="ZipUrl">Прямая ссылка на ZIP-архив сборки из блока Assets.</param>
    public sealed record UpdateRelease(
        bool HasUpdate,
        string TagName,
        string Name,
        string Body,
        string? ZipUrl);
}
