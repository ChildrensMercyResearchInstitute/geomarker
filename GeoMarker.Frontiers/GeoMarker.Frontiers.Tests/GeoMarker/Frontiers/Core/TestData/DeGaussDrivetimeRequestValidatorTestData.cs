namespace GeoMarker.Frontiers.Tests.GeoMarker.Frontiers.Core.TestData
{
    public class DeGaussDrivetimeRequestValidatorTestData : DeGaussRequestValidatorTestData
    {
        public override IEnumerator<object[]> GetEnumerator()
        {
            // TestCase 1: Valid text/csv file
            yield return new object[] { MockFormFile(100, "text/csv", "ID,address,lat,lon"), "mercy", true, new List<string>() { "" } };

            // TestCase 2: Invalid empty file
            yield return new object[] { MockFormFile(0, "text/csv", ""), "mercy", false, new List<string>() { "The file must not be empty." } };

            // TestCase 3: Invalid null file
            yield return new object[] { null, "mercy", false, new List<string>() { "The file must not be null." } };

            // TestCase 4: Invalid file exceeds maximum size
            yield return new object[] { MockFormFile(302, "text/csv", "ID,address,lat,lon"), "mercy", false, new List<string>() { "The file must not exceed 300 rows." } };

            // TestCase 5: invalid site is empty
            yield return new object[] { MockFormFile(100, "text/csv", "ID,address,lat,lon"), "", false, new List<string>() { "The site must not be empty.", "The site must be one of: " } };

            // TestCase 6: invalid site is null
            yield return new object[] { MockFormFile(100, "text/csv", "ID,address,lat,lon"), null, false, new List<string>() { "The site must not be null.","The site must not be empty.","The site must be one of: " } };

            // TestCase 7: Cannot have duplicated headers
            yield return new object[] { MockFormFile(100, "text/csv", "ID,address,address,lat,lon"), "mercy", false, new List<string>() { "The file must not contain duplicate header columns." } };
        }
    }
}
