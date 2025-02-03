
using System.ComponentModel.DataAnnotations;

namespace GeoMarker.Frontiers.Core.Models.Request
{
    public class DeGaussAsyncRequest
    {
        [Required]
        public string Guid { get; set; } = string.Empty;
    }
}
