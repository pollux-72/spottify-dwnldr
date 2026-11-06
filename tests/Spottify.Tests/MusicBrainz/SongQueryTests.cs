using Spottify.Infrastructure.MusicBrainz;

namespace Spottify.Tests.MusicBrainz;

public sealed class SongQueryTests
{
    [Theory]
    [InlineData("Daft Punk - One More Time", "Daft Punk", "One More Time")]
    [InlineData("  Daft Punk  -  One More Time  ", "Daft Punk", "One More Time")]
    [InlineData("Daft Punk – One More Time", "Daft Punk", "One More Time")]
    [InlineData("Daft Punk — One More Time", "Daft Punk", "One More Time")]
    [InlineData("Jay-Z - 99 Problems", "Jay-Z", "99 Problems")]
    [InlineData("Artist - Song - Remastered", "Artist", "Song - Remastered")]
    public void Artist_and_title_are_split_at_the_first_separator(string input, string artist, string title)
    {
        var query = SongQuery.Parse(input);

        Assert.Equal(new SongQuery(artist, title), query);
    }

    [Theory]
    [InlineData("One More Time")]
    [InlineData("Jay-Z")]
    [InlineData("- Song")]
    public void Text_without_a_usable_separator_is_a_title_only_search(string input)
    {
        var query = SongQuery.Parse(input);

        Assert.NotNull(query);
        Assert.Null(query.Artist);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_input_yields_no_query(string? input) => Assert.Null(SongQuery.Parse(input));

    [Fact]
    public void Lucene_query_uses_quoted_phrases_for_both_fields()
    {
        var lucene = new SongQuery("Daft Punk", "One More Time").ToLuceneQuery();

        Assert.Equal("recording:\"One More Time\" AND artist:\"Daft Punk\"", lucene);
    }

    [Fact]
    public void Quotes_and_backslashes_are_escaped_so_user_text_cannot_change_the_query()
    {
        var lucene = new SongQuery(null, "He said \"hi\" \\ OR artist:*").ToLuceneQuery();

        Assert.Equal("recording:\"He said \\\"hi\\\" \\\\ OR artist:*\"", lucene);
    }

    [Fact]
    public void Studio_restriction_is_appended_only_when_requested()
    {
        var song = new SongQuery("Nirvana", "Smells Like Teen Spirit");

        var plain = song.ToLuceneQuery();
        var studio = song.ToLuceneQuery(studioAlbumsOnly: true);

        Assert.DoesNotContain("primarytype", plain, StringComparison.Ordinal);
        Assert.StartsWith(plain, studio, StringComparison.Ordinal);
        Assert.Contains("status:official AND primarytype:album", studio, StringComparison.Ordinal);
    }
}
