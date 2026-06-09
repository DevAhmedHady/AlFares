using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace BuildingBlocks.Messaging;

public interface IDispatcher
{
    Task<Result<TResponse>> Send<TResponse>(object request, CancellationToken ct);
}

public sealed class Dispatcher(IServiceProvider sp) : IDispatcher
{
    public Task<Result<TResponse>> Send<TResponse>(object request, CancellationToken ct)
    {
        var handlerType = typeof(IHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        dynamic handler = sp.GetRequiredService(handlerType); // resolves decorated chain (Logging -> Validation -> concrete)
        return (Task<Result<TResponse>>)handler.Handle((dynamic)request, ct);
    }
}
