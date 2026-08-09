using NUnit.Framework;
using OpenQA.Selenium;
using UiAutomation.Tests.Configuration;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests.Basket;

/// <summary>
/// Один браузер и один вход на весь набор сценариев корзины.
/// </summary>
/// <remarks>
/// Раньше каждая фикстура поднимала свой Chrome и логинилась заново. Пять входов
/// подряд сервер выдерживал не всегда: последняя фикстура регулярно упиралась
/// в таймаут аутентификации, причём падение зависело не от кода, а от нагрузки.
/// Здесь вход выполняется однократно, а изоляцию между тестами обеспечивает
/// открытие корзины заново в каждом <c>SetUp</c> и уборка добавленных позиций.
/// </remarks>
[SetUpFixture]
public sealed class BasketSession
{
    private static IWebDriver? _driver;

    internal static IWebDriver Driver => _driver
        ?? throw new InvalidOperationException("Сессия корзины не инициализирована.");

    internal static TestSettings Settings { get; private set; } = null!;

    internal static TimeSpan Timeout { get; private set; }

    [OneTimeSetUp]
    public void LoginOnce()
    {
        Settings = TestSettings.FromEnvironment();

        Assume.That(
            Settings.HasUsableLoginPassword,
            Is.True,
            "Set OMEGA_PASSWORD or configure loginPassword in testsettings.local.json.");

        Timeout = TimeSpan.FromSeconds(Settings.ExplicitWaitSeconds);
        _driver = DriverFactory.Create(Settings);

        var loginPage = new LoginPage(_driver, Timeout);
        loginPage.Open(Settings.BaseUrl);

        if (!loginPage.IsAlreadyAuthenticated)
        {
            loginPage.Login(Settings.LoginEmail, Settings.LoginPassword);
        }

        Assert.That(loginPage.WaitUntilAuthenticated(), Is.True, "Authentication failed.");
    }

    [OneTimeTearDown]
    public void CloseBrowser()
    {
        try
        {
            _driver?.Quit();
        }
        finally
        {
            _driver?.Dispose();
            _driver = null;
        }
    }
}
