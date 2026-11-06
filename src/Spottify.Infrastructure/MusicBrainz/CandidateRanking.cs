using System.Globalization;
using System.Text;
using Spottify.Core.Abstractions;

namespace Spottify.Infrastructure.MusicBrainz;

/// <summary>
/// Turns a recording search result into a ranked list of albums.
/// A song usually appears on dozens of releases (studio album, reissues, singles, compilations, DJ mixes).
/// Releases are folded into their release group so each album appears once, and the list is ordered so that
/// the original studio album comes first.
/// </summary>
internal static class CandidateRanking
{
    // Release groups carrying one of these secondary types are not what someone asking for a song wants.
    private static readonly HashSet<string> NoiseTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Compilation", "Live", "Remix", "DJ-mix", "Mixtape/Street", "Demo", "Spokenword",
        "Interview", "Audiobook", "Audio drama", "Field recording",
    };

    /// <param name="requestedArtist">
    /// The artist the user asked for, if any. Albums by that artist rank first: MusicBrainz scores a recording higher
    /// the closer its credit is to the query, so "Get Lucky" by "Daft Punk feat. Pharrell Williams" (the album track)
    /// scores below an exact "Get Lucky" on a Various Artists soundtrack, which is not what the user wants.
    /// </param>
    public static IReadOnlyList<AlbumCandidate> Rank(IEnumerable<RecordingDto> recordings, int maxCandidates, string? requestedArtist = null)
    {
        var groups = new Dictionary<string, Group>(StringComparer.OrdinalIgnoreCase);

        foreach (var recording in recordings)
        {
            foreach (var release in recording.Releases ?? [])
            {
                if (release.ReleaseGroup is not { } releaseGroup || IsUnofficial(release.Status))
                {
                    continue;
                }

                if (!groups.TryGetValue(releaseGroup.Id, out var group))
                {
                    group = new Group(releaseGroup);
                    groups.Add(releaseGroup.Id, group);
                }

                group.Add(recording, release);
            }
        }

        var wanted = Normalize(requestedArtist);

        return groups.Values
            .OrderByDescending(g => MatchesArtist(g.Artist, wanted))
            .ThenByDescending(g => g.Score)
            .ThenBy(g => g.TypeRank)
            .ThenByDescending(g => g.ReleaseCount)
            .ThenBy(g => g.Year ?? int.MaxValue)
            .ThenBy(g => g.Title, StringComparer.Ordinal)
            .Take(maxCandidates)
            .Select(g => g.ToCandidate())
            .ToList();
    }

    private static bool MatchesArtist(string albumArtist, string wanted) =>
        wanted.Length > 0 && Normalize(albumArtist).Contains(wanted, StringComparison.Ordinal);

    // Compare artists by letters and digits only, ignoring case and accents ("Beyoncé" == "beyonce", "AC/DC" == "ac dc").
    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var result = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (char.IsLetterOrDigit(c))
            {
                result.Append(char.ToLowerInvariant(c));
            }
        }

        return result.ToString();
    }

    public static string JoinArtistCredit(IEnumerable<ArtistCreditDto>? credits)
    {
        var text = new StringBuilder();
        foreach (var credit in credits ?? [])
        {
            text.Append(credit.Name).Append(credit.JoinPhrase);
        }

        return text.ToString().Trim();
    }

    public static int? ParseYear(string? date) =>
        date is { Length: >= 4 } && int.TryParse(date.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            ? year
            : null;

    // Bootlegs and pseudo-releases are not real editions and would only add noise.
    private static bool IsUnofficial(string? status) =>
        string.Equals(status, "Bootleg", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Pseudo-Release", StringComparison.OrdinalIgnoreCase);

    private sealed class Group(ReleaseGroupDto releaseGroup)
    {
        // Editions credit the artist differently (a Japanese edition may use katakana), so the album's artist is
        // the credit most of its editions agree on.
        private readonly Dictionary<string, int> _artistVotes = new(StringComparer.Ordinal);
        private string _trackTitle = string.Empty;

        public string Artist => _artistVotes.Count == 0 ? string.Empty : _artistVotes.MaxBy(vote => vote.Value).Key;

        public int Score { get; private set; } = -1;

        public int ReleaseCount { get; private set; }

        public int? Year { get; private set; }

        public string Title { get; private set; } = releaseGroup.Title ?? string.Empty;

        public int TypeRank => Rank(releaseGroup);

        public void Add(RecordingDto recording, ReleaseDto release)
        {
            ReleaseCount++;

            if (ParseYear(release.Date) is { } year && (Year is null || year < Year))
            {
                Year = year;
            }

            if (Title.Length == 0)
            {
                Title = release.Title;
            }

            var artist = JoinArtistCredit(release.ArtistCredit ?? recording.ArtistCredit);
            _artistVotes[artist] = _artistVotes.GetValueOrDefault(artist) + 1;

            // Keep what the best-scoring recording says about the song title.
            if (recording.Score > Score)
            {
                Score = recording.Score;
                _trackTitle = recording.Title;
            }
        }

        public AlbumCandidate ToCandidate() => new(
            releaseGroup.Id,
            Artist,
            Title,
            Year,
            Describe(releaseGroup),
            _trackTitle,
            Score);

        private static string Describe(ReleaseGroupDto group)
        {
            var parts = new List<string> { group.PrimaryType ?? "Other" };
            parts.AddRange(group.SecondaryTypes ?? []);
            return string.Join('+', parts);
        }

        // 0 = studio album, 1 = single or EP, 2 = other album variants (soundtrack ...), 3 = compilations, live, mixes.
        private static int Rank(ReleaseGroupDto group)
        {
            var secondary = group.SecondaryTypes ?? [];
            if (secondary.Any(NoiseTypes.Contains))
            {
                return 3;
            }

            return (group.PrimaryType, secondary.Count) switch
            {
                ("Album", 0) => 0,
                ("Single" or "EP", 0) => 1,
                ("Album" or "Single" or "EP", _) => 2,
                _ => 3,
            };
        }
    }
}
