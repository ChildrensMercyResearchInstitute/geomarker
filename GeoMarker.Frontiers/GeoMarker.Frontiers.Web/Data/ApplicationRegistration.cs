namespace GeoMarker.Frontiers.Web.Data
{
    public class ApplicationRegistration
    {        
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string PostLogoutRedirectUri { get; set; } = string.Empty;
        public List<string> Scopes { get; set;}
        public string Issuer { get; set; } = string.Empty;
    }
}
