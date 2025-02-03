
using GeoMarker.Frontiers.Core.Resources;
using Microsoft.Extensions.Logging;
using Quartz;

namespace GeoMarker.Frontiers.Core.Jobs
{
    public class FileCleanupJob : IJob
    {
        private ILogger<FileCleanupJob> _logger;

        public FileCleanupJob(ILogger<FileCleanupJob> logger)
        {
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if( !context.JobDetail.JobDataMap.ContainsKey("KeepAliveDays"))
            {
                _logger.LogError(string.Format(CoreMessages.FileCleanupJob_ConfigurationError, "KeepAliveDays not present in JobDataMap."));
                return;
            }

            var keepAliveDaysStr = context.JobDetail.JobDataMap["KeepAliveDays"] as string;
            int keepAliveDays;
            var result = int.TryParse(keepAliveDaysStr, out keepAliveDays);

            if ( !result || keepAliveDays < 0 )
            {
                _logger.LogError(string.Format(CoreMessages.FileCleanupJob_ConfigurationError, "KeepAliveDays must be a numeric value greater than or equal to 0."));
                return;
            }

            _logger.LogInformation(string.Format(CoreMessages.FileCleanupJob_Starting, $"KeepAliveDays {context.JobDetail.JobDataMap["KeepAliveDays"]}"));

            if( !Directory.Exists(Directory.GetCurrentDirectory() + "/tmp") )
            {
                _logger.LogInformation(CoreMessages.FileCleanupJob_NoWorkingDirectory);
                return;
            }

            var requestDirectories = Directory.GetDirectories(Directory.GetCurrentDirectory() + "/tmp", "*", SearchOption.TopDirectoryOnly);
            _logger.LogInformation(string.Format(CoreMessages.FileCleanupJob_RequestDirectoriesFound, requestDirectories.Length));
            foreach( var directory in requestDirectories )
            {
                var writeTime = Directory.GetLastWriteTimeUtc(directory);

                if( writeTime.AddDays((double)keepAliveDays) > DateTime.UtcNow )
                {
                    _logger.LogInformation(string.Format(CoreMessages.FileCleanupJob_RequestUnderCutoff, directory, keepAliveDays));
                }
                else
                {
                    _logger.LogInformation(string.Format(CoreMessages.FileCleanupJob_RequestOverCutoff, directory, keepAliveDays));
                    Directory.Delete(directory, true);
                }
            }
        }
    }
}
