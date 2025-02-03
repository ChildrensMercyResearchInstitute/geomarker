namespace GeoMarker.Frontiers.Core.HealthCheck
{
    public class PingService : IPingService
    {
        private readonly HttpClient _httpClient;
        public PingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> CheckServiceAvailablityAsync(string apiHealthEndpoint)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, apiHealthEndpoint);
                using var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseValue = await response.Content.ReadAsStringAsync();
                return responseValue == "Healthy" || responseValue == "Degraded";
            }
            catch (Exception)
            {
                return false;
            }

        }

    }
}
