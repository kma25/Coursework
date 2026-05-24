using MainApp.Infrastructure.Repositories;
using MainApp.Models;

namespace MainApp.Services;

/// <summary>
/// Реализует бизнес-логику работы с заметками.
/// </summary>
public sealed class NoteService
{
    private readonly INoteRepository _notes;

    /// <summary>
    /// Создает сервис заметок.
    /// </summary>
    public NoteService(INoteRepository notes)
    {
        _notes = notes;
    }

    /// <summary>
    /// Добавляет заметку текущего пользователя.
    /// </summary>
    public async Task<(OperationResult Result, Note? Note)> AddAsync(UserSession session, string text, CancellationToken cancellationToken = default)
    {
        if (!CanUsePersonalNotes(session))
        {
            return (OperationResult.Fail("Заметки недоступны роли statistician."), null);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return (OperationResult.Fail("Нельзя добавить пустую заметку."), null);
        }

        var note = await _notes.AddAsync(session.User.Id, text.Trim(), session.RoleConnectionString, cancellationToken);
        return (OperationResult.Ok("Заметка добавлена."), note);
    }

    /// <summary>
    /// Возвращает все заметки пользователя.
    /// </summary>
    public Task<IReadOnlyList<Note>> ListAsync(UserSession session, CancellationToken cancellationToken = default)
        => CanUsePersonalNotes(session)
            ? _notes.GetByUserAsync(session.User.Id, session.RoleConnectionString, cancellationToken)
            : Task.FromResult<IReadOnlyList<Note>>(Array.Empty<Note>());

    /// <summary>
    /// Возвращает последние заметки пользователя.
    /// </summary>
    public Task<IReadOnlyList<Note>> RecentAsync(UserSession session, int count, CancellationToken cancellationToken = default)
        => CanUsePersonalNotes(session)
            ? _notes.GetRecentAsync(session.User.Id, Math.Clamp(count, 1, 100), session.RoleConnectionString, cancellationToken)
            : Task.FromResult<IReadOnlyList<Note>>(Array.Empty<Note>());

    /// <summary>
    /// Ищет заметки пользователя по тексту.
    /// </summary>
    public Task<IReadOnlyList<Note>> SearchAsync(UserSession session, string query, CancellationToken cancellationToken = default)
        => CanUsePersonalNotes(session)
            ? _notes.SearchAsync(session.User.Id, query, session.RoleConnectionString, cancellationToken)
            : Task.FromResult<IReadOnlyList<Note>>(Array.Empty<Note>());

    /// <summary>
    /// Изменяет свою заметку или любую заметку администратора.
    /// </summary>
    public async Task<OperationResult> EditAsync(UserSession session, int id, string text, CancellationToken cancellationToken = default)
    {
        if (!CanUsePersonalNotes(session))
        {
            return OperationResult.Fail("Заметки недоступны роли statistician.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return OperationResult.Fail("Текст заметки не может быть пустым.");
        }

        var note = await _notes.FindAsync(id, session.RoleConnectionString, cancellationToken);
        if (note is null)
        {
            return OperationResult.Fail("Заметка с указанным id не найдена.");
        }

        if (session.User.Role != UserRole.Admin && note.UserId != session.User.Id)
        {
            return OperationResult.Fail("Нельзя изменить чужую заметку.");
        }

        await _notes.UpdateAsync(id, text.Trim(), session.RoleConnectionString, cancellationToken);
        return OperationResult.Ok("Заметка изменена.");
    }

    /// <summary>
    /// Удаляет свою заметку или любую заметку администратора.
    /// </summary>
    public async Task<OperationResult> DeleteAsync(UserSession session, int id, CancellationToken cancellationToken = default)
    {
        if (!CanUsePersonalNotes(session))
        {
            return OperationResult.Fail("Заметки недоступны роли statistician.");
        }

        var note = await _notes.FindAsync(id, session.RoleConnectionString, cancellationToken);
        if (note is null)
        {
            return OperationResult.Fail("Заметка с указанным id не найдена.");
        }

        if (session.User.Role != UserRole.Admin && note.UserId != session.User.Id)
        {
            return OperationResult.Fail("Нельзя удалить чужую заметку.");
        }

        await _notes.DeleteAsync(id, session.RoleConnectionString, cancellationToken);
        return OperationResult.Ok("Заметка удалена.");
    }

    /// <summary>
    /// Возвращает заметку по id для администратора.
    /// </summary>
    public async Task<(OperationResult Result, Note? Note)> AdminViewAsync(UserSession session, int id, CancellationToken cancellationToken = default)
    {
        if (session.User.Role != UserRole.Admin)
        {
            return (OperationResult.Fail("Команда доступна только администратору."), null);
        }

        var note = await _notes.FindAsync(id, session.RoleConnectionString, cancellationToken);
        return note is null
            ? (OperationResult.Fail("Заметка с указанным id не найдена."), null)
            : (OperationResult.Ok("Заметка найдена."), note);
    }

    /// <summary>
    /// Возвращает заметки указанного пользователя для администратора.
    /// </summary>
    public async Task<(OperationResult Result, IReadOnlyList<Note> Notes)> AdminListUserNotesAsync(
        UserSession session,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (session.User.Role != UserRole.Admin)
        {
            return (OperationResult.Fail("Команда доступна только администратору."), Array.Empty<Note>());
        }

        var notes = await _notes.GetByUserAsync(userId, session.RoleConnectionString, cancellationToken);
        return (OperationResult.Ok("Заметки пользователя получены."), notes);
    }

    private static bool CanUsePersonalNotes(UserSession session)
        => session.User.Role is UserRole.User or UserRole.Admin;
}
