using System.ComponentModel.DataAnnotations;
using Spottify.Infrastructure.Sabnzbd;

namespace Spottify.Tests.Sabnzbd;

public sealed class SabnzbdOptionsTests
{
    private static SabnzbdOptions Valid() => new() { BaseUrl = "http://sabnzbd:8080/", ApiKey = "key" };

    private static List<ValidationResult> Validate(SabnzbdOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void A_complete_configuration_is_valid() => Assert.Empty(Validate(Valid()));

    [Fact]
    public void Address_and_key_are_required()
    {
        var errors = Validate(new SabnzbdOptions());

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(SabnzbdOptions.BaseUrl)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(SabnzbdOptions.ApiKey)));
    }

    [Fact]
    public void The_category_defaults_to_music_and_cannot_be_empty()
    {
        Assert.Equal("music", Valid().Category);

        var options = Valid();
        options.Category = string.Empty;
        Assert.NotEmpty(Validate(options));
    }

    [Theory]
    [InlineData("sabnzbd:8080")]
    [InlineData("ftp://sabnzbd/")]
    [InlineData("not a url")]
    public void The_address_must_be_absolute_http_or_https(string url)
    {
        var options = Valid();
        options.BaseUrl = url;

        Assert.Contains(Validate(options), e => e.MemberNames.Contains(nameof(SabnzbdOptions.BaseUrl)));
    }

    [Theory]
    [InlineData("https://sab.example.com/")]
    [InlineData("http://sabnzbd:8080/")]
    [InlineData("http://localhost:8080")]
    [InlineData("http://192.168.1.5:8080/")]
    [InlineData("http://nas.local:8080/")]
    public void The_key_may_travel_over_https_or_inside_the_local_network(string url)
    {
        var options = Valid();
        options.BaseUrl = url;

        Assert.Empty(Validate(options));
    }

    [Theory]
    [InlineData("http://sab.example.com/")]
    [InlineData("http://8.8.8.8:8080/")]
    public void Plain_http_to_a_public_address_is_refused(string url)
    {
        var options = Valid();
        options.BaseUrl = url;

        var error = Assert.Single(Validate(options));
        Assert.Contains("unencrypted", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Folder_mapping_needs_both_sides_or_none()
    {
        var onlyRemote = Valid();
        onlyRemote.RemotePath = "/downloads";
        var onlyLocal = Valid();
        onlyLocal.LocalPath = "/data";
        var both = Valid();
        both.RemotePath = "/downloads";
        both.LocalPath = "/data";

        Assert.NotEmpty(Validate(onlyRemote));
        Assert.NotEmpty(Validate(onlyLocal));
        Assert.Empty(Validate(both));
    }
}
