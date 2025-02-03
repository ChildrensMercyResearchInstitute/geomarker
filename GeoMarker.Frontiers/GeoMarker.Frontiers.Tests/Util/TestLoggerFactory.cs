using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GeoMarker.Frontiers.Tests.Util
{
    public static class TestLoggerFactory<T>
    { 
        public static ILogger<T> CreateTestLogger()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging(l =>
                {
                    l.AddDebug();
                    l.AddConsole();
                })
                .BuildServiceProvider();
            var factory = serviceProvider.GetService<ILoggerFactory>();
            return factory.CreateLogger<T>();
        }
    }
}
