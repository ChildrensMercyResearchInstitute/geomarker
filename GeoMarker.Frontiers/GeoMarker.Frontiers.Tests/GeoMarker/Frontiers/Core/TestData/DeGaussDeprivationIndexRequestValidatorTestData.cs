namespace GeoMarker.Frontiers.Tests.GeoMarker.Frontiers.Core.TestData
{
    public class DeGaussDeprivationIndexRequestValidatorTestData : DeGaussRequestValidatorTestData
    {
        public override IEnumerator<object[]> GetEnumerator()
        {
            // TestCase 1: Valid text/csv file
            yield return new object[] { MockFormFile(100, "text/csv", "ID,address,lat,lon"), true, "" };

            // TestCase 2: Invalid, empty file
            yield return new object[] { MockFormFile(0, "text/csv", ""), false, "The file must not be empty." };

            // TestCase 3: Invalid null file
            yield return new object[] { null, false, "The file must not be null." };

            // TestCase 4: Invalid file size greater than max
            yield return new object[] { MockFormFile(302, "text/csv", "ID,address,lat,lon"), false, "The file must not exceed 300 rows." };

            // TestCase 5: Cannot have duplicated headers
            yield return new object[] { MockFormFile(100, "text/csv", "ID,address,address,lat,lon"), false, "The file must not contain duplicate header columns." };
        }
    }
}
