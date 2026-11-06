namespace Spottify.Tests;

/// <summary>Runs only when <c>SPOTTIFY_LIVE_TESTS=1</c>, so CI and normal runs never call real external services.</summary>
public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SPOTTIFY_LIVE_TESTS") != "1")
        {
            Skip = "Set SPOTTIFY_LIVE_TESTS=1 to call the real external services.";
        }
    }
}
