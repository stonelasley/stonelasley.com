using StoneLasley.Api.Ingest;
using StoneLasley.Api.Posts;
using StoneLasley.Application;
using StoneLasley.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddOptions<IngestOptions>()
    .BindConfiguration(IngestOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapIngest();
app.MapPosts();

app.Run();

public partial class Program;
