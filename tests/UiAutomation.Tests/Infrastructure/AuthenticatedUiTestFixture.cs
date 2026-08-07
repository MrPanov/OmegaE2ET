using NUnit.Framework;
using OpenQA.Selenium;
using UiAutomation.Tests.Configuration;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Infrastructure;

/// <summary>
/// Base fixture for tests that require an authenticated session. The whole class
/// shares a single browser session: login runs once in <see cref="LoginOnce"/>
/// and the browser is closed once after all tests. Derived fixtures build their
/// own page objects in <see cref="OnAuthenticated"/>.
/// </summary>
public abstract class AuthenticatedUiTestFixture
{
    protected IWebDriver Driver { get; private set; } = null!;
    protected TestSettings Settings { get; private set; } = null!;
    protected TimeSpan Timeout { get; private set; }

    [OneTimeSetUp]
    public void LoginOnce()
    {
        Settings = TestSettings.FromEnvironment();

        Assume.That(
            Settings.HasUsableLoginPassword,
            Is.True,
            "Set OMEGA_PASSWORD or configure loginPassword in testsettings.local.json.");

        Driver = DriverFactory.Create(Settings);
        Timeout = TimeSpan.FromSeconds(Settings.ExplicitWaitSeconds);

        var loginPage = new LoginPage(Driver, Timeout);
        loginPage.Open(Settings.BaseUrl);

        if (!loginPage.IsAlreadyAuthenticated)
        {
            loginPage.Login(Settings.LoginEmail, Settings.LoginPassword);
        }

        Assert.That(loginPage.WaitUntilAuthenticated(), Is.True, "Authentication failed.");

        OnAuthenticated();
    }

    /// <summary>
    /// Runs once after a successful login. Derived fixtures create their page
    /// objects and perform any additional first-time setup here.
    /// </summary>
    protected virtual void OnAuthenticated()
    {
    }

    [SetUp]
    public void EnforceProductionPolicy() =>
        ProductionTestPolicy.EnsureCurrentTestIsAllowed(Settings);

    [TearDown]
    public void SaveScreenshotWhenTestFails() =>
        Screenshots.SaveWhenCurrentTestFailed(Driver);

    [OneTimeTearDown]
    public void CloseBrowser()
    {
        try
        {
            Driver?.Quit();
        }
        finally
        {
            Driver?.Dispose();
        }
    }
}
