using Microsoft.Extensions.Logging;
using BuildingBlocks.Messaging;
using SharedKernel;

namespace BuildingBlocks.Pipelines;

public sealed class LoggingDecorator<TRequest, TResponse>(
    IHandler<TRequest, TResponse> inner,
    ILogger<LoggingDecorator<TRequest, TResponse>> logger) : IHandler<TRequest, TResponse>
{
    public async Task<Result<TResponse>> Handle(TRequest request, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("Handling {Request}", name);

        var result = await inner.Handle(request, ct);

        if (result.IsSuccess)
            logger.LogInformation("Handled {Request}", name);
        else
            logger.LogWarning("{Request} failed: {Code}", name, result.Error.Code);

        return result;
    }
}
