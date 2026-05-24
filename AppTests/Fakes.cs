using MainApp.Infrastructure.Repositories;
using MainApp.Models;

namespace AppTests;

internal sealed class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = [];
    private int _nextId = 1;

    public void Seed(User user)
    {
        _users.Add(user);
        _nextId = Math.Max(_nextId, user.Id + 1);
    }

    public Task<User?> FindByLoginAsync(string login, string connectionString, CancellationToken cancellationToken = default)
        => Task.FromResult(_users.FirstOrDefault(user => user.Login.Equals(login, StringComparison.OrdinalIgnoreCase)));

    public Task<User> CreateAsync(string login, string passwordHash, UserRole role, string connectionString, CancellationToken cancellationToken = default)
    {
        var user = new User(_nextId++, login, passwordHash, role, false);
        _users.Add(user);
        return Task.FromResult(user);
    }

    public Task<IReadOnlyList<User>> GetAllAsync(string connectionString, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<User>>(_users.ToArray());

    public Task SetBlockedAsync(string login, bool isBlocked, string connectionString, CancellationToken cancellationToken = default)
    {
        var index = _users.FindIndex(user => user.Login.Equals(login, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            _users[index] = _users[index] with { IsBlocked = isBlocked };
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string login, string connectionString, CancellationToken cancellationToken = default)
    {
        _users.RemoveAll(user => user.Login.Equals(login, StringComparison.OrdinalIgnoreCase));
        return Task.CompletedTask;
    }

    public Task<string> GetRoleConnectionFragmentAsync(UserRole role, string connectionString, CancellationToken cancellationToken = default)
        => Task.FromResult(role switch
        {
            UserRole.Admin => "Username=app_admin_role;Password=admin_pwd",
            UserRole.Statistician => "Username=app_statistician_role;Password=stat_pwd",
            _ => "Username=app_user_role;Password=user_pwd"
        });
}

internal sealed class InMemoryNoteRepository : INoteRepository
{
    private readonly List<Note> _notes = [];
    private int _nextId = 1;

    public Task<Note> AddAsync(int userId, string text, string connectionString, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var note = new Note(_nextId++, userId, text, now, now);
        _notes.Add(note);
        return Task.FromResult(note);
    }

    public Task<IReadOnlyList<Note>> GetByUserAsync(int userId, string connectionString, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Note>>(_notes.Where(note => note.UserId == userId).OrderByDescending(note => note.CreatedAt).ToArray());

    public Task<IReadOnlyList<Note>> GetRecentAsync(int userId, int count, string connectionString, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Note>>(_notes.Where(note => note.UserId == userId).OrderByDescending(note => note.CreatedAt).Take(count).ToArray());

    public Task<IReadOnlyList<Note>> SearchAsync(int userId, string query, string connectionString, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Note>>(_notes.Where(note => note.UserId == userId && note.Text.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray());

    public Task<Note?> FindAsync(int id, string connectionString, CancellationToken cancellationToken = default)
        => Task.FromResult(_notes.FirstOrDefault(note => note.Id == id));

    public Task UpdateAsync(int id, string text, string connectionString, CancellationToken cancellationToken = default)
    {
        var index = _notes.FindIndex(note => note.Id == id);
        if (index >= 0)
        {
            var note = _notes[index];
            _notes[index] = note with { Text = text, UpdatedAt = DateTime.UtcNow };
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, string connectionString, CancellationToken cancellationToken = default)
    {
        _notes.RemoveAll(note => note.Id == id);
        return Task.CompletedTask;
    }
}

internal sealed class InMemorySecurityLogRepository : ISecurityLogRepository
{
    private readonly List<SecurityLogEntry> _logs = [];
    private int _nextId = 1;

    public Task AddAsync(int? userId, string eventType, string details, string connectionString, CancellationToken cancellationToken = default)
    {
        _logs.Add(new SecurityLogEntry(_nextId++, userId, eventType, details, DateTime.UtcNow));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SecurityLogEntry>> GetRecentAsync(int count, string connectionString, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SecurityLogEntry>>(_logs.OrderByDescending(log => log.CreatedAt).Take(count).ToArray());
}

internal sealed class InMemoryMonitoringRepository : IMonitoringRepository
{
    private readonly List<MonitoredDevice> _devices =
    [
        new(1, "dev-workstation-01", "DEV-WORKSTATION-01", DateTime.UtcNow)
    ];

    private readonly List<SystemMetric> _metrics =
    [
        new(1, 1, 10.5, 42.0, 80.1, DateTime.UtcNow.AddMinutes(-2)),
        new(2, 1, 11.0, 43.0, 80.2, DateTime.UtcNow.AddMinutes(-1)),
        new(3, 1, 12.0, 44.0, 80.3, DateTime.UtcNow)
    ];

    public Task<IReadOnlyList<MonitoredDevice>> GetDevicesAsync(string connectionString, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<MonitoredDevice>>(_devices.ToArray());

    public Task<IReadOnlyList<SystemMetric>> GetMetricsAsync(string deviceName, int count, string connectionString, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SystemMetric>>(_metrics.Take(count).ToArray());
}

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handle;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handle)
    {
        _handle = handle;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_handle(request));
}
