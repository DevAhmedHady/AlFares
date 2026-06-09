using Scalar.AspNetCore;
using Api.Exceptions;
using BuildingBlocks;
using BuildingBlocks.Authentication;
using BuildingBlocks.Modules;
using Clients;
using DashboardCharts;
using Expenses;
using Identity;
using Todos;

const string SpaCorsPolicy = "spa";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBuildingBlocks();
builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddModules(builder.Configuration,
    typeof(IdentityModule).Assembly,
    typeof(ClientsModule).Assembly,
    typeof(ExpensesModule).Assembly,
    typeof(TodosModule).Assembly,
    typeof(DashboardChartsModule).Assembly);

// Allow the Angular dev SPA (Arabic RTL client) to call the API during development.
builder.Services.AddCors(options => options.AddPolicy(SpaCorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

// Register the embedded Arabic font + QuestPDF license once, before any PDF export request.
BuildingBlocks.Export.PdfGridExporter.RegisterFonts();

app.UseExceptionHandler();
app.MapOpenApi();
app.MapScalarApiReference(); // UI at /scalar/v1
app.UseCors(SpaCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapModuleEndpoints();
app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new() { Predicate = _ => false });

app.Run();

public partial class Program; // exposed for integration tests
