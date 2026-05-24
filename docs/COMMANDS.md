# Система сопровождения ИТ-инфраструктуры

Учебный курсовой проект по тестированию ПО: консольное приложение для заметок, авторизации, мониторинга CPU/RAM/HDD, журнала безопасности и проверки обновлений.

## Состав решения

- `MainApp` - основное консольное приложение.
- `SystemWatcher` - отдельный watcher-агент, который отправляет метрики в PostgreSQL.
- `AppInstaller` - отдельный установщик обновлений из ZIP-релиза.
- `AppTests` - MSTest-проект с XML-наборами данных и fake HTTP handler.
- `database/create_database.sql` - SQL-скрипт PostgreSQL.
- `docs/COMMANDS.md` - инструкция по запуску и командам.

## Настройка базы данных

1. Создайте или выберите рабочую базу `Coursework tester`.
2. Выполните `database/create_database.sql` от административной учетной записи PostgreSQL.
3. Замените пароли `change_me_*` в ролях БД.
4. Укажите эти же пароли в `MainApp/App.config` и `SystemWatcher/watcher.yml`.

В рабочем коде не используется учетная запись `postgres`. Она нужна только для администрирования БД вне приложения.

## App.config

`MainApp/App.config` содержит общие параметры:

- `DatabaseHost`
- `DatabasePort`
- `DatabaseName`
- `AuthUsername`
- `AuthPassword`
- `Version`
- `UpdateConfigPath`
- `CheckUpdatesOnStartup`
- `WatcherConfigPath`
- `WatcherExecutablePath`
- `AutoStartWatcher`

Host, Port и Database берутся только из App.config. Строка подключения роли содержит только `Username` и `Password`, которые возвращает функция `get_role_connection`.

## watcher.yml

`SystemWatcher/watcher.yml`:

- `enabled`
- `intervalSeconds`
- `deviceKey`
- `deviceName`
- `databaseTimeoutSeconds`
- `connectionString`

Параметры `runOnce`, `collectOnly`, `watch add` и `watch del` не используются: watcher сам добавляет устройство при первой отправке метрик.

При `AutoStartWatcher=true` основное приложение запускает watcher автоматически в фоновом режиме. Отдельная команда `dotnet run --project SystemWatcher -- --config SystemWatcher\watcher.yml` нужна только для отладки watcher вручную.

## Команды MainApp

Общие:

```text
help
clear
version
exit
auth logout
```

Заметки:

```text
nt add "Проверить VPN-шлюз"
nt list
nt recent 5
nt edit 1 "Новый текст"
nt del 1
nt search "VPN"
```

Администратор:

```text
admin nt view 1
admin nt edit 1 "Исправленная заметка"
admin user nt ivan
admin user create ivan secret123 user
admin user list
admin user info ivan
admin user block ivan
admin user unblock ivan
admin user delete ivan
sec logs 20
```

Мониторинг:

```text
watch list
watch status
watch show DEV-WORKSTATION-01 10
```

Обновления:

```text
update check
update apply
```

## Обновления

`MainApp/update.yml` содержит:

- `updateOwner`
- `updateRepo`
- `updateAssetExtension`
- `updateHttpTimeoutSeconds`

`update check` обращается к GitHub Releases API, читает `tag_name`, `name`, `body`, `assets`, ищет ZIP-архив и сравнивает версию с текущей.

По умолчанию `CheckUpdatesOnStartup=false`, поэтому GitHub не проверяется при каждом запуске приложения. Это защищает демонстрацию от лишних сообщений про лимит GitHub API; проверку лучше запускать вручную командой `update check`.

Для команды `update apply` в GitHub Release нужно прикреплять ZIP именно в блок `Assets`. Архив `Source code (zip)` и ссылка на файл в описании релиза не подходят: это не готовая установленная сборка приложения.

`AppInstaller` можно запустить отдельно:

```text
AppInstaller --download-url https://example.com/app.zip --target C:\Apps\Coursework --main-pid 1234 --app MainApp.exe
```

## Тесты

Запуск:

```text
dotnet test
```

Тесты проверяют:

- авторизацию через XML-наборы и `DynamicData`;
- запрет подключения под `postgres`;
- работу с заметками;
- проверку обновлений без настоящего GitHub через fake `HttpMessageHandler`.
