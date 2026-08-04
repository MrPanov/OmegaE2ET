using System.Text.Json;

namespace UiAutomation.Tests.Configuration;

public sealed record TestSettings(
    string EnvironmentName,
    string BaseUrl,
    string Browser,
    bool Headless,
    int ExplicitWaitSeconds,
    int SearchMinimumIntervalSeconds,
    string LoginEmail,
    string LoginPassword,
    SearchTestData SearchData)
{
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

    public bool IsProduction =>
        string.Equals(EnvironmentName, "Production", StringComparison.OrdinalIgnoreCase);

    public bool HasUsableLoginPassword => IsConfigured(LoginPassword);

    public static TestSettings FromEnvironment()
    {
        var localSettings = LoadLocalSettings();
        var environmentName = Get("OMEGA_ENVIRONMENT", localSettings?.ActiveEnvironment ?? "Test").Trim();
        var localEnvironment = localSettings?.Environments?
            .FirstOrDefault(item => string.Equals(
                item.Key,
                environmentName,
                StringComparison.OrdinalIgnoreCase))
            .Value;
        var isTestEnvironment = string.Equals(
            environmentName,
            "Test",
            StringComparison.OrdinalIgnoreCase);

        var baseUrl = Get(
            "BASE_URL",
            localEnvironment?.BaseUrl ?? (isTestEnvironment ? "https://test.omega.page/" : string.Empty));
        var loginEmail = Get(
            "OMEGA_EMAIL",
            localEnvironment?.LoginEmail ?? (isTestEnvironment ? "web@omega-auto.biz" : string.Empty));
        var loginPassword = Get(
            "OMEGA_PASSWORD",
            localEnvironment?.LoginPassword ?? string.Empty);
        var searchData = CreateSearchData(localEnvironment?.Search, isTestEnvironment);
        var searchMinimumIntervalSeconds = GetInt(
            "SEARCH_MIN_INTERVAL_SECONDS",
            localEnvironment?.SearchMinimumIntervalSeconds ?? (isTestEnvironment ? 5 : 10),
            minimum: 0,
            maximum: 60);

        ValidateSelectedEnvironment(
            environmentName,
            baseUrl,
            loginEmail,
            loginPassword,
            searchData);

        return new TestSettings(
            EnvironmentName: environmentName,
            BaseUrl: baseUrl,
            Browser: Get("BROWSER", "chrome").Trim().ToLowerInvariant(),
            Headless: bool.TryParse(Get("HEADLESS", "false"), out var headless) && headless,
            ExplicitWaitSeconds: GetInt("EXPLICIT_WAIT_SECONDS", 20, minimum: 1, maximum: 120),
            SearchMinimumIntervalSeconds: searchMinimumIntervalSeconds,
            LoginEmail: loginEmail,
            LoginPassword: loginPassword,
            SearchData: searchData);
    }

    private static SearchTestData CreateSearchData(
        LocalSearchTestData? local,
        bool isTestEnvironment)
    {
        var fallback = isTestEnvironment ? DefaultTestSearchData : SearchTestData.Empty;

        return new SearchTestData(
            ProductCode: Get("SEARCH_PRODUCT_CODE", local?.ProductCode ?? fallback.ProductCode),
            ProductCard: Get("SEARCH_PRODUCT_CARD", local?.ProductCard ?? fallback.ProductCard),
            ProductDescription: Get(
                "SEARCH_PRODUCT_DESCRIPTION",
                local?.ProductDescription ?? fallback.ProductDescription),
            ProductBrand: Get("SEARCH_PRODUCT_BRAND", local?.ProductBrand ?? fallback.ProductBrand),
            AlternativeProductCode: Get(
                "SEARCH_ALTERNATIVE_PRODUCT_CODE",
                local?.AlternativeProductCode ?? fallback.AlternativeProductCode),
            PartialDescription: Get(
                "SEARCH_PARTIAL_DESCRIPTION",
                local?.PartialDescription ?? fallback.PartialDescription),
            CyrillicQuery: Get(
                "SEARCH_CYRILLIC_QUERY",
                local?.CyrillicQuery ?? fallback.CyrillicQuery),
            CyrillicExpectedText: Get(
                "SEARCH_CYRILLIC_EXPECTED_TEXT",
                local?.CyrillicExpectedText ?? fallback.CyrillicExpectedText),
            LatinQuery: Get("SEARCH_LATIN_QUERY", local?.LatinQuery ?? fallback.LatinQuery),
            LatinExpectedText: Get(
                "SEARCH_LATIN_EXPECTED_TEXT",
                local?.LatinExpectedText ?? fallback.LatinExpectedText),
            PunctuatedProductCode: Get(
                "SEARCH_PUNCTUATED_PRODUCT_CODE",
                local?.PunctuatedProductCode ?? fallback.PunctuatedProductCode),
            MissingProductQuery: Get(
                "SEARCH_MISSING_PRODUCT_QUERY",
                local?.MissingProductQuery ?? fallback.MissingProductQuery),
            SearchPlaceholder: Get(
                "SEARCH_PLACEHOLDER",
                local?.SearchPlaceholder ?? fallback.SearchPlaceholder));
    }

    private static string Get(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

    private static int GetInt(string name, int fallback, int minimum, int maximum)
    {
        var raw = Get(name, fallback.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (!int.TryParse(raw, out var value) || value < minimum || value > maximum)
        {
            throw new InvalidOperationException(
                $"Setting '{name}' must be an integer from {minimum} to {maximum}.");
        }

        return value;
    }

    private static void ValidateSelectedEnvironment(
        string environmentName,
        string baseUrl,
        string loginEmail,
        string loginPassword,
        SearchTestData searchData)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                $"Base URL is not configured for environment '{environmentName}'.");
        }

        if (!searchData.IsConfigured)
        {
            throw new InvalidOperationException(
                $"Search reference data are not configured for environment '{environmentName}'.");
        }

        if (!string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase) &&
            (!IsConfigured(loginEmail) || !IsConfigured(loginPassword)))
        {
            throw new InvalidOperationException(
                $"Client credentials or search reference data are not configured for " +
                $"environment '{environmentName}'.");
        }
    }

    private static bool IsConfigured(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        !(value.StartsWith('<') && value.EndsWith('>'));

    private static LocalSettings? LoadLocalSettings()
    {
        var settingsPath = FindLocalSettingsPath();
        if (settingsPath is null)
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<LocalSettings>(json, new JsonSerializerOptions
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
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }
        }

        return null;
    }

    private sealed record LocalSettings(
        string? ActiveEnvironment,
        Dictionary<string, LocalEnvironmentSettings>? Environments);

    private sealed record LocalEnvironmentSettings(
        string? BaseUrl,
        int? SearchMinimumIntervalSeconds,
        string? LoginEmail,
        string? LoginPassword,
        LocalSearchTestData? Search);

    private sealed record LocalSearchTestData(
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
}

public sealed record SearchTestData(
    string ProductCode,
    string ProductCard,
    string ProductDescription,
    string ProductBrand,
    string AlternativeProductCode,
    string PartialDescription,
    string CyrillicQuery,
    string CyrillicExpectedText,
    string LatinQuery,
    string LatinExpectedText,
    string PunctuatedProductCode,
    string MissingProductQuery,
    string SearchPlaceholder)
{
    public static SearchTestData Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);

    public bool IsConfigured =>
        ProductCode.Length >= 2 &&
        new[]
        {
            ProductCode,
            ProductCard,
            ProductDescription,
            ProductBrand,
            AlternativeProductCode,
            PartialDescription,
            CyrillicQuery,
            CyrillicExpectedText,
            LatinQuery,
            LatinExpectedText,
            PunctuatedProductCode,
            MissingProductQuery,
            SearchPlaceholder
        }.All(value =>
            !string.IsNullOrWhiteSpace(value) &&
            !(value.StartsWith('<') && value.EndsWith('>')));
}
