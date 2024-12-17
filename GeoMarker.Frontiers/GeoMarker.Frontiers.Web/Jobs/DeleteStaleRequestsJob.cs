using GeoMarker.Frontiers.Core.Resources;
using GeoMarker.Frontiers.Web.Data;
using GeoMarker.Frontiers.Web.Models;
using Quartz;

namespace GeoMarker.Frontiers.Web.Jobs
{
    public class DeleteStaleRequestsJob : IJob
    {
        private ILogger<DeleteStaleRequestsJob> _logger;
        private readonly UserRequestsDbContext _dbContext;

        public DeleteStaleRequestsJob(ILogger<DeleteStaleRequestsJob> logger,
            UserRequestsDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (!context.JobDetail.JobDataMap.ContainsKey("KeepAliveDays"))
            {
                _logger.LogError(string.Format(CoreMessages.FileCleanupJob_ConfigurationError, "KeepAliveDays not present in JobDataMap."));
                return;
            }

            var keepAliveDaysStr = context.JobDetail.JobDataMap["KeepAliveDays"] as string;
            int keepAliveDays;
            var result = int.TryParse(keepAliveDaysStr, out keepAliveDays);

            if (!result || keepAliveDays < 0)
            {
                _logger.LogError(string.Format(CoreMessages.FileCleanupJob_ConfigurationError, "KeepAliveDays must be a numeric value greater than or equal to 0."));
                return;
            }

            _logger.LogInformation(string.Format(CoreMessages.FileCleanupJob_Starting, $"KeepAliveDays {context.JobDetail.JobDataMap["KeepAliveDays"]}"));

            var staleRequests = _dbContext.Requests.Where(r =>
                r.RequestType == DeGaussRequestType.SingleAddress.ToString() &&
                r.UploadDateTime.AddDays(keepAliveDays) < DateTime.UtcNow).ToList();

            foreach (var request in staleRequests)
            {
                _dbContext.Requests.Remove(request);
                _dbContext.SaveChanges();
            }
            _logger.LogInformation($"Deleted {staleRequests.Count} stale requests");
        }
    }
}
