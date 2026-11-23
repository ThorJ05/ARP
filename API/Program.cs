using System.Globalization;
using BusinessEsbjerg.Holiday.Api.Auth;
using BusinessEsbjerg.Holiday.Api.Endpoints;
using BusinessEsbjerg.Holiday.Api.Errors;
using BusinessEsbjerg.Holiday.Api.Setup;
using BusinessEsbjerg.Holiday.Infrastructure;
using BusinessEsbjerg.Holiday.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("en");

builder.Host.UseSerilog((context, services, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddInfrastructure();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddClientCors(builder.Configuration);
builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddOpenApiWithLogin();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

await app.PrepareDatabaseAsync();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

app.UseOpenApi();   // /swagger/v1/swagger.json
app.UseSwaggerUi(); // /swagger

// everything after this needs a valid token, unless the endpoint says AllowAnonymous
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapAuthEndpoints();
app.MapMeEndpoints();
app.MapMyRequestEndpoints();
app.MapMyTransferEndpoints();
app.MapReportEndpoints();
app.MapOverviewEndpoints();
app.MapIllnessEndpoints();
app.MapRuleEndpoints();
app.MapCompanyDayOffEndpoints();
app.MapRequestReviewEndpoints();
app.MapEmployeeEndpoints();

app.Run();

// so integration tests can start the API with WebApplicationFactory<Program>
public partial class Program;
