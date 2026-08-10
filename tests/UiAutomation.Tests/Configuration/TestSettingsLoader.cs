using System.Globalization;
using System.Text.Json;
using NUnit.Framework;

namespace UiAutomation.Tests.Configuration;

internal static class TestSettingsLoader
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
        SearchPlaceholder: "VIN, Держ. номер, OE, найменування, картка, код",
        BrandQuery: "Фільтр оливний KNECHT-MAHLE",
        Vin: "WBSJF0C54JB283303",
        LicensePlate: "ке0666ан");

    /// <summary>
    /// Всё, что отличает одну среду от другой. Собрано в одном месте, чтобы
    /// добавление среды не требовало править ветвления по всему загрузчику.
    /// </summary>
    private sealed record EnvironmentProfile(
        string Name,
        string Host,
        string BaseUrl,
        string LoginEmail,
        int SearchIntervalSeconds,
        SearchTestData SearchData);

    private static readonly EnvironmentProfile TestProfile = new(
        Name: "Test",
        Host: "test.omega.page",
        BaseUrl: "https://test.omega.page/",
        LoginEmail: "web@omega-auto.biz",
        SearchIntervalSeconds: 5,
        SearchData: DefaultTestSearchData);

    private static readonly EnvironmentProfile ProductionProfile = new(
        Name: "Production",
        Host: "my.omega.page",
        BaseUrl: "https://my.omega.page/",
        LoginEmail: string.Empty,
        SearchIntervalSeconds: 10,
        SearchData: SearchTestData.Empty);

    private static readonly EnvironmentProfile[] Profiles = [TestProfile, ProductionProfile];

    public static TestSettings LoadFromProcess()
    {
        var localSettings = LoadLocalSettings();
        return Load(GetRunSetting, localSettings);
    }

    private static string? GetRunSetting(string name)
    {
        var testRunParameter = TestContext.Parameters.Get(name);
        return !string.IsNullOrWhiteSpace(testRunParameter)
            ? testRunParameter
            : Environment.GetEnvironmentVariable(name);
    }

    internal static TestSettings Load(
        Func<string, string?> getEnvironmentVariable,
        LocalTestSettings? localSettings = null)
    {
        string Get(string name, string fallback) =>
            getEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

        var requestedEnvironment = Get(
            "OMEGA_ENVIRONMENT",
            localSettings?.ActiveEnvironment ?? TestProfile.Name).Trim();
        var profile = Profiles.FirstOrDefault(candidate => string.Equals(
                candidate.Name,
                requestedEnvironment,
                StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Unknown environment '{requestedEnvironment}'. Supported values: " +
                $"{string.Join(", ", Profiles.Select(item => item.Name))}.");

        // Дальше используется каноническое имя профиля, а не то, как его написали.
        var environmentName = profile.Name;
        var isProductionEnvironment = profile == ProductionProfile;
        var localEnvironment = localSettings?.Environments?
            .FirstOrDefault(item => string.Equals(
                item.Key,
                environmentName,
                StringComparison.OrdinalIgnoreCase))
            .Value;
        var credentialsEnvironment = ResolveCredentialsEnvironment(
            localSettings,
            localEnvironment,
            environmentName);

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

        var baseUrl = Get("BASE_URL", string.IsNullOrWhiteSpace(localEnvironment?.BaseUrl)
            ? profile.BaseUrl
            : localEnvironment.BaseUrl);
        var loginEmail = Get(
            "OMEGA_EMAIL",
            FirstConfigured(
                localEnvironment?.LoginEmail,
                credentialsEnvironment?.LoginEmail,
                profile.LoginEmail));
        var loginPassword = Get(
            "OMEGA_PASSWORD",
            FirstConfigured(
                localEnvironment?.LoginPassword,
                credentialsEnvironment?.LoginPassword,
                string.Empty));
        var searchData = CreateSearchData(localEnvironment?.Search, profile.SearchData, Get);

        ValidateBaseUrl(profile, baseUrl);
        ValidateCredentials(
            environmentName,
            isProductionEnvironment,
            loginEmail,
            loginPassword,
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
                localEnvironment?.SearchMinimumIntervalSeconds ?? profile.SearchIntervalSeconds,
                minimum: 0,
                maximum: 60,
                Get),
            LoginEmail: loginEmail,
            LoginPassword: loginPassword,
            SearchData: searchData,
            AllowProductionTests: allowProductionTests,
            RequireAuthentication: requireAuthentication);
    }

    private static LocalEnvironmentSettings? ResolveCredentialsEnvironment(
        LocalTestSettings? localSettings,
        LocalEnvironmentSettings? localEnvironment,
        string environmentName)
    {
        var sourceName = localEnvironment?.CredentialsFromEnvironment?.Trim();
        if (string.IsNullOrWhiteSpace(sourceName)) return null;

        var source = localSettings?.Environments?
            .FirstOrDefault(item => string.Equals(
                item.Key,
                sourceName,
                StringComparison.OrdinalIgnoreCase))
            .Value;
        if (source is not null) return source;

        throw new InvalidOperationException(
            $"Credentials source environment '{sourceName}' configured for " +
            $"'{environmentName}' was not found in testsettings.local.json.");
    }

    private static string FirstConfigured(params string?[] values) =>
        values.FirstOrDefault(value =>
            value is not null && TestSettings.IsConfigured(value)) ?? string.Empty;

    /// <summary>
    /// Приоритет одинаков для всех полей: переменная окружения, затем локальный
    /// профиль, затем эталон среды. Поэтому правило вынесено в <c>Field</c>,
    /// а перечисление осталось плоским списком имён.
    /// </summary>
    private static SearchTestData CreateSearchData(
        LocalSearchTestData? local,
        SearchTestData fallback,
        Func<string, string, string> get)
    {
        string Field(string name, string? profileValue, string defaultValue) =>
            get($"SEARCH_{name}", profileValue ?? defaultValue);

        return new SearchTestData(
            ProductCode: Field("PRODUCT_CODE", local?.ProductCode, fallback.ProductCode),
            ProductCard: Field("PRODUCT_CARD", local?.ProductCard, fallback.ProductCard),
            ProductDescription: Field("PRODUCT_DESCRIPTION", local?.ProductDescription, fallback.ProductDescription),
            ProductBrand: Field("PRODUCT_BRAND", local?.ProductBrand, fallback.ProductBrand),
            AlternativeProductCode: Field("ALTERNATIVE_PRODUCT_CODE", local?.AlternativeProductCode, fallback.AlternativeProductCode),
            PartialDescription: Field("PARTIAL_DESCRIPTION", local?.PartialDescription, fallback.PartialDescription),
            CyrillicQuery: Field("CYRILLIC_QUERY", local?.CyrillicQuery, fallback.CyrillicQuery),
            CyrillicExpectedText: Field("CYRILLIC_EXPECTED_TEXT", local?.CyrillicExpectedText, fallback.CyrillicExpectedText),
            LatinQuery: Field("LATIN_QUERY", local?.LatinQuery, fallback.LatinQuery),
            LatinExpectedText: Field("LATIN_EXPECTED_TEXT", local?.LatinExpectedText, fallback.LatinExpectedText),
            PunctuatedProductCode: Field("PUNCTUATED_PRODUCT_CODE", local?.PunctuatedProductCode, fallback.PunctuatedProductCode),
            MissingProductQuery: Field("MISSING_PRODUCT_QUERY", local?.MissingProductQuery, fallback.MissingProductQuery),
            SearchPlaceholder: Field("PLACEHOLDER", local?.SearchPlaceholder, fallback.SearchPlaceholder),
            BrandQuery: Field("BRAND_QUERY", local?.BrandQuery, fallback.BrandQuery),
            Vin: Field("VIN", local?.Vin, fallback.Vin),
            LicensePlate: Field("LICENSE_PLATE", local?.LicensePlate, fallback.LicensePlate));
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

    private static void ValidateBaseUrl(EnvironmentProfile profile, string baseUrl)
    {
        if (!TestSettings.IsConfigured(baseUrl) ||
            !Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                $"Base URL is not configured for environment '{profile.Name}'.");
        }

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                $"Base URL for environment '{profile.Name}' must use HTTPS.");
        }

        if (!string.Equals(uri.Host, profile.Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Base URL host '{uri.Host}' does not match environment '{profile.Name}' " +
                $"(expected '{profile.Host}').");
        }
    }

    private static void ValidateCredentials(
        string environmentName,
        bool isProductionEnvironment,
        string loginEmail,
        string loginPassword,
        bool requireAuthentication)
    {
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
    LocalSearchTestData? Search,
    string? CredentialsFromEnvironment = null);

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
    string? SearchPlaceholder,
    string? BrandQuery = null,
    string? Vin = null,
    string? LicensePlate = null);
