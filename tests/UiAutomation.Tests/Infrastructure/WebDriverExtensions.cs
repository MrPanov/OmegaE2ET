using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace UiAutomation.Tests.Infrastructure;

internal static class WebDriverExtensions
{
    /// <summary>
    /// Оверлей, которым приложение закрывает страницу на время загрузки раздела.
    /// </summary>
    private static readonly By BlockingOverlayBy = By.CssSelector("div.block-ui-overlay");

    /// <summary>
    /// Считает незавершённые запросы приложения. Возвращает <c>0</c>, если
    /// спросить не у кого — страница ещё не подняла Angular.
    /// </summary>
    private const string PendingRequestCountScript = """
        try {
          if (!window.angular) return 0;
          var injector = angular.element(document.body).injector();
          if (!injector) return 0;
          return injector.get('$http').pendingRequests.length;
        } catch (error) {
          return 0;
        }
        """;

    /// <summary>
    /// Ждёт, пока приложение догрузит текущий раздел: снимет оверлей загрузки
    /// и завершит все запросы страницы.
    /// </summary>
    /// <remarks>
    /// Смена адреса — не признак того, что раздел готов: маршрут переключается
    /// сразу, а содержимое приезжает позже. Одного оверлея тоже мало — он
    /// снимается между запросами, и проверка успевает посмотреть на полупустую
    /// страницу. Поэтому ждём обоих условий сразу: приложение на AngularJS,
    /// и число незавершённых запросов у него можно спросить напрямую.
    /// </remarks>
    public static void WaitUntilSectionIsLoaded(this IWebDriver driver, TimeSpan timeout) =>
        new WebDriverWait(driver, timeout).Until(d =>
            !d.IsVisible(BlockingOverlayBy) && d.PendingRequestCount() == 0);

    private static long PendingRequestCount(this IWebDriver driver)
    {
        var pending = ((IJavaScriptExecutor)driver).ExecuteScript(PendingRequestCountScript);
        return pending is long count ? count : 0;
    }

    /// <summary>
    /// Ждёт появления элемента и говорит, дождался ли. Таймаут гасится: там, где
    /// отсутствие элемента и есть предмет проверки, падать должно утверждение
    /// теста с внятным текстом, а не ожидание внутри страницы.
    /// </summary>
    public static bool WaitUntilVisible(this IWebDriver driver, By by, TimeSpan timeout)
    {
        try
        {
            return new WebDriverWait(driver, timeout).Until(d => d.IsVisible(by));
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

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
