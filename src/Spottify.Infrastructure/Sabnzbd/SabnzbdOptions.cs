using System.ComponentModel.DataAnnotations;
using Spottify.Infrastructure.Http;

namespace Spottify.Infrastructure.Sabnzbd;

/// <summary>
/// Settings of SABnzbd. Bound from <c>Spottify:Sabnzbd</c>. The API key is a secret: set it through the environment
/// (<c>SPOTTIFY_Spottify__Sabnzbd__ApiKey</c>) and never commit it.
/// </summary>
public sealed class SabnzbdOptions : IValidatableObject
{
    public const string SectionName = "Spottify:Sabnzbd";

    /// <summary>
    /// Address of SABnzbd, for example <c>http://sabnzbd:8080/</c>. The API is called at <c>api</c> below it.
    /// Plain <c>http</c> is accepted only for local addresses (see <see cref="LocalAddress.Description"/>).
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>The API key of SABnzbd (Config, General). The NZB key does not work for this.</summary>
    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Category given to every job. It decides the download folder and lets SABnzbd treat music differently from
    /// other downloads. Create it in SABnzbd under Config, Categories.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(64)]
    public string Category { get; set; } = "music";

    /// <summary>Longest wait for the whole answer, headers and body.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:05:00")]
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Largest answer that is read. A job status is a few kilobytes.</summary>
    [Range(1024, 200_000_000)]
    public long MaxResponseBytes { get; set; } = 5_000_000;

    /// <summary>
    /// Folder prefix of finished downloads as <b>SABnzbd</b> sees it, for example <c>/downloads</c>. Only needed when
    /// SABnzbd and this service see the same folder under different paths; set it together with <see cref="LocalPath"/>.
    /// </summary>
    public string? RemotePath { get; set; }

    /// <summary>The same folder as <b>this service</b> sees it, for example <c>/data/downloads</c>.</summary>
    public string? LocalPath { get; set; }

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

        if (string.IsNullOrWhiteSpace(RemotePath) != string.IsNullOrWhiteSpace(LocalPath))
        {
            yield return new ValidationResult(
                $"{nameof(RemotePath)} and {nameof(LocalPath)} must be set together or not at all.",
                [nameof(RemotePath), nameof(LocalPath)]);
        }
    }
}
