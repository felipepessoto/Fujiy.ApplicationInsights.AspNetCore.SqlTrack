using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;

namespace Fujiy.ApplicationInsights.AspNetCore.SqlTrack
{
    public class AiEfCoreLoggerProvider : ILoggerProvider
    {
        private static string category = typeof(Microsoft.EntityFrameworkCore.Storage.Internal.RelationalCommandBuilderFactory).FullName;

        private readonly TelemetryClient telemetryClient;

        public AiEfCoreLoggerProvider(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (category == categoryName)
            {
                return new AiEfCoreLogger(telemetryClient);
            }

            return new NullLogger();
        }

        public void Dispose()
        { }

        private class AiEfCoreLogger : ILogger
        {
            private readonly TelemetryClient telemetryClient;

            public AiEfCoreLogger(TelemetryClient telemetryClient)
            {
                this.telemetryClient = telemetryClient;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var dbCommandLogData = state as Microsoft.EntityFrameworkCore.Storage.DbCommandLogData;
                if (dbCommandLogData != null)
                {
                    DependencyTelemetry dependencyTelemetry = new DependencyTelemetry();
                    dependencyTelemetry.Timestamp = DateTimeOffset.Now;

                    dependencyTelemetry.Name = dbCommandLogData.CommandText;
                    if (dbCommandLogData.ElapsedMilliseconds.HasValue)
                    {
                        dependencyTelemetry.Duration = TimeSpan.FromMilliseconds(dbCommandLogData.ElapsedMilliseconds.GetValueOrDefault());
                    }
                    dependencyTelemetry.DependencyKind = "SQL";


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
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }
        }

        private class NullLogger : ILogger
        {
            public bool IsEnabled(LogLevel logLevel)
            {
                return false;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            { }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }
        }
    }
}
