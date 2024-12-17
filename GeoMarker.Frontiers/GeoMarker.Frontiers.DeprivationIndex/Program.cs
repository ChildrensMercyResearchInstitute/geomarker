using GeoMarker.Frontiers.DeprivationIndex;
using Microsoft.Extensions.Logging.ApplicationInsights;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);
        builder.ConfigureAppConfiguration(cfgBuilder =>
        {
        });
        builder.ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder
            .UseStartup<Startup>()
            .UseKestrel(options =>
             {
                 options.Limits.MaxRequestBodySize = int.MaxValue; ;
             })
            .ConfigureLogging(logBuilder =>
            {
                logBuilder.ClearProviders();
                logBuilder.AddConsole();
                logBuilder.AddDebug();

                var appInsightsConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
                bool isDevelopment = (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development") == "Development";
                if (!string.IsNullOrEmpty(appInsightsConnectionString))
                {
                    logBuilder.AddApplicationInsights(configureTelemetryConfiguration: (config) =>
                        config.ConnectionString = appInsightsConnectionString,
                        configureApplicationInsightsLoggerOptions: (options) => { });
                }
                else if (isDevelopment)
                {
                    logBuilder.AddApplicationInsights();
                }

                logBuilder.AddFilter<ApplicationInsightsLoggerProvider>(
                    typeof(Startup).FullName, LogLevel.Trace);
            });
        });
        return builder;
    }
}