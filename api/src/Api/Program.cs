using System.Text.Json.Serialization;
using StoneLasley.Api.Ingest;
using StoneLasley.Api.Posts;
using StoneLasley.Application;
using StoneLasley.Application.Search;
using StoneLasley.Domain.Posts;
using StoneLasley.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Converters in options outrank [JsonConverter] on a type, so the make's own converter goes first.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new WatchMakeJsonConverter());
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOptions<SearchOptions>().BindConfiguration(SearchOptions.SectionName);
builder.Services.AddOptions<IngestOptions>()
    .BindConfiguration(IngestOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

// Preview must never be indexed: everything it serves says so (Site:NoIndex).
if (app.Configuration.GetValue<bool>("Site:NoIndex"))
{
    app.Use((context, next) =>
    {
        context.Response.Headers["X-Robots-Tag"] = "noindex";
        return next(context);
    });
}

app.MapHealthChecks("/health");
app.MapIngest();
app.MapPosts();

app.Run();

public partial class Program;
