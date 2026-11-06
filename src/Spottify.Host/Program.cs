using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Spottify.Host;
using Spottify.Infrastructure.MusicBrainz;
using Spottify.Infrastructure.Newznab;
using Spottify.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "SPOTTIFY_");

// Bind and validate once, before anything touches the file system or the network. The same instances are
// registered for injection, so there is a single source of truth and invalid settings fail with a clear message.
var options = BindAndValidate<SpottifyOptions>(builder.Configuration, SpottifyOptions.SectionName);
builder.Services.AddSingleton(Options.Create(options));

var musicBrainz = BindAndValidate<MusicBrainzOptions>(builder.Configuration, MusicBrainzOptions.SectionName);

Directory.CreateDirectory(options.DataDirectory);

builder.Services.AddSpottifyPersistence(options.DatabasePath);
builder.Services.AddMusicBrainz(musicBrainz);

// The indexer is optional until it is set up (backlog TASK-15). Setting any Spottify:Indexer value switches it on
// and then requires a complete configuration, so a half-configured indexer fails at startup instead of at search time.
if (builder.Configuration.GetSection(NewznabOptions.SectionName).Exists())
{
    builder.Services.AddNewznabIndexer(BindAndValidate<NewznabOptions>(builder.Configuration, NewznabOptions.SectionName));
}

builder.Services.AddHealthChecks().AddDbContextCheck<SpottifyDbContext>();

var app = builder.Build();

await app.Services.MigrateSpottifyDatabaseAsync();

app.MapHealthChecks("/healthz");

await app.RunAsync();

static T BindAndValidate<T>(IConfiguration configuration, string section)
    where T : class, new()
{
    var value = configuration.GetSection(section).Get<T>() ?? new T();
    Validator.ValidateObject(value, new ValidationContext(value), validateAllProperties: true);
    return value;
}

// Makes the entry point visible to WebApplicationFactory in the test project.
public partial class Program;
