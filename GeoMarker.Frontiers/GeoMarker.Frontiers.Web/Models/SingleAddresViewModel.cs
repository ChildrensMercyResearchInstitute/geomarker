using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace GeoMarker.Frontiers.Web.Models
{
    public class SingleAddressViewModel : BaseViewModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; }
        public ResponseType ResponseType { get; set; }
        public string? ResponseMessage { get; set; }

        public string GetMessageClass()
        {
            switch (ResponseType)
            {
                case ResponseType.Success: return "alert-success";
                case ResponseType.Warning: return "alert-warning";
                case ResponseType.Error: return "alert-danger";
            }
            return "";
        }
    }

    public enum ResponseType
    {
        Success,
        Warning,
        Error
    }
}
