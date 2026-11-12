using BusinessEsbjerg.Holiday.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
    // SQLite i stedet: o.UseSqlite("Data Source=holiday.db")

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

var api = app.MapGroup("/api");
api.MapGet("/health", () => Results.Ok(new { status = "ok" }));
using API.Errors;
using API.Options;
using API.Validation;
using FluentValidation;
using Infrastructure;
using Serilog;
using Service.Abstractions;

var builder = WebApplication.CreateBuilder(args);
FluentValidation.ValidatorOptions.Global.LanguageManager.Culture = new System.Globalization.CultureInfo("en");
FluentValidation.ValidatorOptions.Global.LanguageManager.Culture = new System.Globalization.CultureInfo("en");

// Structured logging
builder.Host.UseSerilog((context, services, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Typed configuration, CORS, health
builder.Services.AddOptions<ClientOptions>()
    .Bind(builder.Configuration.GetSection(ClientOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var clientOptions = builder.Configuration.GetSection(ClientOptions.SectionName).Get<ClientOptions>()
    ?? throw new InvalidOperationException("Missing 'Client' configuration section.");

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(clientOptions.Origin)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddHealthChecks();

// Consistent errors
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Validation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "ARP API";
    config.Version = "v1";
});

builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

app.UseOpenApi();      // /swagger/v1/swagger.json
app.UseSwaggerUi();    // /swagger

app.MapHealthChecks("/health");

app.MapGet("/", (IClock clock) => $"Hello World, today is {clock.Today}")
    .WithName("GetHello")
    .WithSummary("Returns a greeting with today's date");

// Temporary demo endpoints, delete when real endpoints exist
app.MapPost("/api/validation-demo", (ValidationDemoRequest request) => Results.Ok(request))
    .WithName("ValidationDemo")
    .WithValidation<ValidationDemoRequest>();

app.MapGet("/api/error-demo", IResult () => throw new InvalidOperationException("Demo error"))
    .WithName("ErrorDemo");

app.Run();