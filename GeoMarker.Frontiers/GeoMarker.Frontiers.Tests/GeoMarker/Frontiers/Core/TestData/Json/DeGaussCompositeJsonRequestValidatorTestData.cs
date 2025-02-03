namespace GeoMarker.Frontiers.Tests.GeoMarker.Frontiers.Core.TestData
{
    public class DeGaussCompositeJsonRequestValidatorTestData : DeGaussJsonRequestValidatorTestData
    {
        public override IEnumerator<object[]> GetEnumerator()
        {
            // TestCase 1: Valid address to geocoded
            yield return new object[] { GenerateAddressList(1), new List<string>(), null, null, true, "" };

            // TestCase 2: Valid address and parameters for all services
            yield return new object[] { GenerateAddressList(1), new List<string>() { "censusblockgroup", "deprivationindex", "drivetime" }, "mercy", 2020, true, "" };

            // TestCase 3: Invalid, empty list
            yield return new object[] { GenerateAddressList(0), new List<string>(), null, null, false, "At least one address must be present." };

            // TestCase 4: Invalid null list
            yield return new object[] { null, new List<string>(), null, null, false, "'Addresses' must not be empty." };

            // TestCase 5: Invalid too many addresses
            yield return new object[] { GenerateAddressList(302), new List<string>(), null, null, false, "The request cannot exceed 300 addresses." };

            // TestCase 6: Invalid id must be present
            yield return new object[] { GenerateAddressList(2, true), new List<string>(), null, null, false, "'Id' must not be empty." };

            // TestCase 7: Invalid address must be present
            yield return new object[] { GenerateAddressList(2, false, true), new List<string>(), null, null, false, "'Address' must not be empty." };

            // TestCase 8: Invalid no site for drivetime
            yield return new object[] { GenerateAddressList(2), new List<string>() { "drivetime" }, null, null, false, "The site must not be null." };

            // TestCase 9: Invalid wrong site for drivetime
            yield return new object[] { GenerateAddressList(2), new List<string>() { "drivetime" }, "blah", null, false, "The site must be one of:" };

            // TestCase 10: Invalid no year for censusblockgroup
            yield return new object[] { GenerateAddressList(2), new List<string>() { "censusblockgroup" }, null, null, false, "The year must not be null." };

            // TestCase 11: Invalid wrong year for censusblockgroup
            yield return new object[] { GenerateAddressList(2), new List<string>() { "censusblockgroup" }, null, 100, false, "The year must be one of:" };
        }
    }
}
