using GeoMarker.Frontiers.Core.Models.Request.Validation;
using GeoMarker.Frontiers.Web.Models.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GeoMarker.Frontiers.Web.Models
{
    public class BaseViewModel
    {
        [Required(ErrorMessage = "Type is required.")]
        public List<DeGaussRequestType> Types { get; set; } = new List<DeGaussRequestType>();
        [SiteRequiredIfDriveTime]
        public string? Site { get; set; }
        [YearRequiredIfCensusBlockGroup]
        public int? Year { get; set; }
        public IActionResult? Response { get; set; }
        public string? SuccessResponse { get; set; }
        public string? ErrorResponse { get; set; }
        public List<UserRequest> UserRequests { get; set; } = new List<UserRequest>();
        public SortedDictionary<DateTimeOffset, List<UserRequest>> UserRequestGroups { get; set; } = new SortedDictionary<DateTimeOffset, List<UserRequest>>(Comparer<DateTimeOffset>.Create((x, y) => y.CompareTo(x)));
        public SelectList SiteOptions { get; } = new SelectList(DeGaussDrivetimeRequestValidator.SITES, "Value", "Key");
        public SelectList YearOptions { get; } = new SelectList(DeGaussCensusBlockGroupRequestValidator.YEARS);
        public bool GeoCodeApiHealthy { get; set; }
        public bool DriveTimeApiHealthy { get; set; }
        public bool DeprivationIndexApiHealthy { get; set; }
        public bool CensusBlockApiHealthy { get; set; }

    }
}
