using DeveUnityLicenseActivator.CLI;
using Xunit;

namespace DeveUnityLicenseActivator.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var cli = new CLIOptions() { Email = "test@gmail.com", Password = "pwtest" };

            Assert.NotNull(cli);
        }
    }
}
