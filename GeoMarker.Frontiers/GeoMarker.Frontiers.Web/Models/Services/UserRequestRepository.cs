using GeoMarker.Frontiers.Web.Clients;
using GeoMarker.Frontiers.Web.Data;

namespace GeoMarker.Frontiers.Web.Models.Services
{
    /// <summary>
    /// User Request Repository. Db call to add, remove and edit user request dataset. 
    /// </summary>
    public class UserRequestRepository : IUserRequestRepository
    {
        private readonly UserRequestsDbContext _dbContext;
        private readonly ILogger<UserRequestRepository> _logger;
        /// <summary>
        /// Initiate User Request Repositoery. 
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="logger"></param>
        public UserRequestRepository(UserRequestsDbContext dbContext, ILogger<UserRequestRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }
        /// <inheritdoc cref="IUserRequestRepository" />
        public string AddUserRequest(UserRequest userRequest)
        {
            try
            {
                _dbContext.Add(userRequest);
                var returnValue = _dbContext.SaveChanges();
                return userRequest.Guid;
            }
            catch(Exception)
            {
                throw;
            }

        }
        /// <inheritdoc cref="IUserRequestRepository" />
        public List<UserRequest> DeleteRequestChain(string guid)
        {
            UserRequest? request;
            List<UserRequest> requestsToDelete = new();
            do
            {
                request = _dbContext.Requests.FirstOrDefault(x => x.Guid == guid);

                if (request == null || string.IsNullOrEmpty(request.Guid))
                {
                    guid = string.Empty;
                    continue;
                }

                requestsToDelete.Add(request);
                _dbContext.Requests.Remove(request);
                _dbContext.SaveChanges();
                guid = request.NextRequest;
            } while (!string.IsNullOrEmpty(guid));
            return requestsToDelete;
        }
        /// <inheritdoc cref="IUserRequestRepository" />
        public List<UserRequest> GetBatchUserRequests(string userId)
        {
            return _dbContext.Requests.Where(r => r.UserId == userId &&
                r.RequestType != DeGaussRequestType.SingleAddress.ToString() &&
                r.RequestType != DeGaussRequestType.Composite.ToString()
                ).OrderByDescending(r => r.UploadDateTime).ToList();
        }

        /// <inheritdoc cref="IUserRequestRepository" />
        public List<UserRequest> GetCompositeUserRequests()
        {
            return _dbContext.Requests.Where(r => r.RequestType.Equals(DeGaussRequestType.Composite.ToString())).ToList();
        }
        /// <inheritdoc cref="IUserRequestRepository" />
        public List<UserRequest> GetCompositeUserRequests(string userId)
        {
            return _dbContext.Requests.Where(r => r.UserId == userId && r.RequestType.Equals(DeGaussRequestType.Composite.ToString())).ToList();
        }
        /// <inheritdoc cref="IUserRequestRepository" />
        public List<UserRequest> GetIncompleteCompositeUserRequests()
        {
            return _dbContext.Requests.Where(r => r.RequestType.Equals(DeGaussRequestType.Composite.ToString()) &&
              (r.Status.Equals(CommandStatus.Requested) ||
               r.Status.Equals(CommandStatus.Queued) ||
               r.Status.Equals(CommandStatus.Processing))).ToList();
        }
        /// <inheritdoc cref="IUserRequestRepository" />
        public UserRequest? GetUserRequest(string guId)
        {
            return _dbContext.Requests.FirstOrDefault(r => r.Guid == guId && r.RequestType != DeGaussRequestType.SingleAddress.ToString());
        }
        /// <inheritdoc cref="IUserRequestRepository" />
        public UserRequest? GetBatchUserRequests(string userId, string guid)
        {
            return _dbContext.Requests.Where(r => r.UserId == userId && r.RequestType != DeGaussRequestType.SingleAddress.ToString() &&
                                             r.Guid.Equals(guid)).FirstOrDefault();
        }
        /// <inheritdoc cref="IUserRequestRepository" />
        public void ApplyStatusToRequestChain(string guid, CommandStatus commandStatus)
        {
            try
            {
                UserRequest? request;
                do
                {
                    request = _dbContext.Requests.FirstOrDefault(x => x.Guid == guid);

                    if (request == null || string.IsNullOrEmpty(request.Guid))
                        return;

                    request.Status = commandStatus;
                    _dbContext.Requests.Update(request);
                    _dbContext.SaveChanges();
                    guid = request.NextRequest;
                } while (!string.IsNullOrEmpty(guid));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error encountered attempting to apply status {commandStatus} to {guid}.");
                throw;
            }
        }
        /// <inheritdoc cref="IUserRequestRepository" />
        public List<UserRequest> GetIncompleteUserRequests()
        {
            try
            {
                var incompleteRequests = _dbContext.Requests.Where(r =>
                                        !r.RequestType.Equals(DeGaussRequestType.Composite.ToString()) &&
                                        (r.Status == CommandStatus.Processing ||
                                        r.Status == CommandStatus.Queued)).ToList();
                return incompleteRequests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }

    }
}
