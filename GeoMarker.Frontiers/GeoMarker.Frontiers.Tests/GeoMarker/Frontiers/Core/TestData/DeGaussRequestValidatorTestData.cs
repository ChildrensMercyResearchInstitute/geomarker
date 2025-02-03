using Microsoft.AspNetCore.Http;
using Moq;
using System.Collections;
using System.Text;

namespace GeoMarker.Frontiers.Tests.GeoMarker.Frontiers.Core.TestData
{
    public class DeGaussRequestValidatorTestData : IEnumerable<object[]>
    {
        public virtual IEnumerator<object[]> GetEnumerator()
        {
            // TestCase 1: Valid text/csv file
            yield return new object[] { MockFormFile(100, "text/csv", "ID,address"), true, "" };

            // TestCase 2: Invalid, empty file
            yield return new object[] { MockFormFile(0, "text/csv", ""), false, "The file must not be empty." };

            // TestCase 3: Invalid null file
            yield return new object[] { null, false, "The file must not be null." };

            // TestCase 4: Invalid file size greater than max
            yield return new object[] { MockFormFile(302, "text/csv", "ID,address"), false, "The file must not exceed 300 rows." };

            // TestCase 5: Cannot have duplicated headers
            yield return new object[] { MockFormFile(100, "text/csv", "ID,address,address"), false, "The file must not contain duplicate header columns." };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IFormFile MockFormFile(int count, string contentType, string contentHeader, string filename="test.csv")
        {
            var content = new StringBuilder();
            var start = 0;
            if (contentHeader.Length > 0)
            {
                content.AppendLine(contentHeader);
                start++;
            }
            Enumerable.Range(start, count).ToList().ForEach(line => content.AppendLine("This is a test file"));

            var file = new Mock<IFormFile>();
            file.Setup(f => f.ContentType).Returns(contentType);
            file.Setup(f => f.OpenReadStream()).Returns(() => CreateMemoryStream(content.ToString()));
            file.Setup(f => f.FileName).Returns(filename);

            return file.Object;
        }

        private MemoryStream CreateMemoryStream(string content)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content.ToString());
            writer.Flush();
            stream.Position = 0;

            return stream;
        }
    }
}
