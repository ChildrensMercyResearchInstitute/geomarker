using GeoMarker.Frontiers.Web.Models.Validation;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace GeoMarker.Frontiers.Web.Models
{
    public class MultiAddressIndexViewModel : BaseViewModel
    {
        [BindProperty]
        [Required(ErrorMessage = "File is required.")]
        [FileSizeLimit]
        [ValidateFileName]
        public IFormFile? File { get; set; }


    }
}
