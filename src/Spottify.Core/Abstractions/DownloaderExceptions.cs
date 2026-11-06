namespace Spottify.Core.Abstractions;

/// <summary>A request to the downloader failed. Messages never contain the API key.</summary>
public class DownloaderException : Exception
{
    public DownloaderException(string message)
        : base(message)
    {
    }

    public DownloaderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>The downloader rejected the API key. Retrying will not help.</summary>
public sealed class DownloaderAuthenticationException(string message) : DownloaderException(message);
