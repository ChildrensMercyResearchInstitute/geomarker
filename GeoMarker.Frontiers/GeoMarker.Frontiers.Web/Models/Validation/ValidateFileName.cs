using GeoMarker.Frontiers.Core.Resources;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace GeoMarker.Frontiers.Web.Models.Validation
{
    public class ValidateFileName : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var model = validationContext.ObjectInstance as MultiAddressIndexViewModel;
            Regex regularExpression = new Regex("^[a-zA-Z0-9-_]*$");
            if (model != null)
            {
                var fileName = model.File?.FileName;

                if (fileName != null)
                {
                    if (Path.GetExtension(fileName).ToLower() != ".csv")
                    {
                        return new ValidationResult("Invalid file extension. " +
                            "Please upload csv file.");
                    }
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

                    if(regularExpression.IsMatch(fileNameWithoutExtension))
                    {
                        if (model.Types.Contains(DeGaussRequestType.GeoCode) && fileName.Contains("geocoder", StringComparison.OrdinalIgnoreCase))
                            return new ValidationResult("Invalid file name. " + string.Format(CoreMessages.ValidatorController_ReservedFileName, "geocoder", "Geocode"));
                        if (model.Types.Contains(DeGaussRequestType.DriveTime) && fileName.Contains("drivetime", StringComparison.OrdinalIgnoreCase))
                            return new ValidationResult("Invalid file name. " + string.Format(CoreMessages.ValidatorController_ReservedFileName, "drivetime", "Drive Time"));
                        if (model.Types.Contains(DeGaussRequestType.DeprivationIndex) && fileName.Contains("dep_index", StringComparison.OrdinalIgnoreCase))
                            return new ValidationResult("Invalid file name. " + string.Format(CoreMessages.ValidatorController_ReservedFileName, "dep_index", "Deprivation Index"));
                        if (model.Types.Contains(DeGaussRequestType.CensusBlockGroup) && fileName.Contains("census_block_group", StringComparison.OrdinalIgnoreCase))
                            return new ValidationResult("Invalid file name. " + string.Format(CoreMessages.ValidatorController_ReservedFileName, "census_block_group", "Census Block Group"));
                        return ValidationResult.Success;
                    }
                    else
                    {
                        return new ValidationResult("Invalid file name. " +
                            "File name must be alphanumeric (no spaces or special characters).");
                    }
                }
                else
                {
                    return new ValidationResult("File name is required.");
                }

            }
            else
            {
                return new ValidationResult("File is required.");
            }

        }
    }
}
