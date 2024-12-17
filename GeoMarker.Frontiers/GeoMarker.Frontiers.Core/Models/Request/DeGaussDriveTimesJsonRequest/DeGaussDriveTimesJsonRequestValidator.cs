using FluentValidation;
using GeoMarker.Frontiers.Core.Resources;

namespace GeoMarker.Frontiers.Core.Models.Request.Validation
{
    public class DeGaussDriveTimesJsonRequestValidator : AbstractValidator<DeGaussDriveTimesJsonRequest>
    {
        public DeGaussDriveTimesJsonRequestValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x).NotNull();

            When(x => x != null, () =>
            {
                RuleFor(x => x.Records).NotNull().NotEmpty().WithMessage(CoreMessages.ValidatorController_GeocodedJsonLatEmptyMessage);
                RuleFor(x => x.Site).NotNull().WithMessage(CoreMessages.ValidatorController_SiteNullMessage);
                RuleFor(x => x.Site).NotEmpty().WithMessage(CoreMessages.ValidatorController_SiteEmptyMessage);
                RuleFor(x => x.Site).Must(site => DeGaussDrivetimeRequestValidator.SITES.Values.ToList().Contains(site!))
                                .WithMessage(string.Format(CoreMessages.ValidatorController_SiteInvalidMessage, DeGaussDrivetimeRequestValidator.SITES.Values.ToList().Aggregate("", (acc, next) => acc += " " + next)));
                RuleFor(x => x.Records.Count).Must(x => x <= DeGaussRequestValidator.MAX_LINES).WithMessage(string.Format(CoreMessages.ValidatorController_JsonExceedsAddressCountMessage, DeGaussRequestValidator.MAX_LINES));
                When(x => x.Records.Count > 0 && x.Records.Count <= DeGaussRequestValidator.MAX_LINES, () =>
                {
                    RuleForEach(x => x.Records).SetValidator(new DeGaussGeocodedJsonRecordValidator());
                });
            });
        }
    }
}
