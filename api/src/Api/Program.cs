using StoneLasley.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure();

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();

public partial class Program;
