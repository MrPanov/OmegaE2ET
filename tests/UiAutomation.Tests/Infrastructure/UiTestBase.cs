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
            Screenshots.SaveWhenCurrentTestFailed(Driver);
        }
        finally
        {
            Driver?.Quit();
            Driver?.Dispose();
        }
    }
}
