using Scalar.AspNetCore;
using Api.Exceptions;
using BuildingBlocks;
using BuildingBlocks.Authentication;
using BuildingBlocks.Modules;
using Catalog;
using Identity;
using Ordering;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBuildingBlocks();
builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddModules(builder.Configuration,
    typeof(CatalogModule).Assembly,
    typeof(OrderingModule).Assembly,
    typeof(IdentityModule).Assembly);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.MapOpenApi();
app.MapScalarApiReference(); // UI at /scalar/v1
app.UseAuthentication();
app.UseAuthorization();
app.MapModuleEndpoints();
app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new() { Predicate = _ => false });

app.Run();

public partial class Program; // exposed for integration tests
