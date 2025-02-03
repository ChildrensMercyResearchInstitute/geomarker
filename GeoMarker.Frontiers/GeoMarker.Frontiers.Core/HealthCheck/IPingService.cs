namespace GeoMarker.Frontiers.Core.HealthCheck
{
    public interface IPingService
    {
        Task<bool> CheckServiceAvailablityAsync(string apiHealthEndpoint);
    }
}