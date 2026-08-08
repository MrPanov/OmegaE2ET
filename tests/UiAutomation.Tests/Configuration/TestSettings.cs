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
    SearchTestData SearchData,
    bool AllowProductionTests,
    bool RequireAuthentication)
{
    public bool IsProduction =>
        string.Equals(EnvironmentName, "Production", StringComparison.OrdinalIgnoreCase);

    public bool HasUsableLoginPassword => IsConfigured(LoginPassword);

    public static TestSettings FromEnvironment() => TestSettingsLoader.LoadFromProcess();

    internal static bool IsConfigured(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        !(value.StartsWith('<') && value.EndsWith('>'));
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
        }.All(TestSettings.IsConfigured);
}
