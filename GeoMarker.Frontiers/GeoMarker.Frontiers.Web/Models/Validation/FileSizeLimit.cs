using System.ComponentModel.DataAnnotations;

namespace GeoMarker.Frontiers.Web.Models.Validation
{
    public class FileSizeLimit : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var model = validationContext.ObjectInstance as MultiAddressIndexViewModel;

            var configuration = validationContext.GetService(typeof(IConfiguration)) as IConfiguration;

            long defaultFileSizeInBytes = 25000000;

            if (configuration != null)
            {
                long MaxFileSizeInBytes = configuration.GetValue<long>("FileMetadata:MaxFileSizeInBytes");
                if (MaxFileSizeInBytes > 0)
                {
                    defaultFileSizeInBytes = MaxFileSizeInBytes;
                }
            }

            if (model != null)
            {
                if (model.File?.Length > defaultFileSizeInBytes)
                {
                    var fileSizeInMb = (defaultFileSizeInBytes / 1024f) / 1024f;
                    return new ValidationResult("File size cannot be more than " + Math.Ceiling(fileSizeInMb).ToString() + " MB");
                }
            }
            else
            {
                return new ValidationResult("File is required.");
            }

            return ValidationResult.Success;
        }
    }
}
