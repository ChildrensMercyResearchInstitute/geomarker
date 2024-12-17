using GeoMarker.Frontiers.Web.Clients;

namespace GeoMarker.Frontiers.Web.Models.Services
{
    /// <summary>
    /// This service include different API calls made from GeoMarker Web UI. 
    /// </summary>
    public interface IGeoMarkerAPIRequestService
    {
        /// <summary>
        /// GetOutputFile will give you out file stream based on user request and accesstoken. 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        Task<FileResponse?> GetOutputFile(UserRequest request, string accessToken);
        /// <summary>
        /// Check DB user requests against API status and update the database. 
        /// User will be notified based on success or failure of the user request. 
        /// </summary>
        /// <param name="incompleteRequests"></param>
        /// <param name="accessToken"></param>
        /// <param name="sendEmail"></param>
        /// <returns></returns>
        Task RefreshUserRequests(List<UserRequest> incompleteRequests, string accessToken, bool sendEmail = false);       
        /// <summary>
        /// Invoke or start async api calls. 
        /// Delete composite request if errored out.
        /// </summary>
        /// <param name="deGaussRequestType"></param>
        /// <param name="file"></param>
        /// <param name="accessToken"></param>
        /// <param name="guid"></param>
        /// <param name="year"></param>
        /// <param name="site"></param>
        /// <param name="nextRequestGuid"></param>
        /// <param name="isComposite"></param>
        /// <returns></returns>
        Task<DeGaussAsyncResponse> InvokeStartGetAsync(DeGaussRequestType deGaussRequestType, IFormFile file,
                                                                   string accessToken, string guid,
                                                                   int? year, string? site, string? nextRequestGuid , bool isComposite = false);
        /// <summary>
        /// Ping all four API services and assign to SingleAddressViewModel or MultiAddressIndexViewModel health indicators. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<BaseViewModel> HealthCheck(BaseViewModel model);
        /// <summary>
        /// loop through GetCompletedUserRequests result set to check for next requests which needs to be processed. 
        /// </summary>
        /// <param name="compositeRequests"></param>
        /// <param name="accessToken"></param>
        /// <param name="sendEmail"></param>
        /// <returns></returns>
        Task RefreshCompositeRequests(List<UserRequest> compositeRequests, string accessToken, bool sendEmail = false);
    }
}