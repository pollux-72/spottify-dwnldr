using System.ComponentModel.DataAnnotations;
using Spottify.Infrastructure.Newznab;

namespace Spottify.Tests.Newznab;

public sealed class NewznabOptionsTests
{
    private static NewznabOptions Valid() => new() { BaseUrl = "https://indexer.example/", ApiKey = "key" };

    private static List<ValidationResult> Validate(NewznabOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void A_complete_configuration_is_valid() => Assert.Empty(Validate(Valid()));

    [Fact]
    public void The_address_and_the_key_are_required()
    {
        var errors = Validate(new NewznabOptions());

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(NewznabOptions.BaseUrl)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(NewznabOptions.ApiKey)));
    }

    [Theory]
    [InlineData("indexer.example")]
    [InlineData("ftp://indexer.example/")]
    [InlineData("not a url")]
    public void The_address_must_be_absolute_http_or_https(string url)
    {
        var options = Valid();
        options.BaseUrl = url;

        Assert.Contains(Validate(options), e => e.MemberNames.Contains(nameof(NewznabOptions.BaseUrl)));
    }

    [Theory]
    [InlineData("https://indexer.example/")]
    [InlineData("https://8.8.8.8/")]
    [InlineData("http://localhost:5076/")]
    [InlineData("http://127.0.0.1:9696")]
    [InlineData("http://[::1]:9696/")]
    [InlineData("http://192.168.1.20:5076/")]
    [InlineData("http://10.0.0.2/")]
    [InlineData("http://172.16.0.5/")]
    [InlineData("http://172.31.255.1/")]
    [InlineData("http://prowlarr:9696/")]
    [InlineData("http://nas.local/")]
    [InlineData("http://hydra.lan:5076/")]
    public void The_key_may_travel_over_https_or_inside_the_local_network(string url)
    {
        var options = Valid();
        options.BaseUrl = url;

        Assert.Empty(Validate(options));
    }

    [Theory]
    [InlineData("http://indexer.example/")]
    [InlineData("http://8.8.8.8/")]
    [InlineData("http://172.32.0.1/")]
    [InlineData("http://172.15.0.1/")]
    [InlineData("http://11.0.0.1/")]
    [InlineData("http://192.169.0.1/")]
    [InlineData("http://localhost.example.com/")]
    [InlineData("http://evil.example/localhost")]
    public void Plain_http_to_a_public_address_is_refused_because_the_key_would_be_readable(string url)
    {
        var options = Valid();
        options.BaseUrl = url;

        var errors = Validate(options);

        var error = Assert.Single(errors);
        Assert.Contains("unencrypted", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("3000")]
    [InlineData("3010,3040")]
    public void Valid_categories_are_accepted(string categories)
    {
        var options = Valid();
        options.Categories = categories;

        Assert.Empty(Validate(options));
    }

    [Theory]
    [InlineData("")]
    [InlineData("audio")]
    [InlineData("3000;DROP")]
    [InlineData("3000,")]
    [InlineData("3000&apikey=x")]
    public void Categories_must_be_numbers_so_they_cannot_change_the_query(string categories)
    {
        var options = Valid();
        options.Categories = categories;

        Assert.NotEmpty(Validate(options));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void The_limit_is_bounded(int limit)
    {
        var options = Valid();
        options.Limit = limit;

        Assert.NotEmpty(Validate(options));
    }
}
