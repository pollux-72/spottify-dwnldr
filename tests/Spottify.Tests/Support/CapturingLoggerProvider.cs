using Microsoft.Extensions.Logging;

namespace Spottify.Tests.Support;

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<string> _messages = [];

    public IReadOnlyList<string> Messages
    {
        get
        {
            lock (_messages)
            {
                return [.. _messages];
            }
        }
    }

    public ILogger CreateLogger(string categoryName) => new Capture(this);

    public void Dispose()
    {
    }

    private sealed class Capture(CapturingLoggerProvider owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (owner._messages)
            {
                owner._messages.Add(formatter(state, exception));
                if (exception is not null)
                {
                    owner._messages.Add(exception.ToString());
                }
            }
        }
    }
}
