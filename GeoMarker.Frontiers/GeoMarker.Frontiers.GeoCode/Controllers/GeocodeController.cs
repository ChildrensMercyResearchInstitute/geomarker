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

namespace GeoMarker.Frontiers.GeoCode.Controllers
{
    /// <summary>
    /// API Controller for the GeoCoding of address data.
    /// </summary>
    [ApiController]
    [Authorize]
    public class GeoCodeApiController : ControllerBase
    {
        private readonly ILogger<GeoCodeApiController> _logger;
        private readonly IDeGaussCommandService _deGaussCommandService;
        private readonly IValidator<DeGaussRequest> _requestValidator;
        private readonly IValidator<DeGaussJsonRequest> _jsonRequestValdiator;
        private readonly IValidator<DeGaussAsyncRequest> _asyncRequestValidator;
        private readonly string type = "GeoCodes";

        /// <summary>
        /// Construct the API controller.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="deGaussCommandService"></param>
        /// <param name="requestValidator"></param>
        /// <param name="jsonRequestValdiator"></param>
        /// <param name="asyncRequestValidator"></param>
        public GeoCodeApiController(ILogger<GeoCodeApiController> logger,
                                    IDeGaussCommandService deGaussCommandService,
                                    IValidator<DeGaussRequest> requestValidator,
                                    IValidator<DeGaussJsonRequest> jsonRequestValdiator,
                                    IValidator<DeGaussAsyncRequest> asyncRequestValidator)
        {
            _logger = logger;
            _deGaussCommandService = deGaussCommandService;
            _requestValidator = requestValidator;
            _jsonRequestValdiator = jsonRequestValdiator;
            _asyncRequestValidator = asyncRequestValidator;
        }

        /// <summary>
        /// POST GetGeocodes - Accepts and GeoCodes a .csv file containing address records.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [Consumes("multipart/form-data")]
        [SwaggerResponse(StatusCodes.Status200OK, "CSV File Stream", typeof(FileResult))]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetGeocodes([FromForm] DeGaussRequest request)
        {
            var methodName = MethodHelper.GetCurrentMethodName();
            try
            {
                var result = _requestValidator.Validate(request, options => options.IncludeRuleSets("Base", "GeoCode", "BelowMaxRows", "MaxFileSize", "ValidateFileName", "ValidateFileNameGeocode"));
                if (!result.IsValid)
                    return ValidationProblem(ValidationHelper.MapProblemDetails(result));

                var resultTask = await _deGaussCommandService.GetService(new DeGaussCommandTask()
                {
                    File = request.File!,
                    Type = type
                }, string.IsNullOrEmpty(request.RequestGuid) ? Guid.NewGuid().ToString() : request.RequestGuid);

                if (resultTask.Stream == null)
                    return Problem(string.Format(CoreMessages.Controller_NullStream, methodName));

                return File(resultTask.Stream, "text/csv");
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

        /// <summary>
        /// POST StartGetGeocodesAsync - Accepts and geocodes a .csv file containing address records asynchronously.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [Consumes("multipart/form-data")]
        [SwaggerResponse(StatusCodes.Status202Accepted, "GUID for accepted request", typeof(DeGaussAsyncResponse), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StartGetGeocodesAsync([FromForm] DeGaussRequest request)
        {
            var methodName = MethodHelper.GetCurrentMethodName();
            try
            {
                var result = _requestValidator.Validate(request, options => options.IncludeRuleSets("Base", "GeoCode", "MaxFileSize", "ValidateFileName", "ValidateFileNameGeocode"));
                if (!result.IsValid)
                    return ValidationProblem(ValidationHelper.MapProblemDetails(result));

                var guid = await _deGaussCommandService.StartGetServiceAsync(new DeGaussCommandTask()
                {
                    File = request.File!,
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
                _logger.LogError(ex, CoreMessages.Controller_Failure, methodName);
                return Problem(string.Format(CoreMessages.Controller_CommandFailure, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, methodName);
                return Problem(string.Format(CoreMessages.Controller_Failure, methodName));
            }
        }

        /// <summary>
        /// POST GetGeocodesStatus - Get the status of a asynchronous geocoding operation.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [Consumes("application/json")]
        [SwaggerResponse(StatusCodes.Status200OK, "Processing status", typeof(DeGaussAsyncResponse), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetGeocodesStatus([FromBody] DeGaussAsyncRequest request)
        {
            try
            {
                var validateResult = _asyncRequestValidator.Validate(request);
                if (!validateResult.IsValid)
                    return ValidationProblem(ValidationHelper.MapProblemDetails(validateResult));

                var resultTask = await _deGaussCommandService.GetServiceStatusAsync(request.Guid);

                var response = new DeGaussAsyncResponse()
                {
                    Status = resultTask.Status,
                    Guid = request.Guid
                };

                Response.StatusCode = StatusCodes.Status200OK;
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
                var methodName = MethodHelper.GetCurrentMethodName();
                _logger.LogError(ex, CoreMessages.Controller_Failure, methodName);
                return Problem(string.Format(CoreMessages.Controller_Failure, methodName));
            }
        }

        /// <summary>
        /// POST GetGeocodesResult - Get the result of an asynchronous geocoding operation.
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
        public async Task<IActionResult> GetGeocodesResult([FromBody] DeGaussAsyncRequest request)
        {
            var methodName = MethodHelper.GetCurrentMethodName();
            try
            {
                var validateResult = _asyncRequestValidator.Validate(request);
                if (!validateResult.IsValid)
                    return ValidationProblem(ValidationHelper.MapProblemDetails(validateResult));

                var resultTask = await _deGaussCommandService.GetServiceResultAsync(request.Guid, type);

                if (resultTask.Status.Equals(CommandStatus.Success) && resultTask.Stream != null)
                    return File(resultTask.Stream, "text/csv");

                return NoContent();
            }
            catch (CommandException ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, methodName);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CoreMessages.Controller_Failure, methodName);
                return Problem(string.Format(CoreMessages.Controller_Failure, methodName));
            }
        }

        /// <summary>
        /// POST GetGeocodesJson - Accepts and geocodes a json string of addresses. 
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
        public async Task<IActionResult> GetGeocodesJson([FromBody] DeGaussJsonRequest request)
        {
            var methodName = MethodHelper.GetCurrentMethodName();
            try
            {
                var result = _jsonRequestValdiator.Validate(request);
                if (!result.IsValid)
                    return ValidationProblem(ValidationHelper.MapProblemDetails(result));

                var resultTask = await _deGaussCommandService.GetJsonAddressService(new JsonAddressCommandTask()
                {
                    Addresses = request.Addresses
                });

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
                resultTask.GeocodedAddress = "[" + resultTask.GeocodedAddress.Replace("null", "").Replace("}{", "},{") + "]";

                return new JsonResult(JsonConvert.DeserializeObject<List<DeGaussGeocodedJsonRecord>>(resultTask.GeocodedAddress));
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
