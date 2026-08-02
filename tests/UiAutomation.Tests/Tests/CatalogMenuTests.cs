using NUnit.Framework;
using OpenQA.Selenium;
using UiAutomation.Tests.Configuration;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests;

[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
public sealed class CatalogMenuTests
{
    private IWebDriver _driver = null!;
    private TestSettings _settings = null!;
    private CatalogMenuPage _catalogMenu = null!;

    private static readonly string[] CatalogGroups =
    [
        "Підбір по авто",
        "Шини та диски",
        "Оливи і тех. рідини",
        "Спец. каталоги",
        "Інше"
    ];

    private static readonly string[] CatalogItems =
    [
        "VIN",
        "VIN TRUCK",
        "Запчастини для ТО",
        "Для легкових авто",
        "Для вантажних авто",
        "Шини",
        "Колісні диски",
        "Камери",
        "Оливи",
        "Тех. рідини",
        "ЗЧ до сільгосптехніки",
        "АКБ",
        "Кузов та оптика",
        "Лампи",
        "Підшипники",
        "Ремені Агро техніка",
        "Аварійні з'єднувачі",
        "Аксесуари / Обладнання / Інструмент",
        "Товари для господарських потреб / Побутова хімія / Товари для бізнесу / Канцтовари",
        "Туризм і риболовля",
        "Гаражне обладнання",
        "Корисні товари",
        "Подарунки"
    ];

    [OneTimeSetUp]
    public void LoginOnceAndOpenCatalogMenu()
    {
        _settings = TestSettings.FromEnvironment();

        Assume.That(
            _settings.LoginPassword,
            Is.Not.Empty,
            "Set the OMEGA_PASSWORD environment variable to run catalog tests.");

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

        _catalogMenu = new CatalogMenuPage(
            _driver,
            TimeSpan.FromSeconds(_settings.ExplicitWaitSeconds));
        _catalogMenu.OpenMenu();
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
    public void CloseBrowserAfterAllCatalogTests()
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
    public void CatalogButtonIsDisplayedAfterLogin()
    {
        Assert.That(_catalogMenu.IsCatalogButtonDisplayed, Is.True);
    }

    [Test]
    [Category("Smoke")]
    public void CatalogMenuCanBeExpandedAndCollapsed()
    {
        _catalogMenu.OpenMenu();
        Assert.That(_catalogMenu.IsMenuExpanded, Is.True);

        _catalogMenu.CloseMenu();
        Assert.That(_catalogMenu.IsMenuExpanded, Is.False);
    }

    [TestCaseSource(nameof(CatalogGroups))]
    [Category("Smoke")]
    public void CatalogMenuContainsExpectedGroup(string groupName)
    {
        Assert.That(
            _catalogMenu.IsGroupDisplayed(groupName),
            Is.True,
            $"Catalog group '{groupName}' is not displayed.");
    }

    [TestCaseSource(nameof(CatalogItems))]
    [Category("Smoke")]
    public void CatalogMenuContainsExpectedItem(string itemName)
    {
        Assert.That(
            _catalogMenu.IsCatalogItemDisplayed(itemName),
            Is.True,
            $"Catalog item '{itemName}' is not displayed.");
    }

    [TestCaseSource(nameof(CatalogItems))]
    [Category("Smoke")]
    public void CatalogItemCanBeSelected(string itemName)
    {
        Assert.That(
            _catalogMenu.SelectCatalog(itemName),
            Is.True,
            $"Catalog item '{itemName}' did not trigger navigation or close the menu.");
    }

    [TestCase("VIN", "#/app/modelsearch")]
    [TestCase("VIN TRUCK", "#/app/modelsearchtruck")]
    [TestCase("Запчастини для ТО", "#/app/maintenancesearch")]
    [TestCase("Для легкових авто", "#/app/carsCatalog")]
    [TestCase("Для вантажних авто", "#/app/trucksCatalog")]
    [TestCase("Подарунки", "#/app/catalogGifts")]
    [Category("Smoke")]
    public void DirectCatalogCanBeOpened(string itemName, string expectedRoute)
    {
        _catalogMenu.OpenCatalog(itemName, expectedRoute);

        Assert.That(_driver.Url, Does.Contain(expectedRoute).IgnoreCase);
    }
}
