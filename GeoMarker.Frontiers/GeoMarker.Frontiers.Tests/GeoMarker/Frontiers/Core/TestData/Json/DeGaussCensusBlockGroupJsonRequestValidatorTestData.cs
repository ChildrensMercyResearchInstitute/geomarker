namespace GeoMarker.Frontiers.Tests.GeoMarker.Frontiers.Core.TestData
{
    public class DeGaussCensusBlockGroupJsonRequestValidatorTestData : DeGaussGeocodedJsonRequestValidatorTestData
    {
        public override IEnumerator<object[]> GetEnumerator()
        {
            // TestCase 1: Valid address
            yield return new object[] { GenerateGeocodedAddressList(1), 2020, true, "" };

            // TestCase 2: Invalid, empty list
            yield return new object[] { GenerateGeocodedAddressList(0), 2020, false, "The lat must not be empty." };

            // TestCase 3: Invalid too many addresses
            yield return new object[] { GenerateGeocodedAddressList(302), 2020, false, "The request cannot exceed 300 addresses." };

            // TestCase 4: Invalid lat must be present
            yield return new object[] { GenerateGeocodedAddressList(2, true), 2020, false, "The lat must not be empty." };

            // TestCase 5: Invalid null year
            yield return new object[] { GenerateGeocodedAddressList(2, true), null, false, "The year must not be null." };

            // TestCase 6: Invalid wrong year
            yield return new object[] { GenerateGeocodedAddressList(2, true), 100, false, "The year must be one of: " };

        }
    }
}
