using System.ComponentModel.DataAnnotations;

namespace GeoMarker.Frontiers.Web.Models.Validation
{
    public class YearRequiredIfCensusBlockGroup : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var model = validationContext.ObjectInstance as BaseViewModel;

            if (model == null ||
               (model.Types.Contains(DeGaussRequestType.CensusBlockGroup) &&
                model.Year == null))
            {
                return new ValidationResult("Year is required.");
            }

            return ValidationResult.Success;
        }
    }
}
