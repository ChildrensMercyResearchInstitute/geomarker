using FluentValidation;
using GeoMarker.Frontiers.Core.Resources;

namespace GeoMarker.Frontiers.Core.Models.Request.Validation
{
    public class DeGaussJsonRequestValidator : AbstractValidator<DeGaussJsonRequest>
    {
        public DeGaussJsonRequestValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x).NotNull();

            When(x => x != null, () =>
            {
                RuleFor(x => x.Addresses).NotNull().NotEmpty().WithMessage(CoreMessages.ValidatorController_JsonAddressEmptyMessage);
                RuleFor(x => x.Addresses).Must(x => x.Count <= DeGaussRequestValidator.MAX_LINES).WithMessage(string.Format(CoreMessages.ValidatorController_JsonExceedsAddressCountMessage, DeGaussRequestValidator.MAX_LINES));
                RuleForEach(x => x.Addresses).SetValidator(new DeGaussAddressRequestValidator());
            });
        }
    }
}
