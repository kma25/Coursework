using System.Text;
using MainApp.Commands;
using MainApp.Infrastructure;
using MainApp.Infrastructure.Repositories;
using MainApp.Services;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var settings = AppConfiguration.LoadAppSettings();
var updateSettings = AppConfiguration.LoadUpdateSettings(settings.UpdateConfigPath);

var passwordHasher = new PasswordHasher();
var users = new PostgresUserRepository();
var notes = new PostgresNoteRepository();
var securityLogs = new PostgresSecurityLogRepository();
var monitoring = new PostgresMonitoringRepository();

var authService = new AuthService(settings, users, securityLogs, passwordHasher);
var noteService = new NoteService(notes);
var adminUserService = new AdminUserService(users, securityLogs, passwordHasher);
var watcherProcess = new WatcherProcessService(settings);
var monitoringService = new MonitoringService(monitoring, watcherProcess);
var securityLogService = new SecurityLogService(securityLogs);
var updateService = new UpdateService(updateSettings, settings.Version);
var installerLaunchService = new InstallerLaunchService();
var router = new CommandRouter(settings.Version, authService, noteService, adminUserService, monitoringService, securityLogService, updateService, installerLaunchService);

Console.WriteLine("Система сопровождения ИТ-инфраструктуры");

try
{
    var authConnection = settings.BuildConnectionString(settings.AuthUsername, settings.AuthPassword);
    var dbStatus = await new DatabaseHealthService().CheckAsync(authConnection);
    Console.WriteLine(dbStatus.Message);
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка чтения настроек подключения: {ex.Message}");
}

if (settings.AutoStartWatcher)
{
    Console.WriteLine(watcherProcess.StartIfNeeded().Message);
}
else
{
    Console.WriteLine("Автоматический запуск watcher отключен в App.config.");
}

if (settings.CheckUpdatesOnStartup)
{
    var updateCheck = await updateService.CheckAsync();
    Console.WriteLine(updateCheck.Result.Message);
}

while (true)
{
    Console.WriteLine("Выберите действие: 1 - вход, 2 - регистрация, 0 - выход");
    Console.Write("> ");
    var action = Console.ReadLine();

    if (action == "0")
    {
        return;
    }

    Console.Write("Логин: ");
    var login = Console.ReadLine() ?? string.Empty;
    Console.Write("Пароль: ");
    var password = ReadPassword();
    Console.WriteLine();

    if (action == "2")
    {
        var registration = await authService.RegisterAsync(login, password);
        Console.WriteLine(registration.Message);
        continue;
    }

    var loginResult = await authService.LoginAsync(login, password);
    Console.WriteLine(loginResult.Result.Message);
    if (loginResult.Session is not null)
    {
        await router.RunAsync(loginResult.Session);
    }
}

static string ReadPassword()
{
    var password = new StringBuilder();

    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            return password.ToString();
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (password.Length > 0)
            {
                password.Length--;
                Console.Write("\b \b");
            }

            continue;
        }

        password.Append(key.KeyChar);
        Console.Write('*');
    }
}
