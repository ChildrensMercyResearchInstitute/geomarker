using GeoMarker.Frontiers.Web.Clients;
using GeoMarker.Frontiers.Web.Models.Services;
using System.ComponentModel.DataAnnotations;

namespace GeoMarker.Frontiers.Web.Models
{
    public class RecordsProcessed
    {
        [Key]
        public string RequestGuid { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public MetadataSource Format { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTimeOffset UploadDateTime { get; set; }
        public DateTimeOffset CompletedDateTime { get; set; }
        public int Records { get; set; }
        public CommandStatus Status { get; set; }

        public string? GetUploadDateTimeString()
        {
            return UserRequest.DateToString(UploadDateTime);
        }

        public TimeSpan GetProcessTime()
        {
            TimeSpan timeSpan = CompletedDateTime.Subtract(UploadDateTime);
            if (timeSpan.Ticks <= 0 || Status != CommandStatus.Success)
                return TimeSpan.Zero;
            return timeSpan;
        }

        public static string TimeSpanToString(TimeSpan timeSpan, RecordsProcessed? record = null, bool showProcessing = false)
        {
            if (record != null && record.Status == CommandStatus.Failure)
                return "Failed";
            if (showProcessing && timeSpan.Ticks <= 0)
                return "Processing";
            return string.Format("{0} hrs {1} mins {2} secs", (int)timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds);
        }
    }
}
