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

namespace GeoMarker.Frontiers.DeprivationIndex.Controllers
{

    /// <summary>
    /// API Controller to get different deprivation indexes based on the given GeoCoded addresses.
    /// </summary>
    [ApiController]
    [Authorize]
    public class DeprivationIndexController : ControllerBase
    {
        private readonly ILogger<DeprivationIndexController> _logger;
        private readonly IDeGaussCommandService _deGaussCommandService;
        private readonly IValidator<DeGaussRequest> _requestValidator;
        private readonly IValidator<DeGaussGeocodedJsonRequest> _geocodedJsonRequestValdiator;
        private readonly IValidator<DeGaussAsyncRequest> _asyncRequestValidator;
        private readonly string type = "Dep_Index";

        /// <summary>
        /// Construct the API controller.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="deGaussCommandService"></param>
        /// <param name="requestValidator"></param>
        /// <param name="geocodedJsonRequestValdiator"></param>
        /// <param name="asyncRequestValidator"></param>
        public DeprivationIndexController(ILogger<DeprivationIndexController> logger,
                                    IDeGaussCommandService deGaussCommandService,
                                    IValidator<DeGaussRequest> requestValidator,
                                    IValidator<DeGaussGeocodedJsonRequest> geocodedJsonRequestValdiator,
                                    IValidator<DeGaussAsyncRequest> asyncRequestValidator)
        {
            _logger = logger;
            _deGaussCommandService = deGaussCommandService;
            _requestValidator = requestValidator;
            _geocodedJsonRequestValdiator = geocodedJsonRequestValdiator;
            _asyncRequestValidator = asyncRequestValidator;
        }

        /// <summary>
        /// POST GetDeprivationIndex - Accepts GeoCoded .csv file to generate dep_index .csv file.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [Consumes("multipart/form-data")]
        [SwaggerResponse(StatusCodes.Status200OK, "CSV File Stream", typeof(FileResult))]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDeprivationIndex([FromForm] DeGaussRequest request)
        {
            var methodName = MethodHelper.GetCurrentMethodName();
            try
            {
                var result = _requestValidator.Validate(request, options => options.IncludeRuleSets("Base", "Geocoded", "BelowMaxRows", "MaxFileSize", "ValidateFileName", "ValidateFileNameDeprivationIndex"));
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
        /// POST StartGetDeprivationIndexesAsync -  Accepts GeoCoded csv file and site. Asynchronously generate .csv file containing deprivation indexes.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [Consumes("multipart/form-data")]
        [SwaggerResponse(StatusCodes.Status202Accepted, "GUID for accepted request", typeof(DeGaussAsyncResponse), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StartGetDeprivationIndexesAsync([FromForm] DeGaussRequest request)
        {
            var methodName = MethodHelper.GetCurrentMethodName();
            try
            {
                var result = _requestValidator.Validate(request, options => options.IncludeRuleSets("Base", "Geocoded", "MaxFileSize", "ValidateFileName", "ValidateFileNameDeprivationIndex"));
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
        /// POST GetDeprivationIndexesStatus -  Get the status of a asynchronous deprivation index operation.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/GeoMarker/[action]")]
        [Consumes("application/json")]
        [SwaggerResponse(StatusCodes.Status200OK, "Processing status", typeof(DeGaussAsyncResponse), MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status400BadRequest)]
        [SwaggerResponse(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDeprivationIndexesStatus([FromBody] DeGaussAsyncRequest request)
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
        /// POST GetDeprivationIndexesResult - Get the result of an asynchronous deprivation index operation.
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
        public async Task<IActionResult> GetDeprivationIndexesResult([FromBody] DeGaussAsyncRequest request)
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
        /// POST GetDeprivationIndexesJson - Accepts a json of lat and lon and gets the deprivation indexes
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
        public async Task<IActionResult> GetDeprivationIndexesJson([FromBody] DeGaussGeocodedJsonRequest request)
        {
            var methodName = MethodHelper.GetCurrentMethodName();
            try
            {
                var result = _geocodedJsonRequestValdiator.Validate(request);
                if (!result.IsValid)
                    return ValidationProblem(ValidationHelper.MapProblemDetails(result));

                var recordsJson = JsonConvert.SerializeObject(request.Records);
                var resultTask = await _deGaussCommandService.GetJsonService(recordsJson);

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
