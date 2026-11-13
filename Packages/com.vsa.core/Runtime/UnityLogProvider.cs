using System;
using Microsoft.Extensions.Logging;
using Debug = UnityEngine.Debug;

namespace VSA.Unity {
    // Writes the logs of the app to the Unity console. The Unity host adds it.
    public sealed class UnityLogProvider : ILoggerProvider {
        public ILogger CreateLogger(string categoryName) => new UnityLogger(categoryName);

        public void Dispose() {
        }

        sealed class UnityLogger : ILogger {
            readonly string category;

            public UnityLogger(string category) => this.category = category;

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter) {
                if (!IsEnabled(logLevel)) {
                    return;
                }
                var message = $"[{category}] {formatter(state, exception)}";
                switch (logLevel) {
                    case LogLevel.Critical:
                    case LogLevel.Error:
                        Debug.LogError(message);
                        break;
                    case LogLevel.Warning:
                        Debug.LogWarning(message);
                        break;
                    default:
                        Debug.Log(message);
                        break;
                }
                if (exception != null) {
                    Debug.LogException(exception);
                }
            }
        }
    }
}
