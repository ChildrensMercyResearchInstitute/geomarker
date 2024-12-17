using FluentValidation;
using GeoMarker.Frontiers.Core.HealthCheck;
using GeoMarker.Frontiers.Core.Helpers;
using GeoMarker.Frontiers.Core.Models.Request;
using GeoMarker.Frontiers.Core.Models.Request.Validation;
using GeoMarker.Frontiers.Core.Resources;
using GeoMarker.Frontiers.Web.Clients;
using GeoMarker.Frontiers.Web.Models;
using GeoMarker.Frontiers.Web.Models.Services;
using GeoMarker.Frontiers.Web.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

namespace GeoMarker.Frontiers.Web.Controllers
{
    /// <summary>
    /// Gateway REST controller for aggregating GeoMarker services.
    /// </summary>
    public class GatewayController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GeoCodeClient _geoCodeClient;
        private readonly CensusBlockGroupClient _censusBlockGroupClient;
        private readonly DriveTimeClient _driveTimeClient;
        private readonly DeprivationIndexClient _deprivationIndexClient;
        private readonly IValidator<DeGaussRequest> _degaussRequestValidator;
        private readonly IValidator<DeGaussDrivetimeRequest> _drivetimeRequestValidator;
        private readonly IValidator<DeGaussCensusBlockGroupRequest> _censusBlockGroupRequestValidator;
        private readonly IValidator<Core.Models.Request.DeGaussJsonRequest> _jsonRequestValidator;
        private readonly IValidator<Core.Models.Request.DeGaussGeocodedJsonRequest> _geocodedJsonRequestValidator;
        private readonly IValidator<Core.Models.Request.DeGaussDriveTimesJsonRequest> _driveTimesJsonRequestValidator;
        private readonly IValidator<Core.Models.Request.DeGaussCensusBlockGroupsJsonRequest> _censusBlockGroupsJsonRequestValidator;
        private readonly IValidator<DeGaussCompositeJsonRequest> _compositeJsonRequestValidator;
        private readonly IMetadataService _metadataService;
        private readonly IPingService _pingService;

        private const string _500ResponseDescriptor = "An unexpected error has occurred.";
        private const string _401ResponseDescriptor = "The request was not authorized.";
        private const string _400ResponseDescriptor = "The request was improperly formatted.";
        private const string _204ResponseDescriptor = "No records were successfully processed.";

        private const string _censusBlockGroupTag = "Census Block Group";
        private const string _driveTimeTag = "Drive Time";
        private const string _geocodeTag = "Geocode";
        private const string _depIndexTag = "Deprivation Index";
        private const string _compositeTag = "Composite";

        /// <summary>
        /// Constructor for the GatewayController
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="geoCodeClient"></param>
        /// <param name="censusBlockGroupClient"></param>
        /// <param name="driveTimeClient"></param>
        /// <param name="deprivationIndexClient"></param>
        /// <param name="degaussRequestValidator"></param>
        /// <param name="drivetimeRequestValidator"></param>
        /// <param name="censusBlockGroupRequestValidator"></param>
        /// <param name="jsonRequestValidator"></param>
        /// <param name="geocodedJsonRequestValidator"></param>
        /// <param name="driveTimesJsonRequestValidator"></param>
        /// <param name="censusBlockGroupsJsonRequestValidator"></param>
        /// <param name="compositeJsonRequestValidator"></param>
        /// <param name="metadataService"></param>
        /// <param name="pingService"></param>
        public GatewayController(ILogger<HomeController> logger,
                                 GeoCodeClient geoCodeClient,
                                 CensusBlockGroupClient censusBlockGroupClient,
                                 DriveTimeClient driveTimeClient,
                                 DeprivationIndexClient deprivationIndexClient,
                                 IValidator<DeGaussRequest> degaussRequestValidator,
                                 IValidator<DeGaussDrivetimeRequest> drivetimeRequestValidator,
                                 IValidator<DeGaussCensusBlockGroupRequest> censusBlockGroupRequestValidator,
                                 IValidator<Core.Models.Request.DeGaussJsonRequest> jsonRequestValidator,
                                 IValidator<Core.Models.Request.DeGaussGeocodedJsonRequest> geocodedJsonRequestValidator,
                                 IValidator<Core.Models.Request.DeGaussDriveTimesJsonRequest> driveTimesJsonRequestValidator,
                                 IValidator<Core.Models.Request.DeGaussCensusBlockGroupsJsonRequest> censusBlockGroupsJsonRequestValidator,
                                 IValidator<DeGaussCompositeJsonRequest> compositeJsonRequestValidator,
                                 IMetadataService metadataService,
                                 IPingService pingService)
        {
            _logger = logger;
            _geoCodeClient = geoCodeClient;
            _censusBlockGroupClient = censusBlockGroupClient;
            _driveTimeClient = driveTimeClient;
            _deprivationIndexClient = deprivationIndexClient;
            _degaussRequestValidator = degaussRequestValidator;
            _drivetimeRequestValidator = drivetimeRequestValidator;
            _censusBlockGroupRequestValidator = censusBlockGroupRequestValidator;
            _jsonRequestValidator = jsonRequestValidator;
            _geocodedJsonRequestValidator = geocodedJsonRequestValidator;
            _driveTimesJsonRequestValidator = driveTimesJsonRequestValidator;
            _censusBlockGroupsJsonRequestValidator = censusBlockGroupsJsonRequestValidator;
            _compositeJsonRequestValidator = compositeJsonRequestValidator;
            _metadataService = metadataService;
            _pingService = pingService;
        }

        #region GetGeocodes
        /// <summary>
        /// Accepts and GeoCodes a .csv file containing up to 300 address records.
        /// </summary>
        /// <remarks>
        /// Standard requests can send an empty GUID. Composite requests require a GUID as context for the request chain. 
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _geocodeTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Geocode response", typeof(Stream), "text/csv")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("multipart/form-data")]
        [Authorize(AuthenticationSchemes = "geocodeclient")]
        public async Task<IActionResult> GetGeocodes([FromForm] DeGaussRequest request)
        {
            if (!await _pingService.CheckServiceAvailablityAsync(_geoCodeClient.BaseUrl + "/health"))
                return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "Geocode"), null, StatusCodes.Status503ServiceUnavailable);

            var validator = _degaussRequestValidator.Validate(request, options => options.IncludeRuleSets("Base", "Geocode", "BelowMaxRows", "MaxFileSize", "ValidateFileName", "ValidateFileNameGeocode"));
            if (!validator.IsValid)
                return ValidationProblem(ValidationHelper.MapProblemDetails(validator));

            using var stream = new MemoryStream();
            request.File!.CopyTo(stream);
            stream!.Seek(0, SeekOrigin.Begin);

            var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            _geoCodeClient.SetBearerToken(token);
            var startDate = DateTime.UtcNow;
            var result = await _geoCodeClient.GetGeocodesAsync(new FileParameter(stream, request.File.FileName, request.File.ContentType), string.Empty);
            var endDate = DateTime.UtcNow;

            _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
            {
                File = request.File,
                DeGaussRequestType = DeGaussRequestType.GeoCode,
                StartDate = startDate,
                EndDate = endDate,
                FileResponse = result,
                Format = MetadataSource.API,
                UserId = GetClientId()
            });
            return File(result.Stream, "text/csv");
        }

        /// <summary>
        /// Accepts and geocodes a .csv file containing address records asynchronously.
        /// </summary>
        /// <remarks>
        /// Standard requests can send an empty GUID. Composite requests require a GUID as context for the request chain. 
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _geocodeTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Geocode async response", typeof(Clients.DeGaussAsyncResponse), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("multipart/form-data")]
        [Authorize(AuthenticationSchemes = "geocodeclient")]
        public async Task<IActionResult> StartGetGeocodes([FromForm] DeGaussRequest request)
        {
            if (!await _pingService.CheckServiceAvailablityAsync(_geoCodeClient.BaseUrl + "/health"))
                return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "Geocode", null, StatusCodes.Status503ServiceUnavailable));

            var validator = _degaussRequestValidator.Validate(request, options => options.IncludeRuleSets("Base", "Geocode", "MaxFileSize", "ValidateFileName", "ValidateFileNameGeocode"));
            if (!validator.IsValid)
                return ValidationProblem(ValidationHelper.MapProblemDetails(validator));

            using var stream = new MemoryStream();
            request.File!.CopyTo(stream);
            stream!.Seek(0, SeekOrigin.Begin);

            var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            _geoCodeClient.SetBearerToken(token);
            DeGaussAsyncResponse? result = await _geoCodeClient.StartGetGeocodesAsync(new FileParameter(stream, request.File.FileName, request.File.ContentType), string.Empty);

            _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
            {
                File = request.File,
                DeGaussRequestType = DeGaussRequestType.GeoCode,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.MinValue,
                Guid = result.Guid,
                Format = MetadataSource.API,
                UserId = GetClientId()
            });
            return new JsonResult(result);
        }

        /// <summary>
        /// Get the status of an asynchronous geocoding operation.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _geocodeTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Geocode async response", typeof(Clients.DeGaussAsyncResponse), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("application/json")]
        [Authorize(AuthenticationSchemes = "geocodeclient")]
        public async Task<IActionResult> GetGeocodesStatus([FromBody] Clients.DeGaussAsyncRequest request)
        {
            if (!await _pingService.CheckServiceAvailablityAsync(_geoCodeClient.BaseUrl + "/health"))
                return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "GeoCode", null, StatusCodes.Status503ServiceUnavailable));

            if (string.IsNullOrEmpty(request.Guid))
                return ValidationProblem();

            var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            _geoCodeClient.SetBearerToken(token);
            var statusResponse = await _geoCodeClient.GetGeocodesStatusAsync(request);
            CheckCommandFailed(statusResponse);
            return new JsonResult(statusResponse);
        }

        /// <summary>
        /// Get the result of an asynchronous geocoding operation.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _geocodeTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Geocode async response", typeof(Stream), "text/csv")]
        [SwaggerResponse(StatusCodes.Status204NoContent, _204ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("application/json")]
        [Authorize(AuthenticationSchemes = "geocodeclient")]
        public async Task<IActionResult> GetGeocodesResult([FromBody] Clients.DeGaussAsyncRequest request)
        {
            try
            {
                if (!await _pingService.CheckServiceAvailablityAsync(_geoCodeClient.BaseUrl + "/health"))
                    return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "GeoCode", null, StatusCodes.Status503ServiceUnavailable));

                if (string.IsNullOrEmpty(request.Guid))
                    return ValidationProblem();

                var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                _geoCodeClient.SetBearerToken(token);
                var result = await _geoCodeClient.GetGeocodesResultAsync(request);

                _metadataService.CompleteRecordsProcessed(new MetadataServiceCriteria()
                {
                    Guid = request.Guid,
                    EndDate = DateTime.UtcNow,
                    FileResponse = result
                });
                return File(result.Stream, "text/csv");
            }
            catch (ApiException ex)
            {
                var statusResponse = await _geoCodeClient.GetGeocodesStatusAsync(request);
                CheckCommandFailed(statusResponse);
                if (ex.StatusCode == 204)
                    return NoContent();
                else if (ex.StatusCode == 400)
                    return BadRequest(ex.Response);
                else
                {
                    _logger.LogError(ex, CoreMessages.Controller_Failure, "GetGeocodesResult");
                    return Problem(string.Format(CoreMessages.Controller_Failure, "GetGeocodesResult"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, "GetGeocodesResult");
                return Problem(string.Format(CoreMessages.Controller_Failure, "GetGeocodesResult"));
            }
        }

        /// <summary>
        /// Geocode up to 300 addresses provided in JSON format. 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _geocodeTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Geocode response", typeof(List<Core.Models.Request.DeGaussGeocodedJsonRecord>), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status204NoContent, _204ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("application/json")]
        [Authorize(AuthenticationSchemes = "geocodeclient")]
        public async Task<IActionResult> GetGeocodesJson([FromBody] Core.Models.Request.DeGaussJsonRequest request)
        {
            if (!await _pingService.CheckServiceAvailablityAsync(_geoCodeClient.BaseUrl + "/health"))
                return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "Geocode", null, StatusCodes.Status503ServiceUnavailable));

            if (request == null || request.Addresses.Count == 0)
                return ValidationProblem(ValidationHelper.GetIncorrectJson("Addresses", CoreMessages.JsonAddressFormat));

            var validator = _jsonRequestValidator.Validate(request);
            if (!validator.IsValid)
                return ValidationProblem(ValidationHelper.MapProblemDetails(validator));

            try
            {
                var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                _geoCodeClient.SetBearerToken(token);
                var startDate = DateTime.UtcNow;
                var response = await _geoCodeClient.GetGeocodesJsonAsync(new Clients.DeGaussJsonRequest { Addresses = ConvertAddressRequests(request.Addresses) });
                var endDate = DateTime.UtcNow;

                if (response == null || response.Count == 0)
                    return NoContent();

                _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
                {
                    DeGaussRequestType = DeGaussRequestType.SingleAddress,
                    Records = request.Addresses.Count,
                    StartDate = startDate,
                    EndDate = endDate,
                    Format = MetadataSource.API,
                    UserId = GetClientId()
                });
                return new JsonResult(response);
            }
            catch (ApiException ex)
            {
                if (ex.StatusCode == 204)
                    return NoContent();
                else if (ex.StatusCode == 400)
                    return BadRequest(ex.Response);
                else
                {
                    _logger.LogError(ex, CoreMessages.Controller_Failure, "GetSingleAddressGeocode");
                    return Problem(string.Format(CoreMessages.Controller_Failure, "GetSingleAddressGeocode"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, "GetSingleAddressGeocode");
                return Problem(string.Format(CoreMessages.Controller_Failure, "GetSingleAddressGeocode"));
            }
        }
        #endregion

        #region GetDriveTimes
        /// <summary>
        /// Accepts a geocoded .csv file with up to 300 records and site to generate .csv file containing drive_time in minutes and distance in meters.
        /// </summary>
        /// <remarks>
        /// Standard requests can send an empty GUID. Composite requests require a GUID as context for the request chain. 
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _driveTimeTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Drivetime response", typeof(Stream), "text/csv")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("multipart/form-data")]
        [Authorize(AuthenticationSchemes = "drivetimeclient")]
        public async Task<IActionResult> GetDriveTimes([FromForm] DeGaussDrivetimeRequest request)
        {
            if (!await _pingService.CheckServiceAvailablityAsync(_driveTimeClient.BaseUrl + "/health"))
                return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "DriveTime", null, StatusCodes.Status503ServiceUnavailable));

            var validator = _drivetimeRequestValidator.Validate(request, options => options.IncludeRuleSets("Base", "Geocoded", "Drivetime", "BelowMaxRows", "MaxFileSize", "ValidateFileName", "ValidateFileNameDriveTime"));
            if (!validator.IsValid)
                return ValidationProblem(ValidationHelper.MapProblemDetails(validator));

            using var stream = new MemoryStream();
            request.File!.CopyTo(stream);
            stream!.Seek(0, SeekOrigin.Begin);

            var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            _driveTimeClient.SetBearerToken(token);
            var startDate = DateTime.UtcNow;
            var result = await _driveTimeClient.GetDriveTimesAsync(request.Site, new FileParameter(stream, request.File.FileName, request.File.ContentType), string.Empty);
            var endDate = DateTime.UtcNow;

            _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
            {
                File = request.File,
                DeGaussRequestType = DeGaussRequestType.DriveTime,
                StartDate = startDate,
                EndDate = endDate,
                FileResponse = result,
                Format = MetadataSource.API,
                UserId = GetClientId()
            });
            return File(result.Stream, "text/csv");
        }

        /// <summary>
        /// Accepts a geocoded .csv file and site. Asynchronously generate .csv file containing drive_time in minutes and distance in meters.
        /// </summary>
        /// <remarks>
        /// Standard requests can send an empty GUID. Composite requests require a GUID as context for the request chain. 
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _driveTimeTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Drivetime async response", typeof(Clients.DeGaussAsyncResponse), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("multipart/form-data")]
        [Authorize(AuthenticationSchemes = "drivetimeclient")]
        public async Task<IActionResult> StartGetDriveTimes([FromForm] DeGaussDrivetimeRequest request)
        {
            if (!await _pingService.CheckServiceAvailablityAsync(_driveTimeClient.BaseUrl + "/health"))
                return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "DriveTime", null, StatusCodes.Status503ServiceUnavailable));

            var validator = _drivetimeRequestValidator.Validate(request, options => options.IncludeRuleSets("Base", "Geocoded", "Drivetime", "MaxFileSize", "ValidateFileName", "ValidateFileNameDriveTime"));
            if (!validator.IsValid)
                return ValidationProblem(ValidationHelper.MapProblemDetails(validator));

            using var stream = new MemoryStream();
            request.File!.CopyTo(stream);
            stream!.Seek(0, SeekOrigin.Begin);

            var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            _driveTimeClient.SetBearerToken(token);
            DeGaussAsyncResponse? result = await _driveTimeClient.StartGetDriveTimesAsync(request.Site, new FileParameter(stream, request.File.FileName, request.File.ContentType), string.Empty);

            _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
            {
                File = request.File,
                DeGaussRequestType = DeGaussRequestType.DriveTime,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.MinValue,
                Guid = result.Guid,
                Format = MetadataSource.API,
                UserId = GetClientId()
            });
            return new JsonResult(result);
        }

        /// <summary>
        /// Get the status of an asynchronous drivetime operation.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _driveTimeTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Drivetime async response", typeof(Clients.DeGaussAsyncResponse), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("application/json")]
        [Authorize(AuthenticationSchemes = "drivetimeclient")]
        public async Task<IActionResult> GetDriveTimesStatus([FromBody] Clients.DeGaussAsyncRequest request)
        {
            if (!await _pingService.CheckServiceAvailablityAsync(_driveTimeClient.BaseUrl + "/health"))
                return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "DriveTime", null, StatusCodes.Status503ServiceUnavailable));

            if (string.IsNullOrEmpty(request.Guid))
                return ValidationProblem();

            var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            _driveTimeClient.SetBearerToken(token);
            var statusResponse = await _driveTimeClient.GetDriveTimesStatusAsync(request);
            CheckCommandFailed(statusResponse);
            return new JsonResult(statusResponse);
        }

        /// <summary>
        /// Get the result of an asynchronous drivetime operation.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _driveTimeTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Drivetime response", typeof(Stream), "text/csv")]
        [SwaggerResponse(StatusCodes.Status204NoContent, _204ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("application/json")]
        [Authorize(AuthenticationSchemes = "drivetimeclient")]
        public async Task<IActionResult> GetDriveTimesResult([FromBody] Clients.DeGaussAsyncRequest request)
        {
            try
            {
                if (!await _pingService.CheckServiceAvailablityAsync(_driveTimeClient.BaseUrl + "/health"))
                    return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "DriveTime", null, StatusCodes.Status503ServiceUnavailable));

                if (string.IsNullOrEmpty(request.Guid))
                    return ValidationProblem();

                var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                _driveTimeClient.SetBearerToken(token);
                var result = await _driveTimeClient.GetDriveTimesResultAsync(request);

                _metadataService.CompleteRecordsProcessed(new MetadataServiceCriteria()
                {
                    Guid = request.Guid,
                    EndDate = DateTime.UtcNow,
                    FileResponse = result
                });
                return File(result.Stream, "text/csv");
            }
            catch (ApiException ex)
            {
                var statusResponse = await _driveTimeClient.GetDriveTimesStatusAsync(request);
                CheckCommandFailed(statusResponse);
                if (ex.StatusCode == 204)
                    return NoContent();
                else if (ex.StatusCode == 400)
                    return BadRequest(ex.Response);
                else
                {
                    _logger.LogError(ex, CoreMessages.Controller_Failure, "GetDriveTimesResult");
                    return Problem(string.Format(CoreMessages.Controller_Failure, "GetDriveTimesResult"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, "GetDriveTimesResult");
                return Problem(string.Format(CoreMessages.Controller_Failure, "GetDriveTimesResult"));
            }
        }

        /// <summary>
        /// Decorate up to 300 geocoded addresses with drive time data. 
        /// </summary>
        /// <remarks>
        /// 
        /// Accepts a collection of 'Records' with geocoded address data and other decoration data that will be passed though the application for final output.
        /// 
        /// Intended as a follow up method to be called with data from GetGeocodesJson, or any other geocoded API endpoint. 
        /// 
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _driveTimeTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Drivetime response", typeof(List<Core.Models.Request.DeGaussGeocodedJsonRecord>), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status204NoContent, _204ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("application/json")]
        [Authorize(AuthenticationSchemes = "drivetimeclient")]
        public async Task<IActionResult> GetDriveTimesJson([FromBody] Core.Models.Request.DeGaussDriveTimesJsonRequest request)
        {
            if (!await _pingService.CheckServiceAvailablityAsync(_driveTimeClient.BaseUrl + "/health"))
                return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "DriveTime", null, StatusCodes.Status503ServiceUnavailable));

            try
            {
                if (request == null || request.Records == null || request.Records.Count == 0)
                    return ValidationProblem(ValidationHelper.GetIncorrectJson("Addresses", string.Format(CoreMessages.JsonLatLonFormat, ", \"site\":\"site\"")));
                if (request.Records.Count > DeGaussRequestValidator.MAX_LINES)
                    return ValidationProblem(ValidationHelper.GetTooManyAddresses());

                var validation = _driveTimesJsonRequestValidator.Validate(new Core.Models.Request.DeGaussDriveTimesJsonRequest() { Records = request.Records, Site = request.Site });
                if (!validation.IsValid)
                    return ValidationProblem(ValidationHelper.MapProblemDetails(validation));

                var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                _driveTimeClient.SetBearerToken(token);
                var startDate = DateTime.UtcNow;
                var response = await _driveTimeClient.GetDriveTimesJsonAsync(new Clients.DeGaussDriveTimesJsonRequest() { Records = ConvertRecordList(request.Records), Site = request.Site });
                var endDate = DateTime.UtcNow;

                if (response == null || response.Count == 0)
                    return NoContent();

                _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
                {
                    DeGaussRequestType = DeGaussRequestType.DriveTime,
                    Records = request.Records.Count,
                    StartDate = startDate,
                    EndDate = endDate,
                    Format = MetadataSource.API,
                    UserId = GetClientId()
                });
                return new JsonResult(response);
            }
            catch (ApiException ex)
            {
                if (ex.StatusCode == 204)
                    return NoContent();
                else if (ex.StatusCode == 400)
                    return BadRequest(ex.Response);
                else
                {
                    _logger.LogError(ex, CoreMessages.Controller_Failure, "GetDriveTimesJson");
                    return Problem(string.Format(CoreMessages.Controller_Failure, "GetDriveTimesJson"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, "GetDriveTimesJson");
                return Problem(string.Format(CoreMessages.Controller_Failure, "GetDriveTimesJson"));
            }
        }
        #endregion

        #region GetDeprivationIndexes
        /// <summary>
        /// Accepts a geocoded .csv file with up to 300 records to generate a .csv file containing deprivation indexes.
        /// </summary>
        /// <remarks>
        /// Standard requests can send an empty GUID. Composite requests require a GUID as context for the request chain. 
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _depIndexTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Deprivation index response", typeof(Stream), "text/csv")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("multipart/form-data")]
        [Authorize(AuthenticationSchemes = "deprivationindexclient")]
        public async Task<IActionResult> GetDeprivationIndexes([FromForm] DeGaussRequest request)
        {
            if (!await _pingService.CheckServiceAvailablityAsync(_deprivationIndexClient.BaseUrl + "/health"))
                return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "DeprivationIndex", null, StatusCodes.Status503ServiceUnavailable));

            var validator = _degaussRequestValidator.Validate(request, options => options.IncludeRuleSets("Base", "Geocoded", "BelowMaxRows", "MaxFileSize", "ValidateFileName", "ValidateFileNameDeprivationIndex"));
            if (!validator.IsValid)
                return ValidationProblem(ValidationHelper.MapProblemDetails(validator));

            using var stream = new MemoryStream();
            request.File!.CopyTo(stream);
            stream!.Seek(0, SeekOrigin.Begin);

            var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            _deprivationIndexClient.SetBearerToken(token);
            var startDate = DateTime.UtcNow;
            var result = await _deprivationIndexClient.GetDeprivationIndexAsync(new FileParameter(stream, request.File.FileName, request.File.ContentType), string.Empty);
            var endDate = DateTime.UtcNow;

            _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
            {
                File = request.File,
                DeGaussRequestType = DeGaussRequestType.DeprivationIndex,
                StartDate = startDate,
                EndDate = endDate,
                FileResponse = result,
                Format = MetadataSource.API,
                UserId = GetClientId()
            });
            return File(result.Stream, "text/csv");
        }

        /// <summary>
        /// Accepts a geocoded .csv file and site. Asynchronously generate a .csv file containing deprivation indexes.
        /// </summary>
        /// <remarks>
        /// Standard requests can send an empty GUID. Composite requests require a GUID as context for the request chain. 
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _depIndexTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Deprivation index async response", typeof(Clients.DeGaussAsyncResponse), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("multipart/form-data")]
        [Authorize(AuthenticationSchemes = "deprivationindexclient")]
        public async Task<IActionResult> StartGetDeprivationIndexes([FromForm] DeGaussRequest request)
        {
            if (!await _pingService.CheckServiceAvailablityAsync(_deprivationIndexClient.BaseUrl + "/health"))
                return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "DeprivationIndex", null, StatusCodes.Status503ServiceUnavailable));

            var validator = _degaussRequestValidator.Validate(request, options => options.IncludeRuleSets("Base", "Geocoded", "MaxFileSize", "ValidateFileName", "ValidateFileNameDeprivationIndex"));
            if (!validator.IsValid)
                return ValidationProblem(ValidationHelper.MapProblemDetails(validator));

            using var stream = new MemoryStream();
            request.File!.CopyTo(stream);
            stream!.Seek(0, SeekOrigin.Begin);

            var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            _deprivationIndexClient.SetBearerToken(token);
            DeGaussAsyncResponse? result = await _deprivationIndexClient.StartGetDeprivationIndexesAsync(new FileParameter(stream, request.File.FileName, request.File.ContentType), string.Empty);

            _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
            {
                File = request.File,
                DeGaussRequestType = DeGaussRequestType.DeprivationIndex,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.MinValue,
                Guid = result.Guid,
                Format = MetadataSource.API,
                UserId = GetClientId()
            });
            return new JsonResult(result);
        }

        /// <summary>
        /// Get the status of an asynchronous deprivation index operation.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _depIndexTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Deprivation index async response", typeof(Clients.DeGaussAsyncResponse), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("application/json")]
        [Authorize(AuthenticationSchemes = "deprivationindexclient")]
        public async Task<IActionResult> GetDeprivationIndexesStatus([FromBody] Clients.DeGaussAsyncRequest request)
        {
            if (!await _pingService.CheckServiceAvailablityAsync(_deprivationIndexClient.BaseUrl + "/health"))
                return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "DeprivationIndex", null, StatusCodes.Status503ServiceUnavailable));

            if (string.IsNullOrEmpty(request.Guid))
                return ValidationProblem();

            var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            _deprivationIndexClient.SetBearerToken(token);
            var statusResponse = await _deprivationIndexClient.GetDeprivationIndexesStatusAsync(request);
            CheckCommandFailed(statusResponse);
            return new JsonResult(statusResponse);
        }

        /// <summary>
        /// Get the result of an asynchronous deprivation index operation.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _depIndexTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Deprivation index response", typeof(Stream), "text/csv")]
        [SwaggerResponse(StatusCodes.Status204NoContent, _204ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("application/json")]
        [Authorize(AuthenticationSchemes = "deprivationindexclient")]
        public async Task<IActionResult> GetDeprivationIndexesResult([FromBody] Clients.DeGaussAsyncRequest request)
        {
            try
            {
                if (!await _pingService.CheckServiceAvailablityAsync(_deprivationIndexClient.BaseUrl + "/health"))
                    return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "DeprivationIndex", null, StatusCodes.Status503ServiceUnavailable));

                if (string.IsNullOrEmpty(request.Guid))
                    return ValidationProblem();

                var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                _deprivationIndexClient.SetBearerToken(token);
                var result = await _deprivationIndexClient.GetDeprivationIndexesResultAsync(request);

                _metadataService.CompleteRecordsProcessed(new MetadataServiceCriteria()
                {
                    Guid = request.Guid,
                    EndDate = DateTime.UtcNow,
                    FileResponse = result
                });
                return File(result.Stream, "text/csv");
            }
            catch (ApiException ex)
            {
                var statusResponse = await _deprivationIndexClient.GetDeprivationIndexesStatusAsync(request);
                CheckCommandFailed(statusResponse);
                if (ex.StatusCode == 204)
                    return NoContent();
                else if (ex.StatusCode == 400)
                    return BadRequest(ex.Response);
                else
                {
                    _logger.LogError(ex, CoreMessages.Controller_Failure, "GetDeprivationIndexesResult");
                    return Problem(string.Format(CoreMessages.Controller_Failure, "GetDeprivationIndexesResult"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, "GetDeprivationIndexesResult");
                return Problem(string.Format(CoreMessages.Controller_Failure, "GetDeprivationIndexesResult"));
            }
        }

        /// <summary>
        /// Decorate up to 300 geocoded addresses with deprivation index data. 
        /// </summary>
        /// <remarks>
        /// 
        /// Accepts a collection of 'Records' with geocoded address data and other decoration data that will be passed though the application for final output.
        /// 
        /// Intended as a follow up method to be called with data from GetGeocodesJson, or any other geocoded API endpoint. 
        /// 
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _depIndexTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Deprivation index response", typeof(List<Core.Models.Request.DeGaussGeocodedJsonRecord>), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("application/json")]
        [Authorize(AuthenticationSchemes = "deprivationindexclient")]
        public async Task<IActionResult> GetDeprivationIndexesJson([FromBody] Core.Models.Request.DeGaussGeocodedJsonRequest request)
        {
            if (!await _pingService.CheckServiceAvailablityAsync(_deprivationIndexClient.BaseUrl + "/health"))
                return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "DeprivationIndex", null, StatusCodes.Status503ServiceUnavailable));

            try
            {
                if (request == null || request.Records == null || request.Records.Count == 0)
                    return ValidationProblem(ValidationHelper.GetIncorrectJson("Addresses", string.Format(CoreMessages.JsonLatLonFormat, "")));
                if (request.Records.Count > DeGaussRequestValidator.MAX_LINES)
                    return ValidationProblem(ValidationHelper.GetTooManyAddresses());

                var validation = _geocodedJsonRequestValidator.Validate(new Core.Models.Request.DeGaussGeocodedJsonRequest() { Records = request.Records });
                if (!validation.IsValid)
                    return ValidationProblem(ValidationHelper.MapProblemDetails(validation));

                var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                _deprivationIndexClient.SetBearerToken(token);
                var startDate = DateTime.UtcNow;
                var response = await _deprivationIndexClient.GetDeprivationIndexesJsonAsync(new Clients.DeGaussGeocodedJsonRequest() { Records = ConvertRecordList(request.Records) });
                var endDate = DateTime.UtcNow;

                if (response == null || response.Count == 0)
                    return NoContent();

                _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
                {
                    DeGaussRequestType = DeGaussRequestType.DeprivationIndex,
                    Records = request.Records.Count,
                    StartDate = startDate,
                    EndDate = endDate,
                    Format = MetadataSource.API,
                    UserId = GetClientId()
                });
                return new JsonResult(response);
            }
            catch (ApiException ex)
            {
                if (ex.StatusCode == 204)
                    return NoContent();
                else if (ex.StatusCode == 400)
                    return BadRequest(ex.Response);
                else
                {
                    _logger.LogError(ex, CoreMessages.Controller_Failure, "GetDeprivationIndexesJson");
                    return Problem(string.Format(CoreMessages.Controller_Failure, "GetDeprivationIndexesJson"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, "GetDeprivationIndexesJson");
                return Problem(string.Format(CoreMessages.Controller_Failure, "GetDeprivationIndexesJson"));
            }
        }
        #endregion

        #region GetCensusBlockGroups
        /// <summary>
        /// Accepts a geocoded .csv file with up to 300 records to generate a .csv file with census block group data.
        /// </summary>
        /// <remarks>
        /// Standard requests can send an empty GUID. Composite requests require a GUID as context for the request chain. 
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _censusBlockGroupTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Census block group response", typeof(Stream), "text/csv")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("multipart/form-data")]
        [Authorize(AuthenticationSchemes = "censusblockclient")]
        public async Task<IActionResult> GetCensusBlockGroups([FromForm] DeGaussCensusBlockGroupRequest request)
        {
            if (!await _pingService.CheckServiceAvailablityAsync(_censusBlockGroupClient.BaseUrl + "/health"))
                return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "CensusBlockGroup", null, StatusCodes.Status503ServiceUnavailable));

            var validator = _censusBlockGroupRequestValidator.Validate(request, options => options.IncludeRuleSets("Base", "Geocoded", "CensusBlockGroup", "BelowMaxRows", "MaxFileSize", "ValidateFileName", "ValidateFileNameCensusBlockGroup"));
            if (!validator.IsValid)
                return ValidationProblem(ValidationHelper.MapProblemDetails(validator));

            using var stream = new MemoryStream();
            request.File!.CopyTo(stream);
            stream!.Seek(0, SeekOrigin.Begin);

            var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            _censusBlockGroupClient.SetBearerToken(token);
            var startDate = DateTime.UtcNow;
            var result = await _censusBlockGroupClient.GetCensusBlockGroupsAsync(request.Year, new FileParameter(stream, request.File.FileName, request.File.ContentType), string.Empty);
            var endDate = DateTime.UtcNow;

            _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
            {
                File = request.File,
                DeGaussRequestType = DeGaussRequestType.CensusBlockGroup,
                StartDate = startDate,
                EndDate = endDate,
                FileResponse = result,
                Format = MetadataSource.API,
                UserId = GetClientId()
            });
            return File(result.Stream, "text/csv");
        }

        /// <summary>
        /// Accepts a geocoded .csv file to generate a .csv file with census block group data asynchronously.
        /// </summary>
        /// <remarks>
        /// Standard requests can send an empty GUID. Composite requests require a GUID as context for the request chain. 
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _censusBlockGroupTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Census block group async response", typeof(DeGaussAsyncResponse), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("multipart/form-data")]
        [Authorize(AuthenticationSchemes = "censusblockclient")]
        public async Task<IActionResult> StartGetCensusBlockGroups([FromForm] DeGaussCensusBlockGroupRequest request)
        {
            if (!await _pingService.CheckServiceAvailablityAsync(_censusBlockGroupClient.BaseUrl + "/health"))
                return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "CensusBlockGroup", null, StatusCodes.Status503ServiceUnavailable));

            var validator = _censusBlockGroupRequestValidator.Validate(request, options => options.IncludeRuleSets("Base", "Geocoded", "CensusBlockGroup", "MaxFileSize", "ValidateFileName", "ValidateFileNameCensusBlockGroup"));
            if (!validator.IsValid)
                return ValidationProblem(ValidationHelper.MapProblemDetails(validator));

            using var stream = new MemoryStream();
            request.File!.CopyTo(stream);
            stream!.Seek(0, SeekOrigin.Begin);

            var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            _censusBlockGroupClient.SetBearerToken(token);
            DeGaussAsyncResponse? result = await _censusBlockGroupClient.StartGetCensusBlockGroupsAsync(request.Year, new FileParameter(stream, request.File.FileName, request.File.ContentType), string.Empty);

            _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
            {
                File = request.File,
                DeGaussRequestType = DeGaussRequestType.CensusBlockGroup,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.MinValue,
                Guid = result.Guid,
                Format = MetadataSource.API,
                UserId = GetClientId()
            });
            return new JsonResult(result);
        }

        /// <summary>
        /// Get the status of a asynchronous census block group operation.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _censusBlockGroupTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Census block group async response", typeof(DeGaussAsyncResponse), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("application/json")]
        [Authorize(AuthenticationSchemes = "censusblockclient")]
        public async Task<IActionResult> GetCensusBlockGroupsStatus([FromBody] Clients.DeGaussAsyncRequest request)
        {
            if (!await _pingService.CheckServiceAvailablityAsync(_censusBlockGroupClient.BaseUrl + "/health"))
                return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "CensusBlockGroup", null, StatusCodes.Status503ServiceUnavailable));

            if (string.IsNullOrEmpty(request.Guid))
                return ValidationProblem();

            var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
            _censusBlockGroupClient.SetBearerToken(token);
            var statusResponse = await _censusBlockGroupClient.GetCensusBlockGroupsStatusAsync(request);
            CheckCommandFailed(statusResponse);
            return new JsonResult(statusResponse);
        }

        /// <summary>
        /// Get the result of an asynchronous census block group operation.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _censusBlockGroupTag })]
        [SwaggerResponse(StatusCodes.Status200OK, "Census block group response", typeof(Stream), "text/csv")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Consumes("application/json")]
        [Authorize(AuthenticationSchemes = "censusblockclient")]
        public async Task<IActionResult> GetCensusBlockGroupsResult([FromBody] Clients.DeGaussAsyncRequest request)
        {
            try
            {
                if (!await _pingService.CheckServiceAvailablityAsync(_censusBlockGroupClient.BaseUrl + "/health"))
                    return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "CensusBlockGroup", null, StatusCodes.Status503ServiceUnavailable));

                if (string.IsNullOrEmpty(request.Guid))
                    return ValidationProblem();

                var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                _censusBlockGroupClient.SetBearerToken(token);
                var result = await _censusBlockGroupClient.GetCensusBlockGroupsResultAsync(request);

                _metadataService.CompleteRecordsProcessed(new MetadataServiceCriteria()
                {
                    Guid = request.Guid,
                    EndDate = DateTime.UtcNow,
                    FileResponse = result
                });
                return File(result.Stream, "text/csv");
            }
            catch (ApiException ex)
            {
                var statusResponse = await _censusBlockGroupClient.GetCensusBlockGroupsStatusAsync(request);
                CheckCommandFailed(statusResponse);
                if (ex.StatusCode == 204)
                    return NoContent();
                else if (ex.StatusCode == 400)
                    return BadRequest(ex.Response);
                else
                {
                    _logger.LogError(ex, CoreMessages.Controller_Failure, "GetCensusBlockGroupsResult");
                    return Problem(string.Format(CoreMessages.Controller_Failure, "GetCensusBlockGroupsResult"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, "GetCensusBlockGroupsResult");
                return Problem(string.Format(CoreMessages.Controller_Failure, "GetCensusBlockGroupsResult"));
            }
        }

        /// <summary>
        /// Decorate up to 300 geocoded addresses with census block group data. 
        /// </summary>
        /// <remarks>
        /// 
        /// Accepts a collection of 'Records' with geocoded address data and other decoration data that will be passed though the application for final output.
        /// 
        /// Intended as a follow up method to be called with data from GetGeocodesJson, or any other geocoded API endpoint. 
        /// 
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { "Census Block Group" })]
        [Consumes("application/json")]
        [SwaggerResponse(StatusCodes.Status200OK, "Census block group response", typeof(List<Core.Models.Request.DeGaussGeocodedJsonRecord>), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Authorize(AuthenticationSchemes = "censusblockclient")]
        public async Task<IActionResult> GetCensusBlockGroupsJson([FromBody] Core.Models.Request.DeGaussCensusBlockGroupsJsonRequest request)
        {
            if (!await _pingService.CheckServiceAvailablityAsync(_censusBlockGroupClient.BaseUrl + "/health"))
                return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "CensusBlockGroup", null, StatusCodes.Status503ServiceUnavailable));

            try
            {
                if (request == null || request.Records == null || request.Records.Count == 0)
                    return ValidationProblem(ValidationHelper.GetIncorrectJson("Addresses", string.Format(CoreMessages.JsonLatLonFormat, ", \"year\":2020")));
                if (request.Records.Count > DeGaussRequestValidator.MAX_LINES)
                    return ValidationProblem(ValidationHelper.GetTooManyAddresses());

                var validation = _censusBlockGroupsJsonRequestValidator.Validate(new Core.Models.Request.DeGaussCensusBlockGroupsJsonRequest() { Records = request.Records, Year = request.Year });
                if (!validation.IsValid)
                    return ValidationProblem(ValidationHelper.MapProblemDetails(validation));

                var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                _censusBlockGroupClient.SetBearerToken(token);
                var startDate = DateTime.UtcNow;
                var response = await _censusBlockGroupClient.GetCensusBlockGroupsJsonAsync(new Clients.DeGaussCensusBlockGroupsJsonRequest() { Records = ConvertRecordList(request.Records), Year = request.Year });
                var endDate = DateTime.UtcNow;

                if (response == null || response.Count == 0)
                    return NoContent();

                _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
                {
                    DeGaussRequestType = DeGaussRequestType.CensusBlockGroup,
                    Records = request.Records.Count,
                    StartDate = startDate,
                    EndDate = endDate,
                    Format = MetadataSource.API,
                    UserId = GetClientId()
                });
                return new JsonResult(response);
            }
            catch (ApiException ex)
            {
                if (ex.StatusCode == 204)
                    return NoContent();
                else if (ex.StatusCode == 400)
                    return BadRequest(ex.Response);
                else
                {
                    _logger.LogError(ex, CoreMessages.Controller_Failure, "GetCensusBlockGroupsJson");
                    return Problem(string.Format(CoreMessages.Controller_Failure, "GetCensusBlockGroupsJson"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, "GetCensusBlockGroupsJson");
                return Problem(string.Format(CoreMessages.Controller_Failure, "GetCensusBlockGroupsJson"));
            }
        }
        #endregion

        /// <summary>
        /// Geocode and decorate up to 300 addresses with drive time, deprivation index, and census block group data. 
        /// </summary>
        /// <remarks>
        /// 
        /// Accepted service types: "drivetime", "deprivationindex", "censusblockgroup". Geocoding will always occur.
        /// 
        /// "site" is the location to which drive time is computed and is required for drive time requests. 
        /// See: https://degauss.org/drivetime/ for more details and available sites.
        /// 
        /// "year" is the vintage for the US census data used for the census block group request and is required for census block group requests. 
        /// See: https://degauss.org/census_block_group/ for more details and available years.
        /// 
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [SwaggerOperation(Tags = new[] { _compositeTag })]
        [Consumes("application/json")]
        [SwaggerResponse(StatusCodes.Status200OK, "Composite response", typeof(List<Core.Models.Request.DeGaussGeocodedJsonRecord>), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest, _400ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, _401ResponseDescriptor)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, _500ResponseDescriptor)]
        [Authorize(AuthenticationSchemes = "geocodeclient,drivetimeclient,deprivationindexclient,censusblockclient")]
        public async Task<IActionResult> GetCompositeJson([FromBody] DeGaussCompositeJsonRequest request)
        {
            try
            {
                var scopes = User.Claims.FirstOrDefault(c => c.Type == "scope")?.Value;
                List<string> requiredScopes = new();
                if (scopes == null || !scopes.Contains("geocode"))
                    requiredScopes.Add("geocode");
                requiredScopes.AddRange(request.Services.Select(x => x == "censusblockgroup" ? scopes.Contains("censusblock") ? null : "censusblock" : scopes.Contains(x) ? null : x).Where(x => x != null));
                if (requiredScopes.Count != 0)
                    return StatusCode(StatusCodes.Status401Unauthorized, string.Format(Messages.AuthController_InvalidScope, string.Join(", ", requiredScopes)));

                if (request == null)
                    return ValidationProblem(ValidationHelper.GetIncorrectJson("Addresses", CoreMessages.JsonCompositeFormat));

                if (!await _pingService.CheckServiceAvailablityAsync(_geoCodeClient.BaseUrl + "/health"))
                    return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "GeoCode"), null, StatusCodes.Status503ServiceUnavailable);
                if (request.Services.Contains("drivetime") && !await _pingService.CheckServiceAvailablityAsync(_driveTimeClient.BaseUrl + "/health"))
                    return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "DriveTime"), null, StatusCodes.Status503ServiceUnavailable);
                if (request.Services.Contains("deprivationindex") && !await _pingService.CheckServiceAvailablityAsync(_deprivationIndexClient.BaseUrl + "/health"))
                    return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "DeprivationIndex"), null, StatusCodes.Status503ServiceUnavailable);
                if (request.Services.Contains("censusblockgroup") && !await _pingService.CheckServiceAvailablityAsync(_censusBlockGroupClient.BaseUrl + "/health"))
                    return Problem(string.Format(Messages.GatewayController_ServiceUnavailable, "CensusBlockGroup"), null, StatusCodes.Status503ServiceUnavailable);

                var validation = _compositeJsonRequestValidator.Validate(request);
                if (!validation.IsValid)
                    return ValidationProblem(ValidationHelper.MapProblemDetails(validation));
                
                var token = Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
                _geoCodeClient.SetBearerToken(token);
                var startDate = DateTime.UtcNow;
                var response = await _geoCodeClient.GetGeocodesJsonAsync(new Clients.DeGaussJsonRequest { Addresses = ConvertAddressRequests(request.Addresses) });
                var endDate = DateTime.UtcNow;
                var clientId = GetClientId();
                _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
                {
                    DeGaussRequestType = DeGaussRequestType.GeoCode,
                    Records = request.Addresses.Count,
                    StartDate = startDate,
                    EndDate = endDate,
                    Format = MetadataSource.API,
                    UserId = clientId
                });

                if (response.Count == 0)
                    return ValidationProblem(ValidationHelper.NoAddressesGeocoded());

                if (request.Services.Contains("drivetime"))
                {
                    _driveTimeClient.SetBearerToken(token);
                    startDate = DateTime.UtcNow;
                    response = await _driveTimeClient.GetDriveTimesJsonAsync(new Clients.DeGaussDriveTimesJsonRequest() { Records = response, Site = request.Site });

                    endDate = DateTime.UtcNow;
                    _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
                    {
                        DeGaussRequestType = DeGaussRequestType.DriveTime,
                        Records = request.Addresses.Count,
                        StartDate = startDate,
                        EndDate = endDate,
                        Format = MetadataSource.API,
                        UserId = clientId
                    });
                }
                if (request.Services.Contains("deprivationindex"))
                {
                    _deprivationIndexClient.SetBearerToken(token);

                    startDate = DateTime.UtcNow;
                    response = await _deprivationIndexClient.GetDeprivationIndexesJsonAsync(new Clients.DeGaussGeocodedJsonRequest() { Records = response });

                    endDate = DateTime.UtcNow;
                    _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
                    {
                        DeGaussRequestType = DeGaussRequestType.DeprivationIndex,
                        Records = request.Addresses.Count,
                        StartDate = startDate,
                        EndDate = endDate,
                        Format = MetadataSource.API,
                        UserId = clientId
                    });
                }
                if (request.Services.Contains("censusblockgroup"))
                {
                    _censusBlockGroupClient.SetBearerToken(token);

                    startDate = DateTime.UtcNow;
                    response = await _censusBlockGroupClient.GetCensusBlockGroupsJsonAsync(new Clients.DeGaussCensusBlockGroupsJsonRequest() { Records = response, Year = request.Year });

                    endDate = DateTime.UtcNow;
                    _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
                    {
                        DeGaussRequestType = DeGaussRequestType.CensusBlockGroup,
                        Records = request.Addresses.Count,
                        StartDate = startDate,
                        EndDate = endDate,
                        Format = MetadataSource.API,
                        UserId = clientId
                    });
                }
                return new JsonResult(response);
            }
            catch (ApiException ex)
            {
                if (ex.StatusCode == 204)
                    return NoContent();
                else if (ex.StatusCode == 400)
                    return BadRequest(ex.Response);
                else
                {
                    _logger.LogError(ex, CoreMessages.Controller_Failure, "GetCompositeJson");
                    return Problem(string.Format(CoreMessages.Controller_Failure, "GetCompositeJson"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, "GetCompositeJson");
                return Problem(string.Format(CoreMessages.Controller_Failure, "GetCompositeJson"));
            }
        }

        private void CheckCommandFailed(DeGaussAsyncResponse request)
        {
            if (request.Status == CommandStatus.Duplicate ||
                request.Status == CommandStatus.Failure ||
                request.Status == CommandStatus.Rejected)
            {
                _metadataService.FailRecordsProcessed(new MetadataServiceCriteria()
                {
                    Guid = request.Guid,
                    EndDate = DateTime.UtcNow
                });
            }
        }

        private List<Clients.DeGaussGeocodedJsonRecord> ConvertRecordList(List<Core.Models.Request.DeGaussGeocodedJsonRecord> records)
        {
            var clientsRecords = new List<Clients.DeGaussGeocodedJsonRecord>();
            foreach (var record in records)
            {
                clientsRecords.Add(new Clients.DeGaussGeocodedJsonRecord
                {
                    Id = record.id,
                    Lat = record.lat,
                    Lon = record.lon,
                    Zip = record.zip,
                    City = record.city,
                    State = record.state,
                    Fips_county = record.fips_county,
                    Score = record.score,
                    Precision = record.precision,
                    Drive_time = record.drive_time,
                    Distance = record.distance,
                    Census_tract_id = record.census_tract_id,
                    Fraction_assisted_income = record.fraction_assisted_income,
                    Fraction_high_school_edu = record.fraction_high_school_edu,
                    Median_income = record.median_income,
                    Fraction_no_health_ins = record.fraction_no_health_ins,
                    Fraction_poverty = record.fraction_poverty,
                    Fraction_vacant_housing = record.fraction_vacant_housing,
                    Dep_index = record.dep_index,
                    Census_block_group_id_1990 = record.census_block_group_id_1990,
                    Census_block_group_id_2000 = record.census_block_group_id_2000,
                    Census_block_group_id_2010 = record.census_block_group_id_2010,
                    Census_block_group_id_2020 = record.census_block_group_id_2020
                });
            }
            return clientsRecords;
        }

        private List<Clients.DeGaussAddressRequest> ConvertAddressRequests(List<Core.Models.Request.DeGaussAddressRequest> addresses)
        {
            var clientAddresses = new List<Clients.DeGaussAddressRequest>();
            foreach (var address in addresses)
            {
                clientAddresses.Add(new Clients.DeGaussAddressRequest()
                {
                    Id = address.Id,
                    Address = address.Address
                });
            }
            return clientAddresses;
        }
        /// <summary>
        /// GetGeoMarkerRequests API will get user requests by user email id. 
        /// </summary>
        /// <param name="email">Should not be empty or null. </param>
        /// <returns></returns>

        [HttpGet]
        [Route("api/GeoMarker/[action]")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(AuthenticationSchemes = "geomarkerclient")]
        public async Task<IActionResult> GetGeoMarkerRequests([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest();
                var emailAttributes = new EmailAddressAttribute();

                if (!emailAttributes.IsValid(email))
                    return Problem("GetGeoMarkerRequests", null, StatusCodes.Status400BadRequest);

                var result = _metadataService.GetGeocodeUserRequests(email, "GeoCode");
                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem(string.Format(CoreMessages.Controller_Failure, "GetGeoMarkerRequests"));
            }
        }
        private string? GetClientId()
        {
            return User.Claims.FirstOrDefault(c => c.Type == "client_id")?.Value;
        }
    }
}
