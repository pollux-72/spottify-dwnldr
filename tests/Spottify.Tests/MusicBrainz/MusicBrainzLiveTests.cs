using Microsoft.Extensions.DependencyInjection;
using Spottify.Core.Abstractions;
using Spottify.Infrastructure.MusicBrainz;

namespace Spottify.Tests.MusicBrainz;

/// <summary>Runs only when <c>SPOTTIFY_LIVE_TESTS=1</c>, so CI and normal runs never hit the real service.</summary>
public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SPOTTIFY_LIVE_TESTS") != "1")
        {
            Skip = "Set SPOTTIFY_LIVE_TESTS=1 to call the real MusicBrainz API.";
        }
    }
}

public sealed class MusicBrainzLiveTests
{
    [LiveFact]
    public async Task A_well_known_song_resolves_to_its_studio_album_with_the_full_track_list()
    {
        var services = new ServiceCollection();
        services.AddMusicBrainz(new MusicBrainzOptions());
        await using var provider = services.BuildServiceProvider();
        var metadata = provider.GetRequiredService<IMetadataProvider>();

        var candidates = await metadata.SearchAsync("Daft Punk - One More Time", CancellationToken.None);

        var top = candidates[0];
        Assert.Equal("Discovery", top.AlbumTitle);

        var album = await metadata.GetAlbumAsync(top.ReleaseGroupId, CancellationToken.None);

        Assert.NotNull(album);
        Assert.Equal(14, album.TrackCount);
        Assert.Contains("One More Time", album.TrackTitles);
    }
}
