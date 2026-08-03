using System.Text.Json;

namespace UiAutomation.Tests.Configuration;

public sealed record TestSettings(
    string BaseUrl,
    string Browser,
    bool Headless,
    int ExplicitWaitSeconds,
    string LoginEmail,
    string LoginPassword)
{
    public bool HasUsableLoginPassword =>
        !string.IsNullOrWhiteSpace(LoginPassword) &&
        !(LoginPassword.StartsWith('<') && LoginPassword.EndsWith('>'));

    public static TestSettings FromEnvironment()
    {
        var localSettings = LoadLocalSettings();
        var environmentName = Get("OMEGA_ENVIRONMENT", localSettings?.ActiveEnvironment ?? "Test");
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

        ValidateSelectedEnvironment(environmentName, baseUrl, loginEmail, loginPassword);

        return new TestSettings(
            BaseUrl: baseUrl,
            Browser: Get("BROWSER", "chrome").ToLowerInvariant(),
            Headless: bool.TryParse(Get("HEADLESS", "false"), out var headless) && headless,
            ExplicitWaitSeconds: int.TryParse(Get("EXPLICIT_WAIT_SECONDS", "20"), out var wait)
                ? wait
                : 20,
            LoginEmail: loginEmail,
            LoginPassword: loginPassword);
    }

    private static string Get(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

    private static void ValidateSelectedEnvironment(
        string environmentName,
        string baseUrl,
        string loginEmail,
        string loginPassword)
    {
        if (string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                $"Base URL is not configured for environment '{environmentName}'.");
        }

        if (string.IsNullOrWhiteSpace(loginEmail) || string.IsNullOrWhiteSpace(loginPassword))
        {
            throw new InvalidOperationException(
                $"Client credentials are not configured for environment '{environmentName}'.");
        }
    }

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
        string? LoginEmail,
        string? LoginPassword);
}
