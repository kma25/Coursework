# Coursework

Курсовой проект по дисциплине `Тестирование и отладка программного обеспечения`.

Проект представляет собой консольную систему сопровождения небольшой ИТ-инфраструктуры: авторизация, роли, заметки, мониторинг CPU/RAM/HDD, журнал безопасности, проверка обновлений и отдельный установщик обновлений.

## Состав проекта

```text
Coursework/
├─ Coursework.sln
├─ MainApp/
├─ SystemWatcher/
├─ AppInstaller/
├─ AppTests/
├─ database/
└─ docs/
```

## Что уже сделано

- Создано решение `Coursework.sln`.
- Реализовано основное консольное приложение `MainApp`.
- Реализован watcher-агент `SystemWatcher`.
- Реализован отдельный установщик `AppInstaller`.
- Добавлен SQL-скрипт PostgreSQL.
- Добавлены MSTest-тесты и XML-наборы данных.
- Проект подключен к GitHub: `https://github.com/kma25/Coursework`.

## Что осталось сделать перед демонстрацией

1. В pgAdmin выполнить `database/create_database.sql` в базе `Coursework tester`.
2. Выполнить тот же скрипт в запасной тестовой базе `Coursework test bsses`.
3. Проверить пароли ролей в SQL, `MainApp/App.config` и `SystemWatcher/watcher.yml`.
4. Запустить `MainApp` и зарегистрировать первого пользователя: он автоматически станет `admin`.
5. При необходимости настроить `MainApp/update.yml` на реальные GitHub Releases и прикрепить готовую сборку ZIP в `Assets` релиза.
6. Оформить пояснительную записку по структуре из `docs/COURSEWORK_STRUCTURE_NOTES.md` и тест-кейсам из `docs/TEST_CASES.md`.

## Быстрый запуск

```powershell
dotnet build Coursework.sln
dotnet test Coursework.sln
dotnet run --project MainApp
```

Watcher запускается автоматически вместе с `MainApp`. Отдельный запуск нужен только для отладки:

```powershell
dotnet run --project SystemWatcher -- --config SystemWatcher\watcher.yml
```

При запуске `MainApp` сразу проверяет GitHub Releases и сообщает, есть ли доступная версия для обновления.

Подробная инструкция по pgAdmin находится в `docs/PGADMIN_SETUP.md`.
