using BuildingBlocks.Messaging;
using FluentValidation;
using SharedKernel;

namespace BuildingBlocks.Pipelines;

public sealed class ValidationDecorator<TRequest, TResponse>(
    IHandler<TRequest, TResponse> inner,
    IEnumerable<IValidator<TRequest>> validators
) : IHandler<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, CancellationToken ct)
    {
        var validator = validators.FirstOrDefault();
        if (validator is not null)
        {
            var result = await validator.ValidateAsync(request, ct);
            if (!result.IsValid)
            {
                var first = result.Errors[0];
                return Result.Failure<TResponse>(
                    Error.Validation(first.PropertyName, first.ErrorMessage)
                );
            }
        }

        return await inner.Handle(request, ct);
    }
}
