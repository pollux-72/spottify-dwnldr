using System.ComponentModel.DataAnnotations;

namespace Spottify.Host;

/// <summary>
/// Settings of the core service. Bound from the <c>Spottify</c> section of appsettings.json and from
/// environment variables with the prefix <c>SPOTTIFY_</c>, e.g. <c>SPOTTIFY_Spottify__DataDirectory=/data</c>.
/// Secrets (API keys, tokens) are added by later tasks and must only come from the environment.
/// </summary>
public sealed class SpottifyOptions
{
    public const string SectionName = "Spottify";

    /// <summary>Folder for persistent state. In Docker this is the mounted <c>/data</c> volume.</summary>
    [Required(AllowEmptyStrings = false)]
    public string DataDirectory { get; set; } = "data";

    /// <summary>SQLite file name inside <see cref="DataDirectory"/>.</summary>
    [Required(AllowEmptyStrings = false)]
    public string DatabaseFileName { get; set; } = "spottify.db";

    public string DatabasePath => Path.Combine(DataDirectory, DatabaseFileName);
}
