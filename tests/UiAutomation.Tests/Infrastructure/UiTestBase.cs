using NUnit.Framework;
using OpenQA.Selenium;
using UiAutomation.Tests.Configuration;

namespace UiAutomation.Tests.Infrastructure;

public abstract class UiTestBase
{
    protected IWebDriver Driver { get; private set; } = null!;
    protected TestSettings Settings { get; private set; } = null!;

    [SetUp]
    public void SetUp()
    {
        Settings = TestSettings.FromEnvironment();
        Driver = DriverFactory.Create(Settings);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                SaveScreenshot();
            }
        }
        finally
        {
            Driver?.Quit();
            Driver?.Dispose();
        }
    }

    private void SaveScreenshot()
    {
        if (Driver is not ITakesScreenshot screenshotDriver) return;

        var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots");
        Directory.CreateDirectory(directory);
        var testName = string.Concat(
            TestContext.CurrentContext.Test.Name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var path = Path.Combine(directory, $"{testName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.png");
        screenshotDriver.GetScreenshot().SaveAsFile(path);
        TestContext.AddTestAttachment(path, "Screenshot on failure");
    }
}
