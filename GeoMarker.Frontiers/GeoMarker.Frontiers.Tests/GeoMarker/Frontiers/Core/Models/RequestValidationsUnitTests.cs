using FluentValidation;
using GeoMarker.Frontiers.Core.Models;
using GeoMarker.Frontiers.Core.Models.Request;
using GeoMarker.Frontiers.Core.Models.Request.Validation;
using GeoMarker.Frontiers.Tests.GeoMarker.Frontiers.Core.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;

namespace GeoMarker.Frontiers.Tests.GeoMarker.Frontiers.Core.Jobs
{
    public class RequestValidationsUnitTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("123")]
        public void DeGaussAsyncRequestValidator_ValidateGuid(string guid)
        {
            DeGaussAsyncRequestValidator sut = new();
            var result = sut.Validate(new DeGaussAsyncRequest() { Guid = guid });
            if (guid == "")
            {
                Assert.False(result.IsValid);
                Assert.Equal("'Guid' must not be empty.", result.Errors.First().ErrorMessage);
            }
            else
                Assert.True(result.IsValid);
        }

        [Theory]
        [ClassData(typeof(DeGaussRequestValidatorTestData))]
        public void GeocodeRequestValidator_ShouldValidate(IFormFile file, bool isValid, string resultMessage)
        {
            var sut = new DeGaussRequestValidator(GetMockMetadata());
            var request = new DeGaussRequest() { File = file };

            var result = sut.Validate(request, options => { options.IncludeRuleSets("Base", "Geocode", "BelowMaxRows", "MaxFileSize", "ValidateFileName", "ValidateFileNameDeprivationIndex"); });
            var message = new StringBuilder();
            result.Errors.ForEach(error => message.AppendLine(error.ErrorMessage));

            Assert.Equal(isValid, result.IsValid);
            Assert.StartsWith(resultMessage, message.ToString());
        }

        [Theory]
        [ClassData(typeof(DeGaussDrivetimeRequestValidatorTestData))]
        public void DriveTimeRequestValidator_ShouldValidate(IFormFile file, string site, bool isValid, List<string> resultMessages)
        {
            var sut = new DeGaussDrivetimeRequestValidator(GetMockMetadata());
            var request = new DeGaussDrivetimeRequest() { File = file, Site = site };
            
            var result = sut.Validate(request, options => { options.IncludeRuleSets("Base", "Geocoded", "Drivetime", "BelowMaxRows", "MaxFileSize", "ValidateFileName", "ValidateFileNameDriveTime"); });
            var message = new StringBuilder();
            result.Errors.ForEach(error => message.AppendLine(error.ErrorMessage));

            Assert.Equal(isValid, result.IsValid);
            foreach (var error in resultMessages)
                Assert.Contains(error, message.ToString());
        }

        [Theory]
        [ClassData(typeof(DeGaussDeprivationIndexRequestValidatorTestData))]
        public void DeprivationIndexRequestValidator_ShouldValidate(IFormFile file, bool isValid, string resultMessage)
        {
            var sut = new DeGaussRequestValidator(GetMockMetadata());
            var request = new DeGaussRequest() { File = file };

            var result = sut.Validate(request, options => { options.IncludeRuleSets("Base", "Geocoded", "BelowMaxRows", "MaxFileSize", "ValidateFileName", "ValidateFileNameGeocode"); });
            var message = new StringBuilder();
            result.Errors.ForEach(error => message.AppendLine(error.ErrorMessage));

            Assert.Equal(isValid, result.IsValid);
            Assert.StartsWith(resultMessage, message.ToString());
        }

        [Theory]
        [ClassData(typeof(DeGaussCensusBlockGroupRequestValidatorTestData))]
        public void CensusBlockGroupRequestValidator_ShouldValidate(IFormFile file, int year, bool isValid, string resultMessage)
        {
            var sut = new DeGaussCensusBlockGroupRequestValidator(GetMockMetadata());
            var request = new DeGaussCensusBlockGroupRequest() { File = file, Year = year };

            var result = sut.Validate(request, options => { options.IncludeRuleSets("Base", "Geocoded", "CensusBlockGroup", "BelowMaxRows", "MaxFileSize", "ValidateFileName", "ValidateFileNameCensusBlockGroup"); });
            var message = new StringBuilder();
            result.Errors.ForEach(error => message.AppendLine(error.ErrorMessage));

            Assert.Equal(isValid, result.IsValid);
            Assert.StartsWith(resultMessage, message.ToString());
        }

        [Theory]
        [ClassData(typeof(DeGaussJsonRequestValidatorTestData))]
        public void GeocodeJsonRequestValidator_ShouldValidate(List<DeGaussAddressRequest> addresses, bool isValid, string resultMessage)
        {
            var sut = new DeGaussJsonRequestValidator();
            var request = new DeGaussJsonRequest() { Addresses = addresses };

            var result = sut.Validate(request);
            var message = new StringBuilder();
            result.Errors.ForEach(error => message.AppendLine(error.ErrorMessage));

            Assert.Equal(isValid, result.IsValid);
            Assert.StartsWith(resultMessage, message.ToString());
        }

        [Theory]
        [ClassData(typeof(DeGaussDriveTimeJsonRequestValidatorTestData))]
        public void DriveTimeJsonRequestValidator_ShouldValidate(List<DeGaussGeocodedJsonRecord> records, string site, bool isValid, string resultMessage)
        {
            var sut = new DeGaussDriveTimesJsonRequestValidator();
            var request = new DeGaussDriveTimesJsonRequest() { Records = records, Site = site };

            var result = sut.Validate(request);
            var message = new StringBuilder();
            result.Errors.ForEach(error => message.AppendLine(error.ErrorMessage));

            Assert.Equal(isValid, result.IsValid);
            Assert.StartsWith(resultMessage, message.ToString());
        }

        [Theory]
        [ClassData(typeof(DeGaussGeocodedJsonRequestValidatorTestData))]
        public void DeprivationIndexJsonRequestValidator_ShouldValidate(List<DeGaussGeocodedJsonRecord> records, bool isValid, string resultMessage)
        {
            var sut = new DeGaussGeocodedJsonRequestValidator();
            var request = new DeGaussGeocodedJsonRequest() { Records = records };

            var result = sut.Validate(request);
            var message = new StringBuilder();
            result.Errors.ForEach(error => message.AppendLine(error.ErrorMessage));

            Assert.Equal(isValid, result.IsValid);
            Assert.StartsWith(resultMessage, message.ToString());
        }

        [Theory]
        [ClassData(typeof(DeGaussCensusBlockGroupJsonRequestValidatorTestData))]
        public void CensusBlockGroupJsonRequestValidator_ShouldValidate(List<DeGaussGeocodedJsonRecord> records, int year, bool isValid, string resultMessage)
        {
            var sut = new DeGaussCensusBlockGroupsJsonRequestValidator();
            var request = new DeGaussCensusBlockGroupsJsonRequest() { Records = records, Year = year };

            var result = sut.Validate(request);
            var message = new StringBuilder();
            result.Errors.ForEach(error => message.AppendLine(error.ErrorMessage));

            Assert.Equal(isValid, result.IsValid);
            Assert.StartsWith(resultMessage, message.ToString());
        }

        [Theory]
        [ClassData(typeof(DeGaussCompositeJsonRequestValidatorTestData))]
        public void CompositeJsonRequestValidator_ShouldValidate(List<DeGaussAddressRequest> addresses, List<string> services, string site, int year, bool isValid, string resultMessage)
        {
            var sut = new DeGaussCompositeJsonRequestValidator();
            var request = new DeGaussCompositeJsonRequest() { Addresses = addresses, Site = site, Year = year, Services = services };

            var result = sut.Validate(request);
            var message = new StringBuilder();
            result.Errors.ForEach(error => message.AppendLine(error.ErrorMessage));

            Assert.Equal(isValid, result.IsValid);
            Assert.StartsWith(resultMessage, message.ToString());
        }

        private IOptions<FileMetadata> GetMockMetadata()
        {
            var metadata = new Mock<IOptions<FileMetadata>>();
            metadata.Setup(m => m.Value).Returns(new FileMetadata() { MaxFileSizeInBytes = 25000000 });
            return metadata.Object;
        }
    }
}
