using GeoMarker.Frontiers.Web.Clients;

namespace GeoMarker.Frontiers.Web.Models.Services
{
    public class MetadataServiceCriteria
    {
        public string Guid { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public IFormFile File { get; set; }
        public Stream Stream { get; set; }
        public DeGaussRequestType? DeGaussRequestType { get; set; }
        public int Records { get; set; } = 0;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public MetadataSource Format { get; set; }
        public FileResponse FileResponse { get; set; }

    }
}
