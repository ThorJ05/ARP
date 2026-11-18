using BusinessEsbjerg.Holiday.Api.Auth;
using BusinessEsbjerg.Holiday.Api.Endpoints;
using BusinessEsbjerg.Holiday.Api.Errors;
using BusinessEsbjerg.Holiday.Api.Options;
using BusinessEsbjerg.Holiday.Domain.Enums;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using FluentValidation;
using BusinessEsbjerg.Holiday.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NSwag;
using NSwag.Generation.Processors.Security;
using Serilog;
using BusinessEsbjerg.Holiday.Service.Abstractions;
using BusinessEsbjerg.Holiday.Service.Auth;
using BusinessEsbjerg.Holiday.Service.Employees;
using BusinessEsbjerg.Holiday.Service.Requests;
using BusinessEsbjerg.Holiday.Service.Schedules;
using BusinessEsbjerg.Holiday.Service.Users;

var builder = WebApplication.CreateBuilder(args);
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

//db connect
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

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

// Login (JWT) and who can access what (policies)
builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddSingleton(builder.Configuration.GetSection("Lockout").Get<LockoutSettings>() ?? new LockoutSettings());
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<ScheduleService>();
builder.Services.AddScoped<HolidayRequestService>();

// OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "ARP API";
    config.Version = "v1";

    // "Authorize" button in Swagger: paste the token from /api/auth/login
    config.AddSecurity("Bearer", new OpenApiSecurityScheme
    {
        Type = OpenApiSecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the token from /api/auth/login"
    });
    config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
});

builder.Services.AddInfrastructure();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db =  scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // rule values always, test data only in development
    await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync(includeTestData: app.Environment.IsDevelopment());

    // the accountant and system owner logins on a new database (fly secrets in production)
    var warnings = await scope.ServiceProvider.GetRequiredService<AccountBootstrapper>().EnsureAsync(
    [
        new BootstrapLogin(Role.Accountant,
            app.Configuration["Bootstrap:Accountant:Email"], app.Configuration["Bootstrap:Accountant:Password"]),
        new BootstrapLogin(Role.SystemOwner,
            app.Configuration["Bootstrap:SystemOwner:Email"], app.Configuration["Bootstrap:SystemOwner:Password"])
    ]);
    foreach (var warning in warnings)
        app.Logger.LogWarning("{Warning}", warning);
    
}

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

app.UseOpenApi();      // /swagger/v1/swagger.json
app.UseSwaggerUi();    // /swagger

// Everything after this needs a valid token, unless the endpoint says AllowAnonymous
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();

app.MapGet("/", (IClock clock) => $"Hello World, today is {clock.Today}")
    .AllowAnonymous()
    .WithName("GetHello")
    .WithSummary("Returns a greeting with today's date");

app.MapAuthEndpoints();
app.MapMeEndpoints();
app.MapEmployeeEndpoints();

app.Run();
