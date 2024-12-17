namespace GeoMarker.Frontiers.Tests.GeoMarker.Frontiers.Core.TestData
{
    public class DeGaussDriveTimeJsonRequestValidatorTestData : DeGaussGeocodedJsonRequestValidatorTestData
    {
        public override IEnumerator<object[]> GetEnumerator()
        {
            // TestCase 1: Valid address
            yield return new object[] { GenerateGeocodedAddressList(1), "mercy", true, "" };

            // TestCase 2: Invalid, empty list
            yield return new object[] { GenerateGeocodedAddressList(0), "mercy", false, "The lat must not be empty." };

            // TestCase 3: Invalid too many addresses
            yield return new object[] { GenerateGeocodedAddressList(302), "mercy", false, "The request cannot exceed 300 addresses." };

            // TestCase 4: Invalid lat must be present
            yield return new object[] { GenerateGeocodedAddressList(2, true), "mercy", false, "The lat must not be empty." };

            // TestCase 5: Invalid null site
            yield return new object[] { GenerateGeocodedAddressList(2, true), null, false, "The site must not be null." };

            // TestCase 6: Invalid wrong site
            yield return new object[] { GenerateGeocodedAddressList(2, true), "blah", false, "The site must be one of: " };

        }
    }
}
