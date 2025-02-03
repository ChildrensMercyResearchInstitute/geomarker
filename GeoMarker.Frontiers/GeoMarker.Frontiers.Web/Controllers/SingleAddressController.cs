using GeoMarker.Frontiers.Core.HealthCheck;
using GeoMarker.Frontiers.Web.Clients;
using GeoMarker.Frontiers.Web.Data;
using GeoMarker.Frontiers.Web.Models;
using GeoMarker.Frontiers.Web.Models.Services;
using GeoMarker.Frontiers.Web.Resources;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenIddict.Client.AspNetCore;
using System.Linq.Expressions;
using System.Security.Claims;

namespace GeoMarker.Frontiers.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SingleAddressController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GeoCodeClient _geoCodeClient;
        private readonly CensusBlockGroupClient _censusBlockGroupClient;
        private readonly DriveTimeClient _driveTimeClient;
        private readonly DeprivationIndexClient _deprivationIndexClient;
        private readonly IPingService _pingService;
        private readonly UserRequestsDbContext _dbContext;
        private readonly IMetadataService _metadataService;
        private readonly IGeoMarkerAPIRequestService _apiRequestService;

        public SingleAddressController(ILogger<HomeController> logger,
                              GeoCodeClient geoCodeClient,
                              IPingService pingService,
                              UserRequestsDbContext dbContext,
                              IMetadataService metadataService,
                              IGeoMarkerAPIRequestService apiRequestService,
                              CensusBlockGroupClient censusBlockGroupClient,
                              DriveTimeClient driveTimeClient,
                              DeprivationIndexClient deprivationIndexClient)
        {
            _logger = logger;
            _geoCodeClient = geoCodeClient;
            _pingService = pingService;
            _dbContext = dbContext;
            _metadataService = metadataService;
            _apiRequestService = apiRequestService;
            _censusBlockGroupClient = censusBlockGroupClient;
            _driveTimeClient = driveTimeClient;
            _deprivationIndexClient = deprivationIndexClient;
        }

        /// <summary>
        /// Landing page index path. Simply gets the current requests for the user.
        /// </summary>
        /// <returns>ViewResult to trigger a re-render of the UI.</returns>
        [HttpGet("~/SingleAddress")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = "GeoMarkerUserPolicy")]
        public async Task<IActionResult> Index()
        {
            SingleAddressViewModel model = new();
            await _apiRequestService.HealthCheck(model);
            model.UserRequests = GetUserRequests();
            return View(model);
        }

        /// <summary>
        /// Action fired on Index form submission. Routes to the geocode client to do the request
        /// </summary>
        /// <param name="model">ViewModel for the SingleAddress page.</param>
        /// <returns>IActionResult - Success view, or Problem if an error has occurred.</returns>
        [HttpPost]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = "GeoMarkerUserPolicy")]
        public async Task<IActionResult> Index(SingleAddressViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var accessToken = await HttpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        OpenIddictClientAspNetCoreConstants.Tokens.BackchannelAccessToken);
                    _geoCodeClient.SetBearerToken(accessToken);
                    var startDate = DateTime.UtcNow;
                    var response = await _geoCodeClient.GetGeocodesJsonAsync(new DeGaussJsonRequest()
                    {
                        Addresses = new List<DeGaussAddressRequest>() { new DeGaussAddressRequest() { Id = "1", Address = model.Address } }
                    }
                    );
                    var endDate = DateTime.UtcNow;

                    _logger.LogInformation($"response: {response}");

                    if (response == null || response.Count == 0)
                    {
                        model.ResponseType = ResponseType.Warning;
                        model.ResponseMessage = "We couldn't generate a geocode for the given address!";
                    }
                    else
                    {
                        var guid = Guid.NewGuid().ToString();

                        _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
                        {
                            Guid = guid,
                            UserId = GetLoggedInUserId(),
                            DeGaussRequestType = DeGaussRequestType.GeoCode,
                            StartDate = startDate,
                            EndDate = endDate,
                            Records = 1,
                            Format = MetadataSource.UI
                        });

                        if (model.Types.Count > 0)
                        {
                            response = await Composite(model, accessToken, response);
                            model.ResponseMessage = "Address has successfully been geocoded and any selected data types have been appended.";
                        }
                        else
                        {
                            model.ResponseMessage = "Address has successfully been geocoded.";
                        }

                        var request = new UserRequest()
                        {
                            Guid = guid,
                            UserId = GetLoggedInUserId(),
                            Status = CommandStatus.Success,
                            UploadDateTime = startDate,
                            CompletedDateTime = endDate,
                            Address = model.Address,
                            GeocodedAddress = JsonConvert.SerializeObject(response.FirstOrDefault()),
                            RequestType = DeGaussRequestType.SingleAddress.ToString()
                        };
                        _dbContext.Add(request);
                        _dbContext.SaveChanges();
                        model.ResponseType = ResponseType.Success;
                    }
                }
                catch(ApiException ex)
                {
                    _logger.LogError(ex, ex.Message);
                    model.ResponseType = ResponseType.Error;       
                    model.ResponseMessage = string.Empty;
                    var degaussRequestTypes = Enum.GetValues(typeof(DeGaussRequestType)).Cast<DeGaussRequestType>().ToList();

                    foreach(var degaussRequestType in degaussRequestTypes)
                    {
                        if(ex.Response.Contains(degaussRequestType.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            model.ResponseMessage =ex.Response.Contains("The command was rejected", StringComparison.InvariantCultureIgnoreCase) ?
                                                   $"Your {degaussRequestType.ToString()} request was not submitted. Please try again and contact support if this issue persists." :
                                                   $"Unable to process {degaussRequestType.ToString()} request. Please try again and contact support if this issue persists.";
                            break;
                        }
                    }
                    if(string.IsNullOrEmpty(model.ResponseMessage))
                    {
                        model.ResponseMessage = "Unable to process your request. Please try again and contact support if this issue persists.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    model.ResponseType = ResponseType.Error;
                    model.ResponseMessage = ex.Message.Contains("The command was rejected") ?
                        "Your request was not submitted. Please try again and contact support if this issue persists." :
                        "Unable to process your request. Please try again and contact support if this issue persists.";
                }
            }

            await _apiRequestService.HealthCheck(model);
            model.UserRequests = GetUserRequests();
            return View(model);
        }

        private async Task<ICollection<DeGaussGeocodedJsonRecord>> Composite(SingleAddressViewModel model, string? accessToken, ICollection<DeGaussGeocodedJsonRecord> response)
        {
            DateTime startDate;
            DateTime endDate;

            foreach (var type in model.Types)
            {
                switch (type)
                {
                    case DeGaussRequestType.DriveTime:

                            _driveTimeClient.SetBearerToken(accessToken);
                            startDate = DateTime.UtcNow;
                            response = await _driveTimeClient.GetDriveTimesJsonAsync(new Clients.DeGaussDriveTimesJsonRequest() { Records = response, Site = model.Site });
                            endDate = DateTime.UtcNow;
                            _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
                            {
                                DeGaussRequestType = DeGaussRequestType.DriveTime,
                                Records = 1,
                                StartDate = startDate,
                                EndDate = endDate,
                                Format = MetadataSource.UI,
                                UserId = GetLoggedInUserId(),
                            });
                        break;

                    case DeGaussRequestType.DeprivationIndex:
                        _deprivationIndexClient.SetBearerToken(accessToken);

                        startDate = DateTime.UtcNow;
                        response = await _deprivationIndexClient.GetDeprivationIndexesJsonAsync(new Clients.DeGaussGeocodedJsonRequest() { Records = response });

                        endDate = DateTime.UtcNow;
                        _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
                        {
                            DeGaussRequestType = DeGaussRequestType.DeprivationIndex,
                            Records = 1,
                            StartDate = startDate,
                            EndDate = endDate,
                            Format = MetadataSource.UI,
                            UserId = GetLoggedInUserId(),
                        });
                        break;
                    case DeGaussRequestType.CensusBlockGroup:
                        _censusBlockGroupClient.SetBearerToken(accessToken);

                        startDate = DateTime.UtcNow;
                        response = await _censusBlockGroupClient.GetCensusBlockGroupsJsonAsync(new Clients.DeGaussCensusBlockGroupsJsonRequest() { Records = response, Year = model.Year ?? 0 });

                        endDate = DateTime.UtcNow;
                        _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
                        {
                            DeGaussRequestType = DeGaussRequestType.CensusBlockGroup,
                            Records = 1,
                            StartDate = startDate,
                            EndDate = endDate,
                            Format = MetadataSource.UI,
                            UserId = GetLoggedInUserId(),
                        });
                        break;

                }

            }

            return response;
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = "GeoMarkerUserPolicy")]
        public JsonResult GetGeocodedAddress(string guid)
        {
            UserRequest userRequest = _dbContext.Requests.Where(r => r.Guid == guid).FirstOrDefault();
            if (userRequest == null) throw new Exception($"Could not find user request with guid: {guid}");
            return Json(userRequest.GeocodedAddress);
        }

        /// <summary>
        /// Gets the user claim that's associated with the name
        /// </summary>
        /// <returns>The user id (probably an email) or an empty string</returns>
        private string GetLoggedInUserId()
        {
            return User.Identity != null && User.Identity.IsAuthenticated ? @User.Claims.First(x => x.Type == ClaimTypes.Name).Value : string.Empty;
        }

        /// <summary>
        /// Gets the user requests associated to the current user
        /// </summary>
        /// <param name="predicate">A query to get certain user requests. If null it will return all of the current user's requests</param>
        /// <returns>A list of the current user's requests</returns>
        private List<UserRequest> GetUserRequests(Expression<Func<UserRequest, bool>>? predicate = null)
        {
            try
            {
                var query = predicate == null ? r => r.UserId == GetLoggedInUserId() && r.RequestType == DeGaussRequestType.SingleAddress.ToString() : predicate;
                return _dbContext.Requests.Where(query).OrderByDescending(r => r.UploadDateTime).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Messages.HomeController_UserRequestError);
                return new List<UserRequest>();
            }
        }
    }
}