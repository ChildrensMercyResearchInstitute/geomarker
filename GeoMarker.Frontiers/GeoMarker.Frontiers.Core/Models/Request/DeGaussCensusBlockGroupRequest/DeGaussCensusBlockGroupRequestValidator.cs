using FluentValidation;
using GeoMarker.Frontiers.Core.Resources;
using Microsoft.Extensions.Options;

namespace GeoMarker.Frontiers.Core.Models.Request.Validation
{
    public class DeGaussCensusBlockGroupRequestValidator : AbstractValidator<DeGaussCensusBlockGroupRequest>
    {
        public readonly static List<int> YEARS = new List<int>()
        {
            1970,
            1980,
            1990,
            2000,
            2010,
            2020
        };

        public DeGaussCensusBlockGroupRequestValidator(IOptions<FileMetadata> fileMetadata)
        {
            Include(new DeGaussRequestValidator(fileMetadata));

            RuleSet("CensusBlockGroup", () =>
            {
                RuleFor(x => x.Year).NotNull().WithMessage(CoreMessages.ValidatorController_YearNullMessage);
                RuleFor(x => x.Year).Must(year => YEARS.Contains(year!)).WithMessage(string.Format(CoreMessages.ValidatorController_YearInvalidMessage, YEARS.Aggregate("", (acc, next) => acc += " " + next)));
            });
        }
    }
}
