using System.Net;
using Spottify.Infrastructure.MusicBrainz;
using static Spottify.Tests.MusicBrainz.MusicBrainzTestHarness;

namespace Spottify.Tests.MusicBrainz;

public sealed class MusicBrainzClientTests
{
    private const string DiscoveryGroupId = "48117b90-a16e-34ca-a514-19c702df1158";

    [Fact]
    public async Task Search_ranks_the_studio_album_before_compilations_although_every_hit_scores_100()
    {
        await using var harness = new MusicBrainzTestHarness(_ => Json(Fixture("recording-search-one-more-time.json")));

        var candidates = await harness.Provider.SearchAsync("Daft Punk - One More Time", CancellationToken.None);

        var first = candidates[0];
        Assert.Equal(DiscoveryGroupId, first.ReleaseGroupId);
        Assert.Equal("Discovery", first.AlbumTitle);
        Assert.Equal("Daft Punk", first.Artist);
        Assert.Equal("Album", first.ReleaseType);
        Assert.Equal("One More Time", first.MatchedTrackTitle);
        Assert.Equal(100, first.Score);
        Assert.All(candidates.Skip(1), c => Assert.Contains("Compilation", c.ReleaseType, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Search_reports_each_album_once_with_its_earliest_official_year()
    {
        await using var harness = new MusicBrainzTestHarness(_ => Json(Fixture("recording-search-one-more-time.json")));

        var candidates = await harness.Provider.SearchAsync("Daft Punk - One More Time", CancellationToken.None);

        Assert.Equal(candidates.Count, candidates.Select(c => c.ReleaseGroupId).Distinct().Count());

        // The group has releases from 2002 to 2024 plus a 2001 bootleg that must not count.
        var discovery = candidates.Single(c => c.ReleaseGroupId == DiscoveryGroupId);
        Assert.Equal(2002, discovery.Year);
    }

    [Fact]
    public async Task Search_ignores_bootleg_releases()
    {
        await using var harness = new MusicBrainzTestHarness(_ => Json(Fixture("recording-search-one-more-time.json")));

        var candidates = await harness.Provider.SearchAsync("Daft Punk - One More Time", CancellationToken.None);

        Assert.DoesNotContain(candidates, c => c.AlbumTitle.Contains("Beta Version", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Search_returns_several_albums_instead_of_choosing_silently()
    {
        await using var harness = new MusicBrainzTestHarness(_ => Json(Fixture("recording-search-one-more-time.json")));

        var candidates = await harness.Provider.SearchAsync("Daft Punk - One More Time", CancellationToken.None);

        Assert.True(candidates.Count > 1);
    }

    [Fact]
    public async Task Search_returns_at_most_the_configured_number_of_albums()
    {
        var options = new MusicBrainzOptions { MinRequestInterval = TimeSpan.Zero, MaxCandidates = 2 };
        await using var harness = new MusicBrainzTestHarness(_ => Json(Fixture("recording-search-one-more-time.json")), options);

        var candidates = await harness.Provider.SearchAsync("Daft Punk - One More Time", CancellationToken.None);

        Assert.Equal(2, candidates.Count);
    }

    [Fact]
    public async Task Search_sends_an_escaped_lucene_query_with_a_user_agent()
    {
        await using var harness = new MusicBrainzTestHarness(_ => Json("""{"recordings":[]}"""));

        await harness.Provider.SearchAsync("AC/DC - Back In Black", CancellationToken.None);

        var request = harness.Requests[0];
        Assert.Equal("/ws/2/recording", request.AbsolutePath);
        Assert.Contains("recording%3A%22Back%20In%20Black%22%20AND%20artist%3A%22AC%2FDC%22", request.Query, StringComparison.Ordinal);
        Assert.Contains("fmt=json", request.Query, StringComparison.Ordinal);
        Assert.All(harness.UserAgents, agent => Assert.StartsWith("SpottifyDwnldr/", agent, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Search_first_restricts_to_official_studio_albums_and_does_not_search_again_when_that_finds_something()
    {
        await using var harness = new MusicBrainzTestHarness(_ => Json(Fixture("recording-search-one-more-time.json")));

        await harness.Provider.SearchAsync("Daft Punk - One More Time", CancellationToken.None);

        var request = Assert.Single(harness.Requests);
        var query = Uri.UnescapeDataString(request.Query);
        Assert.Contains("status:official", query, StringComparison.Ordinal);
        Assert.Contains("primarytype:album", query, StringComparison.Ordinal);
        Assert.Contains("NOT secondarytype:live", query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Search_falls_back_to_an_unrestricted_search_when_no_studio_album_has_the_song()
    {
        var calls = 0;
        await using var harness = new MusicBrainzTestHarness(_ =>
            ++calls == 1 ? Json("""{"recordings":[]}""") : Json(Fixture("recording-search-one-more-time.json")));

        var candidates = await harness.Provider.SearchAsync("Daft Punk - One More Time", CancellationToken.None);

        Assert.NotEmpty(candidates);
        Assert.Equal(2, harness.Requests.Count);
        Assert.DoesNotContain("primarytype", Uri.UnescapeDataString(harness.Requests[1].Query), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Albums_by_the_requested_artist_rank_above_better_scoring_albums_by_others()
    {
        // Modelled on "Daft Punk - Get Lucky": the album track is credited "Daft Punk feat. ..." and scores lower
        // than the exact-title recording on a Various Artists soundtrack.
        const string json = """
            {"recordings":[
              {"title":"Get Lucky","score":100,"artist-credit":[{"name":"Daft Punk","joinphrase":""}],
               "releases":[{"id":"r1","title":"Piece by Piece","status":"Official","date":"2024",
                 "artist-credit":[{"name":"Various Artists","joinphrase":""}],
                 "release-group":{"id":"g1","title":"Piece by Piece","primary-type":"Album","secondary-types":["Soundtrack"]}}]},
              {"title":"Get Lucky (feat. Pharrell Williams & Nile Rodgers)","score":59,
               "artist-credit":[{"name":"Daft Punk","joinphrase":" feat. "},{"name":"Pharrell Williams","joinphrase":""}],
               "releases":[{"id":"r2","title":"Random Access Memories","status":"Official","date":"2013-05-17",
                 "artist-credit":[{"name":"Daft Punk","joinphrase":""}],
                 "release-group":{"id":"g2","title":"Random Access Memories","primary-type":"Album","secondary-types":[]}}]}]}
            """;
        await using var harness = new MusicBrainzTestHarness(_ => Json(json));

        var withArtist = await harness.Provider.SearchAsync("daft punk - Get Lucky", CancellationToken.None);
        var titleOnly = await harness.Provider.SearchAsync("Get Lucky", CancellationToken.None);

        Assert.Equal("Random Access Memories", withArtist[0].AlbumTitle);
        Assert.Equal("Piece by Piece", titleOnly[0].AlbumTitle); // without an artist, the provider's score decides
    }

    [Fact]
    public async Task The_albums_artist_is_the_credit_most_of_its_editions_use()
    {
        // A Japanese edition credits the artist in katakana; the album must still show the usual name.
        const string json = """
            {"recordings":[{"title":"bad guy","score":100,
              "artist-credit":[{"name":"Billie Eilish","joinphrase":""}],
              "releases":[
                {"id":"r1","title":"WWAFAWDWG","status":"Official","date":"2019-03-29",
                  "artist-credit":[{"name":"ビリー・アイリッシュ","joinphrase":""}],
                  "release-group":{"id":"g1","title":"WWAFAWDWG","primary-type":"Album","secondary-types":[]}},
                {"id":"r2","title":"WWAFAWDWG","status":"Official","date":"2019-03-29",
                  "artist-credit":[{"name":"Billie Eilish","joinphrase":""}],
                  "release-group":{"id":"g1","title":"WWAFAWDWG","primary-type":"Album","secondary-types":[]}},
                {"id":"r3","title":"WWAFAWDWG","status":"Official","date":"2019-04-01",
                  "artist-credit":[{"name":"Billie Eilish","joinphrase":""}],
                  "release-group":{"id":"g1","title":"WWAFAWDWG","primary-type":"Album","secondary-types":[]}}]}]}
            """;
        await using var harness = new MusicBrainzTestHarness(_ => Json(json));

        var candidates = await harness.Provider.SearchAsync("Billie Eilish - bad guy", CancellationToken.None);

        Assert.Equal("Billie Eilish", Assert.Single(candidates).Artist);
    }

    [Fact]
    public async Task Blank_search_makes_no_request()
    {
        await using var harness = new MusicBrainzTestHarness(_ => Json("{}"));

        var candidates = await harness.Provider.SearchAsync("   ", CancellationToken.None);

        Assert.Empty(candidates);
        Assert.Empty(harness.Requests);
    }

    [Fact]
    public async Task Album_details_use_the_standard_edition_and_list_all_tracks()
    {
        await using var harness = new MusicBrainzTestHarness(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/release", StringComparison.Ordinal)
                ? Json(Fixture("release-browse-discovery.json"))
                : Json(Fixture("release-lookup-discovery.json")));

        var album = await harness.Provider.GetAlbumAsync(DiscoveryGroupId, CancellationToken.None);

        Assert.NotNull(album);
        Assert.Equal(DiscoveryGroupId, album.ReleaseGroupId);
        Assert.Equal("Discovery", album.AlbumTitle);
        Assert.Equal("Daft Punk", album.Artist);
        Assert.Equal(2001, album.Year);
        Assert.Equal(14, album.TrackCount);
        Assert.Equal("One More Time", album.TrackTitles[0]);
        Assert.Equal("Aerodynamic", album.TrackTitles[1]);
    }

    [Fact]
    public async Task Album_details_look_up_the_earliest_release_with_the_most_common_track_count()
    {
        await using var harness = new MusicBrainzTestHarness(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/release", StringComparison.Ordinal)
                ? Json(Fixture("release-browse-discovery.json"))
                : Json(Fixture("release-lookup-discovery.json")));

        await harness.Provider.GetAlbumAsync(DiscoveryGroupId, CancellationToken.None);

        Assert.Equal(2, harness.Requests.Count);
        Assert.Contains("release-group=" + DiscoveryGroupId, harness.Requests[0].Query, StringComparison.Ordinal);
        Assert.StartsWith("/ws/2/release/", harness.Requests[1].AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("inc=recordings", harness.Requests[1].Query, StringComparison.Ordinal);

        // All 23 recorded editions have 14 tracks; the earliest dated one is the lookup target.
        Assert.EndsWith("/30e45d86-707d-4042-9df5-93724557aadb", harness.Requests[1].AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Album_details_are_null_for_an_unknown_album()
    {
        await using var harness = new MusicBrainzTestHarness(_ => Json("""{"releases":[]}"""));

        var album = await harness.Provider.GetAlbumAsync(DiscoveryGroupId, CancellationToken.None);

        Assert.Null(album);
        Assert.Equal(2, harness.Requests.Count); // official-only browse, then the fallback without the filter
    }

    [Fact]
    public async Task Album_details_are_null_for_a_404()
    {
        await using var harness = new MusicBrainzTestHarness(_ => Json("{}", HttpStatusCode.NotFound));

        Assert.Null(await harness.Provider.GetAlbumAsync(DiscoveryGroupId, CancellationToken.None));
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("48117b90-a16e-34ca-a514-19c702df1158&limit=1")]
    [InlineData("")]
    public async Task Album_ids_that_are_not_guids_never_reach_the_network(string id)
    {
        await using var harness = new MusicBrainzTestHarness(_ => Json("{}"));

        Assert.Null(await harness.Provider.GetAlbumAsync(id, CancellationToken.None));
        Assert.Empty(harness.Requests);
    }

    [Fact]
    public async Task Server_errors_surface_as_http_request_exceptions()
    {
        var options = new MusicBrainzOptions { MinRequestInterval = TimeSpan.Zero, MaxRetries = 0 };
        await using var harness = new MusicBrainzTestHarness(_ => Json("{}", HttpStatusCode.InternalServerError), options);

        await Assert.ThrowsAsync<HttpRequestException>(() => harness.Provider.SearchAsync("A - B", CancellationToken.None));
    }
}
