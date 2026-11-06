using Spottify.Host;
using Spottify.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "SPOTTIFY_");

builder.Services
    .AddOptions<SpottifyOptions>()
    .Bind(builder.Configuration.GetSection(SpottifyOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var options = builder.Configuration.GetSection(SpottifyOptions.SectionName).Get<SpottifyOptions>() ?? new SpottifyOptions();
Directory.CreateDirectory(options.DataDirectory);

builder.Services.AddSpottifyPersistence(options.DatabasePath);
builder.Services.AddHealthChecks().AddDbContextCheck<SpottifyDbContext>();

var app = builder.Build();

await app.Services.MigrateSpottifyDatabaseAsync();

app.MapHealthChecks("/healthz");

await app.RunAsync();

// Makes the entry point visible to WebApplicationFactory in the test project.
public partial class Program;
