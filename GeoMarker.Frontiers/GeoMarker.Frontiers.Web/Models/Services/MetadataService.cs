using GeoMarker.Frontiers.Web.Clients;
using GeoMarker.Frontiers.Web.Data;
using GeoMarker.Frontiers.Web.Resources;
using System.Linq.Expressions;

namespace GeoMarker.Frontiers.Web.Models.Services
{
    public class MetadataService : IMetadataService
    {
        private readonly ILogger<MetadataService> _logger;
        private readonly UserRequestsDbContext _dbContext;

        public MetadataService(ILogger<MetadataService> logger, UserRequestsDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Add a RecordsProcessed object to the db base on the criteria of the parameter
        /// </summary>
        /// <param name="criteria">The criteria of the record to create</param>
        public void AddRecordsProcessed(MetadataServiceCriteria criteria)
        {
            if (criteria.FileResponse == null || (criteria.FileResponse.StatusCode >= 200 && criteria.FileResponse.StatusCode <= 299))
            {
                string guid = string.IsNullOrEmpty(criteria.Guid) ? Guid.NewGuid().ToString() : criteria.Guid;
                var processed = new RecordsProcessed()
                {
                    RequestGuid = guid,
                    UserId = criteria.UserId,
                    RequestType = criteria.DeGaussRequestType.ToString(),
                    UploadDateTime = criteria.StartDate,
                    CompletedDateTime = criteria.EndDate,
                    Records = GetRecordCount(criteria),
                    Format = criteria.Format,
                    Status = criteria.EndDate != DateTime.MinValue ? CommandStatus.Success : CommandStatus.Processing
                };
                _dbContext.Add(processed);
                _dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Set the completed date of a record based on the criteria of the parameter
        /// </summary>
        /// <param name="criteria">The criteria of the record to complete</param>
        public void CompleteRecordsProcessed(MetadataServiceCriteria criteria)
        {
            try
            {
                var recordProcessed = _dbContext.RecordsProcessed.FirstOrDefault(r => r.RequestGuid == criteria.Guid);

                if (recordProcessed != null && recordProcessed.CompletedDateTime == DateTime.MinValue && (criteria.FileResponse == null || (criteria.FileResponse.StatusCode >= 200 && criteria.FileResponse.StatusCode <= 299)))
                {
                    recordProcessed.CompletedDateTime = criteria.EndDate;
                    recordProcessed.Status = CommandStatus.Success;
                    _dbContext.Update(recordProcessed);
                    _dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unable to complete the request {criteria.Guid}");
            }
        }

        public void FailRecordsProcessed(MetadataServiceCriteria criteria)
        {
            try
            {
                var recordProcessed = _dbContext.RecordsProcessed.FirstOrDefault(r => r.RequestGuid == criteria.Guid);
                if (recordProcessed != null)
                {
                    recordProcessed.CompletedDateTime = criteria.EndDate;
                    recordProcessed.Status = CommandStatus.Failure;
                    _dbContext.Update(recordProcessed);
                    _dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unable to complete the request {criteria.Guid}");
            }
        }

        public List<RecordsProcessed> GetRecordsProcessed(Expression<Func<RecordsProcessed, bool>>? predicate = null)
        {
            try
            {
                if (predicate == null)
                    return _dbContext.RecordsProcessed.OrderByDescending(r => r.UploadDateTime).ToList();

                return _dbContext.RecordsProcessed.Where(predicate).OrderByDescending(r => r.UploadDateTime).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Messages.HomeController_UserRequestError);
                return new List<RecordsProcessed>();
            }
        }

        public List<string> GetRecordsProcessedUsers(Expression<Func<RecordsProcessed, bool>>? predicate = null)
        {
            try
            {
                var records = _dbContext.RecordsProcessed.Where(r => !string.IsNullOrEmpty(r.UserId));

                if (predicate != null)
                    records = records.Where(predicate);
                
                return records.OrderBy(r => r.UserId).Select(r => r.UserId).Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Messages.HomeController_UserRequestError);
                return new List<string>();
            }
        }


        /// <summary>
        /// Get the record count for a file or stream of the criteria of the parameter
        /// </summary>
        /// <param name="criteria">The criteria of the record to find out the record count</param>
        /// <returns></returns>
        private int GetRecordCount(MetadataServiceCriteria criteria)
        {
            if (criteria.Records > 0)
                return criteria.Records;

            int numberOfRecords = 0;

            if (criteria.File != null)
            {
                using (StreamReader sr = new StreamReader(criteria.File.OpenReadStream()))
                    while (sr.ReadLine() != null)
                        numberOfRecords++;
            }
            else if (criteria.Stream != null)
            {
                var ms = new MemoryStream();
                criteria.FileResponse.Stream.CopyTo(ms);
                ms!.Seek(0, SeekOrigin.Begin);

                using (StreamReader sr = new StreamReader(ms, leaveOpen: true))
                    while (sr.ReadLine() != null)
                        numberOfRecords++;
                ms!.Seek(0, SeekOrigin.Begin);
            }
            numberOfRecords = numberOfRecords > 0 ? numberOfRecords - 1 : 0;
            return numberOfRecords;
        }
        /// <summary>
        /// Get geocoded user requests by email or user id. 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="requestType"></param>
        /// <returns>List of User Requests or Null</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public List<UserRequest>? GetGeocodeUserRequests(string email, string requestType)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                    throw new ArgumentNullException(nameof(email));
                if (string.IsNullOrEmpty(requestType))
                    throw new ArgumentNullException(nameof(requestType));

                email = email.Trim();
                return _dbContext.Requests.Where(x => x.UserId == email && x.Status == CommandStatus.Success &&
                                                 (x.RequestType == requestType || x.RequestSubType == requestType))?.ToList();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }
    }
}
