using BusinessEsbjerg.Holiday.Api.Options;
using BusinessEsbjerg.Holiday.Service.Auth;
using BusinessEsbjerg.Holiday.Service.Balances;
using BusinessEsbjerg.Holiday.Service.Calendar;
using BusinessEsbjerg.Holiday.Service.Employees;
using BusinessEsbjerg.Holiday.Service.Reports;
using BusinessEsbjerg.Holiday.Service.Requests;
using BusinessEsbjerg.Holiday.Service.Schedules;
using BusinessEsbjerg.Holiday.Service.Users;
using NSwag;
using NSwag.Generation.Processors.Security;

namespace BusinessEsbjerg.Holiday.Api.Setup;

// Registrations used by Program.cs, grouped so Program.cs only shows the order of things.
public static class ApiSetup
{
    // The services with the business logic.
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(config.GetSection("Lockout").Get<LockoutSettings>() ?? new LockoutSettings());
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<EmployeeService>();
        services.AddScoped<ScheduleService>();
        services.AddScoped<BalanceService>();
        services.AddScoped<HolidayRequestService>();
        services.AddScoped<RequestReviewService>();
        services.AddScoped<TransferRequestService>();
        services.AddScoped<DaysOffService>();
        services.AddScoped<ReportService>();
        services.AddScoped<IllnessService>();
        return services;
    }

    // Only the React client may call the API from a browser.
    public static IServiceCollection AddClientCors(this IServiceCollection services, IConfiguration config)
    {
        var section = config.GetSection(ClientOptions.SectionName);
        services.AddOptions<ClientOptions>().Bind(section).ValidateDataAnnotations().ValidateOnStart();

        var client = section.Get<ClientOptions>()
            ?? throw new InvalidOperationException("Missing 'Client' configuration section.");
        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(client.Origin)
            .AllowAnyHeader()
            .AllowAnyMethod()));
        return services;
    }

    // Swagger with an "Authorize" button where you paste the token from /api/auth/login.
    public static IServiceCollection AddOpenApiWithLogin(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApiDocument(config =>
        {
            config.Title = "ARP API";
            config.Version = "v1";
            config.AddSecurity("Bearer", new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste the token from /api/auth/login"
            });
            config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
        });
        return services;
    }
}
