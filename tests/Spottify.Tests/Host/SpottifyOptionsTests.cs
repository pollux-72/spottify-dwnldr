using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Spottify.Host;

namespace Spottify.Tests.Host;

public sealed class SpottifyOptionsTests
{
    [Fact]
    public void Defaults_are_usable_without_any_configuration()
    {
        var options = new SpottifyOptions();

        Assert.Equal(Path.Combine("data", "spottify.db"), options.DatabasePath);
        Assert.Empty(Validate(options));
    }

    [Fact]
    public void Values_bind_from_configuration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Spottify:DataDirectory"] = "/data",
                ["Spottify:DatabaseFileName"] = "state.db",
            })
            .Build();

        var options = configuration.GetSection(SpottifyOptions.SectionName).Get<SpottifyOptions>()!;

        Assert.Equal("/data", options.DataDirectory);
        Assert.Equal(Path.Combine("/data", "state.db"), options.DatabasePath);
    }

    [Theory]
    [InlineData("", "spottify.db")]
    [InlineData("data", "")]
    public void Empty_required_values_are_rejected(string directory, string fileName)
    {
        var options = new SpottifyOptions { DataDirectory = directory, DatabaseFileName = fileName };

        Assert.NotEmpty(Validate(options));
    }

    [Theory]
    [InlineData("../spottify.db")]
    [InlineData("sub/spottify.db")]
    [InlineData("/etc/spottify.db")]
    [InlineData("..")]
    [InlineData(".")]
    public void Database_file_name_must_not_leave_the_data_directory(string fileName)
    {
        var options = new SpottifyOptions { DatabaseFileName = fileName };

        var errors = Validate(options);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(SpottifyOptions.DatabaseFileName)));
    }

    private static List<ValidationResult> Validate(SpottifyOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
