namespace Spottify.Core.Abstractions;

/// <summary>An indexer request failed. Messages never contain the API key.</summary>
public class IndexerException : Exception
{
    public IndexerException(string message)
        : base(message)
    {
    }

    public IndexerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>The indexer rejected the credentials or the account may not use the API. Retrying will not help.</summary>
public sealed class IndexerAuthenticationException(string message) : IndexerException(message);

/// <summary>The daily API or download limit of the account is used up. Try again after it resets.</summary>
public sealed class IndexerLimitReachedException(string message) : IndexerException(message);
