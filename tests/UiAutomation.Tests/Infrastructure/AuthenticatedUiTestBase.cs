using NUnit.Framework;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Infrastructure;

/// <summary>
/// Creates and authenticates a fresh browser for every test. Use this base for
/// state-changing or critical scenarios that must not share a browser session.
/// </summary>
public abstract class AuthenticatedUiTestBase : UiTestBase
{
    protected TimeSpan Timeout { get; private set; }

    [SetUp]
    public void Authenticate()
    {
        Timeout = TimeSpan.FromSeconds(Settings.ExplicitWaitSeconds);
        var loginPage = new LoginPage(Driver, Timeout);
        loginPage.Open(Settings.BaseUrl);

        if (!loginPage.IsAlreadyAuthenticated)
        {
            loginPage.Login(Settings.LoginEmail, Settings.LoginPassword);
        }

        Assert.That(loginPage.WaitUntilAuthenticated(), Is.True, "Authentication failed.");
    }
}
