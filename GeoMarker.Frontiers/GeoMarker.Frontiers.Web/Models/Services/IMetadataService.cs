using System.Linq.Expressions;

namespace GeoMarker.Frontiers.Web.Models.Services
{
    public interface IMetadataService
    {
        public void AddRecordsProcessed(MetadataServiceCriteria criteria);
        public void CompleteRecordsProcessed(MetadataServiceCriteria criteria);
        public void FailRecordsProcessed(MetadataServiceCriteria criteria);
        public List<RecordsProcessed> GetRecordsProcessed(Expression<Func<RecordsProcessed, bool>>? predicate = null);
        public List<string> GetRecordsProcessedUsers(Expression<Func<RecordsProcessed, bool>>? predicate = null);
        public List<UserRequest>? GetGeocodeUserRequests(string email, string requestType);
    }
}
