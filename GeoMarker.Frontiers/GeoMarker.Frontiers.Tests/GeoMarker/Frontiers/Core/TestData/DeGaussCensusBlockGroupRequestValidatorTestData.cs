namespace GeoMarker.Frontiers.Tests.GeoMarker.Frontiers.Core.TestData
{
    public class DeGaussCensusBlockGroupRequestValidatorTestData : DeGaussRequestValidatorTestData
    {
        public override IEnumerator<object[]> GetEnumerator()
        {
            // TestCase 1: Valid text/csv file
            yield return new object[] { MockFormFile(100, "text/csv", "ID,address,lat,lon"), 2010, true, "" };

            // TestCase 2: Invalid empty file
            yield return new object[] { MockFormFile(0, "text/csv", ""), 2010, false, "The file must not be empty." };

            // TestCase 3: Invalid null file
            yield return new object[] { null, 2010, false, "The file must not be null." };

            // TestCase 4: Invalid file exceeds maximum size
            yield return new object[] { MockFormFile(302, "text/csv", "ID,address,lat,lon"), 2010, false, "The file must not exceed 300 rows." };

            // TestCase 5: invalid year is not in YEARS list
            yield return new object[] { MockFormFile(100, "text/csv", "ID,address,lat,lon"), 2040, false, "The year must be one of: " };

            // TestCase 6: invalid year is null
            yield return new object[] { MockFormFile(100, "text/csv", "ID,address,lat,lon"), null, false, "The year must be one of: " };

            // TestCase 7: Cannot have duplicated headers
            yield return new object[] { MockFormFile(100, "text/csv", "ID,address,ID,lat,lon,lon"), 2020, false, "The file must not contain duplicate header columns." };
        }
    }
}
