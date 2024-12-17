using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace GeoMarker.Frontiers.Core.Models.Request
{
    public class DeGaussRequest
    {
        [Required]
        public IFormFile? File { get; set; }        
        public string? RequestGuid { get; set; }
    }
}
