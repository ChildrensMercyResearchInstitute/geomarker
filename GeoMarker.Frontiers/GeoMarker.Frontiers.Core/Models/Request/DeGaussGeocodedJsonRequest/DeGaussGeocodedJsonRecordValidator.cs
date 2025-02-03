using FluentValidation;
using GeoMarker.Frontiers.Core.Resources;

namespace GeoMarker.Frontiers.Core.Models.Request.Validation
{
    public class DeGaussGeocodedJsonRecordValidator : AbstractValidator<DeGaussGeocodedJsonRecord>
    {
        public DeGaussGeocodedJsonRecordValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x).NotNull();

            When(x => x != null, () =>
            {
                RuleFor(x => x.lat).NotNull().NotEmpty().WithMessage(CoreMessages.ValidatorController_GeocodedJsonLatEmptyMessage);
                RuleFor(x => x.lon).NotNull().NotEmpty().WithMessage(CoreMessages.ValidatorController_GeocodedJsonLonEmptyMessage);
            });
        }
    }
}
