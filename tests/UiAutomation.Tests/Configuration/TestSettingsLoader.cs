using System.Globalization;
using System.Text.Json;

namespace UiAutomation.Tests.Configuration;

internal static class TestSettingsLoader
{
    private const string TestEnvironment = "Test";
    private const string ProductionEnvironment = "Production";
    private const string TestHost = "test.omega.page";
    private const string ProductionHost = "my.omega.page";
    private const string TestBaseUrl = "https://test.omega.page/";
    private const string ProductionBaseUrl = "https://my.omega.page/";
    private const string TestLoginEmail = "web@omega-auto.biz";

    private static readonly SearchTestData DefaultTestSearchData = new(
        ProductCode: "OC90",
        ProductCard: "4610495",
        ProductDescription:
            "Фільтр оливний LANOS, AVEO, LACETTI, NUBIRA, NEXIA (вир-во KNECHT-MAHLE)",
        ProductBrand: "KNECHT/MAHLE",
        AlternativeProductCode: "OC90OF",
        PartialDescription: "Фільтр оливний LANOS",
        CyrillicQuery: "Фільтр оливний",
        CyrillicExpectedText: "Фільтр",
        LatinQuery: "LANOS",
        LatinExpectedText: "LANOS",
        PunctuatedProductCode: "23.129.02",
        MissingProductQuery: "zz-no-product-987654321",
        SearchPlaceholder: "VIN, Держ. номер, OE, найменування, картка, код");

    public static TestSettings LoadFromProcess()
    {
        var localSettings = LoadLocalSettings();
        return Load(Environment.GetEnvironmentVariable, localSettings);
    }

    internal static TestSettings Load(
        Func<string, string?> getEnvironmentVariable,
        LocalTestSettings? localSettings = null)
    {
        string Get(string name, string fallback) =>
            getEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

        var environmentName = Get(
            "OMEGA_ENVIRONMENT",
            localSettings?.ActiveEnvironment ?? TestEnvironment).Trim();
        var isTestEnvironment = string.Equals(
            environmentName,
            TestEnvironment,
            StringComparison.OrdinalIgnoreCase);
        var isProductionEnvironment = string.Equals(
            environmentName,
            ProductionEnvironment,
            StringComparison.OrdinalIgnoreCase);

        if (!isTestEnvironment && !isProductionEnvironment)
        {
            throw new InvalidOperationException(
                $"Unknown environment '{environmentName}'. Supported values: Test, Production.");
        }

        environmentName = isProductionEnvironment ? ProductionEnvironment : TestEnvironment;
        var localEnvironment = localSettings?.Environments?
            .FirstOrDefault(item => string.Equals(
                item.Key,
                environmentName,
                StringComparison.OrdinalIgnoreCase))
            .Value;

        var allowProductionTests = GetBool(
            "ALLOW_PRODUCTION_TESTS",
            fallback: false,
            Get);
        var requireAuthentication = GetBool(
            "REQUIRE_AUTHENTICATION",
            fallback: false,
            Get);

        if (isProductionEnvironment && !allowProductionTests)
        {
            throw new InvalidOperationException(
                "Production tests are blocked. Set ALLOW_PRODUCTION_TESTS=true to confirm the run.");
        }

        var baseUrl = Get(
            "BASE_URL",
            localEnvironment?.BaseUrl ?? (isTestEnvironment ? TestBaseUrl : ProductionBaseUrl));
        var loginEmail = Get(
            "OMEGA_EMAIL",
            localEnvironment?.LoginEmail ?? (isTestEnvironment ? TestLoginEmail : string.Empty));
        var loginPassword = Get(
            "OMEGA_PASSWORD",
            localEnvironment?.LoginPassword ?? string.Empty);
        var searchData = CreateSearchData(localEnvironment?.Search, isTestEnvironment, Get);

        ValidateBaseUrl(environmentName, baseUrl);
        ValidateCredentialsAndSearchData(
            environmentName,
            isProductionEnvironment,
            loginEmail,
            loginPassword,
            searchData,
            requireAuthentication);

        return new TestSettings(
            EnvironmentName: environmentName,
            BaseUrl: NormalizeBaseUrl(baseUrl),
            Browser: Get("BROWSER", "chrome").Trim().ToLowerInvariant(),
            Headless: GetBool("HEADLESS", fallback: false, Get),
            ExplicitWaitSeconds: GetInt(
                "EXPLICIT_WAIT_SECONDS",
                fallback: 20,
                minimum: 1,
                maximum: 120,
                Get),
            SearchMinimumIntervalSeconds: GetInt(
                "SEARCH_MIN_INTERVAL_SECONDS",
                localEnvironment?.SearchMinimumIntervalSeconds ?? (isTestEnvironment ? 5 : 10),
                minimum: 0,
                maximum: 60,
                Get),
            LoginEmail: loginEmail,
            LoginPassword: loginPassword,
            SearchData: searchData,
            AllowProductionTests: allowProductionTests,
            RequireAuthentication: requireAuthentication);
    }

    private static SearchTestData CreateSearchData(
        LocalSearchTestData? local,
        bool isTestEnvironment,
        Func<string, string, string> get)
    {
        var fallback = isTestEnvironment ? DefaultTestSearchData : SearchTestData.Empty;

        return new SearchTestData(
            ProductCode: get("SEARCH_PRODUCT_CODE", local?.ProductCode ?? fallback.ProductCode),
            ProductCard: get("SEARCH_PRODUCT_CARD", local?.ProductCard ?? fallback.ProductCard),
            ProductDescription: get(
                "SEARCH_PRODUCT_DESCRIPTION",
                local?.ProductDescription ?? fallback.ProductDescription),
            ProductBrand: get("SEARCH_PRODUCT_BRAND", local?.ProductBrand ?? fallback.ProductBrand),
            AlternativeProductCode: get(
                "SEARCH_ALTERNATIVE_PRODUCT_CODE",
                local?.AlternativeProductCode ?? fallback.AlternativeProductCode),
            PartialDescription: get(
                "SEARCH_PARTIAL_DESCRIPTION",
                local?.PartialDescription ?? fallback.PartialDescription),
            CyrillicQuery: get(
                "SEARCH_CYRILLIC_QUERY",
                local?.CyrillicQuery ?? fallback.CyrillicQuery),
            CyrillicExpectedText: get(
                "SEARCH_CYRILLIC_EXPECTED_TEXT",
                local?.CyrillicExpectedText ?? fallback.CyrillicExpectedText),
            LatinQuery: get("SEARCH_LATIN_QUERY", local?.LatinQuery ?? fallback.LatinQuery),
            LatinExpectedText: get(
                "SEARCH_LATIN_EXPECTED_TEXT",
                local?.LatinExpectedText ?? fallback.LatinExpectedText),
            PunctuatedProductCode: get(
                "SEARCH_PUNCTUATED_PRODUCT_CODE",
                local?.PunctuatedProductCode ?? fallback.PunctuatedProductCode),
            MissingProductQuery: get(
                "SEARCH_MISSING_PRODUCT_QUERY",
                local?.MissingProductQuery ?? fallback.MissingProductQuery),
            SearchPlaceholder: get(
                "SEARCH_PLACEHOLDER",
                local?.SearchPlaceholder ?? fallback.SearchPlaceholder));
    }

    private static bool GetBool(
        string name,
        bool fallback,
        Func<string, string, string> get)
    {
        var raw = get(name, fallback.ToString(CultureInfo.InvariantCulture));
        if (!bool.TryParse(raw, out var value))
        {
            throw new InvalidOperationException($"Setting '{name}' must be true or false.");
        }

        return value;
    }

    private static int GetInt(
        string name,
        int fallback,
        int minimum,
        int maximum,
        Func<string, string, string> get)
    {
        var raw = get(name, fallback.ToString(CultureInfo.InvariantCulture));
        if (!int.TryParse(raw, CultureInfo.InvariantCulture, out var value) ||
            value < minimum ||
            value > maximum)
        {
            throw new InvalidOperationException(
                $"Setting '{name}' must be an integer from {minimum} to {maximum}.");
        }

        return value;
    }

    private static void ValidateBaseUrl(string environmentName, string baseUrl)
    {
        if (!TestSettings.IsConfigured(baseUrl) ||
            !Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                $"Base URL is not configured for environment '{environmentName}'.");
        }

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                $"Base URL for environment '{environmentName}' must use HTTPS.");
        }

        var expectedHost = string.Equals(
            environmentName,
            ProductionEnvironment,
            StringComparison.OrdinalIgnoreCase)
            ? ProductionHost
            : TestHost;
        if (!string.Equals(uri.Host, expectedHost, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Base URL host '{uri.Host}' does not match environment '{environmentName}' " +
                $"(expected '{expectedHost}').");
        }
    }

    private static void ValidateCredentialsAndSearchData(
        string environmentName,
        bool isProductionEnvironment,
        string loginEmail,
        string loginPassword,
        SearchTestData searchData,
        bool requireAuthentication)
    {
        if (!searchData.IsConfigured)
        {
            throw new InvalidOperationException(
                $"Search reference data are not configured for environment '{environmentName}'.");
        }

        if (isProductionEnvironment && !TestSettings.IsConfigured(loginEmail))
        {
            throw new InvalidOperationException(
                "A dedicated production login must be configured explicitly.");
        }

        if ((isProductionEnvironment || requireAuthentication) &&
            !TestSettings.IsConfigured(loginPassword))
        {
            throw new InvalidOperationException(
                $"Login password is required for environment '{environmentName}'.");
        }
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        var builder = new UriBuilder(baseUrl) { Fragment = string.Empty, Query = string.Empty };
        if (!builder.Path.EndsWith('/')) builder.Path += "/";
        return builder.Uri.AbsoluteUri;
    }

    private static LocalTestSettings? LoadLocalSettings()
    {
        var settingsPath = FindLocalSettingsPath();
        if (settingsPath is null) return null;

        try
        {
            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<LocalTestSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Invalid local test settings file: {settingsPath}",
                exception);
        }
    }

    private static string? FindLocalSettingsPath()
    {
        var checkedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(startPath);
            while (directory is not null && checkedDirectories.Add(directory.FullName))
            {
                var candidate = Path.Combine(directory.FullName, "testsettings.local.json");
                if (File.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }
        }

        return null;
    }
}

internal sealed record LocalTestSettings(
    string? ActiveEnvironment,
    Dictionary<string, LocalEnvironmentSettings>? Environments);

internal sealed record LocalEnvironmentSettings(
    string? BaseUrl,
    int? SearchMinimumIntervalSeconds,
    string? LoginEmail,
    string? LoginPassword,
    LocalSearchTestData? Search);

internal sealed record LocalSearchTestData(
    string? ProductCode,
    string? ProductCard,
    string? ProductDescription,
    string? ProductBrand,
    string? AlternativeProductCode,
    string? PartialDescription,
    string? CyrillicQuery,
    string? CyrillicExpectedText,
    string? LatinQuery,
    string? LatinExpectedText,
    string? PunctuatedProductCode,
    string? MissingProductQuery,
    string? SearchPlaceholder);
