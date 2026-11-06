using System.ComponentModel.DataAnnotations;
using Spottify.Infrastructure.MusicBrainz;

namespace Spottify.Tests.MusicBrainz;

public sealed class MusicBrainzOptionsTests
{
    private static List<ValidationResult> Validate(MusicBrainzOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Defaults_are_valid() => Assert.Empty(Validate(new MusicBrainzOptions()));

    [Theory]
    [InlineData("not a url")]
    [InlineData("musicbrainz.org/ws/2")]
    [InlineData("ftp://musicbrainz.org/ws/2/")]
    [InlineData("file:///etc/passwd")]
    public void Base_url_must_be_an_absolute_http_or_https_address(string url)
    {
        var errors = Validate(new MusicBrainzOptions { BaseUrl = url });

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(MusicBrainzOptions.BaseUrl)));
    }

    [Theory]
    [InlineData("https://musicbrainz.org/ws/2", "https://musicbrainz.org/ws/2/")]
    [InlineData("https://musicbrainz.org/ws/2/", "https://musicbrainz.org/ws/2/")]
    [InlineData("http://localhost:5000/mb", "http://localhost:5000/mb/")]
    public void The_trailing_slash_is_added_when_missing(string url, string expected)
    {
        var options = new MusicBrainzOptions { BaseUrl = url };

        Assert.Empty(Validate(options));
        Assert.Equal(expected, options.BaseAddress.ToString());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Retries_are_bounded(int retries) =>
        Assert.NotEmpty(Validate(new MusicBrainzOptions { MaxRetries = retries }));

    [Fact]
    public void Timeouts_must_be_positive() =>
        Assert.NotEmpty(Validate(new MusicBrainzOptions { RequestTimeout = TimeSpan.Zero }));
}
