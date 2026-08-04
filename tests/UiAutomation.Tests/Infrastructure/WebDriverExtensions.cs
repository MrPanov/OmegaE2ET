using OpenQA.Selenium;

namespace UiAutomation.Tests.Infrastructure;

internal static class WebDriverExtensions
{
    public static bool IsVisible(this IWebDriver driver, By by)
    {
        try
        {
            return driver.FindElements(by).Any(element => element.Displayed);
        }
        catch (StaleElementReferenceException)
        {
            return false;
        }
    }

    public static bool IsStale(this IWebElement element)
    {
        try
        {
            _ = element.Enabled;
            return false;
        }
        catch (StaleElementReferenceException)
        {
            return true;
        }
    }

    public static IReadOnlyList<string> VisibleTexts(this IWebDriver driver, By by) =>
        driver.FindElements(by)
            .Where(element => element.Displayed)
            .Select(element => UiText.NormalizeWhitespace(element.Text))
            .Where(text => text.Length > 0)
            .ToArray();

    public static void ClickRobustly(this IWebDriver driver, IWebElement element)
    {
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block: 'center'});",
            element);

        try
        {
            element.Click();
        }
        catch (Exception exception)
            when (exception is ElementClickInterceptedException or ElementNotInteractableException)
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
        }
    }
}
