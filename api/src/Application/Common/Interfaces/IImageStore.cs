namespace StoneLasley.Application.Common.Interfaces;

/// <summary>
/// Where Post images live (R2, through the Worker). Keys are content hashes, so a stored image never changes.
/// </summary>
public interface IImageStore
{
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);

    Task PutAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken);

    /// <summary>The URL readers load the image from: the bucket's custom domain.</summary>
    Uri PublicUrl(string key);
}
