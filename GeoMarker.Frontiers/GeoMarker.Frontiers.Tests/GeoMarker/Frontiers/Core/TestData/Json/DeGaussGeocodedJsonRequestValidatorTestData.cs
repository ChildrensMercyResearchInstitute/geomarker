using GeoMarker.Frontiers.Core.Models.Request;
using System.Collections;

namespace GeoMarker.Frontiers.Tests.GeoMarker.Frontiers.Core.TestData
{
    public class DeGaussGeocodedJsonRequestValidatorTestData : IEnumerable<object[]>
    {
        public virtual IEnumerator<object[]> GetEnumerator()
        {
            // TestCase 1: Valid address
            yield return new object[] { GenerateGeocodedAddressList(1), true, "" };

            // TestCase 2: Invalid, empty list
            yield return new object[] { GenerateGeocodedAddressList(0), false, "The lat must not be empty." };

            // TestCase 3: Invalid too many addresses
            yield return new object[] { GenerateGeocodedAddressList(302), false, "The request cannot exceed 300 addresses." };

            // TestCase 4: Invalid lat must be present
            yield return new object[] { GenerateGeocodedAddressList(2, true), false, "The lat must not be empty." };

        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected List<DeGaussGeocodedJsonRecord> GenerateGeocodedAddressList(int number, bool latBlank = false)
        {
            var records = new List<DeGaussGeocodedJsonRecord>();
            for (int i = 0; i < number; i++)
            {
                records.Add(new DeGaussGeocodedJsonRecord() { lat = latBlank ? "" : "0", lon = "0" });
            }
            return records;
        }
    }
}
