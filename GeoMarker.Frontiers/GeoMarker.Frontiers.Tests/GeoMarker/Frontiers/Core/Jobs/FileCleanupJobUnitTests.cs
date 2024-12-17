using GeoMarker.Frontiers.Core.Jobs;
using GeoMarker.Frontiers.Tests.Util;
using Moq;
using Quartz;

namespace GeoMarker.Frontiers.Tests.GeoMarker.Frontiers.Core.Jobs
{
    public class FileCleanupJobUnitTests
    {
        [Fact]
        public async Task Execute_ShouldCleanupDirectories()
        {
            var logger = TestLoggerFactory<FileCleanupJob>.CreateTestLogger();
            var sut = new FileCleanupJob(logger);

            JobDataMap jobDataMap = new JobDataMap();
            jobDataMap.Add("KeepAliveDays", "30");
            Mock<IJobExecutionContext> context = new();
            Mock<IJobDetail> detail = new();

            var testroot = Directory.GetCurrentDirectory();
            var testdir = testroot + "/tmp/" + Guid.NewGuid().ToString();
            Directory.CreateDirectory(testdir);
            Directory.SetLastWriteTimeUtc(testdir, DateTime.UtcNow.Subtract(TimeSpan.FromDays(31)));
            detail.Setup(x => x.JobDataMap).Returns(jobDataMap);
            context.Setup(x => x.JobDetail).Returns(detail.Object);
            var result = sut.Execute(context.Object);

            Assert.False(Directory.Exists(testdir));
        }

        [Fact]
        public async Task Execute_ShouldValidate_NoKeepAliveDays()
        {
            using StringWriter sw = new();
            Console.SetOut(sw);
            var logger = TestLoggerFactory<FileCleanupJob>.CreateTestLogger();
            var sut = new FileCleanupJob(logger);

            JobDataMap jobDataMap = new JobDataMap();
            Mock<IJobExecutionContext> context = new();
            Mock<IJobDetail> detail = new();
            var testroot = Directory.GetCurrentDirectory();
            var testdir = testroot + "/tmp/" + Guid.NewGuid().ToString();
            Directory.CreateDirectory(testdir);

            detail.Setup(x => x.JobDataMap).Returns(jobDataMap);
            context.Setup(x => x.JobDetail).Returns(detail.Object);
            var result = sut.Execute(context.Object);

            Assert.True(Directory.Exists(testdir));
            Directory.Delete(testdir);
        }


        [Fact]
        public async Task Execute_ShouldValidate_InvalidKeepAliveDays()
        {
            using StringWriter sw = new();
            Console.SetOut(sw);
            var logger = TestLoggerFactory<FileCleanupJob>.CreateTestLogger();
            var sut = new FileCleanupJob(logger);

            JobDataMap jobDataMap = new JobDataMap();
            jobDataMap.Add("KeepAliveDays", "abc");
            Mock<IJobExecutionContext> context = new();
            Mock<IJobDetail> detail = new();
            var testroot = Directory.GetCurrentDirectory();
            var testdir = testroot + "/tmp/" + Guid.NewGuid().ToString();
            Directory.CreateDirectory(testdir);

            detail.Setup(x => x.JobDataMap).Returns(jobDataMap);
            context.Setup(x => x.JobDetail).Returns(detail.Object);
            var result = sut.Execute(context.Object);

            Assert.True(Directory.Exists(testdir));
            Directory.Delete(testdir);
        }
    }
}
