using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace UiAutomation.Tests.Pages;

public sealed class LoginPage(IWebDriver driver, TimeSpan waitTimeout)
{
    private readonly WebDriverWait _wait = new(driver, waitTimeout);

    private IWebElement EmailInput =>
        _wait.Until(d => d.FindElement(By.Id("loginInputEmail")));

    private IWebElement PasswordInput =>
        driver.FindElement(By.Id("loginInputPassword"));

    private IWebElement LoginButton =>
        driver.FindElement(By.Id("buttonLogin"));

    private IWebElement ForgotPasswordLink =>
        driver.FindElement(By.LinkText("Забули пароль?"));

    public void Open(string baseUrl)
    {
        driver.Navigate().GoToUrl(baseUrl);
        _wait.Until(d =>
            HasVisibleElement(d, By.Id("loginInputEmail")) ||
            HasVisibleElement(d, By.Id("headerInputSearch")));
    }

    public void Login(string email, string password)
    {
        if (IsAlreadyAuthenticated) return;

        EnterCredentials(email, password);
        LoginButton.Click();
    }

    public void EnterCredentials(string email, string password)
    {
        EmailInput.Clear();
        EmailInput.SendKeys(email);
        PasswordInput.Clear();
        PasswordInput.SendKeys(password);
    }

    public void SubmitWithEnter() => PasswordInput.SendKeys(Keys.Enter);

    public bool WaitUntilAuthenticated() =>
        _wait.Until(d =>
            d.FindElements(By.Id("headerInputSearch"))
                .Any(element => element.Displayed));

    public bool IsAuthenticatedWithin(TimeSpan timeout)
    {
        try
        {
            return new WebDriverWait(driver, timeout).Until(d =>
                d.FindElements(By.Id("headerInputSearch"))
                    .Any(element => element.Displayed));
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public bool IsLoginFormDisplayed =>
        EmailInput.Displayed && PasswordInput.Displayed && LoginButton.Displayed;

    public bool IsAlreadyAuthenticated =>
        HasVisibleElement(driver, By.Id("headerInputSearch"));

    public bool IsEmailInputDisplayed => EmailInput.Displayed;

    public bool IsPasswordInputDisplayed => PasswordInput.Displayed;

    public bool IsLoginButtonDisplayed => LoginButton.Displayed;

    public bool IsForgotPasswordLinkDisplayed => ForgotPasswordLink.Displayed;

    public string PasswordInputType =>
        PasswordInput.GetAttribute("type") ?? string.Empty;

    private static bool HasVisibleElement(IWebDriver webDriver, By by) =>
        webDriver.FindElements(by).Any(element => element.Displayed);
}
