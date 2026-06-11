using SharedKernel;

namespace Todos.Domain;

/// <summary>Todo status.</summary>
public enum TodoStatus
{
    Open,
    InProgress,
    Done,
}

/// <summary>Todo priority.</summary>
public enum TodoPriority
{
    Low,
    Normal,
    High,
    Urgent,
}

/// <summary>Todo aggregate.</summary>
public sealed class TodoItem : AggregateRoot
{
    public string Title { get; private set; } = string.Empty;
    public DateOnly DueDate { get; private set; }
    public TimeOnly? DueTime { get; private set; }
    public TodoStatus Status { get; private set; }
    public TodoPriority Priority { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private TodoItem() { }

    private TodoItem(
        string title,
        DateOnly due,
        TimeOnly? dueTime,
        TodoPriority priority,
        string? notes
    )
        : base(Guid.NewGuid())
    {
        Title = title;
        DueDate = due;
        DueTime = dueTime;
        Priority = priority;
        Notes = notes;
        Status = TodoStatus.Open;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
    }

    public static Result<TodoItem> Create(
        string? title,
        DateOnly due,
        TodoPriority priority,
        string? notes
    ) => Create(title, due, null, priority, notes, DateOnly.FromDateTime(DateTime.UtcNow));

    public static Result<TodoItem> Create(
        string? title,
        DateOnly due,
        TodoPriority priority,
        string? notes,
        DateOnly today
    ) => Create(title, due, null, priority, notes, today);

    public static Result<TodoItem> Create(
        string? title,
        DateOnly due,
        TimeOnly? dueTime,
        TodoPriority priority,
        string? notes
    ) => Create(title, due, dueTime, priority, notes, DateOnly.FromDateTime(DateTime.UtcNow));

    public static Result<TodoItem> Create(
        string? title,
        DateOnly due,
        TimeOnly? dueTime,
        TodoPriority priority,
        string? notes,
        DateOnly today
    )
    {
        if (string.IsNullOrWhiteSpace(title))
            return TodoErrors.TitleRequired;
        if (due < today)
            return TodoErrors.DueDatePast;
        return new TodoItem(
            title.Trim(),
            due,
            dueTime,
            priority,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        );
    }

    public Result<TodoItem> Update(
        string? title,
        DateOnly due,
        TodoPriority priority,
        string? notes,
        DateOnly today
    ) => Update(title, due, null, priority, notes, today);

    public Result<TodoItem> Update(
        string? title,
        DateOnly due,
        TimeOnly? dueTime,
        TodoPriority priority,
        string? notes,
        DateOnly today
    )
    {
        if (string.IsNullOrWhiteSpace(title))
            return TodoErrors.TitleRequired;
        if (due < today)
            return TodoErrors.DueDatePast;
        Title = title.Trim();
        DueDate = due;
        DueTime = dueTime;
        Priority = priority;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
        return this;
    }

    public Result<TodoItem> ChangeStatus(TodoStatus status)
    {
        Status = status;
        UpdatedAtUtc = DateTime.UtcNow;
        return this;
    }
}
