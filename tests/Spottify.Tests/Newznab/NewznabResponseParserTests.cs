using System.Text;
using Spottify.Core.Abstractions;
using Spottify.Infrastructure.Newznab;

namespace Spottify.Tests.Newznab;

public sealed class NewznabResponseParserTests
{
    private static MemoryStream Xml(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

    private static IReadOnlyList<IndexerRelease> ParseFixture() =>
        NewznabResponseParser.Parse(File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Newznab", "Fixtures", "search-discovery.xml")));

    [Fact]
    public void A_complete_item_yields_title_size_age_category_and_link()
    {
        var release = ParseFixture()[0];

        Assert.Equal("Daft.Punk-Discovery-2001-FLAC", release.Title);
        Assert.Equal(312_345_678, release.SizeBytes);
        Assert.Equal(new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero), release.PublishedAt);
        Assert.Equal("3040", release.Category); // the sub category, not the parent 3000
        Assert.Equal("https://indexer.example/getnzb/aaa111.nzb&i=7&r=SECRETKEY123", release.NzbUrl.ToString());
    }

    [Fact]
    public void Size_falls_back_to_the_enclosure_and_dates_keep_their_offset()
    {
        var release = ParseFixture()[1];

        Assert.Equal(98_765_432, release.SizeBytes);
        Assert.Equal("3010", release.Category);
        Assert.Equal(new DateTimeOffset(2018, 11, 5, 8, 30, 0, TimeSpan.Zero), release.PublishedAt.ToUniversalTime());
    }

    [Fact]
    public void The_enclosure_supplies_the_link_when_there_is_no_link_element()
    {
        var release = ParseFixture()[2];

        Assert.Equal("Daft.Punk.Discovery.WEB.2021", release.Title);
        Assert.Equal("https://indexer.example/getnzb/ccc333.nzb&r=SECRETKEY123", release.NzbUrl.ToString());
        Assert.Equal(250_000_000, release.SizeBytes);
        Assert.Equal("3000", release.Category); // only the parent was listed
        Assert.Equal(DateTimeOffset.MinValue, release.PublishedAt); // unknown date
    }

    [Fact]
    public void Items_without_a_title_or_with_an_unusable_link_are_skipped()
    {
        var titles = ParseFixture().Select(r => r.Title).ToList();

        Assert.Equal(4, titles.Count);
        Assert.DoesNotContain("Daft.Punk-Discovery-Broken", titles);
    }

    [Fact]
    public void A_size_that_is_not_a_number_becomes_zero_and_a_text_category_is_kept()
    {
        var release = ParseFixture()[3];

        Assert.Equal("Daft.Punk-Discovery-Remastered", release.Title);
        Assert.Equal(0, release.SizeBytes);
        Assert.Equal("Audio", release.Category);
    }

    [Fact]
    public void A_date_with_the_wrong_weekday_is_still_read()
    {
        // 6 November 2026 is a Friday; some indexers send another weekday.
        const string feed = """
            <rss version="2.0"><channel><item><title>T</title><link>https://x.example/a.nzb</link>
            <pubDate>Tue, 06 Nov 2026 12:00:00 +0000</pubDate></item></channel></rss>
            """;

        var release = Assert.Single(NewznabResponseParser.Parse(Xml(feed)));

        Assert.Equal(new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero), release.PublishedAt);
    }

    [Fact]
    public void A_feed_without_items_is_an_empty_result()
    {
        const string empty = """<rss version="2.0"><channel><title>x</title></channel></rss>""";

        Assert.Empty(NewznabResponseParser.Parse(Xml(empty)));
    }

    [Theory]
    [InlineData(100, typeof(IndexerAuthenticationException))]
    [InlineData(101, typeof(IndexerAuthenticationException))]
    [InlineData(102, typeof(IndexerAuthenticationException))]
    [InlineData(500, typeof(IndexerLimitReachedException))]
    [InlineData(501, typeof(IndexerLimitReachedException))]
    [InlineData(200, typeof(IndexerException))]
    [InlineData(900, typeof(IndexerException))]
    public void Error_documents_become_the_matching_exception(int code, Type expected)
    {
        var xml = $"""<?xml version="1.0"?><error code="{code}" description="Something happened"/>""";

        var failure = Assert.ThrowsAny<IndexerException>(() => NewznabResponseParser.Parse(Xml(xml)));

        Assert.Equal(expected, failure.GetType());
        Assert.Contains($"error {code}", failure.Message, StringComparison.Ordinal);
        Assert.Contains("Something happened", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("this is not xml")]
    [InlineData("")]
    [InlineData("<rss><channel>")]
    public void Answers_that_are_not_xml_are_reported_as_indexer_errors(string text) =>
        Assert.ThrowsAny<IndexerException>(() => NewznabResponseParser.Parse(Xml(text)));

    [Fact]
    public void Xml_that_is_not_a_feed_is_rejected()
    {
        var failure = Assert.ThrowsAny<IndexerException>(() => NewznabResponseParser.Parse(Xml("<html><body>Maintenance</body></html>")));

        Assert.Contains("not a Newznab feed", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Documents_with_a_dtd_are_refused_so_entities_cannot_be_expanded()
    {
        const string bomb = """
            <?xml version="1.0"?>
            <!DOCTYPE rss [<!ENTITY a "aaaaaaaaaa"><!ENTITY b "&a;&a;&a;&a;&a;&a;&a;&a;&a;&a;">]>
            <rss version="2.0"><channel><item><title>&b;</title><link>https://x.example/a.nzb</link></item></channel></rss>
            """;

        Assert.ThrowsAny<IndexerException>(() => NewznabResponseParser.Parse(Xml(bomb)));
    }
}
