using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Spottify.Core.Abstractions;

namespace Spottify.Infrastructure.MusicBrainz;

/// <summary>
/// <see cref="IMetadataProvider"/> backed by the MusicBrainz web service. Politeness (user agent, rate limit,
/// retries) is handled by <see cref="MusicBrainzRequestHandler"/> in the HTTP pipeline.
/// </summary>
internal sealed class MusicBrainzClient(HttpClient http, Microsoft.Extensions.Options.IOptions<MusicBrainzOptions> options) : IMetadataProvider
{
    // MusicBrainz returns at most 100 per page; 25 recordings are enough to see the albums of one song.
    private const int SearchPageSize = 25;

    // Editions are read in pages of 100, at most 5 pages (500 editions) per album, to bound the request count.
    private const int BrowsePageSize = 100;
    private const int MaxBrowsePages = 5;

    public async Task<IReadOnlyList<AlbumCandidate>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var song = SongQuery.Parse(query);
        if (song is null)
        {
            return [];
        }

        // Studio albums first. A song that exists only as a single, on a compilation or live has no hit there,
        // so fall back to the unrestricted search.
        var studio = await SearchRecordingsAsync(song.ToLuceneQuery(studioAlbumsOnly: true), song.Artist, cancellationToken).ConfigureAwait(false);
        if (studio.Count > 0)
        {
            return studio;
        }

        return await SearchRecordingsAsync(song.ToLuceneQuery(), song.Artist, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<AlbumCandidate>> SearchRecordingsAsync(string lucene, string? artist, CancellationToken cancellationToken)
    {
        var url = $"recording?query={Uri.EscapeDataString(lucene)}&limit={SearchPageSize}&fmt=json";
        var response = await GetAsync<RecordingSearchResponse>(url, cancellationToken).ConfigureAwait(false);

        return CandidateRanking.Rank(response?.Recordings ?? [], options.Value.MaxCandidates, artist);
    }

    public async Task<AlbumDetails?> GetAlbumAsync(string releaseGroupId, CancellationToken cancellationToken)
    {
        // Parsing also keeps arbitrary text out of the request URL.
        if (!Guid.TryParse(releaseGroupId, out var id))
        {
            return null;
        }

        var group = id.ToString("D", CultureInfo.InvariantCulture);

        // Editions of an album differ in track count (bonus tracks, vinyl splits). Browse them all, then
        // look up only the one that represents the album best.
        var releases = await BrowseReleasesAsync(group, officialOnly: true, cancellationToken).ConfigureAwait(false);
        if (releases.Count == 0)
        {
            releases = await BrowseReleasesAsync(group, officialOnly: false, cancellationToken).ConfigureAwait(false);
        }

        if (releases.Count == 0)
        {
            return null;
        }

        var chosen = ReleaseChooser.Choose(releases);
        var detail = await GetAsync<ReleaseDto>(
            $"release/{chosen.Id}?inc=recordings+artist-credits&fmt=json", cancellationToken).ConfigureAwait(false);

        if (detail is null)
        {
            return null;
        }

        var tracks = (detail.Media ?? [])
            .OrderBy(m => m.Position)
            .SelectMany(m => (m.Tracks ?? []).OrderBy(t => t.Position))
            .Select(t => t.Title)
            .ToList();

        var year = releases
            .Select(r => CandidateRanking.ParseYear(r.Date))
            .Where(y => y is not null)
            .Min();

        return new AlbumDetails(
            group,
            detail.Id,
            CandidateRanking.JoinArtistCredit(detail.ArtistCredit),
            detail.Title,
            year,
            tracks);
    }

    private async Task<List<ReleaseDto>> BrowseReleasesAsync(string releaseGroupId, bool officialOnly, CancellationToken cancellationToken)
    {
        var status = officialOnly ? "&status=official" : string.Empty;
        var releases = new List<ReleaseDto>();

        // Albums such as reissued classics have hundreds of editions. Read them all (up to a limit) so the
        // standard edition and the original year are not decided by an arbitrary first page.
        for (var page = 0; page < MaxBrowsePages; page++)
        {
            var browse = await GetAsync<ReleaseBrowseResponse>(
                $"release?release-group={releaseGroupId}&inc=media+artist-credits{status}&limit={BrowsePageSize}&offset={page * BrowsePageSize}&fmt=json",
                cancellationToken).ConfigureAwait(false);

            var batch = browse?.Releases ?? [];
            releases.AddRange(batch);

            if (batch.Count < BrowsePageSize || releases.Count >= browse!.ReleaseCount)
            {
                break;
            }
        }

        return releases;
    }

    /// <summary>Returns <c>null</c> for 404 and throws <see cref="HttpRequestException"/> for any other failure.</summary>
    private async Task<T?> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken) where T : class
    {
        using var response = await http.GetAsync(relativeUrl, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Picks the edition that represents an album: the usual track count, in its earliest edition.</summary>
internal static class ReleaseChooser
{
    public static ReleaseDto Choose(IReadOnlyList<ReleaseDto> releases)
    {
        var withTracks = releases.Where(r => TotalTracks(r) > 0).ToList();
        var pool = withTracks.Count > 0 ? withTracks : [.. releases];

        // Bonus-track and vinyl editions are the exception, so the most common length is the standard album.
        return pool
            .GroupBy(TotalTracks)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Min(r => DateKey(r.Date)), StringComparer.Ordinal)
            .First()
            .OrderBy(r => DateKey(r.Date), StringComparer.Ordinal)
            .ThenBy(r => r.Id, StringComparer.Ordinal)
            .First();
    }

    private static int TotalTracks(ReleaseDto release) => release.Media?.Sum(m => m.TrackCount) ?? 0;

    // Partial dates such as "2001" sort correctly as text; a missing date sorts last.
    private static string DateKey(string? date) => string.IsNullOrWhiteSpace(date) ? "9999" : date;
}
