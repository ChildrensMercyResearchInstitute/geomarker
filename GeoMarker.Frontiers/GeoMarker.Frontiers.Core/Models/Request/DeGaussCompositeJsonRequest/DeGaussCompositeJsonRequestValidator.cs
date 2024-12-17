using FluentValidation;
using GeoMarker.Frontiers.Core.Resources;

namespace GeoMarker.Frontiers.Core.Models.Request.Validation
{
    public class DeGaussCompositeJsonRequestValidator : AbstractValidator<DeGaussCompositeJsonRequest>
    {
        public DeGaussCompositeJsonRequestValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x).NotNull();

            When(x => x != null, () =>
            {
                RuleFor(x => x.Addresses).NotNull().NotEmpty().WithMessage(CoreMessages.ValidatorController_JsonAddressEmptyMessage);
                RuleFor(x => x.Addresses).Must(x => x.Count <= DeGaussRequestValidator.MAX_LINES).WithMessage(string.Format(CoreMessages.ValidatorController_JsonExceedsAddressCountMessage, DeGaussRequestValidator.MAX_LINES));
                When(x => x.Addresses.Count > 0 && x.Addresses.Count <= DeGaussRequestValidator.MAX_LINES, () =>
                {
                    RuleForEach(x => x.Addresses).SetValidator(new DeGaussAddressRequestValidator());
                });
                When(x => x.Services.Contains("drivetime"), () =>
                {
                    RuleFor(x => x.Site).NotNull().WithMessage(CoreMessages.ValidatorController_SiteNullMessage);
                    RuleFor(x => x.Site).NotEmpty().WithMessage(CoreMessages.ValidatorController_SiteEmptyMessage);
                    RuleFor(x => x.Site).Must(site => DeGaussDrivetimeRequestValidator.SITES.Values.ToList().Contains(site!))
                                    .WithMessage(string.Format(CoreMessages.ValidatorController_SiteInvalidMessage, DeGaussDrivetimeRequestValidator.SITES.Values.ToList().Aggregate("", (acc, next) => acc += " " + next)));
                });
                When(x => x.Services.Contains("censusblockgroup"), () =>
                {
                    RuleFor(x => x.Year).NotNull().NotEmpty().WithMessage(CoreMessages.ValidatorController_YearNullMessage);
                    RuleFor(x => x.Year).Must(year => DeGaussCensusBlockGroupRequestValidator.YEARS.Contains(year!)).WithMessage(string.Format(CoreMessages.ValidatorController_YearInvalidMessage, DeGaussCensusBlockGroupRequestValidator.YEARS.Aggregate("", (acc, next) => acc += " " + next)));
                });
            });
        }
    }
}
