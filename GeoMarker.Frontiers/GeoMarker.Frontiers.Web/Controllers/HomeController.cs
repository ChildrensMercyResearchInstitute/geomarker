using GeoMarker.Frontiers.Web.Clients;
using GeoMarker.Frontiers.Web.Models;
using GeoMarker.Frontiers.Web.Models.Services;
using GeoMarker.Frontiers.Web.Resources;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenIddict.Client.AspNetCore;
using System.Diagnostics;
using System.Security.Claims;

namespace GeoMarker.Frontiers.Web.Controllers
{
    /// <summary>
    /// User can request batch request by uploading csv file and leverage chaining feature for multiple requests. 
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMetadataService _metadataService;
        private readonly IUserRequestRepository _userRequestRepository;
        private readonly IGeoMarkerAPIRequestService _apiRequestService;
        /// <summary>
        /// Initiate dependencies for this controller
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="metadataService"></param>
        /// <param name="userRequestRepository"></param>

        /// <param name="apiRequestService"></param>
        public HomeController(ILogger<HomeController> logger,
                              IMetadataService metadataService,
                              IUserRequestRepository userRequestRepository,
                              IGeoMarkerAPIRequestService apiRequestService)
        {
            _logger = logger;
            _metadataService = metadataService;
            _userRequestRepository = userRequestRepository;
            _apiRequestService = apiRequestService;
        }
      
        /// <summary>
        /// Landing page index path. Simply gets the current requests for the user.
        /// </summary>
        /// <returns>ViewResult to trigger a re-render of the UI.</returns>
        ///     
        [HttpGet("~/")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = "GeoMarkerUserPolicy")]
        public async Task<IActionResult> Index(bool refresh)
        {
            MultiAddressIndexViewModel IndexModel = new();
            await _apiRequestService.HealthCheck(IndexModel);
            await RefreshUserRequestData(IndexModel);
            if (refresh)
            {
                IndexModel.SuccessResponse = Messages.Refreshed;
            }
            return View(IndexModel);
        }

        /// <summary>
        /// Action fired on Index form submission. Routes to the correct operation based on DeGaussRequestType 
        /// bound to the model for the request.
        /// </summary>
        /// <param name="model">ViewModel for the GeoMarker page.</param>
        /// <returns>IActionResult - Success view, or Problem if an error has occurred.</returns>
        [HttpPost]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = "GeoMarkerUserPolicy")]
        public async Task<IActionResult> Index(MultiAddressIndexViewModel model)
        {
            await _apiRequestService.HealthCheck(model);
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.Types.Count <= 0)
            {
                model.ErrorResponse = Messages.UiController_NoTypeSelected;
                return View(model);
            }
            var accessToken = await GetAccessToken();

            if (!string.IsNullOrEmpty(accessToken))
            {
                if (model.Types.Count > 1)
                    return await Composite(model, accessToken);
                else
                    return await StartGetAsyncOperation(model, accessToken);
            }
            else
            {
                model.ErrorResponse = Messages.HomeController_GeneralError;
                return View(model);
            }
        }

        /// <summary>
        /// Call the 'Start*Async' DeGauss API's and store the GUID for the request to the users current session.
        /// </summary>
        /// <param name="model">ViewModel for the GeoMarker page.</param>
        /// <param name="accessToken">The access token used to authorize backend services.</param>
        /// <returns>IActionResult - View with model data for Success or Failure.</returns>
        private async Task<IActionResult> StartGetAsyncOperation(MultiAddressIndexViewModel model, string accessToken)
        {
            try
            {
                if (model.File != null)
                {
                    var result = await _apiRequestService.InvokeStartGetAsync(model.Types.First(), model.File, accessToken,
                                                                                string.Empty, model.Year, model.Site, string.Empty, false);

                    if (result.Status == CommandStatus.Processing)
                    {
                        var request = new UserRequest()
                        {
                            Guid = result.Guid,
                            UserId = GetLoggedInUserId(),
                            Status = CommandStatus.Processing,
                            InputFileName = model.File.FileName,
                            RequestType = model.Types.First().ToString()!,
                            UploadDateTime = DateTime.UtcNow,
                            Site = model.Site,
                            Year = model.Year
                        };
                        _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
                        {
                            Guid = result.Guid,
                            UserId = GetLoggedInUserId(),
                            File = model.File,
                            DeGaussRequestType = model.Types.First(),
                            StartDate = DateTime.UtcNow,
                            Format = MetadataSource.UI
                        });
                        _userRequestRepository.AddUserRequest(request);
                        model.SuccessResponse = Messages.HomeController_StartSuccess;
                    }
                    else
                        model.ErrorResponse = string.Format(Messages.HomeController_Error, "StartGetAsyncOperation", result.Message);
                }
                else
                {
                    model.ErrorResponse = Messages.HomeController_FileRequired;
                }
                await RefreshUserRequestData(model);
                return View(model);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, Messages.HomeController_Error, "StartGetAsyncOperation", ex.Message);
                var message = ExtractValidationError(ex);
                model.ErrorResponse = message;
                await RefreshUserRequestData(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Messages.HomeController_Error, "StartGetAsyncOperation", ex.Message);
                model.ErrorResponse = Messages.HomeController_GeneralError;
                await RefreshUserRequestData(model);
            }

            return View("Index", model);
        }

        /// <summary>
        /// Download the DeGauss result from the results endpoint.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = "GeoMarkerUserPolicy")]
        public async Task<IActionResult> DownloadResponse(string guid, MultiAddressIndexViewModel model)
        {
            try
            {
                if (!string.IsNullOrEmpty(guid))
                {
                    var request = _userRequestRepository.GetBatchUserRequests(GetLoggedInUserId(), guid);

                    if (request is not null)
                    {

                        var accessToken = await GetAccessToken();
                        var result = await _apiRequestService.GetOutputFile(request, accessToken);

                        if (result is null)
                        {
                            model.ErrorResponse = string.Format(Messages.HomeController_NoResult, guid);
                            return View("Index", model);
                        }

                        var substLeadingDirectories = request.OutputFileName.LastIndexOf('/') == -1 ? 0 : request.OutputFileName.LastIndexOf('/') + 1;
                        var outputFileName = request.OutputFileName.Substring(substLeadingDirectories);

                        return File(result.Stream, "text/csv", outputFileName);
                    }
                }

                model.ErrorResponse = string.Format(Messages.HomeController_NoRequest, guid);
                return View("Index", model);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, Messages.HomeController_Error, "DownloadResponse", ex.Message);
                var message = ExtractValidationError(ex);
                model.ErrorResponse = message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Messages.HomeController_Error, "DownloadResponse", ex.Message);
                model.ErrorResponse = Messages.HomeController_GeneralError;
            }
            finally
            {
                await RefreshUserRequestData(model);
            }

            return View("Index", model);
        }

        /// <summary>
        /// Check the status for each UserRequest and update the status based on a REST call to the appropriate status endpoint.
        /// </summary>
        /// <param name="model"></param>
        private async Task RefreshUserRequestData(MultiAddressIndexViewModel model)
        {
            try
            {
                var userId = GetLoggedInUserId();
                var userRequests = _userRequestRepository.GetBatchUserRequests(userId);
                var compositeRequests = _userRequestRepository.GetCompositeUserRequests(userId);

                var accessToken = await GetAccessToken();
                await _apiRequestService.RefreshUserRequests(userRequests, accessToken);
                await _apiRequestService.RefreshCompositeRequests(compositeRequests, accessToken);
                userRequests.AddRange(compositeRequests);

                model.UserRequests = userRequests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Messages.HomeController_GeneralError);
                model.ErrorResponse = Messages.HomeController_GeneralError;
            }
        }

        private async Task<string> GetAccessToken()
        {
            try
            {
                var accessToken = await HttpContext.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                         OpenIddictClientAspNetCoreConstants.Tokens.BackchannelAccessToken);
                if (!string.IsNullOrEmpty(accessToken))
                    return accessToken;
                else
                    return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Messages.HomeController_GeneralError);
                throw;
            }
        }

        /// <summary>
        /// Gets the user claim that's associated with the name
        /// </summary>
        /// <returns>The user id (probably an email) or an empty string</returns>
        private string GetLoggedInUserId()
        {
            return User.Identity != null && User.Identity.IsAuthenticated ? @User.Claims.First(x => x.Type == ClaimTypes.Name).Value : string.Empty;
        }

        private string ExtractValidationError(ApiException ex)
        {
            try
            {
                var response = ex.Response;

                if (ex.StatusCode == StatusCodes.Status400BadRequest)
                {
                    var problemOutput = JsonConvert.DeserializeObject<ValidationProblemDetails>(ex.Response);

                    if (problemOutput != null)
                    {
                        var error = string.Empty;
                        MarshallProblemDetailsToString(problemOutput, ref error);
                        return string.Format(Messages.HomeController_ServerValidationFailure, error);
                    }
                }

                return Messages.HomeController_GeneralError;
            }
            catch
            {
                return Messages.HomeController_GeneralError;
            }
        }

        private void MarshallProblemDetailsToString(ValidationProblemDetails? errorDetails, ref string response)
        {
            if (errorDetails != null)
            {
                if (errorDetails.Errors.Count > 0)
                {
                    foreach (var item in errorDetails.Errors)
                    {
                        foreach (var value in item.Value)
                            response += value.ToString();
                    }
                }
                else if (errorDetails.Detail != null)
                    response = errorDetails.Detail.ToString();
                else
                    response = "";
            }
        }

        /// <summary>
        ///  Store all the user requests in the database. 
        ///  Sort and reverse types to invoke the last request and store others as partial requests. 
        /// </summary>
        /// <param name="IndexModel"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        private async Task<IActionResult> Composite(MultiAddressIndexViewModel IndexModel, string accessToken)
        {
            var userId = GetLoggedInUserId();
            var uploadDateTime = DateTimeOffset.Now;
            var types = IndexModel.Types;
            types.Sort();
            types.Reverse();

            var guid = string.Empty;
            var count = 0;

            using var stream = new MemoryStream();
            IndexModel.File!.CopyTo(stream);
            stream.Seek(0, SeekOrigin.Begin);

            foreach (var type in IndexModel.Types)
            {
                try
                {
                    count++;
                    UserRequest newRequest;
                    if (IndexModel.Types.Count == count)
                    {
                        // Submit Job and create full request object
                        var result = await _apiRequestService.InvokeStartGetAsync(type, IndexModel.File, accessToken,
                                                                                 guid, IndexModel.Year, IndexModel.Site, string.Empty, true);

                        newRequest = new UserRequest()
                        {
                            Guid = result.Guid,
                            UserId = GetLoggedInUserId(),
                            Status = CommandStatus.Processing,
                            InputFileName = IndexModel.File.FileName,
                            RequestType = DeGaussRequestType.Composite.ToString(),
                            UploadDateTime = uploadDateTime,
                            Site = type == DeGaussRequestType.DriveTime ? IndexModel.Site : string.Empty,
                            Year = type == DeGaussRequestType.CensusBlockGroup ? IndexModel.Year : null,
                            RequestSubType = type.ToString(),
                            NextRequest = guid
                        };
                        _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
                        {
                            Guid = result.Guid,
                            UserId = GetLoggedInUserId(),
                            File = IndexModel.File,
                            DeGaussRequestType = IndexModel.Types.Last(),
                            StartDate = DateTime.UtcNow,
                            Format = MetadataSource.UI
                        });

                        IndexModel.SuccessResponse = Messages.HomeController_StartSuccess;
                    }
                    else
                    {
                        // Create partial request
                        newRequest = new UserRequest()
                        {
                            Guid = Guid.NewGuid().ToString(),
                            UserId = userId,
                            Status = CommandStatus.Requested,
                            Site = type == DeGaussRequestType.DriveTime ? IndexModel.Site : string.Empty,
                            Year = type == DeGaussRequestType.CensusBlockGroup ? IndexModel.Year : null,
                            RequestType = DeGaussRequestType.Composite.ToString(),
                            RequestSubType = type.ToString(),
                            UploadDateTime = uploadDateTime,
                            NextRequest = guid
                        };
                    }

                    guid = _userRequestRepository.AddUserRequest(newRequest);
                }
                catch (ApiException ex)
                {
                    _logger.LogError(ex, Messages.HomeController_Error, "StartGetAsyncOperation", ex.Message);
                    var message = ExtractValidationError(ex);
                    IndexModel.ErrorResponse = message;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, Messages.HomeController_Error, "StartGetAsyncOperation", ex.Message);
                    IndexModel.ErrorResponse = Messages.HomeController_GeneralError;
                }
            }

            await RefreshUserRequestData(IndexModel);
            return View(IndexModel);
        }
      
        /// <summary>
        /// Error method will catch invalid token error due to State Token Timeout and redirect to home page to renew state token. 
        /// </summary>
        /// <param name="StatusCode"></param>
        /// <returns></returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(string StatusCode = "")
        {
            var error = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
            if (!string.IsNullOrEmpty(StatusCode))
            {
                _logger.LogError("StatusCode:" + StatusCode + "; RequestId:" + error.RequestId);               
            }
            return View(error);
        }
    }
}