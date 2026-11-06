using Microsoft.Extensions.DependencyInjection;
using Spottify.Core.Abstractions;
using Spottify.Infrastructure.MusicBrainz;

namespace Spottify.Tests.MusicBrainz;

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
