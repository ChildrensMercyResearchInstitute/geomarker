using GeoMarker.Frontiers.Web.Clients;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace GeoMarker.Frontiers.Web.Models
{
    public class UserRequest
    {
        [Key]
        public string Guid { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string InputFileName { get; set; } = string.Empty;
        public string OutputFileName { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string RequestSubType { get; set; } = string.Empty;
        public string? Site { get; set; }
        public int? Year { get; set; }
        public DateTimeOffset UploadDateTime { get; set; }
        public DateTimeOffset? CompletedDateTime { get; set; }
        public CommandStatus Status { get; set; }
        public string? Address { get; set; }
        public string? GeocodedAddress { get; set; }
        public string NextRequest { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; } = default!;

        public string? GetUploadDateTimeString()
        {
            return DateToString(UploadDateTime);
        }

        public static string? DateToString(DateTimeOffset? date)
        {
            return date?.ToLocalTime().ToString("M/d/yyyy h:mm:ss tt");
        }

        public static string PascalCaseToHumanCase(string pascalCase)
        {
            if (pascalCase == "GeoCode") return pascalCase;
            Regex r = new Regex("(?<=[a-z])(?<x>[A-Z])|(?<=.)(?<x>[A-Z])(?=[a-z])");
            return r.Replace(pascalCase, " ${x}");
        }
    }
}
