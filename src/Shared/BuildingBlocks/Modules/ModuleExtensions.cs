using System.Reflection;
using BuildingBlocks.Mapping;
using BuildingBlocks.Messaging;
using BuildingBlocks.Pipelines;
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Modules;

public static class ModuleExtensions
{
    // Discovers every IModule in the given assemblies, registers each module, then wires the
    // cross-cutting concerns (handlers, validators, decorator pipeline, Mapster profiles) across
    // all of them. Adding a module = passing one more assembly here; nothing else in the host changes.
    public static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration config,
        params Assembly[] moduleAssemblies
    )
    {
        var modules = moduleAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                t is { IsAbstract: false, IsInterface: false }
                && typeof(IModule).IsAssignableFrom(t)
            )
            .Select(t => (IModule)Activator.CreateInstance(t)!)
            .ToList();

        foreach (var module in modules)
            module.Register(services, config);

        // Register every concrete IHandler<,> across all module assemblies.
        services.Scan(scan =>
            scan.FromAssemblies(moduleAssemblies)
                .AddClasses(c => c.AssignableTo(typeof(IHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );

        services.AddValidatorsFromAssemblies(moduleAssemblies);

        // Decorate inner -> outer. Last decorate is outermost, so Logging runs first.
        services.Decorate(typeof(IHandler<,>), typeof(ValidationDecorator<,>));
        services.Decorate(typeof(IHandler<,>), typeof(LoggingDecorator<,>));

        // Mapster: scan all IRegister profiles across modules and build the shared IMapper.
        services.AddMappings(moduleAssemblies);

        // Keep the module instances so the host can map their endpoints later.
        services.AddSingleton<IReadOnlyList<IModule>>(modules);
        return services;
    }

    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder app)
    {
        var modules = app.ServiceProvider.GetRequiredService<IReadOnlyList<IModule>>();
        foreach (var module in modules)
            module.MapEndpoints(app);
        return app;
    }
}
