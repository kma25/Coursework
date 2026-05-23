# Настройка PostgreSQL через pgAdmin

Ниже инструкция под уже созданные базы:

- `Coursework tester`
- `Coursework test bsses`

## 1. Открыть pgAdmin

1. Запустите `pgAdmin 4`.
2. Слева раскройте `Servers`.
3. Выберите свой сервер PostgreSQL.
4. Введите пароль от PostgreSQL, если pgAdmin попросит.

## 2. Проверить базы

1. Слева раскройте сервер.
2. Раскройте `Databases`.
3. Убедитесь, что есть две базы:
   - `Coursework tester`
   - `Coursework test bsses`

Если какой-то базы нет:

1. Нажмите правой кнопкой по `Databases`.
2. Нажмите `Create` -> `Database...`.
3. В поле `Database` введите имя базы.
4. Нажмите `Save`.

## 3. Выполнить SQL для рабочей базы

1. Нажмите на базу `Coursework tester`.
2. Сверху нажмите кнопку `Query Tool`.
3. Откройте файл:

```text
database/create_database.sql
```

4. Вставьте содержимое в окно Query Tool.
5. Нажмите кнопку запуска `Execute/Refresh` или клавишу `F5`.
6. Дождитесь сообщения об успешном выполнении.

## 4. Выполнить SQL для тестовой базы

Повторите те же действия для базы `Coursework test bsses`:

1. Нажмите на `Coursework test bsses`.
2. Откройте `Query Tool`.
3. Вставьте `database/create_database.sql`.
4. Нажмите `F5`.

Так рабочая и тестовая базы будут иметь одинаковую структуру, но данные будут раздельными.

## 5. Где менять подключение в проекте

Рабочая база указана в:

```text
MainApp/App.config
SystemWatcher/watcher.yml
```

Сейчас там стоит рабочая база:

```text
Coursework tester
```

Для экспериментов можно временно заменить ее на:

```text
Coursework test bsses
```

После проверки лучше вернуть обратно `Coursework tester`, чтобы основное приложение работало с рабочей базой.

## 6. Пароли ролей

В учебном скрипте стоят пароли вида:

```text
change_me_auth_password
change_me_user_password
change_me_admin_password
change_me_statistician_password
change_me_watcher_password
```

Для сдачи курсовой можно оставить как учебные значения, но в реальном проекте их нужно заменить и синхронно обновить в конфигурационных файлах.
