var builder = DistributedApplication.CreateBuilder(args);

// Shared by the API (sender) and the Worker (checker). Set with `dotnet user-secrets` or Parameters__worker-secret.
var workerSecret = builder.AddParameter("worker-secret", secret: true);

var worker = builder.AddJavaScriptApp("worker", "../../../worker", "dev")
    .WithHttpEndpoint(port: 8787, isProxied: false)
    .WithEnvironment("CLOUDFLARE_INCLUDE_PROCESS_ENV", "true")
    .WithEnvironment("WORKER_SECRET", workerSecret);

var api = builder.AddProject<Projects.Api>("api")
    .WithEnvironment("Worker__BaseUrl", worker.GetEndpoint("http"))
    .WithEnvironment("Worker__Secret", workerSecret)
    .WithHttpHealthCheck("/health")
    .WaitFor(worker);

builder.AddJavaScriptApp("web", "../../../web", "dev")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WaitFor(api);

builder.Build().Run();
