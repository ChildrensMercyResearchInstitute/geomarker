using GeoMarker.Frontiers.Core.HealthCheck;
using GeoMarker.Frontiers.Web.Clients;
using GeoMarker.Frontiers.Web.Data;
using GeoMarker.Frontiers.Web.Resources;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace GeoMarker.Frontiers.Web.Models.Services
{
    /// <inheritdoc cref="IGeoMarkerAPIRequestService" />
    public class GeoMarkerAPIRequestService : IGeoMarkerAPIRequestService
    {
        private readonly ILogger<GeoMarkerAPIRequestService> _logger;
        private readonly GeoCodeClient _geoCodeClient;
        private readonly CensusBlockGroupClient _censusBlockGroupClient;
        private readonly DriveTimeClient _driveTimeClient;
        private readonly DeprivationIndexClient _deprivationIndexClient;
        private readonly UserRequestsDbContext _dbContext;
        private readonly IMetadataService _metadataService;
        private readonly IUserRequestRepository _userRequestRepository;
        private readonly IEmailSender _emailSender;
        private readonly IPingService _pingService;
        private readonly WebApplication _webApplication;
        /// <inheritdoc cref="IGeoMarkerAPIRequestService" />
        public GeoMarkerAPIRequestService(ILogger<GeoMarkerAPIRequestService> logger, GeoCodeClient geoCodeClient,
                                          CensusBlockGroupClient censusBlockGroupClient, DriveTimeClient driveTimeClient,
                                          DeprivationIndexClient deprivationIndexClient, UserRequestsDbContext dbContext,
                                          IMetadataService metadataService, IUserRequestRepository userRequestRepository,
                                          IEmailSender emailSender, IPingService pingService, IOptions<WebApplication> webApplication)
        {
            _logger = logger;
            _geoCodeClient = geoCodeClient;
            _censusBlockGroupClient = censusBlockGroupClient;
            _driveTimeClient = driveTimeClient;
            _deprivationIndexClient = deprivationIndexClient;
            _dbContext = dbContext;
            _metadataService = metadataService;
            _userRequestRepository = userRequestRepository;
            _emailSender = emailSender;
            _pingService = pingService;
            _webApplication = webApplication.Value;
        }
        /// <inheritdoc cref="IGeoMarkerAPIRequestService" />
        public async Task<FileResponse?> GetOutputFile(UserRequest request, string accessToken)
        {
            try
            {
                var requestType = request.RequestType;
                if (requestType.Equals(DeGaussRequestType.Composite.ToString()))
                    requestType = request.RequestSubType;
                var type = Enum.Parse<DeGaussRequestType>(requestType);
                var resultRequest = new DeGaussAsyncRequest() { Guid = request.Guid };
                FileResponse? result = null;


                switch (type)
                {
                    case DeGaussRequestType.GeoCode:
                        _geoCodeClient.SetBearerToken(accessToken);
                        result = await _geoCodeClient.GetGeocodesResultAsync(resultRequest);
                        break;
                    case DeGaussRequestType.CensusBlockGroup:
                        _censusBlockGroupClient.SetBearerToken(accessToken);
                        result = await _censusBlockGroupClient.GetCensusBlockGroupsResultAsync(resultRequest);
                        break;
                    case DeGaussRequestType.DriveTime:
                        _driveTimeClient.SetBearerToken(accessToken);
                        result = await _driveTimeClient.GetDriveTimesResultAsync(resultRequest);
                        break;
                    case DeGaussRequestType.DeprivationIndex:
                        _deprivationIndexClient.SetBearerToken(accessToken);
                        result = await _deprivationIndexClient.GetDeprivationIndexesResultAsync(resultRequest);
                        break;

                }
                if (result is null)
                {
                    _logger.LogError($"Error downloading the file: {request.Guid}");
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }
        /// <inheritdoc cref="IGeoMarkerAPIRequestService" />
        public async Task RefreshUserRequests(List<UserRequest> requests, string accessToken, bool sendEmail = false)
        {
            try
            {
                List<UserRequest> requestsToRemove = new();
                foreach (var request in requests)
                {
                    var type = Enum.Parse<DeGaussRequestType>(request.RequestType);
                    var statusRequest = new DeGaussAsyncRequest() { Guid = request.Guid };

                    var resultStatus = await GetResultStatus(type, accessToken, statusRequest);
                    DeGaussAsyncResponse? result = resultStatus.Result;

                    if (result == null)
                        continue;

                    if (result.Status.Equals(CommandStatus.Removed))
                    {
                        _dbContext.Remove(request);
                        _dbContext.SaveChanges();
                        requestsToRemove.Add(request);
                        continue;
                    }

                    if (result.Status != request.Status)
                    {
                        if (result.Status != CommandStatus.Processing && result.Status != CommandStatus.Queued)
                        {
                            request.CompletedDateTime = DateTime.UtcNow;
                            if (sendEmail)
                                await SendCompletedEmail(request, result);

                            if (result.Status == CommandStatus.Success)
                            {
                                request.OutputFileName = $"tmp/{request.Guid}/{resultStatus.ResultPrefix}{request.InputFileName}";
                                _metadataService.CompleteRecordsProcessed(new MetadataServiceCriteria()
                                {
                                    Guid = result.Guid,
                                    EndDate = DateTime.UtcNow
                                });
                            }
                            else if (result.Status == CommandStatus.Failure)
                            {
                                _metadataService.FailRecordsProcessed(new MetadataServiceCriteria()
                                {
                                    Guid = request.Guid,
                                    EndDate = DateTime.UtcNow
                                });
                            }
                        }
                        
                        request.Status = result.Status;
                        _dbContext.Update(request);
                        _dbContext.SaveChanges();
                    }
                }
                foreach (var request in requestsToRemove)
                    requests.Remove(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error encountered attempting to refresh.");
                throw;
            }
        }
        /// <inheritdoc cref="IGeoMarkerAPIRequestService" />
        public async Task RefreshCompositeRequests(List<UserRequest> compositeRequests, string accessToken, bool sendEmail = false)
        {
            try
            {
                List<UserRequest> requestsToRemove = new();

                foreach (var request in compositeRequests)
                {
                    var type = Enum.Parse<DeGaussRequestType>(request.RequestSubType);
                    var statusRequest = new DeGaussAsyncRequest() { Guid = request.Guid };

                    var resultStatus = await GetResultStatus(type, accessToken, statusRequest);
                    DeGaussAsyncResponse? result = resultStatus.Result;

                    if (result == null || request.Status.Equals(CommandStatus.Requested) ||
                        (request.Status != CommandStatus.Processing &&
                         request.Status != CommandStatus.Queued &&
                         request.CompletedDateTime == null))
                        continue;

                    if (result.Status.Equals(CommandStatus.Removed))
                    {
                        requestsToRemove.AddRange(_userRequestRepository.DeleteRequestChain(request.Guid));
                        continue;
                    }

                    if (result.Status != request.Status)
                    {
                        if (result.Status != CommandStatus.Processing && result.Status != CommandStatus.Queued)
                        {
                            request.CompletedDateTime = DateTime.UtcNow;

                            if (result.Status == CommandStatus.Success)
                            {
                                request.OutputFileName = $"tmp/{request.Guid}/{resultStatus.ResultPrefix}{request.InputFileName}";
                                
                                if (string.IsNullOrEmpty(request.NextRequest))
                                {
                                    if (sendEmail)
                                        await SendCompletedEmail(request, result);
                                }
                                else
                                {
                                    var nextRequestGuid = request.NextRequest;
                                    var nextRequest = _userRequestRepository.GetUserRequest(nextRequestGuid);

                                    if (nextRequest != null && nextRequest.Status.Equals(CommandStatus.Requested))
                                    {
                                        var outputFile = await GetOutputFile(request, accessToken);
                                        if (outputFile != null)
                                            await ProcessChainedRequest(request.OutputFileName, request, outputFile.Stream, accessToken);
                                    }
                                }
                                _metadataService.CompleteRecordsProcessed(new MetadataServiceCriteria()
                                {
                                    Guid = result.Guid,
                                    EndDate = DateTime.UtcNow
                                });
                            }
                            else
                            {
                                _userRequestRepository.ApplyStatusToRequestChain(request.Guid, result.Status);
                                _metadataService.FailRecordsProcessed(new MetadataServiceCriteria()
                                {
                                    Guid = request.Guid,
                                    EndDate = DateTime.UtcNow
                                });

                                if (sendEmail)
                                    await SendCompletedEmail(request, result);
                                continue;
                            }
                        }

                        request.Status = result.Status;
                        _dbContext.Update(request);
                        _dbContext.SaveChanges();
                    }
                }
                foreach (var request in requestsToRemove)
                    compositeRequests.Remove(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error encountered attempting to refresh.");
                throw;
            }
        }
        private async Task<DeGaussAsyncResponse?> SubmitRequest(UserRequest request, IFormFile file, string accessToken)
        {
            try
            {
                var requestType = request.RequestType;
                if (requestType.Equals(DeGaussRequestType.Composite.ToString()))
                    requestType = request.RequestSubType;
                var type = Enum.Parse<DeGaussRequestType>(requestType);

                return await InvokeStartGetAsync(type, file, accessToken, string.Empty, request.Year, request.Site, request.Guid, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error encountered attempting to submit request.");

                return null;
            }
        }
        /// <summary>
        /// Process Chained Requests will check for composite request.
        /// It will initiate or invoke Next user requests based on the status. 
        /// It will notify users if the next request failed to start the job. 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="request"></param>
        /// <param name="log"></param>
        /// <param name="blob"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        private async Task ProcessChainedRequest(string fileName, UserRequest request, Stream blob, string accessToken)
        {
            try
            {
                var completedDateTime = DateTimeOffset.Now;
                var completedDateTimeString = UserRequest.DateToString(completedDateTime);

                var nextRequestGuid = request.NextRequest;

                var nextRequest = _dbContext.Requests.FirstOrDefault(r => r.Guid == nextRequestGuid);

                if (nextRequest == null)
                {
                    _logger.LogError($"Next request was null for request: {request.Guid}");
                    return;
                }

                var substLeadingDirectories = fileName.LastIndexOf('/') == -1 ? 0 : fileName.LastIndexOf('/') + 1;
                nextRequest.InputFileName = fileName.Substring(substLeadingDirectories);

                using var stream = new MemoryStream();
                blob.CopyTo(stream);
                stream.Seek(0, SeekOrigin.Begin);

                IFormFile formFile = new FormFile(stream, 0, stream.Length, "blob", fileName.Substring(substLeadingDirectories))
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "text/csv"
                };

                var response = await SubmitRequest(nextRequest, formFile, accessToken);

                _metadataService.AddRecordsProcessed(new MetadataServiceCriteria()
                {
                    Guid = nextRequestGuid,
                    UserId = request.UserId,
                    File = formFile,
                    DeGaussRequestType = Enum.Parse<DeGaussRequestType>(nextRequest.RequestSubType),
                    StartDate = DateTime.UtcNow,
                    Format = MetadataSource.UI
                });

                if (response != null)
                {
                    nextRequest.Status = CommandStatus.Processing;
                    _dbContext.Update(nextRequest);
                    _dbContext.SaveChanges();
                }
                // Failed to start next request, terminate and fail remaining requests. 
                else
                {
                    _userRequestRepository.ApplyStatusToRequestChain(nextRequest.Guid, CommandStatus.Failure);
                    nextRequest.Status = CommandStatus.Failure;
                    _dbContext.Update(nextRequest);
                    _dbContext.SaveChanges();
                    var subject = "Your GeoMarker Composite Job has partially Succeeded";
                    var emailHtmlConent = "Your GeoMarker Composite Job has partially completed but was unable to start the remaining jobs. <br><br>" +
                    $"Start time: {request.GetUploadDateTimeString()}<br>" +
                    $"End time: {completedDateTimeString}<br><br>" +
                    $"Go to the <a href='{_webApplication.ClientUrl}'>GeoMarker Website</a> to download the results.";


                    try
                    {
                        await _emailSender.SendEmailAsync(request.UserId, subject, emailHtmlConent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Unexpected error encountered attempting to send email.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error encountered attempting to ProcessChainedRequest.");
                throw;
            }
        }
        /// <inheritdoc cref="IGeoMarkerAPIRequestService" />
        public async Task<DeGaussAsyncResponse> InvokeStartGetAsync(DeGaussRequestType deGaussRequestType, IFormFile file,
                                                                   string accessToken, string guid,
                                                                   int? year, string? site, string? nextRequestGuid, bool isComposite = false)
        {
            try
            {
                using var stream = new MemoryStream();
                file!.CopyTo(stream);
                stream.Seek(0, SeekOrigin.Begin);
                DeGaussAsyncResponse? result = null;
                switch (deGaussRequestType)
                {
                    case DeGaussRequestType.GeoCode:
                        try
                        {
                            _geoCodeClient.SetBearerToken(accessToken);
                            result = await _geoCodeClient.StartGetGeocodesAsync(new FileParameter(stream, file.FileName, file.ContentType), nextRequestGuid);
                            break;
                        }
                        catch (Exception)
                        {
                            if (isComposite)
                                _userRequestRepository.DeleteRequestChain(guid);
                            throw;
                        }
                    case DeGaussRequestType.CensusBlockGroup:
                        try
                        {
                            _censusBlockGroupClient.SetBearerToken(accessToken);
                            result = await _censusBlockGroupClient.StartGetCensusBlockGroupsAsync(year, new FileParameter(stream, file.FileName, file.ContentType), nextRequestGuid);
                            break;
                        }
                        catch (Exception)
                        {
                            if (isComposite)
                                _userRequestRepository.DeleteRequestChain(guid);
                            throw;
                        }
                    case DeGaussRequestType.DriveTime:
                        try
                        {
                            _driveTimeClient.SetBearerToken(accessToken);
                            result = await _driveTimeClient.StartGetDriveTimesAsync(site, new FileParameter(stream, file.FileName, file.ContentType), nextRequestGuid);
                            break;
                        }
                        catch (Exception)
                        {
                            if (isComposite)
                                _userRequestRepository.DeleteRequestChain(guid);
                            throw;
                        }
                    case DeGaussRequestType.DeprivationIndex:
                        try
                        {
                            _deprivationIndexClient.SetBearerToken(accessToken);
                            result = await _deprivationIndexClient.StartGetDeprivationIndexesAsync(new FileParameter(stream, file.FileName, file.ContentType), nextRequestGuid);
                            break;
                        }
                        catch (Exception)
                        {
                            if (isComposite)
                                _userRequestRepository.DeleteRequestChain(guid);
                            throw;
                        }
                    default:
                        {
                            result = new();
                            result.Message = Messages.HomeController_NotSupported;
                            break;
                        }

                }
                return result;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error encountered attempting to InvokeStartGetAsync method.");
                throw;
            }
        }
        /// <inheritdoc cref="IGeoMarkerAPIRequestService" />
        public async Task<BaseViewModel> HealthCheck(BaseViewModel model)
        {
            model.GeoCodeApiHealthy = await _pingService.CheckServiceAvailablityAsync(_geoCodeClient.BaseUrl + "/health");
            model.DriveTimeApiHealthy = await _pingService.CheckServiceAvailablityAsync(_driveTimeClient.BaseUrl + "/health");
            model.DeprivationIndexApiHealthy = await _pingService.CheckServiceAvailablityAsync(_deprivationIndexClient.BaseUrl + "/health");
            model.CensusBlockApiHealthy = await _pingService.CheckServiceAvailablityAsync(_censusBlockGroupClient.BaseUrl + "/health");
            return model;
        }

        private async Task<DeGaussResultStatus> GetResultStatus(DeGaussRequestType type, string accessToken, DeGaussAsyncRequest statusRequest)
        {
            DeGaussAsyncResponse result = new();
            string resultPrefix = string.Empty;

            try
            {
                switch (type)
                {
                    case DeGaussRequestType.GeoCode:
                        _geoCodeClient.SetBearerToken(accessToken);
                        result = await _geoCodeClient.GetGeocodesStatusAsync(statusRequest);
                        resultPrefix = "geocoded_";
                        break;
                    case DeGaussRequestType.CensusBlockGroup:
                        _censusBlockGroupClient.SetBearerToken(accessToken);
                        result = await _censusBlockGroupClient.GetCensusBlockGroupsStatusAsync(statusRequest);
                        resultPrefix = "census_block_group_";
                        break;
                    case DeGaussRequestType.DriveTime:
                        _driveTimeClient.SetBearerToken(accessToken);
                        result = await _driveTimeClient.GetDriveTimesStatusAsync(statusRequest);
                        resultPrefix = "drivetime_";
                        break;
                    case DeGaussRequestType.DeprivationIndex:
                        _deprivationIndexClient.SetBearerToken(accessToken);
                        result = await _deprivationIndexClient.GetDeprivationIndexesStatusAsync(statusRequest);
                        resultPrefix = "dep_index_";
                        break;
                }
            }
            catch (ApiException ex)
            {
                result.Guid = statusRequest.Guid;
                result.Status = CommandStatus.Unknown;
            }
            catch (HttpRequestException ex)
            {
                result.Guid = statusRequest.Guid;
                result.Status = CommandStatus.Unknown;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while getting result status for {statusRequest.Guid}");
                throw;
            }
            return new DeGaussResultStatus() { Result = result, ResultPrefix = resultPrefix };
        }

        private async Task SendCompletedEmail(UserRequest request, DeGaussAsyncResponse result)
        {
            var subject = $"Your GeoMarker {request.RequestType} Request has ";
            var body = "";
            switch (result.Status)
            {
                case CommandStatus.Success:
                    subject = subject + "Succeeded";
                    body = subject + ".<br><br>" +
                            $"Go to the <a href='{_webApplication.ClientUrl}'>GeoMarker Website</a> to download the results.";
                    break;
                default:
                    subject = subject + "Failed";
                    body = subject + $". Go to the <a href='{_webApplication.ClientUrl}'>GeoMarker Website</a> to try again. Contact support if this issue persists.";
                    break;
            }
            try
            {
                await _emailSender.SendEmailAsync(request.UserId, subject, body);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error sending an email");
                throw;
            }
        }
    }
}
