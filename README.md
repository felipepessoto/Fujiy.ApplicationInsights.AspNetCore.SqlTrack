# Application Insights for ASP.NET Core doesn't support Dependency Tracking:

> Dependency tracking and performance counter collection are by default enabled in ASP.NET Core on .NET Framework (currently not supported in .NET Core) https://github.com/Microsoft/ApplicationInsights-aspnetcore/wiki/Dependency-Tracking-and-Performance-Counter-Collection

So I created this library to automatically track your SQL queries and send to Application Insights.

Setup:

Add Fujiy.ApplicationInsights.AspNetCore.SqlTrack package

On your Configure method, add TelemetryClient parameter and add AiEfCoreLoggerProvider to ILoggerFactory:

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, TelemetryClient tc)
        {
            loggerFactory.AddProvider(new AiEfCoreLoggerProvider(tc));
            ...
