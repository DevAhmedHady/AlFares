using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace BuildingBlocks.Grids;

/// <summary>Applies allow-listed grid operations to LINQ queries.</summary>
public static class GridQueryExtensions
{
    /// <summary>Counts, projects, and materializes one bounded page asynchronously.</summary>
    /// <typeparam name="T">Source entity type.</typeparam>
    /// <typeparam name="TOut">Projected response type.</typeparam>
    /// <param name="source">Filtered and sorted source query.</param>
    /// <param name="query">Grid paging request.</param>
    /// <param name="projection">Server-translatable projection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Materialized page and total count.</returns>
    public static async Task<PagedResult<TOut>> ToPagedResultAsync<T, TOut>(
        this IQueryable<T> source,
        GridQuery query,
        Expression<Func<T, TOut>> projection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(projection);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var totalCount = await source.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(projection)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<TOut>(items, page, pageSize, totalCount);
    }

    /// <summary>Applies validated filters, global search, and ordered sorting.</summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="source">Source query.</param>
    /// <param name="query">Grid request.</param>
    /// <param name="fieldMap">Safe field allow-list.</param>
    /// <returns>A successful transformed query or validation error.</returns>
    public static Result<IQueryable<T>> ApplyGridQuery<T>(
        this IQueryable<T> source,
        GridQuery query,
        GridFieldMap<T> fieldMap)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(fieldMap);

        var parameter = Expression.Parameter(typeof(T), "entity");

        foreach (var filter in query.Filters)
        {
            if (!fieldMap.TryGet(filter.Field, out var field, out var selector) || !field!.Filterable)
                return UnknownField<T>(filter.Field);

            var member = ReplaceParameter(selector!, parameter);
            var predicate = BuildFilter(member, field, filter);
            if (predicate.IsFailure) return Result.Failure<IQueryable<T>>(predicate.Error);
            source = source.Where(Expression.Lambda<Func<T, bool>>(predicate.Value, parameter));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            Expression? searchBody = null;
            foreach (var field in fieldMap.Fields.Where(x => x.Searchable && x.Type == GridFieldType.Text))
            {
                var member = ReplaceParameter(fieldMap.Selector(field.Key), parameter);
                var contains = Expression.Call(member, nameof(string.Contains), Type.EmptyTypes,
                    Expression.Constant(query.Search.Trim()));
                searchBody = searchBody is null ? contains : Expression.OrElse(searchBody, contains);
            }

            if (searchBody is not null)
                source = source.Where(Expression.Lambda<Func<T, bool>>(searchBody, parameter));
        }

        var ordered = false;
        foreach (var sort in query.Sort)
        {
            if (!fieldMap.TryGet(sort.Field, out var field, out var selector) || !field!.Sortable)
                return UnknownField<T>(sort.Field);

            var member = ReplaceParameter(selector!, parameter);
            var lambda = Expression.Lambda(member, parameter);
            var method = ordered
                ? sort.Desc ? nameof(Queryable.ThenByDescending) : nameof(Queryable.ThenBy)
                : sort.Desc ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);
            source = source.Provider.CreateQuery<T>(Expression.Call(typeof(Queryable), method,
                [typeof(T), member.Type], source.Expression, Expression.Quote(lambda)));
            ordered = true;
        }

        return Result.Success(source);
    }

    private static Result<IQueryable<T>> UnknownField<T>(string key) =>
        Result.Failure<IQueryable<T>>(Error.Validation("grid.unknown_field", key));

    private static Expression ReplaceParameter<T>(Expression<Func<T, object?>> selector, ParameterExpression parameter)
    {
        var body = new ParameterReplaceVisitor(selector.Parameters[0], parameter).Visit(selector.Body)!;
        return body is UnaryExpression { NodeType: ExpressionType.Convert } convert ? convert.Operand : body;
    }

    private static Result<Expression> BuildFilter(Expression member, GridField field, GridFilter filter)
    {
        try
        {
            var targetType = Nullable.GetUnderlyingType(member.Type) ?? member.Type;
            if (filter.Op is GridFilterOp.Contains or GridFilterOp.StartsWith)
            {
                if (targetType != typeof(string)) return InvalidOperation(field.Key, filter.Op);
                var value = Expression.Constant(filter.Value ?? string.Empty);
                return Result.Success<Expression>(Expression.Call(member,
                    filter.Op == GridFilterOp.Contains ? nameof(string.Contains) : nameof(string.StartsWith),
                    Type.EmptyTypes, value));
            }

            if (filter.Op == GridFilterOp.In)
            {
                var values = (filter.Value ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => Parse(value, targetType)).ToArray();
                var array = Array.CreateInstance(targetType, values.Length);
                for (var i = 0; i < values.Length; i++) array.SetValue(values[i], i);
                var contains = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Single(x => x.Name == nameof(Enumerable.Contains) && x.GetParameters().Length == 2)
                    .MakeGenericMethod(targetType);
                return Result.Success<Expression>(Expression.Call(contains, Expression.Constant(array), member));
            }

            var left = member;
            var right = Expression.Constant(Parse(filter.Value, targetType), targetType);
            return filter.Op switch
            {
                GridFilterOp.Eq => Result.Success<Expression>(Expression.Equal(left, right)),
                GridFilterOp.Neq => Result.Success<Expression>(Expression.NotEqual(left, right)),
                GridFilterOp.Gt => Result.Success<Expression>(Expression.GreaterThan(left, right)),
                GridFilterOp.Gte => Result.Success<Expression>(Expression.GreaterThanOrEqual(left, right)),
                GridFilterOp.Lt => Result.Success<Expression>(Expression.LessThan(left, right)),
                GridFilterOp.Lte => Result.Success<Expression>(Expression.LessThanOrEqual(left, right)),
                GridFilterOp.Between => Result.Success<Expression>(Expression.AndAlso(
                    Expression.GreaterThanOrEqual(left, right),
                    Expression.LessThanOrEqual(left, Expression.Constant(Parse(filter.Value2, targetType), targetType)))),
                _ => InvalidOperation(field.Key, filter.Op)
            };
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or ArgumentException)
        {
            return Result.Failure<Expression>(Error.Validation("grid.invalid_value", $"{field.Key}:{filter.Value}"));
        }
    }

    private static Result<Expression> InvalidOperation(string field, GridFilterOp operation) =>
        Result.Failure<Expression>(Error.Validation("grid.invalid_operation", $"{field}:{operation}"));

    private static object Parse(string? value, Type type)
    {
        if (value is null) throw new FormatException("A filter value is required.");
        if (type == typeof(string)) return value;
        if (type == typeof(Guid)) return Guid.Parse(value);
        if (type == typeof(DateOnly)) return DateOnly.Parse(value, CultureInfo.InvariantCulture);
        if (type == typeof(DateTime)) return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        if (type == typeof(bool)) return bool.Parse(value);
        if (type.IsEnum) return Enum.Parse(type, value, true);
        return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
    }

    private sealed class ParameterReplaceVisitor(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
    }
}
