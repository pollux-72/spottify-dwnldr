using System.ComponentModel.DataAnnotations;

namespace Spottify.Infrastructure.MusicBrainz;

/// <summary>Settings for the MusicBrainz web service. Bound from <c>Spottify:MusicBrainz</c>.</summary>
public sealed class MusicBrainzOptions
{
    public const string SectionName = "Spottify:MusicBrainz";

    /// <summary>Must end with a slash so relative request paths resolve below it.</summary>
    [Required, Url]
    public string BaseUrl { get; set; } = "https://musicbrainz.org/ws/2/";

    /// <summary>
    /// MusicBrainz blocks clients without a meaningful User-Agent. Use the form <c>Name/Version (contact)</c>,
    /// where the contact is a project URL or an address you are happy to publish.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string UserAgent { get; set; } = "SpottifyDwnldr/0.1 (https://github.com/pollux-72/spottify-dwnldr)";

    /// <summary>MusicBrainz allows one request per second on average; the default keeps a safety margin.</summary>
    [Range(typeof(TimeSpan), "00:00:00", "00:01:00")]
    public TimeSpan MinRequestInterval { get; set; } = TimeSpan.FromMilliseconds(1100);

    /// <summary>How often a request is repeated after a 429 or 5xx answer before the error is surfaced.</summary>
    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;

    /// <summary>Wait before the first retry when the server sends no Retry-After; later retries wait longer.</summary>
    [Range(typeof(TimeSpan), "00:00:00", "00:05:00")]
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Upper bound of albums returned from a search.</summary>
    [Range(1, 25)]
    public int MaxCandidates { get; set; } = 8;
}
