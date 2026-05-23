using MainApp.Models;
using MainApp.Services;

namespace MainApp.Commands;

/// <summary>
/// Выполняет интерактивные команды основного приложения.
/// </summary>
public sealed class CommandRouter
{
    private readonly string _version;
    private readonly AuthService _authService;
    private readonly NoteService _notes;
    private readonly AdminUserService _adminUsers;
    private readonly MonitoringService _monitoring;
    private readonly SecurityLogService _securityLogs;
    private readonly UpdateService _updates;

    /// <summary>
    /// Создает маршрутизатор команд.
    /// </summary>
    public CommandRouter(
        string version,
        AuthService authService,
        NoteService notes,
        AdminUserService adminUsers,
        MonitoringService monitoring,
        SecurityLogService securityLogs,
        UpdateService updates)
    {
        _version = version;
        _authService = authService;
        _notes = notes;
        _adminUsers = adminUsers;
        _monitoring = monitoring;
        _securityLogs = securityLogs;
        _updates = updates;
    }

    /// <summary>
    /// Запускает интерактивный режим app&gt;.
    /// </summary>
    public async Task RunAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Введите help для просмотра карты команд.");

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write("app> ");
            var input = Console.ReadLine();
            if (input is null)
            {
                return;
            }

            var shouldExit = await ExecuteAsync(session, input, cancellationToken);
            if (shouldExit)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Выполняет одну команду.
    /// </summary>
    public async Task<bool> ExecuteAsync(UserSession session, string input, CancellationToken cancellationToken = default)
    {
        var args = CommandParser.Parse(input);
        if (args.Count == 0)
        {
            return false;
        }

        try
        {
            switch (args[0].ToLowerInvariant())
            {
                case "help":
                    PrintHelp(session.User.Role);
                    break;
                case "clear":
                    Console.Clear();
                    break;
                case "version":
                    Console.WriteLine($"Версия приложения: {_version}");
                    break;
                case "exit":
                    return true;
                case "auth" when args.Count > 1 && args[1].Equals("logout", StringComparison.OrdinalIgnoreCase):
                    await _authService.LogoutAsync(session, cancellationToken);
                    Console.WriteLine("Вы вышли из учетной записи.");
                    return true;
                case "nt":
                    await ExecuteNoteCommandAsync(session, args, cancellationToken);
                    break;
                case "admin":
                    await ExecuteAdminCommandAsync(session, args, cancellationToken);
                    break;
                case "watch":
                    await ExecuteWatchCommandAsync(session, args, cancellationToken);
                    break;
                case "sec":
                    await ExecuteSecurityCommandAsync(session, args, cancellationToken);
                    break;
                case "update":
                    await ExecuteUpdateCommandAsync(args, cancellationToken);
                    break;
                default:
                    Console.WriteLine("Неизвестная команда. Введите help для просмотра списка команд.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Команда завершилась контролируемой ошибкой: {ex.Message}");
        }

        return false;
    }

    private async Task ExecuteNoteCommandAsync(UserSession session, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        if (args.Count < 2)
        {
            Console.WriteLine("Укажите команду заметок. Пример: nt add \"текст\".");
            return;
        }

        switch (args[1].ToLowerInvariant())
        {
            case "add" when args.Count >= 3:
                var add = await _notes.AddAsync(session, args[2], cancellationToken);
                Console.WriteLine(add.Result.Message);
                break;
            case "list":
                PrintNotes(await _notes.ListAsync(session, cancellationToken));
                break;
            case "recent":
                var count = args.Count >= 3 && int.TryParse(args[2], out var recentCount) ? recentCount : 5;
                PrintNotes(await _notes.RecentAsync(session, count, cancellationToken));
                break;
            case "search" when args.Count >= 3:
                PrintNotes(await _notes.SearchAsync(session, args[2], cancellationToken));
                break;
            case "edit" when args.Count >= 4 && int.TryParse(args[2], out var editId):
                Console.WriteLine((await _notes.EditAsync(session, editId, args[3], cancellationToken)).Message);
                break;
            case "del" when args.Count >= 3 && int.TryParse(args[2], out var deleteId):
                Console.WriteLine((await _notes.DeleteAsync(session, deleteId, cancellationToken)).Message);
                break;
            default:
                Console.WriteLine("Некорректная команда заметок.");
                break;
        }
    }

    private async Task ExecuteAdminCommandAsync(UserSession session, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        if (args.Count < 3)
        {
            Console.WriteLine("Некорректная команда администратора.");
            return;
        }

        if (args[1].Equals("nt", StringComparison.OrdinalIgnoreCase))
        {
            await ExecuteAdminNoteCommandAsync(session, args, cancellationToken);
            return;
        }

        if (!args[1].Equals("user", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Неизвестная команда администратора.");
            return;
        }

        if (args[2].Equals("nt", StringComparison.OrdinalIgnoreCase) && args.Count >= 4)
        {
            var user = await _adminUsers.GetUserAsync(session, args[3], cancellationToken);
            Console.WriteLine(user.Result.Message);
            if (user.User is not null)
            {
                var notes = await _notes.AdminListUserNotesAsync(session, user.User.Id, cancellationToken);
                Console.WriteLine(notes.Result.Message);
                PrintNotes(notes.Notes);
            }

            return;
        }

        switch (args[2].ToLowerInvariant())
        {
            case "create" when args.Count >= 5:
                var role = args.Count >= 6 ? args[5] : "user";
                Console.WriteLine((await _adminUsers.CreateUserAsync(session, args[3], args[4], role, cancellationToken)).Message);
                break;
            case "list":
                var users = await _adminUsers.ListUsersAsync(session, cancellationToken);
                Console.WriteLine(users.Result.Message);
                foreach (var user in users.Users)
                {
                    Console.WriteLine($"{user.Login} | {user.Role.ToDatabaseName()} | заблокирован: {user.IsBlocked}");
                }

                break;
            case "info" when args.Count >= 4:
                var all = await _adminUsers.ListUsersAsync(session, cancellationToken);
                var found = all.Users.FirstOrDefault(user => user.Login.Equals(args[3], StringComparison.OrdinalIgnoreCase));
                Console.WriteLine(found is null ? "Пользователь не найден." : $"{found.Login}: роль {found.Role.ToDatabaseName()}, заблокирован: {found.IsBlocked}");
                break;
            case "block" when args.Count >= 4:
                Console.WriteLine((await _adminUsers.BlockAsync(session, args[3], cancellationToken)).Message);
                break;
            case "unblock" when args.Count >= 4:
                Console.WriteLine((await _adminUsers.UnblockAsync(session, args[3], cancellationToken)).Message);
                break;
            case "delete" when args.Count >= 4:
                Console.WriteLine((await _adminUsers.DeleteAsync(session, args[3], cancellationToken)).Message);
                break;
            default:
                Console.WriteLine("Некорректная команда управления пользователями.");
                break;
        }
    }

    private async Task ExecuteAdminNoteCommandAsync(UserSession session, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        switch (args[2].ToLowerInvariant())
        {
            case "view" when args.Count >= 4 && int.TryParse(args[3], out var id):
                var view = await _notes.AdminViewAsync(session, id, cancellationToken);
                Console.WriteLine(view.Result.Message);
                if (view.Note is not null)
                {
                    Console.WriteLine($"#{view.Note.Id} user:{view.Note.UserId} {view.Note.Text}");
                }

                break;
            case "edit" when args.Count >= 5 && int.TryParse(args[3], out var editId):
                Console.WriteLine((await _notes.EditAsync(session, editId, args[4], cancellationToken)).Message);
                break;
            default:
                Console.WriteLine("Некорректная административная команда заметок.");
                break;
        }
    }

    private async Task ExecuteWatchCommandAsync(UserSession session, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        if (args.Count < 2)
        {
            Console.WriteLine("Укажите команду мониторинга.");
            return;
        }

        switch (args[1].ToLowerInvariant())
        {
            case "list":
                var devices = await _monitoring.GetDevicesAsync(session, cancellationToken);
                Console.WriteLine(devices.Result.Message);
                foreach (var device in devices.Devices)
                {
                    Console.WriteLine($"{device.DeviceName} ({device.DeviceKey}) | последний сигнал: {device.LastSeenAt:g}");
                }

                break;
            case "status":
                Console.WriteLine(_monitoring.GetWatcherStatus(session).Message);
                break;
            case "show" when args.Count >= 3:
                var count = args.Count >= 4 && int.TryParse(args[3], out var metricCount) ? metricCount : 10;
                var metrics = await _monitoring.GetMetricsAsync(session, args[2], count, cancellationToken);
                Console.WriteLine(metrics.Result.Message);
                foreach (var metric in metrics.Metrics)
                {
                    Console.WriteLine($"{metric.CreatedAt:g} CPU:{metric.CpuPercent:F1}% RAM:{metric.RamPercent:F1}% HDD:{metric.HddPercent:F1}%");
                }

                break;
            default:
                Console.WriteLine("Некорректная команда мониторинга.");
                break;
        }
    }

    private async Task ExecuteSecurityCommandAsync(UserSession session, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        if (args.Count < 2 || !args[1].Equals("logs", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Используйте команду sec logs [количество].");
            return;
        }

        var count = args.Count >= 3 && int.TryParse(args[2], out var logCount) ? logCount : 20;
        var logs = await _securityLogs.GetRecentAsync(session, count, cancellationToken);
        Console.WriteLine(logs.Result.Message);
        foreach (var log in logs.Logs)
        {
            Console.WriteLine($"{log.CreatedAt:g} | {log.EventType} | {log.Details}");
        }
    }

    private async Task ExecuteUpdateCommandAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        if (args.Count < 2)
        {
            Console.WriteLine("Используйте update check или update apply.");
            return;
        }

        var check = await _updates.CheckAsync(cancellationToken);
        Console.WriteLine(check.Result.Message);
        if (check.Release is not null)
        {
            Console.WriteLine($"{check.Release.TagName} {check.Release.Name}");
            Console.WriteLine(check.Release.Body);
        }

        if (args[1].Equals("apply", StringComparison.OrdinalIgnoreCase) && check.Release?.HasUpdate == true)
        {
            Console.WriteLine("Запустите AppInstaller с URL архива: " + check.Release.ZipUrl);
        }
    }

    private static void PrintNotes(IReadOnlyList<Note> notes)
    {
        if (notes.Count == 0)
        {
            Console.WriteLine("Заметки не найдены.");
            return;
        }

        foreach (var note in notes)
        {
            Console.WriteLine($"#{note.Id} {note.CreatedAt:g}: {note.Text}");
        }
    }

    private static void PrintHelp(UserRole role)
    {
        Console.WriteLine("""
            Общие команды:
              help                         показать карту команд
              clear                        очистить консоль
              version                      показать версию приложения
              auth logout                  выйти из учетной записи
              exit                         выйти из приложения

            Заметки:
              nt add "текст"               добавить заметку
              nt list                      показать свои заметки
              nt recent [количество]       показать последние заметки
              nt edit <id> "новый текст"   изменить свою заметку
              nt del <id>                  удалить свою заметку
              nt search "текст"            найти заметки

            Мониторинг:
              watch list                   список устройств
              watch status                 статус watcher-агента
              watch show <имя ПК> [n]      история CPU/RAM/HDD

            Обновления:
              update check                 проверить обновления
              update apply                 запустить обновление
            """);

        if (role == UserRole.Admin)
        {
            Console.WriteLine("""

                Администратор:
                  admin nt view <id>
                  admin nt edit <id> "новый текст"
                  admin user nt <логин>
                  admin user create <логин> <пароль> [роль]
                  admin user list
                  admin user info <логин>
                  admin user block <логин>
                  admin user unblock <логин>
                  admin user delete <логин>
                  sec logs [количество]
                """);
        }
    }
}
