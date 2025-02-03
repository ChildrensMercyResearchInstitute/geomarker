namespace GeoMarker.Frontiers.Web.Clients
{
    public abstract class AuthenticationClient
    {
        public string? BearerToken { get; private set; }

        public void SetBearerToken(string? token)
        {
            BearerToken = token;
        }

        protected Task<HttpRequestMessage> CreateHttpRequestMessageAsync(CancellationToken cancellationToken)
        {
            var msg = new HttpRequestMessage();
            if (BearerToken != null)
                msg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", BearerToken);
            return Task.FromResult(msg);
        }
    }
}
