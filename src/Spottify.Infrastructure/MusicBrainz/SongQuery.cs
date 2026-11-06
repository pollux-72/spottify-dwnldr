using System.Text;

namespace Spottify.Infrastructure.MusicBrainz;

/// <summary>A song request split into artist and title, as typed by the user ("Artist - Title").</summary>
internal sealed record SongQuery(string? Artist, string Title)
{
    // Chat clients often turn a typed hyphen into an en or em dash.
    private static readonly string[] Separators = [" - ", " – ", " — "];

    public static SongQuery? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var trimmed = text.Trim();

        // Split at the first separator: artists rarely contain " - ", song titles sometimes do.
        var best = -1;
        var length = 0;
        foreach (var separator in Separators)
        {
            var index = trimmed.IndexOf(separator, StringComparison.Ordinal);
            if (index > 0 && (best < 0 || index < best))
            {
                best = index;
                length = separator.Length;
            }
        }

        if (best < 0)
        {
            return new SongQuery(null, trimmed);
        }

        var artist = trimmed[..best].Trim();
        var title = trimmed[(best + length)..].Trim();

        return title.Length == 0 ? new SongQuery(null, artist) : new SongQuery(artist, title);
    }

    // A page of results holds only 25 recordings. For popular songs the first page is full of live bootlegs
    // and compilations, so the studio album may not be on it at all. Restricting the first search to official
    // studio albums puts the relevant ones on the page.
    private const string StudioAlbumFilter =
        " AND status:official AND primarytype:album AND NOT secondarytype:live AND NOT secondarytype:compilation";

    /// <summary>Builds the Lucene query for the recording search. Values are quoted phrases.</summary>
    /// <param name="studioAlbumsOnly">Restrict to official studio albums (see <see cref="StudioAlbumFilter"/>).</param>
    public string ToLuceneQuery(bool studioAlbumsOnly = false)
    {
        var query = $"recording:{Phrase(Title)}";
        if (Artist is not null)
        {
            query += $" AND artist:{Phrase(Artist)}";
        }

        return studioAlbumsOnly ? query + StudioAlbumFilter : query;
    }

    private static string Phrase(string value)
    {
        var escaped = new StringBuilder(value.Length + 2).Append('"');
        foreach (var c in value)
        {
            if (c is '"' or '\\')
            {
                escaped.Append('\\');
            }

            escaped.Append(c);
        }

        return escaped.Append('"').ToString();
    }
}
