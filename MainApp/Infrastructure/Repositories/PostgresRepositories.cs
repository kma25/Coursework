using MainApp.Models;
using Npgsql;

namespace MainApp.Infrastructure.Repositories;

/// <summary>
/// Проверяет доступность PostgreSQL.
/// </summary>
public sealed class DatabaseHealthService
{
    /// <summary>
    /// Выполняет легкую проверку подключения.
    /// </summary>
    public async Task<OperationResult> CheckAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand("select 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            return OperationResult.Ok("Подключение к базе данных успешно.");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Не удалось подключиться к базе данных: {ex.Message}");
        }
    }
}

/// <summary>
/// PostgreSQL-репозиторий пользователей.
/// </summary>
public sealed class PostgresUserRepository : IUserRepository
{
    /// <summary>
    /// Ищет пользователя по логину.
    /// </summary>
    public async Task<User?> FindByLoginAsync(string login, string connectionString, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select u.id, u.login, u.password_hash, r.name, u.is_blocked
            from users u
            join roles r on r.id = u.role_id
            where lower(u.login) = lower(@login)
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("login", login);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        UserRoleExtensions.TryParseRole(reader.GetString(3), out var role);
        return new User(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), role, reader.GetBoolean(4));
    }

    /// <summary>
    /// Создает нового пользователя.
    /// </summary>
    public async Task<User> CreateAsync(string login, string passwordHash, UserRole role, string connectionString, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into users(login, password_hash, role_id)
            values (@login, @password_hash, (select id from roles where name = @role))
            returning id
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("login", login);
        command.Parameters.AddWithValue("password_hash", passwordHash);
        command.Parameters.AddWithValue("role", role.ToDatabaseName());

        var id = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return new User(id, login, passwordHash, role, false);
    }

    /// <summary>
    /// Возвращает список пользователей.
    /// </summary>
    public async Task<IReadOnlyList<User>> GetAllAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select u.id, u.login, u.password_hash, r.name, u.is_blocked
            from users u
            join roles r on r.id = u.role_id
            order by u.login
            """;

        var users = new List<User>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            UserRoleExtensions.TryParseRole(reader.GetString(3), out var role);
            users.Add(new User(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), role, reader.GetBoolean(4)));
        }

        return users;
    }

    /// <summary>
    /// Блокирует или разблокирует пользователя.
    /// </summary>
    public async Task SetBlockedAsync(string login, bool isBlocked, string connectionString, CancellationToken cancellationToken = default)
    {
        const string sql = "update users set is_blocked = @is_blocked where lower(login) = lower(@login)";
        await ExecuteNonQueryAsync(sql, connectionString, cancellationToken, command =>
        {
            command.Parameters.AddWithValue("login", login);
            command.Parameters.AddWithValue("is_blocked", isBlocked);
        });
    }

    /// <summary>
    /// Удаляет пользователя.
    /// </summary>
    public async Task DeleteAsync(string login, string connectionString, CancellationToken cancellationToken = default)
    {
        const string sql = "delete from users where lower(login) = lower(@login)";
        await ExecuteNonQueryAsync(sql, connectionString, cancellationToken, command =>
            command.Parameters.AddWithValue("login", login));
    }

    /// <summary>
    /// Получает из БД ограниченную строку подключения роли.
    /// </summary>
    public async Task<string> GetRoleConnectionFragmentAsync(UserRole role, string connectionString, CancellationToken cancellationToken = default)
    {
        const string sql = "select get_role_connection(@role)";
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("role", role.ToDatabaseName());

        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)) ?? string.Empty;
    }

    private static async Task ExecuteNonQueryAsync(string sql, string connectionString, CancellationToken cancellationToken, Action<NpgsqlCommand> configure)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        configure(command);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

/// <summary>
/// PostgreSQL-репозиторий заметок.
/// </summary>
public sealed class PostgresNoteRepository : INoteRepository
{
    /// <summary>
    /// Добавляет новую заметку.
    /// </summary>
    public async Task<Note> AddAsync(int userId, string text, string connectionString, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into notes(user_id, text)
            values (@user_id, @text)
            returning id, created_at, updated_at
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("user_id", userId);
        command.Parameters.AddWithValue("text", text);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new Note(reader.GetInt32(0), userId, text, reader.GetDateTime(1), reader.GetDateTime(2));
    }

    /// <summary>
    /// Возвращает заметки пользователя.
    /// </summary>
    public Task<IReadOnlyList<Note>> GetByUserAsync(int userId, string connectionString, CancellationToken cancellationToken = default)
        => QueryNotesAsync(
            "select id, user_id, text, created_at, updated_at from notes where user_id = @user_id order by created_at desc",
            connectionString,
            cancellationToken,
            command => command.Parameters.AddWithValue("user_id", userId));

    /// <summary>
    /// Возвращает последние заметки пользователя.
    /// </summary>
    public Task<IReadOnlyList<Note>> GetRecentAsync(int userId, int count, string connectionString, CancellationToken cancellationToken = default)
        => QueryNotesAsync(
            "select id, user_id, text, created_at, updated_at from notes where user_id = @user_id order by created_at desc limit @count",
            connectionString,
            cancellationToken,
            command =>
            {
                command.Parameters.AddWithValue("user_id", userId);
                command.Parameters.AddWithValue("count", count);
            });

    /// <summary>
    /// Ищет заметки пользователя.
    /// </summary>
    public Task<IReadOnlyList<Note>> SearchAsync(int userId, string query, string connectionString, CancellationToken cancellationToken = default)
        => QueryNotesAsync(
            "select id, user_id, text, created_at, updated_at from notes where user_id = @user_id and text ilike @query order by created_at desc",
            connectionString,
            cancellationToken,
            command =>
            {
                command.Parameters.AddWithValue("user_id", userId);
                command.Parameters.AddWithValue("query", $"%{query}%");
            });

    /// <summary>
    /// Ищет заметку по id.
    /// </summary>
    public async Task<Note?> FindAsync(int id, string connectionString, CancellationToken cancellationToken = default)
    {
        var notes = await QueryNotesAsync(
            "select id, user_id, text, created_at, updated_at from notes where id = @id",
            connectionString,
            cancellationToken,
            command => command.Parameters.AddWithValue("id", id));

        return notes.FirstOrDefault();
    }

    /// <summary>
    /// Обновляет текст заметки.
    /// </summary>
    public async Task UpdateAsync(int id, string text, string connectionString, CancellationToken cancellationToken = default)
    {
        const string sql = "update notes set text = @text, updated_at = now() where id = @id";
        await ExecuteAsync(sql, connectionString, cancellationToken, command =>
        {
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("text", text);
        });
    }

    /// <summary>
    /// Удаляет заметку.
    /// </summary>
    public async Task DeleteAsync(int id, string connectionString, CancellationToken cancellationToken = default)
    {
        const string sql = "delete from notes where id = @id";
        await ExecuteAsync(sql, connectionString, cancellationToken, command => command.Parameters.AddWithValue("id", id));
    }

    private static async Task<IReadOnlyList<Note>> QueryNotesAsync(
        string sql,
        string connectionString,
        CancellationToken cancellationToken,
        Action<NpgsqlCommand> configure)
    {
        var notes = new List<Note>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        configure(command);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            notes.Add(new Note(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetDateTime(3), reader.GetDateTime(4)));
        }

        return notes;
    }

    private static async Task ExecuteAsync(string sql, string connectionString, CancellationToken cancellationToken, Action<NpgsqlCommand> configure)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        configure(command);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

/// <summary>
/// PostgreSQL-репозиторий журнала безопасности.
/// </summary>
public sealed class PostgresSecurityLogRepository : ISecurityLogRepository
{
    /// <summary>
    /// Добавляет событие безопасности.
    /// </summary>
    public async Task AddAsync(int? userId, string eventType, string details, string connectionString, CancellationToken cancellationToken = default)
    {
        const string sql = "insert into security_logs(user_id, event_type, details) values (@user_id, @event_type, @details)";
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("user_id", userId is null ? DBNull.Value : userId);
        command.Parameters.AddWithValue("event_type", eventType);
        command.Parameters.AddWithValue("details", details);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Возвращает последние события безопасности.
    /// </summary>
    public async Task<IReadOnlyList<SecurityLogEntry>> GetRecentAsync(int count, string connectionString, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, user_id, event_type, details, created_at
            from security_logs
            order by created_at desc
            limit @count
            """;

        var logs = new List<SecurityLogEntry>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("count", count);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            int? userId = reader.IsDBNull(1) ? null : reader.GetInt32(1);
            logs.Add(new SecurityLogEntry(reader.GetInt32(0), userId, reader.GetString(2), reader.GetString(3), reader.GetDateTime(4)));
        }

        return logs;
    }
}

/// <summary>
/// PostgreSQL-репозиторий мониторинга.
/// </summary>
public sealed class PostgresMonitoringRepository : IMonitoringRepository
{
    /// <summary>
    /// Возвращает устройства мониторинга.
    /// </summary>
    public async Task<IReadOnlyList<MonitoredDevice>> GetDevicesAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        const string sql = "select id, device_key, device_name, last_seen_at from monitored_devices order by device_name";
        var devices = new List<MonitoredDevice>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            devices.Add(new MonitoredDevice(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetDateTime(3)));
        }

        return devices;
    }

    /// <summary>
    /// Возвращает историю метрик по имени ПК.
    /// </summary>
    public async Task<IReadOnlyList<SystemMetric>> GetMetricsAsync(string deviceName, int count, string connectionString, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select m.id, m.device_id, m.cpu_percent, m.ram_percent, m.hdd_percent, m.created_at
            from system_metrics m
            join monitored_devices d on d.id = m.device_id
            where lower(d.device_name) = lower(@device_name)
            order by m.created_at desc
            limit @count
            """;

        var metrics = new List<SystemMetric>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("device_name", deviceName);
        command.Parameters.AddWithValue("count", count);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            metrics.Add(new SystemMetric(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetDouble(2),
                reader.GetDouble(3),
                reader.GetDouble(4),
                reader.GetDateTime(5)));
        }

        return metrics;
    }
}
