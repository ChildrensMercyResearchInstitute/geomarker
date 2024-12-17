using FluentValidation.Results;
using GeoMarker.Frontiers.Core.Models.Request.Validation;
using GeoMarker.Frontiers.Core.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GeoMarker.Frontiers.Core.Helpers
{
    public static class ValidationHelper
    {
        public static ValidationProblemDetails MapProblemDetails(ValidationResult result)
        {
            var problemDetails = new ValidationProblemDetails() { Status = StatusCodes.Status400BadRequest };
            var errorMap = result.Errors.GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

            errorMap.Keys.ToList().ForEach(prop => problemDetails.Errors.Add(prop, errorMap[prop].ToArray()));

            return problemDetails;
        }

        public static ValidationProblemDetails GetIncorrectJson(string category, string format)
        {
            return new ValidationProblemDetails(new Dictionary<string, string[]>() { { category, new string[1] { string.Format(CoreMessages.ValidatorController_JsonIncorrectFormatMessage, format) } } }); ;
        }

        public static ValidationProblemDetails GetTooManyAddresses()
        {
            return new ValidationProblemDetails(new Dictionary<string, string[]>() { { "Addresses", new string[1] { string.Format(CoreMessages.ValidatorController_JsonExceedsAddressCountMessage, DeGaussRequestValidator.MAX_LINES) } } }); ;
        }

        public static ValidationProblemDetails NoAddressesGeocoded()
        {
            return new ValidationProblemDetails(new Dictionary<string, string[]>() { { "Addresses", new string[1] { CoreMessages.ValidatorController_NoAddressesGeocoded } } }); ;
        }
    }
}
