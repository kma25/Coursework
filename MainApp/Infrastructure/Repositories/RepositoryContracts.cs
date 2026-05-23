using MainApp.Models;

namespace MainApp.Infrastructure.Repositories;

/// <summary>
/// Доступ к пользователям приложения.
/// </summary>
public interface IUserRepository
{
    Task<User?> FindByLoginAsync(string login, string connectionString, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(string login, string passwordHash, UserRole role, string connectionString, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetAllAsync(string connectionString, CancellationToken cancellationToken = default);
    Task SetBlockedAsync(string login, bool isBlocked, string connectionString, CancellationToken cancellationToken = default);
    Task DeleteAsync(string login, string connectionString, CancellationToken cancellationToken = default);
    Task<string> GetRoleConnectionFragmentAsync(UserRole role, string connectionString, CancellationToken cancellationToken = default);
}

/// <summary>
/// Доступ к заметкам пользователей.
/// </summary>
public interface INoteRepository
{
    Task<Note> AddAsync(int userId, string text, string connectionString, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Note>> GetByUserAsync(int userId, string connectionString, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Note>> GetRecentAsync(int userId, int count, string connectionString, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Note>> SearchAsync(int userId, string query, string connectionString, CancellationToken cancellationToken = default);
    Task<Note?> FindAsync(int id, string connectionString, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, string text, string connectionString, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, string connectionString, CancellationToken cancellationToken = default);
}

/// <summary>
/// Доступ к журналу безопасности.
/// </summary>
public interface ISecurityLogRepository
{
    Task AddAsync(int? userId, string eventType, string details, string connectionString, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityLogEntry>> GetRecentAsync(int count, string connectionString, CancellationToken cancellationToken = default);
}

/// <summary>
/// Доступ к данным мониторинга.
/// </summary>
public interface IMonitoringRepository
{
    Task<IReadOnlyList<MonitoredDevice>> GetDevicesAsync(string connectionString, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemMetric>> GetMetricsAsync(string deviceName, int count, string connectionString, CancellationToken cancellationToken = default);
}
