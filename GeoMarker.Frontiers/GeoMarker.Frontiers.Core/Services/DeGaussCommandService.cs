using GeoMarker.Frontiers.Core.Models.Commands;
using GeoMarker.Frontiers.Core.Models.Configuration;
using GeoMarker.Frontiers.Core.Resources;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace GeoMarker.Frontiers.Core.Services
{
    public class DeGaussCommandService : ScriptCommandService, IDeGaussCommandService
    {
        private readonly ILogger<DeGaussCommandService> _logger;

        private readonly Matcher _outputMatcher;

        private readonly int _gracePeriod;

        public DeGaussCommandService(ILogger<DeGaussCommandService> logger, Matcher matcher,
                                     IOptions<CommandServiceConfiguration> commandServiceConfig) : base(logger)
        {
            _logger = logger;
            _outputMatcher = matcher;
            _gracePeriod = commandServiceConfig.Value.GracePeriod;
        }

        public async Task<CommandTaskResponse> GetService(DeGaussCommandTask task, string commandGuid)
        {

            task.CommandGuid = commandGuid;

            var requestIndexedFilePath = await WriteIndexedFileFromInput(task);
            task.Command = $"entrypoint.R {requestIndexedFilePath} {task.Site}{task.Year}";
            var result = await ExecuteCommand(task);

            if (result.Process != null)
            {
                await result.Process.WaitForExitAsync();
                var finishedTask = await GetCommandTask(result.CommandGuid);

                if (finishedTask.Status == CommandStatus.Success)
                {
                    var paths = _outputMatcher.AddInclude(task.Matcher).GetResultsInFullPath($"tmp/{task.CommandGuid}/");

                    if (!paths.Any())
                    {
                        _logger.LogError(CoreMessages.CommandService_FileNotFound);
                        return new CommandTaskResponse()
                        {
                            Status = CommandStatus.Failure
                        };
                    }
                    else
                    {
                        string outFile = paths.First();
                        var stream = new MemoryStream(File.ReadAllBytes(outFile));
                        Directory.Delete($"tmp/{task.CommandGuid}", true);

                        return new CommandTaskResponse()
                        {
                            Status = CommandStatus.Success,
                            Stream = stream
                        };
                    }
                }
                else if (finishedTask.Status == CommandStatus.Failure)
                {
                    _logger.LogError(CoreMessages.CommandService_Failure, $"Get{task.Type}");
                    return new CommandTaskResponse()
                    {
                        Status = CommandStatus.Failure
                    };
                }
            }

            throw new CommandException(string.Format(CoreMessages.CommandService_Rejected, $"Get{task.Type}"));
        }

        public async Task<string> StartGetServiceAsync(DeGaussCommandTask task, string commandGuid)
        {

            task.CommandGuid = commandGuid;

            var requestIndexedFilePath = await WriteIndexedFileFromInput(task);
            task.Command = $"entrypoint.R {requestIndexedFilePath} {task.Site}{task.Year}";
            var result = await ExecuteCommand(task);

            if (result.Process != null)
            {
                return commandGuid;
            }

            throw new CommandException(string.Format(CoreMessages.CommandService_Rejected, $"StartGet{task.Type}Async"));
        }

        public async Task<CommandTaskResponse> GetServiceStatusAsync(string guid)
        {
            try
            {
                var requestLogFilePath = $"tmp/{guid}/request_log.txt";

                var status = GetStatus(requestLogFilePath, guid);

                return new CommandTaskResponse()
                {
                    Status = status
                };
            }
            catch
            {
                return await HandleMissingCommandTask(guid, false);
            }
        }

        public async Task<CommandTaskResponse> GetServiceResultAsync(string guid, string type)
        {
            try
            {
                var requestLogFilePath = $"tmp/{guid}/request_log.txt";

                var status = GetStatus(requestLogFilePath, guid);

                if (status == CommandStatus.Success)
                {
                    var paths = _outputMatcher.GetResultsInFullPath($"tmp/{guid}");

                    string outFile = paths.OrderBy(p => p.Length).First();
                    var stream = new MemoryStream(File.ReadAllBytes(outFile));

                    return new CommandTaskResponse()
                    {
                        Status = CommandStatus.Success,
                        Stream = stream
                    };
                }
                else
                {
                    return new CommandTaskResponse()
                    {
                        Status = status
                    };
                }

            }
            catch
            {
                return await HandleMissingCommandTask(guid, true);
            }
        }

        public async Task<JsonAddressResponse> GetJsonAddressService(JsonAddressCommandTask task)
        {
            var commandGuid = Guid.NewGuid().ToString();
            task.CommandGuid = commandGuid;
            string addresses = string.Empty;
            foreach (var address in task.Addresses)
                addresses += $"{address.Id}|{address.Address};";

            task.Command = $"geocode_json.rb \"{addresses}\"";
            var result = await ExecuteCommand(task, true, true);

            if (result.Process != null)
            {
                await result.Process.WaitForExitAsync();
                var finishedTask = await GetCommandTask(result.CommandGuid);

                if (finishedTask.Status == CommandStatus.Success)
                {
                    return new JsonAddressResponse()
                    {
                        Status = CommandStatus.Success,
                        GeocodedAddress = Regex.Replace(finishedTask.StandardOut.Replace("\n", ""), ":(-?[0-9]+.?[0-9]+)", ":\"$1\"")
                    };
                }
                else if (finishedTask.Status == CommandStatus.Failure)
                {
                    _logger.LogError(CoreMessages.CommandService_Failure, $"GetJsonAddress");
                    return new JsonAddressResponse()
                    {
                        Status = CommandStatus.Failure
                    };
                }
            }

            return new JsonAddressResponse()
            {
                Status = CommandStatus.Rejected
            };
        }

        public async Task<GeocodedJsonResponse> GetJsonService(string recordsJson, string? site = null, int? year = null)
        {
            if (string.IsNullOrWhiteSpace(recordsJson))
                throw new ArgumentNullException(nameof(recordsJson));

            CommandTask task = new() { CommandGuid = Guid.NewGuid().ToString() };

            var escapedJson = recordsJson.Replace("'", "");
            var unquotedJson = escapedJson.Replace("\"", "");

            task.Command = $"entrypoint_json.R \"{unquotedJson}\" {site}{year}";
            var result = await ExecuteCommand(task, true);

            if (result.Process != null)
            {
                await result.Process.WaitForExitAsync();
                var finishedTask = await GetCommandTask(result.CommandGuid);

                if (finishedTask.Status == CommandStatus.Success)
                {
                    return new GeocodedJsonResponse()
                    {
                        Status = CommandStatus.Success,
                        Response = finishedTask.StandardOut.Replace(" \n", "")
                    };
                }
                else if (finishedTask.Status == CommandStatus.Failure)
                {
                    _logger.LogError(CoreMessages.CommandService_Failure, $"GetJsonGeocodedService");
                    return new GeocodedJsonResponse()
                    {
                        Status = CommandStatus.Failure
                    };
                }
            }

            return new GeocodedJsonResponse()
            {
                Status = CommandStatus.Rejected
            };
        }

        private async Task<string> WriteIndexedFileFromInput(DeGaussCommandTask task)
        {
            var requestIndexedFilePath = $"tmp/{task.CommandGuid}/{task.File.FileName}";
            Directory.CreateDirectory($"tmp/{task.CommandGuid}");
            using var fileStream = new FileStream(requestIndexedFilePath, FileMode.Create);
            await task.File.CopyToAsync(fileStream);

            return requestIndexedFilePath;
        }

        private async Task<CommandTaskResponse> HandleMissingCommandTask(string guid, bool returnFile)
        {
            var paths = _outputMatcher.GetResultsInFullPath($"tmp/{guid}");

            if (!paths.Any())
            {
                if (!Directory.Exists($"tmp/{guid}"))
                {
                    return new CommandTaskResponse()
                    {
                        Status = CommandStatus.Removed
                    };
                }

                return new CommandTaskResponse()
                {
                    Status = CommandStatus.Failure
                };
            }
            else
            {
                if (returnFile)
                {
                    string outFile = paths.OrderBy(p => p.Length).First();
                    var stream = new MemoryStream(File.ReadAllBytes(outFile));

                    return new CommandTaskResponse()
                    {
                        Status = CommandStatus.Success,
                        Stream = stream
                    };
                }

                return new CommandTaskResponse()
                {
                    Status = CommandStatus.Success
                };
            }
        }

        /// <summary>
        /// In this method, we are checking for process status from request log. 
        /// Request log contain live logs from Degauss R script execution. 
        /// It is important to note the order in which we check for status 
        /// 1) Check if the output directory exists. 
        /// 2) Check if the output file exists. 
        /// 3) Check if the request log has recent updates.  
        /// 4) Check if the request log file exists. Initial api call and immediate 'status check' results in file not found status. 
        ///    Milliseconds difference between call to RScript and Status check. 
        /// 5) If above conditions are not succeeded then it should be a failue. 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        private CommandStatus GetStatus(string filePath, string guid)
        {
            var requestLogLastWriteTime = File.GetLastWriteTimeUtc(filePath);
            var requestLogUpdatedInLastNMinutes = DateTime.UtcNow >= requestLogLastWriteTime && requestLogLastWriteTime >= DateTime.UtcNow.AddMinutes(-_gracePeriod);

            var directory = $"tmp/{guid}";
            var paths = _outputMatcher.GetResultsInFullPath(directory);

            if (!Directory.Exists(directory))
            {
                return CommandStatus.Removed;
            }
            else if (paths.Any())
            {
                return CommandStatus.Success;
            }
            else if (requestLogUpdatedInLastNMinutes)
            {
                return CommandStatus.Processing;
            }
            else if (!File.Exists(filePath))
            {
                return CommandStatus.Queued;
            }
            else
            {
                _logger.LogError(CoreMessages.CommandService_FileNotFound);
                return CommandStatus.Failure;
            }
        }
    }
}
