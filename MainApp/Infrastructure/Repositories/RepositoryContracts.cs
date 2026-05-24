using MainApp.Models;

namespace MainApp.Infrastructure.Repositories
{
    /// <summary>
    /// Доступ к пользователям приложения.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Ищет пользователя по логину без учета регистра.
        /// </summary>
        Task<User?> FindByLoginAsync(string login, string connectionString, CancellationToken cancellationToken = default);

        /// <summary>
        /// Создает учетную запись с уже подготовленным хешем пароля и назначенной ролью.
        /// </summary>
        Task<User> CreateAsync(string login, string passwordHash, UserRole role, string connectionString, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает полный список пользователей для административных операций и проверки первой регистрации.
        /// </summary>
        Task<IReadOnlyList<User>> GetAllAsync(string connectionString, CancellationToken cancellationToken = default);

        /// <summary>
        /// Меняет флаг блокировки учетной записи.
        /// </summary>
        Task SetBlockedAsync(string login, bool isBlocked, string connectionString, CancellationToken cancellationToken = default);

        /// <summary>
        /// Удаляет пользователя по логину.
        /// </summary>
        Task DeleteAsync(string login, string connectionString, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает безопасный фрагмент строки подключения для роли пользователя.
        /// </summary>
        Task<string> GetRoleConnectionFragmentAsync(UserRole role, string connectionString, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Доступ к заметкам пользователей.
    /// </summary>
    public interface INoteRepository
    {
        /// <summary>
        /// Сохраняет новую заметку конкретного пользователя.
        /// </summary>
        Task<Note> AddAsync(int userId, string text, string connectionString, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает все заметки пользователя.
        /// </summary>
        Task<IReadOnlyList<Note>> GetByUserAsync(int userId, string connectionString, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает ограниченное количество последних заметок пользователя.
        /// </summary>
        Task<IReadOnlyList<Note>> GetRecentAsync(int userId, int count, string connectionString, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ищет заметки пользователя по фрагменту текста.
        /// </summary>
        Task<IReadOnlyList<Note>> SearchAsync(int userId, string query, string connectionString, CancellationToken cancellationToken = default);

        /// <summary>
        /// Находит заметку по внутреннему идентификатору.
        /// </summary>
        Task<Note?> FindAsync(int id, string connectionString, CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновляет текст найденной заметки.
        /// </summary>
        Task UpdateAsync(int id, string text, string connectionString, CancellationToken cancellationToken = default);

        /// <summary>
        /// Удаляет заметку по идентификатору.
        /// </summary>
        Task DeleteAsync(int id, string connectionString, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Доступ к журналу безопасности.
    /// </summary>
    public interface ISecurityLogRepository
    {
        /// <summary>
        /// Добавляет запись о событии безопасности или администрирования.
        /// </summary>
        Task AddAsync(int? userId, string eventType, string details, string connectionString, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает последние события журнала безопасности.
        /// </summary>
        Task<IReadOnlyList<SecurityLogEntry>> GetRecentAsync(int count, string connectionString, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Доступ к данным мониторинга.
    /// </summary>
    public interface IMonitoringRepository
    {
        /// <summary>
        /// Возвращает устройства, от которых watcher уже присылал метрики.
        /// </summary>
        Task<IReadOnlyList<MonitoredDevice>> GetDevicesAsync(string connectionString, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает историю метрик выбранного устройства.
        /// </summary>
        Task<IReadOnlyList<SystemMetric>> GetMetricsAsync(string deviceName, int count, string connectionString, CancellationToken cancellationToken = default);
    }
}
