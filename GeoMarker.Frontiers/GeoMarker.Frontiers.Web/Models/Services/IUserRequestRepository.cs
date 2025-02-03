using GeoMarker.Frontiers.Web.Clients;

namespace GeoMarker.Frontiers.Web.Models.Services
{
    /// <summary>
    /// User Request Repository. Db call to add, remove and edit user request dataset. 
    /// </summary>
    public interface IUserRequestRepository
    {
        /// <summary>
        /// Add userRequest to the database
        /// </summary>
        /// <param name="userRequest"></param>
        /// <returns></returns>
        string AddUserRequest(UserRequest userRequest);
        /// <summary>
        /// Get batch user requests which are tied to given user id. 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<UserRequest> GetBatchUserRequests(string userId);
        /// <summary>
        /// Get batch user request which is tied to given user id and guid( guid). 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        UserRequest? GetBatchUserRequests(string userId, string guid);
        /// <summary>
        /// Delete User requests if there are validation error in the composite request. 
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        List<UserRequest> DeleteRequestChain(string guid);
        /// <summary>
        /// Update the status of User requests for composite requests. 
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="commandStatus"></param>
        /// <returns></returns>
        void ApplyStatusToRequestChain(string guid, CommandStatus commandStatus);
        /// <summary>
        /// Get incomplete composite user requests. 
        /// </summary>
        /// <returns></returns>

        List<UserRequest> GetIncompleteUserRequests();
        /// <summary>
        /// Get user request by Guid
        /// </summary>
        /// <param name="guId"></param>
        /// <returns></returns>
        UserRequest? GetUserRequest(string guId);

        /// <summary>
        /// Get composite user requests to see next request and initiate it. 
        /// </summary>
        /// <returns></returns>
        public List<UserRequest> GetCompositeUserRequests();

        /// <summary>
        /// Get composite user requests for a user to see next request and initiate it. 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<UserRequest> GetCompositeUserRequests(string userId);

        /// <summary>
        /// Get incomplete composite user requests to see next request and initiate it. 
        /// </summary>
        /// <returns></returns>
        public List<UserRequest> GetIncompleteCompositeUserRequests();
    }
}
