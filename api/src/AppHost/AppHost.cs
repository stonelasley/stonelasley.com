var builder = DistributedApplication.CreateBuilder(args);

// Shared by the API (sender) and the Worker (checker). Set with `dotnet user-secrets` or Parameters__worker-secret.
var workerSecret = builder.AddParameter("worker-secret", secret: true);
// Sent by `sl ingest` (and the content repo's CI) to the API's ingest endpoints.
var ingestToken = builder.AddParameter("ingest-token", secret: true);
var adminToken = builder.AddParameter("admin-token", secret: true);
var sessionKey = builder.AddParameter("access-session-key", secret: true);

var worker = builder.AddJavaScriptApp("worker", "../../../worker", "dev")
    .WithHttpEndpoint(port: 8787, isProxied: false)
    .WithEnvironment("CLOUDFLARE_INCLUDE_PROCESS_ENV", "true")
    .WithEnvironment("WORKER_SECRET", workerSecret)
    .WithEnvironment("SERVE_IMAGES", "true");

var api = builder.AddProject<Projects.Api>("api")
    .WithEnvironment("Worker__BaseUrl", worker.GetEndpoint("http"))
    .WithEnvironment("Worker__Secret", workerSecret)
    .WithEnvironment("Ingest__Token", ingestToken)
    .WithEnvironment("Admin__Token", adminToken)
    .WithEnvironment("Access__SessionKey", sessionKey)
    // No Turnstile widget locally (no site key), so any token passes.
    .WithEnvironment("Turnstile__Fake", "true")
    // Local dev binds Vectorize remotely to the ci index; keep its vectors in their own namespace.
    .WithEnvironment("Search__Vector__Namespace", "local-dev")
    .WithEnvironment("Images__BaseUrl", ReferenceExpression.Create($"{worker.GetEndpoint("http")}/public/"))
    .WithHttpHealthCheck("/health")
    .WaitFor(worker);

builder.AddJavaScriptApp("web", "../../../web", "dev")
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("API_URL", api.GetEndpoint("http"))
    .WithExternalHttpEndpoints()
    .WaitFor(api);

builder.Build().Run();
