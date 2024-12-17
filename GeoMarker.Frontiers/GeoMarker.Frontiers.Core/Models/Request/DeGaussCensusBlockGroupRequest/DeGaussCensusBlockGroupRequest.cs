
using System.ComponentModel.DataAnnotations;

namespace GeoMarker.Frontiers.Core.Models.Request
{
    public class DeGaussCensusBlockGroupRequest : DeGaussRequest
    {
        [Required]
        public int Year { get; set; } = 2020;
    }
}
