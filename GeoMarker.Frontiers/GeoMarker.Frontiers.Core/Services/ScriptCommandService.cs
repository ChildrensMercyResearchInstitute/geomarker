using GeoMarker.Frontiers.Core.Models.Commands;
using GeoMarker.Frontiers.Core.Resources;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace GeoMarker.Frontiers.Core.Services
{
    public abstract class ScriptCommandService
    {
        private static List<CommandStatus> COMPLETED_STATUS = new List<CommandStatus>() { CommandStatus.Success, CommandStatus.Failure, CommandStatus.Removed };

        protected string WorkingDirectoryBase { get; } = "/app";

        private readonly ILogger<ScriptCommandService> _logger;

        private List<CommandTask> _tasks;

        public ScriptCommandService(ILogger<ScriptCommandService> logger)
        {
            _logger = logger;
            _tasks = new List<CommandTask>();
        }

        /// <summary>
        /// Execute a script on the host device and track the process internally. 
        /// </summary>
        /// <param name="request">A CommandTask to be executed.</param>
        /// <param name="json">Whether the command is a single address command or not</param>
        /// <returns>A CommandTask with the starting status of the process.</returns>
        public async Task<CommandTask> ExecuteCommand(CommandTask request, bool json = false, bool ruby = false)
        {
            if (json)
                _logger.LogInformation(CoreMessages.CommandService_TaskInfo, request.CommandGuid, "[PII]");
            else
                _logger.LogInformation(CoreMessages.CommandService_TaskInfo, request.CommandGuid, request.Command);

            var info = new ProcessStartInfo
            {
                FileName = ruby ? "/usr/bin/ruby" : "/usr/local/bin/Rscript",
                WorkingDirectory = WorkingDirectoryBase,
                Arguments = request.Command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            var proc = new Process { StartInfo = info, EnableRaisingEvents = true };
            proc.Exited += (sender, e) => HandleExited(sender, e, request.CommandGuid);
            bool success = false;

            if (json)
                success = proc.Start();
            else
            {
                var requestLogFilePath = $"tmp/{request.CommandGuid}/request_log.txt";
                proc.ErrorDataReceived += (sender, e) => File.AppendAllText(requestLogFilePath, e.Data + Environment.NewLine);
                proc.OutputDataReceived += (sender, e) => File.AppendAllText(requestLogFilePath, e.Data + Environment.NewLine);

                success = proc.Start();
                proc.BeginErrorReadLine();
                proc.BeginOutputReadLine();
            }

            if (success)
            {
                var result = new CommandTask()
                {
                    Command = request.Command,
                    CommandGuid = request.CommandGuid,
                    Process = proc,
                    Status = CommandStatus.Processing
                };

                _tasks.Add(result);

                if (json)
                {
                    result.StandardOut = await proc.StandardOutput.ReadToEndAsync();
                    result.StandardErr = await proc.StandardError.ReadToEndAsync();
                }

                return result;
            }

            _logger.LogError(CoreMessages.CommandSerivce_Process_StartFailure, request.CommandGuid);
            return new CommandTask()
            {
                Command = request.Command,
                CommandGuid = request.CommandGuid,
                Status = CommandStatus.Rejected
            };
        }

        /// <summary>
        /// Get an active CommandTask from the services internal task cache. 
        /// </summary>
        /// <param name="commandGuid">GUID for the CommandTask.</param>
        /// <param name="purgeCompleted">Boolean value for if the operation should purge the task from the cache once its been retrieved.</param>
        /// <returns></returns>
        /// <exception cref="CommandException">If no command task exists for the provided GUID.</exception>
        public async Task<CommandTask> GetCommandTask(string commandGuid, bool purgeCompleted = true)
        {
            var result = _tasks.Where(x => x.CommandGuid.Equals(commandGuid)).FirstOrDefault();

            if (result == null)
                throw new CommandException(string.Format(CoreMessages.CommandService_Process_NoTask, commandGuid));

            if (COMPLETED_STATUS.Contains(result.Status) && purgeCompleted)
            {
                _tasks.Remove(result);
                return result;
            }

            return result;
        }

        private void HandleExited(object? sender, EventArgs e, string commandGuid)
        {
            var task = _tasks.Where(x => x.CommandGuid.Equals(commandGuid)).FirstOrDefault();

            if (task != null)
                task.Status = CommandStatus.Success;
            else
                _logger.LogError(CoreMessages.CommandService_Process_NoTask, commandGuid);
        }
    }
}
