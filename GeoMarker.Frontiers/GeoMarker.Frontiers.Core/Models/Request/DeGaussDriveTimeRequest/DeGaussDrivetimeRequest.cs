
using System.ComponentModel.DataAnnotations;

namespace GeoMarker.Frontiers.Core.Models.Request
{
    public class DeGaussDrivetimeRequest : DeGaussRequest
    {
        [Required]
        public string? Site { get; set; }
    }
}

