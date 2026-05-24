namespace MainApp.Models
{
    /// <summary>
    /// Роль пользователя в консольной системе.
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Обычный пользователь, работающий только со своими заметками.
        /// </summary>
        User,

        /// <summary>
        /// Администратор с полным доступом к пользователям, заметкам и журналам.
        /// </summary>
        Admin,

        /// <summary>
        /// Статистик, которому разрешен только просмотр данных мониторинга.
        /// </summary>
        Statistician
    }

    /// <summary>
    /// Вспомогательные операции для ролей пользователей.
    /// </summary>
    public static class UserRoleExtensions
    {
        /// <summary>
        /// Преобразует текстовую роль из БД или команды в перечисление.
        /// </summary>
        public static bool TryParseRole(string? value, out UserRole role)
        {
            role = UserRole.User;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            switch (value.Trim().ToLowerInvariant())
            {
                case "user":
                    role = UserRole.User;
                    return true;
                case "admin":
                    role = UserRole.Admin;
                    return true;
                case "statistician":
                    role = UserRole.Statistician;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Возвращает имя роли в формате, который используется в таблице roles.
        /// </summary>
        public static string ToDatabaseName(this UserRole role) => role switch
        {
            UserRole.Admin => "admin",
            UserRole.Statistician => "statistician",
            _ => "user"
        };
    }

    /// <summary>
    /// Пользователь приложения.
    /// </summary>
    /// <param name="Id">Внутренний идентификатор пользователя в базе данных.</param>
    /// <param name="Login">Логин, используемый при входе в систему.</param>
    /// <param name="PasswordHash">Хеш пароля, подготовленный сервисом хеширования.</param>
    /// <param name="Role">Роль пользователя, определяющая доступные команды.</param>
    /// <param name="IsBlocked">Признак административной блокировки учетной записи.</param>
    public sealed record User(int Id, string Login, string PasswordHash, UserRole Role, bool IsBlocked);

    /// <summary>
    /// Активная пользовательская сессия с рабочей строкой подключения роли.
    /// </summary>
    /// <param name="User">Пользователь, прошедший авторизацию.</param>
    /// <param name="RoleConnectionString">Строка подключения, соответствующая роли пользователя.</param>
    public sealed record UserSession(User User, string RoleConnectionString)
    {
        /// <summary>
        /// Проверяет, входит ли пользователь в одну из разрешенных ролей.
        /// </summary>
        public bool IsInRole(params UserRole[] roles) => roles.Contains(User.Role);
    }

    /// <summary>
    /// Заметка пользователя.
    /// </summary>
    /// <param name="Id">Идентификатор заметки.</param>
    /// <param name="UserId">Идентификатор владельца заметки.</param>
    /// <param name="Text">Текст заметки.</param>
    /// <param name="CreatedAt">Дата и время создания заметки.</param>
    /// <param name="UpdatedAt">Дата и время последнего изменения заметки.</param>
    public sealed record Note(int Id, int UserId, string Text, DateTime CreatedAt, DateTime UpdatedAt);

    /// <summary>
    /// Запись журнала безопасности.
    /// </summary>
    /// <param name="Id">Идентификатор события.</param>
    /// <param name="UserId">Пользователь, связанный с событием, если он известен.</param>
    /// <param name="EventType">Технический тип события для фильтрации и анализа.</param>
    /// <param name="Details">Описание события, понятное администратору.</param>
    /// <param name="CreatedAt">Дата и время записи события.</param>
    public sealed record SecurityLogEntry(int Id, int? UserId, string EventType, string Details, DateTime CreatedAt);

    /// <summary>
    /// Устройство, с которого watcher отправляет метрики.
    /// </summary>
    /// <param name="Id">Идентификатор устройства в базе данных.</param>
    /// <param name="DeviceKey">Стабильный ключ устройства.</param>
    /// <param name="DeviceName">Отображаемое имя устройства.</param>
    /// <param name="LastSeenAt">Последний момент получения метрик от устройства.</param>
    public sealed record MonitoredDevice(int Id, string DeviceKey, string DeviceName, DateTime LastSeenAt);

    /// <summary>
    /// Снимок нагрузки CPU/RAM/HDD.
    /// </summary>
    /// <param name="Id">Идентификатор записи метрики.</param>
    /// <param name="DeviceId">Идентификатор устройства, к которому относится метрика.</param>
    /// <param name="CpuPercent">Загрузка процессора в процентах.</param>
    /// <param name="RamPercent">Загрузка оперативной памяти в процентах.</param>
    /// <param name="HddPercent">Заполненность системного диска в процентах.</param>
    /// <param name="CreatedAt">Дата и время получения метрики.</param>
    public sealed record SystemMetric(
        int Id,
        int DeviceId,
        double CpuPercent,
        double RamPercent,
        double HddPercent,
        DateTime CreatedAt);

    /// <summary>
    /// Универсальный результат выполнения операции.
    /// </summary>
    /// <param name="Success">Показывает, завершилась ли операция успешно.</param>
    /// <param name="Message">Сообщение для пользователя или теста.</param>
    public sealed record OperationResult(bool Success, string Message)
    {
        /// <summary>
        /// Создает успешный результат.
        /// </summary>
        public static OperationResult Ok(string message) => new(true, message);

        /// <summary>
        /// Создает результат с контролируемой ошибкой.
        /// </summary>
        public static OperationResult Fail(string message) => new(false, message);
    }
}
