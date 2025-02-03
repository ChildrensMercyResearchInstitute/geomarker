using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoMarker.Frontiers.Core.Models.Request.Validation
{
    public class DeGaussAsyncRequestValidator : AbstractValidator<DeGaussAsyncRequest>
    {
        public DeGaussAsyncRequestValidator()
        {
            RuleFor(x => x.Guid).NotNull().NotEmpty();
        }
    }
}
