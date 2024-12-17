using GeoMarker.Frontiers.Web.Clients;
using GeoMarker.Frontiers.Web.Models.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GeoMarker.Frontiers.Web.Models
{
    public class AdminViewModel
    {
        public List<RecordsProcessed> AllRecords { get; set; } = new List<RecordsProcessed>();
        [BindProperty]
        public DateTimeOffset? StartDate { get; set; }
        [BindProperty]
        public DateTimeOffset? EndDate { get; set; }
        [BindProperty]
        public List<string> UserIds { get; set; } = new();
        [BindProperty]
        public List<string> RequestTypes { get; set; } = new();
        [BindProperty]
        public MetadataSource? RequestFormat { get; set; }

        public List<string> UserIdOptions { get; set; } = new List<string>();
        public List<string> RequestTypeOptions { get; } = Enum.GetValues(typeof(DeGaussRequestType)).Cast<DeGaussRequestType>().Where(t => t.ToString() != "Composite" && t.ToString() != "Unknown").Select(t => t.ToString()).ToList();
        public SelectList RequestFormatOptions { get; } = new SelectList(new Dictionary<string, string>() { { "API", "API" }, { "UI", "UI" } }, "Key", "Value");


        public int ProcessedRecords()
        {
            return AllRecords.Where(r => r.Status == CommandStatus.Success).Sum(r => r.Records);
        }

        public int ProcessingRecords()
        {
            return AllRecords.Where(r => r.Status == CommandStatus.Processing).Sum(r => r.Records);
        }

        public int FailedRecords()
        {
            return AllRecords.Where(r => r.Status == CommandStatus.Failure).Sum(r => r.Records);
        }
    }
}
