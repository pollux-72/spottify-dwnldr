using System.Text.Json.Serialization;

namespace Spottify.Infrastructure.MusicBrainz;

// Only the fields the client reads. Shapes were checked against the live API (see the test fixtures).

internal sealed record RecordingSearchResponse(
    [property: JsonPropertyName("recordings")] List<RecordingDto>? Recordings);

internal sealed record RecordingDto(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("score")] int Score,
    [property: JsonPropertyName("artist-credit")] List<ArtistCreditDto>? ArtistCredit,
    [property: JsonPropertyName("releases")] List<ReleaseDto>? Releases);

internal sealed record ArtistCreditDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("joinphrase")] string? JoinPhrase);

internal sealed record ReleaseDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("date")] string? Date,
    [property: JsonPropertyName("artist-credit")] List<ArtistCreditDto>? ArtistCredit,
    [property: JsonPropertyName("release-group")] ReleaseGroupDto? ReleaseGroup,
    [property: JsonPropertyName("media")] List<MediumDto>? Media);

internal sealed record ReleaseGroupDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("primary-type")] string? PrimaryType,
    [property: JsonPropertyName("secondary-types")] List<string>? SecondaryTypes);

internal sealed record MediumDto(
    [property: JsonPropertyName("position")] int Position,
    [property: JsonPropertyName("track-count")] int TrackCount,
    [property: JsonPropertyName("tracks")] List<TrackDto>? Tracks);

internal sealed record TrackDto(
    [property: JsonPropertyName("position")] int Position,
    [property: JsonPropertyName("title")] string Title);

internal sealed record ReleaseBrowseResponse(
    [property: JsonPropertyName("release-count")] int ReleaseCount,
    [property: JsonPropertyName("releases")] List<ReleaseDto>? Releases);
