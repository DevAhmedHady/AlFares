using Microsoft.AspNetCore.Http;
using SharedKernel;

namespace BuildingBlocks.Http;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null) =>
        result.IsSuccess
            ? (onSuccess?.Invoke(result.Value) ?? Results.Ok(result.Value))
            : Problem(result.Error);

    public static IResult ToHttpResult(this Result result, IResult? onSuccess = null) =>
        result.IsSuccess ? (onSuccess ?? Results.NoContent()) : Problem(result.Error);

    private static IResult Problem(Error error) => error.Type switch
    {
        ErrorType.NotFound => Results.NotFound(new { error.Code, error.Description }),
        ErrorType.Validation => Results.BadRequest(new { error.Code, error.Description }),
        ErrorType.Conflict => Results.Conflict(new { error.Code, error.Description }),
        _ => Results.Problem(error.Description)
    };
}
