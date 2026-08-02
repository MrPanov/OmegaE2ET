using NUnit.Framework;
using OpenQA.Selenium;
using UiAutomation.Tests.Configuration;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests;

[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("MainMenu")]
public sealed class MainMenuTests
{
    private IWebDriver _driver = null!;
    private TestSettings _settings = null!;
    private MainMenuPage _mainMenu = null!;

    private static readonly string[] MenuSections =
    [
        "Звіти",
        "Журнали",
        "Інше"
    ];

    private static readonly (string Name, string? Route)[] MenuItems =
    [
        ("Деб. заборгованість", "#/app/receivablesList"),
        ("Взаєморозрахунки", "#/app/mutualSettlementsList"),
        ("Новий товар", "#/app/simplesearch"),
        ("Розпродаж", "#/app/simplesearch"),
        ("Вибрані товари", "#/app/simplesearch"),
        ("Відвантажені товари", "#/app/simplesearch"),
        ("Залишки по ВЗ", "#/app/safeStorage"),
        ("Рахунки", "#/app/basket"),
        ("Видаткові накладні", "#/app/expenseList"),
        ("Податкові накладні", "#/app/taxInvoiceList"),
        ("Коригування до податкових накладних", "#/app/taxInvoiceChangeList"),
        ("Посилки", "#/app/sendbox"),
        ("Повернення", "#/app/claimsList"),
        ("Заявки АМ", "#/app/assortmentMatrixList"),
        ("Облік закупівель", "#/app/purchase"),
        ("Зворотний звʼязок", "#/app/ticket"),
        ("Запити", "#/app/requestList"),
        ("Аукціон", "#/app/auction"),
        ("Кошик повернень", "#/app/claimsBasket"),
        ("Прайс-листи", "#/app/prices"),
        ("Документи", "#/app/documents"),
        ("EДО", null)
    ];

    private static readonly string[] EdoMenuItems =
    [
        "Підписати ЕЦП",
        "Підключення до ЕДО",
        "Проблема з ЕДО",
        "Запитання ЕДО"
    ];

    public static IEnumerable<TestCaseData> MenuItemNames =>
        MenuItems.Select(item =>
            new TestCaseData(item.Name).SetName($"MenuContains_{item.Name}"));

    public static IEnumerable<TestCaseData> RoutedMenuItems =>
        MenuItems
            .Where(item => item.Route is not null)
            .Select(item =>
                new TestCaseData(item.Name, item.Route!)
                    .SetName($"MenuOpens_{item.Name}"));

    [OneTimeSetUp]
    public void LoginOnceAndOpenMainMenu()
    {
        _settings = TestSettings.FromEnvironment();

        Assume.That(
            _settings.HasUsableLoginPassword,
            Is.True);

        _driver = DriverFactory.Create(_settings);

        var loginPage = new LoginPage(
            _driver,
            TimeSpan.FromSeconds(_settings.ExplicitWaitSeconds));
        loginPage.Open(_settings.BaseUrl);

        if (!loginPage.IsAlreadyAuthenticated)
        {
            loginPage.Login(_settings.LoginEmail, _settings.LoginPassword);
        }

        Assert.That(loginPage.WaitUntilAuthenticated(), Is.True, "Authentication failed.");

        _mainMenu = new MainMenuPage(
            _driver,
            TimeSpan.FromSeconds(_settings.ExplicitWaitSeconds));
    }

    [TearDown]
    public void SaveScreenshotWhenTestFails()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status !=
            NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            return;
        }

        if (_driver is not ITakesScreenshot screenshotDriver) return;

        var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots");
        Directory.CreateDirectory(directory);
        var testName = string.Concat(
            TestContext.CurrentContext.Test.Name.Select(c =>
                Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var path = Path.Combine(directory, $"{testName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.png");
        screenshotDriver.GetScreenshot().SaveAsFile(path);
        TestContext.AddTestAttachment(path, "Screenshot on failure");
    }

    [OneTimeTearDown]
    public void CloseBrowserAfterAllMainMenuTests()
    {
        try
        {
            _driver?.Quit();
        }
        finally
        {
            _driver?.Dispose();
        }
    }

    [Test]
    [Category("Smoke")]
    public void MainMenuButtonIsDisplayedAfterLogin()
    {
        Assert.That(_mainMenu.IsMenuButtonDisplayed, Is.True);
    }

    [Test]
    [Category("Smoke")]
    public void MainMenuCanBeExpandedAndCollapsed()
    {
        _mainMenu.OpenMenu();
        Assert.That(_mainMenu.IsMenuExpanded, Is.True);

        _mainMenu.CloseMenu();
        Assert.That(_mainMenu.IsMenuExpanded, Is.False);
    }

    [TestCaseSource(nameof(MenuSections))]
    [Category("Smoke")]
    public void MainMenuContainsExpectedSection(string sectionName)
    {
        Assert.That(
            _mainMenu.IsSectionDisplayed(sectionName),
            Is.True,
            $"Main menu section '{sectionName}' is not displayed.");
    }

    [TestCaseSource(nameof(MenuItemNames))]
    [Category("Smoke")]
    public void MainMenuContainsExpectedItem(string itemName)
    {
        Assert.That(
            _mainMenu.IsMenuItemDisplayed(itemName),
            Is.True,
            $"Main menu item '{itemName}' is not displayed.");
    }

    [TestCaseSource(nameof(RoutedMenuItems))]
    [Category("Smoke")]
    public void MainMenuItemCanBeOpened(string itemName, string expectedRoute)
    {
        _mainMenu.OpenMenuItem(itemName, expectedRoute);

        Assert.That(_driver.Url, Does.Contain(expectedRoute).IgnoreCase);
    }

    [Test]
    [Category("Smoke")]
    public void EdoMenuCanBeExpanded()
    {
        _mainMenu.OpenSubmenu("EДО");

        Assert.Multiple(() =>
        {
            foreach (var itemName in EdoMenuItems)
            {
                Assert.That(
                    _mainMenu.IsSubmenuItemDisplayed(itemName),
                    Is.True,
                    $"EДО submenu item '{itemName}' is not displayed.");
            }
        });
    }
}
