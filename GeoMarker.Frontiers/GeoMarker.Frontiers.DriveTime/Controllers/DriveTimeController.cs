using FluentValidation;
using GeoMarker.Frontiers.Core.Helpers;
using GeoMarker.Frontiers.Core.Models.Commands;
using GeoMarker.Frontiers.Core.Models.Request;
using GeoMarker.Frontiers.Core.Models.Response;
using GeoMarker.Frontiers.Core.Resources;
using GeoMarker.Frontiers.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using System.Net.Mime;

namespace GeoMarker.Frontiers.DriveTime.Controllers
{
    /// <summary>
    /// API Controller to get drive time and distance for given GeoCoded addresses and site. 
    /// </summary>
    [ApiController]
    [Authorize]
    public class DriveTimeController : ControllerBase
    {
        private readonly ILogger<DriveTimeController> _logger;
        private readonly IDeGaussCommandService _drivetimeCommandService;
        private readonly IValidator<DeGaussDrivetimeRequest> _drivetimeRequestValidator;
        private readonly IValidator<DeGaussGeocodedJsonRequest> _geocodedJsonRequestValdiator;
        private readonly IValidator<DeGaussAsyncRequest> _asyncDrivetimeRequest;
        private readonly string type = "DriveTime";
        /// <summary>
        /// Construct the API controller.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="drivetimeCommandService"></param>
        /// <param name="drivetimeRequestValidator"></param>
        /// <param name="geocodedJsonRequestValdiator"></param>
        /// <param name="asyncDrivetimeRequest"></param>
        public DriveTimeController(ILogger<DriveTimeController> logger,
                                    IDeGaussCommandService drivetimeCommandService,
                                    IValidator<DeGaussDrivetimeRequest> drivetimeRequestValidator,
                                    IValidator<DeGaussGeocodedJsonRequest> geocodedJsonRequestValdiator,
                                    IValidator<DeGaussAsyncRequest> asyncDrivetimeRequest)
        {
            _logger = logger;
            _drivetimeCommandService = drivetimeCommandService;
            _drivetimeRequestValidator = drivetimeRequestValidator;
            _geocodedJsonRequestValdiator = geocodedJsonRequestValdiator;
            _asyncDrivetimeRequest = asyncDrivetimeRequest;
        }

        /// <summary>
        /// POST GetDriveTimes - Accepts GeoCoded csv file and site to generate .csv file containing drive_time in minutes and distable in meters.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [Consumes("multipart/form-data")]
        [SwaggerResponse(StatusCodes.Status200OK, "CSV File Stream", typeof(FileResult))]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDriveTimes([FromForm] DeGaussDrivetimeRequest request)
        {
            try
            {
                var result = _drivetimeRequestValidator.Validate(request, options => options.IncludeRuleSets("Base", "Geocoded", "Drivetime", "BelowMaxRows", "MaxFileSize", "ValidateFileName", "ValidateFileNameDriveTime"));
                if (!result.IsValid)
                    return ValidationProblem(ValidationHelper.MapProblemDetails(result));

                var resultTask = await _drivetimeCommandService.GetService(new DeGaussCommandTask()
                {
                    File = request.File!,
                    Site = request.Site,
                    Type = type
                }, string.IsNullOrEmpty(request.RequestGuid) ? Guid.NewGuid().ToString() : request.RequestGuid);

                if (resultTask.Stream == null)
                    return Problem(string.Format(CoreMessages.Controller_NullStream, MethodHelper.GetCurrentMethodName()));

                return File(resultTask.Stream, "text/csv");
            }
            catch (CommandException ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, MethodHelper.GetCurrentMethodName());
                return Problem(string.Format(CoreMessages.Controller_CommandFailure, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, MethodHelper.GetCurrentMethodName());
                return Problem(string.Format(CoreMessages.Controller_Failure, MethodHelper.GetCurrentMethodName()));
            }
        }

        /// <summary>
        /// POST StartDriveTimesAsync - Accepts GeoCoded csv file and site. Asynchronously generate .csv file containing drive_time in minutes and distable in meters.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [Consumes("multipart/form-data")]
        [SwaggerResponse(StatusCodes.Status202Accepted, "GUID for accepted request", typeof(DeGaussAsyncResponse), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StartGetDriveTimesAsync([FromForm] DeGaussDrivetimeRequest request)
        {
            try
            {
                var result = _drivetimeRequestValidator.Validate(request, options => options.IncludeRuleSets("Base", "Geocoded", "Drivetime", "MaxFileSize", "ValidateFileName", "ValidateFileNameDriveTime"));
                if (!result.IsValid)
                    return ValidationProblem(ValidationHelper.MapProblemDetails(result));

                var guid = await _drivetimeCommandService.StartGetServiceAsync(new DeGaussCommandTask()
                {
                    File = request.File!,
                    Site = request.Site,
                    Type = type
                }, string.IsNullOrEmpty(request.RequestGuid) ? Guid.NewGuid().ToString() : request.RequestGuid);

                var response = new DeGaussAsyncResponse()
                {
                    Guid = guid
                };

                if (!string.IsNullOrEmpty(guid))
                    response.Status = CommandStatus.Processing;
                else
                    response.Status = CommandStatus.Rejected;

                Response.StatusCode = StatusCodes.Status202Accepted;
                return new JsonResult(response);
            }
            catch (CommandException ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, MethodHelper.GetCurrentMethodName());
                return Problem(string.Format(CoreMessages.Controller_CommandFailure, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, MethodHelper.GetCurrentMethodName());
                return Problem(string.Format(CoreMessages.Controller_Failure, MethodHelper.GetCurrentMethodName()));
            }
        }
        /// <summary>
        /// POST GetDriveTimesStatus - Get the status of a asynchronous drivetime operation.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [Consumes("application/json")]
        [SwaggerResponse(StatusCodes.Status200OK, "Processing status", typeof(DeGaussAsyncResponse), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDriveTimesStatus([FromBody] DeGaussAsyncRequest request)
        {
            try
            {
                var validateResult = _asyncDrivetimeRequest.Validate(request);
                if (!validateResult.IsValid)
                    return ValidationProblem(ValidationHelper.MapProblemDetails(validateResult));

                var resultTask = await _drivetimeCommandService.GetServiceStatusAsync(request.Guid);

                var response = new DeGaussAsyncResponse()
                {
                    Status = resultTask.Status,
                    Guid = request.Guid
                };

                return new JsonResult(response);
            }
            catch (CommandException ex)
            {
                return new JsonResult(new DeGaussAsyncResponse()
                {
                    Status = CommandStatus.Unknown,
                    Guid = request.Guid,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, MethodHelper.GetCurrentMethodName());
                return Problem(string.Format(CoreMessages.Controller_Failure, MethodHelper.GetCurrentMethodName()));
            }
        }
        /// <summary>
        /// POST GetDriveTimesResult - Get the result of an asynchronous drivetime operation.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [Consumes("application/json")]
        [SwaggerResponse(StatusCodes.Status200OK, "CSV File Stream", typeof(FileResult))]
        [SwaggerResponse(StatusCodes.Status204NoContent)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDriveTimesResult([FromBody] DeGaussAsyncRequest request)
        {
            try
            {
                var validateResult = _asyncDrivetimeRequest.Validate(request);
                if (!validateResult.IsValid)
                    return ValidationProblem(ValidationHelper.MapProblemDetails(validateResult));

                var resultTask = await _drivetimeCommandService.GetServiceResultAsync(request.Guid, type);

                if (resultTask.Status.Equals(CommandStatus.Success) && resultTask.Stream != null)
                    return File(resultTask.Stream, "text/csv");

                return NoContent();
            }
            catch (CommandException ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, MethodHelper.GetCurrentMethodName());
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, MethodHelper.GetCurrentMethodName());
                return Problem(string.Format(CoreMessages.Controller_Failure, MethodHelper.GetCurrentMethodName()));
            }
        }

        /// <summary>
        /// POST GetDriveTimesJson - Accepts a json of lat and lon and a site and gets the drive times
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [Consumes("application/json")]
        [SwaggerResponse(StatusCodes.Status200OK, "Address response", typeof(List<DeGaussGeocodedJsonRecord>), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status204NoContent)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDriveTimesJson([FromBody] DeGaussDriveTimesJsonRequest request)
        {
            var methodName = MethodHelper.GetCurrentMethodName();
            try
            {
                var result = _geocodedJsonRequestValdiator.Validate(request);
                if (!result.IsValid)
                    return ValidationProblem(ValidationHelper.MapProblemDetails(result));

                var recordsJson = JsonConvert.SerializeObject(request.Records);
                var resultTask = await _drivetimeCommandService.GetJsonService(recordsJson, request.Site);

                if (resultTask.Status == CommandStatus.Rejected)
                {
                    _logger.LogError(CoreMessages.Controller_CommandRejected, methodName);
                    return Problem(string.Format(CoreMessages.Controller_CommandRejected, methodName));
                }
                else if (resultTask.Status == CommandStatus.Failure)
                {
                    _logger.LogError(CoreMessages.Controller_Failure, methodName);
                    return Problem(string.Format(CoreMessages.Controller_Failure, methodName));
                }

                return new JsonResult(JsonConvert.DeserializeObject<List<DeGaussGeocodedJsonRecord>>(resultTask.Response));
            }
            catch (CommandException ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, methodName);
                return Problem(string.Format(CoreMessages.Controller_CommandFailure, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, methodName);
                return Problem(string.Format(CoreMessages.Controller_Failure, methodName));
            }
        }
    }
}
