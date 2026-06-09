using BuildingBlocks.Export;using BuildingBlocks.Grids;using Todos.Domain;
namespace Todos.Contracts;
public sealed record CreateTodoRequest(string Title,DateOnly DueDate,TimeOnly? DueTime,TodoPriority Priority,string? Notes);
public sealed record UpdateTodoRequest(string Title,DateOnly DueDate,TimeOnly? DueTime,TodoPriority Priority,string? Notes);
public sealed record ChangeTodoStatusRequest(TodoStatus Status);
public sealed record TodoResponse(Guid Id,string Title,DateOnly DueDate,TimeOnly? DueTime,TodoStatus Status,TodoPriority Priority,string? Notes,DateTime CreatedAtUtc,DateTime UpdatedAtUtc);
public sealed record TodoExportRequest(GridQuery Grid,ExportFormat Format);
