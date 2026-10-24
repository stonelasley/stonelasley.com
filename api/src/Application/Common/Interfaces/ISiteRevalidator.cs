namespace StoneLasley.Application.Common.Interfaces;

/// <summary>
/// Tells the site its content changed, so pre-rendered pages are rebuilt from the API.
/// </summary>
public interface ISiteRevalidator
{
    Task RevalidateAsync(CancellationToken cancellationToken);
}
