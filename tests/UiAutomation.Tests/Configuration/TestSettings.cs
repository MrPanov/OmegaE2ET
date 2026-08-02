namespace UiAutomation.Tests.Configuration;

public sealed record TestSettings(
    string BaseUrl,
    string Browser,
    bool Headless,
    int ExplicitWaitSeconds,
    string LoginEmail,
    string LoginPassword)
{
    public static TestSettings FromEnvironment() => new(
        BaseUrl: Get("BASE_URL", "https://test.omega.page/"),
        Browser: Get("BROWSER", "chrome").ToLowerInvariant(),
        Headless: bool.TryParse(Get("HEADLESS", "false"), out var headless) && headless,
        ExplicitWaitSeconds: int.TryParse(Get("EXPLICIT_WAIT_SECONDS", "20"), out var wait) ? wait : 20,
        LoginEmail: Get("OMEGA_EMAIL", "web@omega-auto.biz"),
        LoginPassword: Get("OMEGA_PASSWORD", string.Empty));

    private static string Get(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;
}
