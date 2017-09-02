using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Fujiy.ApplicationInsights.AspNetCore.SqlTrack
{
    public class AiEfCoreLoggerProvider : ILoggerProvider
    {
        private readonly TelemetryClient telemetryClient;

        public AiEfCoreLoggerProvider(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (categoryName == "Microsoft.EntityFrameworkCore.Database.Command")
            {
                return new AiEfCoreLogger(telemetryClient);
            }

            return NullLogger.Instance;
        }

        public void Dispose()
        {
        }

        private class AiEfCoreLogger : ILogger
        {
            private readonly TelemetryClient telemetryClient;

            public AiEfCoreLogger(TelemetryClient telemetryClient)
            {
                this.telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (eventId.Id == RelationalEventId.CommandExecuted.Id || eventId.Id == RelationalEventId.CommandError.Id)
                {
                    var data = state as IReadOnlyList<KeyValuePair<string, object>>;
                    if (data != null)
                    {
                        var dataList = data.ToDictionary(x => x.Key, x => x.Value);
                        var commandText = dataList["commandText"] as string;
                        int? elapsedMs = int.TryParse(dataList["elapsed"] as string, out int elapsedMsTemp) ? (int?)elapsedMsTemp : null;

                        SendDependency(commandText, elapsedMs, exception);
                    }
                }
            }

            private void SendDependency(string commandText, int? elapsedMs, Exception exception)
            {
                DependencyTelemetry dependencyTelemetry = new DependencyTelemetry();
                dependencyTelemetry.Timestamp = DateTimeOffset.Now;

                dependencyTelemetry.Name = commandText;
                if (elapsedMs.HasValue)
                {
                    dependencyTelemetry.Duration = TimeSpan.FromMilliseconds(elapsedMs.GetValueOrDefault());
                }
                dependencyTelemetry.Type = "SQL";
                dependencyTelemetry.Success = exception == null;

                var sqlException = exception as SqlException;
                if (sqlException != null)
                {
                    dependencyTelemetry.ResultCode = sqlException.Number.ToString();
                }
                else
                {
                    dependencyTelemetry.ResultCode = "0";
                }

                telemetryClient.TrackDependency(dependencyTelemetry);
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }
        }

        private class NullLogger : ILogger
        {
            public static NullLogger Instance { get; } = new NullLogger();

            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => false;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
            }
        }
    }
}
