using System.Drawing;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using UiAutomation.Tests.Configuration;

namespace UiAutomation.Tests.Infrastructure;

public static class DriverFactory
{
    public static IWebDriver Create(TestSettings settings)
    {
        IWebDriver driver = settings.Browser switch
        {
            "chrome" => CreateChrome(settings.Headless),
            "edge" => CreateEdge(settings.Headless),
            "firefox" => CreateFirefox(settings.Headless),
            _ => throw new ArgumentOutOfRangeException(
                nameof(settings.Browser), settings.Browser, "Supported browsers: chrome, edge, firefox.")
        };

        driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
        driver.Manage().Window.Size = new Size(1440, 900);
        return driver;
    }

    private static ChromeDriver CreateChrome(bool headless)
    {
        var options = new ChromeOptions();
        AddCommonArguments(options, headless);
        return new ChromeDriver(options);
    }

    private static EdgeDriver CreateEdge(bool headless)
    {
        var options = new EdgeOptions();
        if (headless) options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        return new EdgeDriver(options);
    }

    private static FirefoxDriver CreateFirefox(bool headless)
    {
        var options = new FirefoxOptions();
        if (headless) options.AddArgument("-headless");
        return new FirefoxDriver(options);
    }

    private static void AddCommonArguments(ChromiumOptions options, bool headless)
    {
        if (headless) options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-features=PasswordLeakDetection");
        options.AddUserProfilePreference("credentials_enable_service", false);
        options.AddUserProfilePreference("profile.password_manager_enabled", false);
        options.AddUserProfilePreference("profile.password_manager_leak_detection", false);
        options.SetLoggingPreference(LogType.Performance, LogLevel.All);
    }
}
