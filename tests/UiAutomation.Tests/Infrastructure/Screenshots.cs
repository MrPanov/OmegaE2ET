using NUnit.Framework;
using OpenQA.Selenium;

namespace UiAutomation.Tests.Infrastructure;

internal static class Screenshots
{
    public static void SaveWhenCurrentTestFailed(IWebDriver? driver)
    {
        if (TestContext.CurrentContext.Result.Outcome.Status !=
            NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            return;
        }

        if (driver is not ITakesScreenshot screenshotDriver) return;

        var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots");
        Directory.CreateDirectory(directory);
        var testName = string.Concat(
            TestContext.CurrentContext.Test.Name.Select(c =>
                Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var path = Path.Combine(directory, $"{testName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.png");
        screenshotDriver.GetScreenshot().SaveAsFile(path);
        TestContext.AddTestAttachment(path, "Screenshot on failure");
    }
}
