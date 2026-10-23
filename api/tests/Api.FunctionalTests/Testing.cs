namespace StoneLasley.Api.FunctionalTests;

[SetUpFixture]
public class Testing
{
    private static WranglerDev? _worker;

    public static WranglerDev Worker => _worker ?? throw new InvalidOperationException("wrangler dev is not running.");

    [OneTimeSetUp]
    public async Task StartWorker() => _worker = await WranglerDev.StartAsync();

    [OneTimeTearDown]
    public async Task StopWorker()
    {
        if (_worker is not null)
        {
            await _worker.DisposeAsync();
        }
    }
}
