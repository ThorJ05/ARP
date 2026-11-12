using Infrastructure;
using Service.Abstractions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure();

var app = builder.Build();

app.MapGet("/", (IClock clock) => $"Hello World, today is {clock.Today}");

app.Run();