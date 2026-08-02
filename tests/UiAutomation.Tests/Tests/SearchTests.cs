using NUnit.Framework;
using OpenQA.Selenium;
using UiAutomation.Tests.Configuration;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests;

[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Search")]
public sealed class SearchTests
{
    private const string ProductCode = "OC90";
    private const string ProductCard = "4610495";
    private const string ProductDescription =
        "Фільтр оливний LANOS, AVEO, LACETTI, NUBIRA, NEXIA (вир-во KNECHT-MAHLE)";
    private const string ProductBrand = "KNECHT/MAHLE";

    private IWebDriver _driver = null!;
    private SearchResultsPage _search = null!;

    [OneTimeSetUp]
    public void LoginOnce()
    {
        var settings = TestSettings.FromEnvironment();

        Assume.That(
            settings.HasUsableLoginPassword,
            Is.True);

        _driver = DriverFactory.Create(settings);
        var timeout = TimeSpan.FromSeconds(settings.ExplicitWaitSeconds);
        var loginPage = new LoginPage(_driver, timeout);
        loginPage.Open(settings.BaseUrl);

        if (!loginPage.IsAlreadyAuthenticated)
        {
            loginPage.Login(settings.LoginEmail, settings.LoginPassword);
        }

        Assert.That(loginPage.WaitUntilAuthenticated(), Is.True, "Authentication failed.");
        _search = new SearchResultsPage(_driver, timeout);
    }

    [TearDown]
    public void SaveScreenshotWhenTestFails()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status !=
            NUnit.Framework.Interfaces.TestStatus.Failed ||
            _driver is not ITakesScreenshot screenshotDriver)
        {
            return;
        }

        var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots");
        Directory.CreateDirectory(directory);
        var testName = string.Concat(TestContext.CurrentContext.Test.Name.Select(c =>
            Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var path = Path.Combine(directory, $"{testName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.png");
        screenshotDriver.GetScreenshot().SaveAsFile(path);
        TestContext.AddTestAttachment(path, "Screenshot on failure");
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
        }
    }

    [Test]
    [Category("Smoke")]
    public void SearchByLowercaseProductCodeReturnsExpectedProduct()
    {
        _search.Search("oc90");
        var product = _search.GetProduct(ProductCode);

        Assert.Multiple(() =>
        {
            Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по коду: 1"));
            Assert.That(product.Code, Is.EqualTo(ProductCode));
            Assert.That(product.Card, Is.EqualTo(ProductCard));
            Assert.That(product.Description, Is.EqualTo(ProductDescription));
            Assert.That(product.Brand, Is.EqualTo(ProductBrand));
        });
    }

    [Test]
    [Category("Smoke")]
    public void ProductCodeSearchIsCaseInsensitive()
    {
        _search.Search("oc90");
        var lowercaseResult = _search.GetProduct(ProductCode);

        _search.Search("OC90");
        var uppercaseResult = _search.GetProduct(ProductCode);

        Assert.That(uppercaseResult, Is.EqualTo(lowercaseResult));
    }

    [Test]
    [Category("Smoke")]
    public void SearchByCardReturnsExpectedProduct()
    {
        _search.Search(ProductCard);
        var product = _search.GetProduct(ProductCode);

        Assert.Multiple(() =>
        {
            Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по картці: 1"));
            Assert.That(product.Code, Is.EqualTo(ProductCode));
            Assert.That(product.Card, Is.EqualTo(ProductCard));
        });
    }

    [Test]
    [Category("Smoke")]
    public void MissingProductShowsEmptyResultAndClearsPreviousProducts()
    {
        _search.Search("oc90");
        Assert.That(_search.IsProductDisplayed(ProductCode), Is.True);

        _search.Search("zz-no-product-987654321");

        Assert.Multiple(() =>
        {
            Assert.That(_search.HasEmptyResult, Is.True);
            Assert.That(_search.IsProductDisplayed(ProductCode), Is.False);
        });
    }

    [Test]
    [Category("Smoke")]
    public void NewSearchReplacesPreviousResult()
    {
        _search.Search("oc90");
        Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по коду: 1"));

        _search.Search(ProductCard);

        Assert.Multiple(() =>
        {
            Assert.That(_search.Query, Is.EqualTo(ProductCard));
            Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по картці: 1"));
            Assert.That(_search.GetProduct(ProductCode).Card, Is.EqualTo(ProductCard));
        });
    }
}
