using System.ComponentModel.DataAnnotations;
using Spottify.Infrastructure.Http;

namespace Spottify.Infrastructure.Newznab;

/// <summary>
/// Settings of the Usenet indexer. Bound from <c>Spottify:Indexer</c>. The API key is a secret: set it through the
/// environment (<c>SPOTTIFY_Spottify__Indexer__ApiKey</c>) and never commit it.
/// </summary>
public sealed class NewznabOptions : IValidatableObject
{
    public const string SectionName = "Spottify:Indexer";

    /// <summary>
    /// Address of the indexer, for example <c>https://indexer.example/</c>. The API is called at <c>api</c> below it,
    /// so an indexer with a path such as <c>https://example.org/newznab/</c> works too. Plain <c>http</c> is accepted
    /// only for local addresses (localhost, private network ranges, single-word host names such as a Docker service),
    /// because the API key is part of every request.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string BaseUrl { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Comma separated Newznab category ids. 3000 is all of Audio (MP3, Lossless, ...).</summary>
    [Required, RegularExpression(@"^\d+(,\d+)*$", ErrorMessage = "Categories must be comma separated numbers such as 3000 or 3010,3040.")]
    public string Categories { get; set; } = "3000";

    /// <summary>How many releases one search returns at most.</summary>
    [Range(1, 200)]
    public int Limit { get; set; } = 50;

    /// <summary>Longest wait for the whole answer, headers and body.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:05:00")]
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Largest answer that is read. A search result of 200 releases is well below one megabyte.</summary>
    [Range(1024, 200_000_000)]
    public long MaxResponseBytes { get; set; } = 20_000_000;

    /// <summary><see cref="BaseUrl"/> with the trailing slash that relative request paths need.</summary>
    public Uri BaseAddress
    {
        get
        {
            var url = BaseUrl.EndsWith('/') ? BaseUrl : BaseUrl + "/";
            return new Uri(url, UriKind.Absolute);
        }
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            yield return new ValidationResult(
                $"{nameof(BaseUrl)} must be an absolute http or https address.", [nameof(BaseUrl)]);
        }
        else if (uri.Scheme == "http" && !LocalAddress.IsLocal(uri))
        {
            yield return new ValidationResult(
                $"{nameof(BaseUrl)} uses http, so the API key would cross the network unencrypted. Use https. "
                + LocalAddress.Description,
                [nameof(BaseUrl)]);
        }
    }
}
