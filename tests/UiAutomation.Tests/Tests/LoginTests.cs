using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests;

[TestFixture]
[NonParallelizable]
public sealed class LoginTests : UiTestBase
{
    private LoginPage OpenLoginPage()
    {
        var page = new LoginPage(
            Driver,
            TimeSpan.FromSeconds(Settings.ExplicitWaitSeconds));
        page.Open(Settings.BaseUrl);
        return page;
    }

    private void RequirePassword() =>
        Assume.That(
            Settings.HasUsableLoginPassword,
            Is.True);

    [Test]
    [Category("Smoke")]
    [Category("Authentication")]
    public void UserCanLoginWithValidCredentials()
    {
        RequirePassword();
        var loginPage = OpenLoginPage();

        loginPage.Login(Settings.LoginEmail, Settings.LoginPassword);

        Assert.That(loginPage.WaitUntilAuthenticated(), Is.True);
    }

    [Test]
    [Category("Smoke")]
    [Category("Authentication")]
    public void LoginPageDisplaysRequiredControls()
    {
        var loginPage = OpenLoginPage();

        Assert.Multiple(() =>
        {
            Assert.That(loginPage.IsEmailInputDisplayed, Is.True, "Email input is not displayed.");
            Assert.That(loginPage.IsPasswordInputDisplayed, Is.True, "Password input is not displayed.");
            Assert.That(loginPage.IsLoginButtonDisplayed, Is.True, "Login button is not displayed.");
            Assert.That(loginPage.IsForgotPasswordLinkDisplayed, Is.True, "Forgot password link is not displayed.");
        });
    }

    [Test]
    [Category("Authentication")]
    [Category("Security")]
    public void PasswordFieldMasksEnteredValue()
    {
        var loginPage = OpenLoginPage();

        Assert.That(loginPage.PasswordInputType, Is.EqualTo("password"));
    }

    [Test]
    [Category("Smoke")]
    [Category("Authentication")]
    public void EmptyCredentialsDoNotAuthenticateUser()
    {
        var loginPage = OpenLoginPage();

        loginPage.Login(string.Empty, string.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(loginPage.IsAuthenticatedWithin(TimeSpan.FromSeconds(3)), Is.False);
            Assert.That(loginPage.IsLoginFormDisplayed, Is.True);
        });
    }

    [Test]
    [Category("Smoke")]
    [Category("Authentication")]
    public void InvalidCredentialsDoNotAuthenticateUser()
    {
        var loginPage = OpenLoginPage();
        var unknownEmail = $"missing-{Guid.NewGuid():N}@example.invalid";

        loginPage.Login(unknownEmail, "invalid-password");

        Assert.Multiple(() =>
        {
            Assert.That(loginPage.IsAuthenticatedWithin(TimeSpan.FromSeconds(3)), Is.False);
            Assert.That(loginPage.IsLoginFormDisplayed, Is.True);
        });
    }

    [Test]
    [Category("Authentication")]
    public void PressingEnterSubmitsValidCredentials()
    {
        RequirePassword();
        var loginPage = OpenLoginPage();

        loginPage.EnterCredentials(Settings.LoginEmail, Settings.LoginPassword);
        loginPage.SubmitWithEnter();

        Assert.That(loginPage.WaitUntilAuthenticated(), Is.True);
    }
}
