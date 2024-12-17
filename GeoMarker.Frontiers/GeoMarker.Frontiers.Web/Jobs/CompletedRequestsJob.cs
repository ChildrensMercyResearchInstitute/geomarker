using GeoMarker.Frontiers.Web.Models.Clients;
using GeoMarker.Frontiers.Web.Models.Services;
using Newtonsoft.Json;
using Quartz;

namespace GeoMarker.Frontiers.Web.Jobs
{
    /// <summary>
    /// This scheduler job will check for incomplete user requests including composite requests and updates status. 
    /// User will be notified on successful or failure of the job.
    /// </summary>
    public class CompletedRequestsJob : IJob
    {
        private ILogger<CompletedRequestsJob> _logger;
        private readonly ClientCredentials _credentials;
        private readonly IUserRequestRepository _userRequestRepository;
        private readonly IGeoMarkerAPIRequestService _apiRequestService;
        /// <summary>
        /// Initiate ILogger, IUserRequestRepository and IAPIRequestService services. 
        /// ASP.NET CORE Middleware will inject dependencies for this class. 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="credentials"></param>
        /// <param name="userRequestRepository"></param>
        /// <param name="apiRequestService"></param>
        public CompletedRequestsJob(ILogger<CompletedRequestsJob> logger,
                                    ClientCredentials credentials,
                                    IUserRequestRepository userRequestRepository,
                                    IGeoMarkerAPIRequestService apiRequestService)
        {
            _logger = logger;
            _credentials = credentials;
            _userRequestRepository = userRequestRepository;
            _apiRequestService = apiRequestService;
        }
        /// <inheritdoc />
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var incompleteRequests = _userRequestRepository.GetIncompleteUserRequests();
                var compositeRequests = _userRequestRepository.GetIncompleteCompositeUserRequests();
                var accessToken = await GetAccessToken("geocode censusblock drivetime deprivationindex");
                if (accessToken != null)
                {
                    await _apiRequestService.RefreshUserRequests(incompleteRequests, accessToken, true);
                    await _apiRequestService.RefreshCompositeRequests(compositeRequests, accessToken, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }
        private async Task<string?> GetAccessToken(string scope)
        {
            var dict = new Dictionary<string, string>
                {
                    { "client_id", _credentials.ClientId },
                    { "client_secret", _credentials.ClientSecret },
                    { "scope", scope },
                    { "grant_type", "client_credentials" }
                };
            HttpClient client = new HttpClient();
            var response = await client.PostAsync(_credentials.TokenEndpoint, new FormUrlEncodedContent(dict));
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent)?["access_token"];
        }
    }
}
