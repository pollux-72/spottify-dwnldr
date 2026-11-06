using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Spottify.Host;
using Spottify.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "SPOTTIFY_");

// Bind and validate once, before anything touches the file system. The same instance is registered for
// injection, so there is a single source of truth and invalid settings fail with a clear message.
var options = builder.Configuration.GetSection(SpottifyOptions.SectionName).Get<SpottifyOptions>() ?? new SpottifyOptions();
Validator.ValidateObject(options, new ValidationContext(options), validateAllProperties: true);
builder.Services.AddSingleton(Options.Create(options));

Directory.CreateDirectory(options.DataDirectory);

builder.Services.AddSpottifyPersistence(options.DatabasePath);
builder.Services.AddHealthChecks().AddDbContextCheck<SpottifyDbContext>();

var app = builder.Build();

await app.Services.MigrateSpottifyDatabaseAsync();

app.MapHealthChecks("/healthz");

await app.RunAsync();

// Makes the entry point visible to WebApplicationFactory in the test project.
public partial class Program;
