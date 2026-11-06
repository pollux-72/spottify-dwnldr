using Spottify.Infrastructure.MusicBrainz;

namespace Spottify.Tests.MusicBrainz;

public sealed class ReleaseChooserTests
{
    private static ReleaseDto Release(string id, string? date, params int[] discs) =>
        new(id, "Album", "Official", date, null, null, [.. discs.Select((tracks, i) => new MediumDto(i + 1, tracks, null))]);

    [Fact]
    public void The_most_common_track_count_wins_over_an_earlier_deluxe_edition()
    {
        var chosen = ReleaseChooser.Choose(
        [
            Release("deluxe", "2001-01-01", 20),
            Release("standard-late", "2005-05-05", 12),
            Release("standard-early", "2003-03-03", 12),
            Release("standard-mid", "2004-04-04", 12),
        ]);

        Assert.Equal("standard-early", chosen.Id);
    }

    [Fact]
    public void Track_counts_of_all_discs_are_added_up()
    {
        var chosen = ReleaseChooser.Choose(
        [
            Release("double-disc", "2001", 7, 7),
            Release("single-disc", "2002", 14),
            Release("odd-one", "2000", 9),
        ]);

        // The two 14-track editions form the largest group; the earlier of them is picked.
        Assert.Equal("double-disc", chosen.Id);
    }

    [Fact]
    public void Releases_without_a_date_are_chosen_last()
    {
        var chosen = ReleaseChooser.Choose([Release("undated", null, 10), Release("dated", "2010", 10)]);

        Assert.Equal("dated", chosen.Id);
    }

    [Fact]
    public void Releases_without_track_information_are_still_chosen_when_nothing_else_exists()
    {
        var chosen = ReleaseChooser.Choose([new ReleaseDto("only", "Album", "Official", "2001", null, null, null)]);

        Assert.Equal("only", chosen.Id);
    }
}
