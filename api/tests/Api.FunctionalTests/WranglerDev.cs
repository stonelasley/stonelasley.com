using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace StoneLasley.Api.FunctionalTests;

/// <summary>
/// The real Worker under <c>wrangler dev</c>, backed by a fresh local D1 database with migrations applied.
/// </summary>
public sealed class WranglerDev : IAsyncDisposable
{
    public const string Secret = "functional-test-secret";

    private readonly Process _process;
    private readonly string _persistDirectory;

    private WranglerDev(Process process, string persistDirectory, Uri baseUrl)
    {
        _process = process;
        _persistDirectory = persistDirectory;
        BaseUrl = baseUrl;
    }

    public Uri BaseUrl { get; }

    public static async Task<WranglerDev> StartAsync()
    {
        var workerDirectory = FindWorkerDirectory();
        var persistDirectory = Directory.CreateTempSubdirectory("wrangler-d1-").FullName;

        using (var migrate = StartWrangler(workerDirectory, $"d1 migrations apply DB --local --persist-to \"{persistDirectory}\""))
        {
            var output = await migrate.StandardOutput.ReadToEndAsync();
            var error = await migrate.StandardError.ReadToEndAsync();
            await migrate.WaitForExitAsync();
            if (migrate.ExitCode != 0)
            {
                throw new InvalidOperationException($"Applying D1 migrations failed:\n{output}\n{error}");
            }
        }

        var port = FreePort();
        var process = StartWrangler(
            workerDirectory,
            $"dev --ip 127.0.0.1 --port {port} --persist-to \"{persistDirectory}\" --var WORKER_SECRET:{Secret} --show-interactive-dev-session=false");
        var dev = new WranglerDev(process, persistDirectory, new Uri($"http://127.0.0.1:{port}"));

        try
        {
            await dev.WaitUntilListeningAsync(TimeSpan.FromSeconds(60));
        }
        catch
        {
            await dev.DisposeAsync();
            throw;
        }

        return dev;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync();
        }
        _process.Dispose();
        Directory.Delete(_persistDirectory, recursive: true);
    }

    private async Task WaitUntilListeningAsync(TimeSpan timeout)
    {
        using var client = new HttpClient { BaseAddress = BaseUrl };
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException($"wrangler dev exited with code {_process.ExitCode}.");
            }
            try
            {
                await client.GetAsync("/");
                return;
            }
            catch (HttpRequestException)
            {
                await Task.Delay(250);
            }
        }
        throw new TimeoutException($"wrangler dev did not start listening on {BaseUrl} within {timeout}.");
    }

    private static Process StartWrangler(string workerDirectory, string arguments)
    {
        var startInfo = new ProcessStartInfo("npx", $"--no-install wrangler {arguments}")
        {
            WorkingDirectory = workerDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.Environment["CI"] = "true";
        startInfo.Environment["WRANGLER_SEND_METRICS"] = "false";

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start wrangler.");
        return process;
    }

    private static string FindWorkerDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "worker", "wrangler.jsonc");
            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate)!;
            }
        }
        throw new DirectoryNotFoundException("Could not find worker/wrangler.jsonc above the test output directory.");
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
