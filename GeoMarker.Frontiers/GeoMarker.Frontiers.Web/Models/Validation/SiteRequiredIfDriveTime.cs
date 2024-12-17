using System.ComponentModel.DataAnnotations;

namespace GeoMarker.Frontiers.Web.Models.Validation
{
    public class SiteRequiredIfDriveTime : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var model = validationContext.ObjectInstance as BaseViewModel;

            if (model == null ||
               (model.Types.Contains(DeGaussRequestType.DriveTime) &&
                model.Site == null))
            {
                return new ValidationResult("Site is required.");
            }

            return ValidationResult.Success;
        }
    }
}
