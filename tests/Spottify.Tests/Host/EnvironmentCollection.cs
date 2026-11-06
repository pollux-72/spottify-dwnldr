namespace Spottify.Tests.Host;

/// <summary>Environment variables are process-wide, so tests that set them must not run in parallel.</summary>
[CollectionDefinition("Environment variables", DisableParallelization = true)]
public sealed class EnvironmentCollection;
