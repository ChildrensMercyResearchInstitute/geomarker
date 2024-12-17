using GeoMarker.Frontiers.Core.Models.Request;
using System.Collections;

namespace GeoMarker.Frontiers.Tests.GeoMarker.Frontiers.Core.TestData
{
    public class DeGaussJsonRequestValidatorTestData : IEnumerable<object[]>
    {
        public virtual IEnumerator<object[]> GetEnumerator()
        {
            // TestCase 1: Valid address
            yield return new object[] { GenerateAddressList(1), true, "" };

            // TestCase 2: Invalid, empty list
            yield return new object[] { GenerateAddressList(0), false, "At least one address must be present." };

            // TestCase 3: Invalid null list
            yield return new object[] { null, false, "'Addresses' must not be empty." };

            // TestCase 4: Invalid too many addresses
            yield return new object[] { GenerateAddressList(302), false, "The request cannot exceed 300 addresses." };

            // TestCase 5: Invalid id must be present
            yield return new object[] { GenerateAddressList(2, true), false, "'Id' must not be empty." };

            // TestCase 6: Invalid address must be present
            yield return new object[] { GenerateAddressList(2, false, true), false, "'Address' must not be empty." };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected List<DeGaussAddressRequest> GenerateAddressList(int number, bool idBlank = false, bool addressBlank = false)
        {
            var addressList = new List<DeGaussAddressRequest>();
            for (int i = 0; i < number; i++)
            {
                addressList.Add(new DeGaussAddressRequest { Id = idBlank ? "" : i.ToString(), Address = addressBlank ? "" : "Test Address 123456" });
            }
            return addressList;
        }
    }
}
