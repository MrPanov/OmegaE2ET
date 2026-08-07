using NUnit.Framework;
using UiAutomation.Tests.Configuration;

namespace UiAutomation.Tests.Tests.Configuration;

[TestFixture]
[Category("Unit")]
public sealed class TestSettingsLoaderTests
{
    [Test]
    public void EnvironmentVariableOverridesActiveLocalEnvironment()
    {
        var local = new LocalTestSettings(
            "Production",
            new Dictionary<string, LocalEnvironmentSettings>());

        var settings = Load(new Dictionary<string, string?>
        {
            ["OMEGA_ENVIRONMENT"] = "Test"
        }, local);

        Assert.That(settings.EnvironmentName, Is.EqualTo("Test"));
        Assert.That(settings.BaseUrl, Is.EqualTo("https://test.omega.page/"));
    }

    [Test]
    public void BaseUrlEnvironmentVariableOverridesLocalProfileOnSameHost()
    {
        var local = TestLocalSettings(baseUrl: "https://test.omega.page/from-json/");

        var settings = Load(new Dictionary<string, string?>
        {
            ["BASE_URL"] = "https://test.omega.page/from-environment/"
        }, local);

        Assert.That(settings.BaseUrl, Is.EqualTo("https://test.omega.page/from-environment/"));
    }

    [Test]
    public void UnknownEnvironmentIsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Load(new()
        {
            ["OMEGA_ENVIRONMENT"] = "Staging"
        }));

        Assert.That(exception!.Message, Does.Contain("Unknown environment 'Staging'"));
    }

    [Test]
    public void ProductionWithoutExplicitConfirmationIsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Load(ProductionVariables(allowProduction: false), ProductionLocalSettings()));

        Assert.That(exception!.Message, Does.Contain("ALLOW_PRODUCTION_TESTS=true"));
    }

    [TestCase("not-a-url")]
    [TestCase("<production URL>")]
    public void InvalidBaseUrlIsRejected(string baseUrl)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Load(new()
        {
            ["BASE_URL"] = baseUrl
        }));

        Assert.That(exception!.Message, Does.Contain("Base URL is not configured"));
    }

    [Test]
    public void ProductionRequiresHttps()
    {
        var variables = ProductionVariables();
        variables["BASE_URL"] = "http://my.omega.page/";

        var exception = Assert.Throws<InvalidOperationException>(() =>
            Load(variables, ProductionLocalSettings()));

        Assert.That(exception!.Message, Does.Contain("must use HTTPS"));
    }

    [Test]
    public void BaseUrlHostMustMatchSelectedEnvironment()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Load(new()
        {
            ["OMEGA_ENVIRONMENT"] = "Test",
            ["BASE_URL"] = "https://my.omega.page/"
        }));

        Assert.That(exception!.Message, Does.Contain("does not match environment 'Test'"));
    }

    [Test]
    public void RequiredAuthenticationRejectsMissingPassword()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Load(new()
        {
            ["REQUIRE_AUTHENTICATION"] = "true"
        }));

        Assert.That(exception!.Message, Does.Contain("Login password is required"));
    }

    [Test]
    public void ProductionRequiresExplicitCredentialsAndSearchData()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Load(ProductionVariables(), localSettings: null));

        Assert.That(exception!.Message, Does.Contain("Search reference data are not configured"));
    }

    [Test]
    public void ExplicitProductionProfileLoadsAfterConfirmation()
    {
        var settings = Load(ProductionVariables(), ProductionLocalSettings());

        Assert.Multiple(() =>
        {
            Assert.That(settings.IsProduction, Is.True);
            Assert.That(settings.AllowProductionTests, Is.True);
            Assert.That(settings.BaseUrl, Is.EqualTo("https://my.omega.page/"));
            Assert.That(settings.LoginEmail, Is.EqualTo("automation-production@example.test"));
            Assert.That(settings.SearchData.IsConfigured, Is.True);
        });
    }

    [Test]
    public void InvalidBooleanSettingIsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Load(new()
        {
            ["HEADLESS"] = "sometimes"
        }));

        Assert.That(exception!.Message, Does.Contain("HEADLESS").And.Contain("true or false"));
    }

    private static TestSettings Load(
        Dictionary<string, string?> variables,
        LocalTestSettings? localSettings = null) =>
        TestSettingsLoader.Load(
            name => variables.TryGetValue(name, out var value) ? value : null,
            localSettings);

    private static Dictionary<string, string?> ProductionVariables(bool allowProduction = true) =>
        new()
        {
            ["OMEGA_ENVIRONMENT"] = "Production",
            ["ALLOW_PRODUCTION_TESTS"] = allowProduction.ToString()
        };

    private static LocalTestSettings TestLocalSettings(string baseUrl) => new(
        "Test",
        new Dictionary<string, LocalEnvironmentSettings>
        {
            ["Test"] = new(
                BaseUrl: baseUrl,
                SearchMinimumIntervalSeconds: null,
                LoginEmail: null,
                LoginPassword: null,
                Search: null)
        });

    private static LocalTestSettings ProductionLocalSettings() => new(
        "Production",
        new Dictionary<string, LocalEnvironmentSettings>
        {
            ["Production"] = new(
                BaseUrl: "https://my.omega.page/",
                SearchMinimumIntervalSeconds: 10,
                LoginEmail: "automation-production@example.test",
                LoginPassword: "production-test-secret",
                Search: ValidProductionSearchData())
        });

    private static LocalSearchTestData ValidProductionSearchData() => new(
        ProductCode: "PX100",
        ProductCard: "9000001",
        ProductDescription: "Production reference product description",
        ProductBrand: "Production Brand",
        AlternativeProductCode: "PX101",
        PartialDescription: "reference product",
        CyrillicQuery: "еталонний товар",
        CyrillicExpectedText: "товар",
        LatinQuery: "reference",
        LatinExpectedText: "reference",
        PunctuatedProductCode: "PX.100",
        MissingProductQuery: "missing-production-product-000",
        SearchPlaceholder: "Search products");
}
