using StoneLasley.Application.Common.Interfaces;

namespace StoneLasley.Api.FunctionalTests;

public sealed class FakeSiteRevalidator : ISiteRevalidator
{
    private int _calls;

    public int Calls => _calls;

    public Task RevalidateAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        return Task.CompletedTask;
    }
}
