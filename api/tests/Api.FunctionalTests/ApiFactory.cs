using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace StoneLasley.Api.FunctionalTests;

/// <summary>
/// Hosts the API in-process, pointed at a Worker with the given secret.
/// </summary>
public sealed class ApiFactory(Uri workerUrl, string workerSecret) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Worker:BaseUrl", workerUrl.ToString());
        builder.UseSetting("Worker:Secret", workerSecret);
    }
}
