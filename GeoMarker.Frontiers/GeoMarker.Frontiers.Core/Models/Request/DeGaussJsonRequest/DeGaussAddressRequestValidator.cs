using FluentValidation;

namespace GeoMarker.Frontiers.Core.Models.Request.Validation
{
    public class DeGaussAddressRequestValidator : AbstractValidator<DeGaussAddressRequest>
    {
        public DeGaussAddressRequestValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x).NotNull();

            When(x => x != null, () =>
            {
                RuleFor(x => x.Id).NotNull().NotEmpty();
                RuleFor(x => x.Address).NotNull().NotEmpty();
            });
        }
    }
}
