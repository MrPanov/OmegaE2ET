using OpenQA.Selenium;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UiAutomation.Tests.Pages;

public sealed class SearchResultsPage(IWebDriver driver, TimeSpan waitTimeout)
{
    private readonly WebDriverWait _wait = new(driver, waitTimeout);
    private DateTime _lastSearchStartedUtc = DateTime.MinValue;

    private static readonly TimeSpan MinimumSearchInterval = TimeSpan.FromSeconds(5);

    private static readonly By SearchInputBy = By.Id("headerInputSearch");
    private static readonly By ClearSearchBy = By.CssSelector(".navbar-input-search .removeIcon");
    private static readonly By StartsWithCheckboxBy = By.Id("searchBeginWith");
    private static readonly By StartsWithLabelBy = By.CssSelector("label.label-search");
    private static readonly By HistoryButtonBy =
        By.CssSelector("a[ng-click='onclickBut(1)']");
    private static readonly By HistoryContainerBy =
        By.CssSelector(".history-allSearch-container");
    private static readonly By HistoryItemsBy =
        By.CssSelector(".history-allSearch-container #search.active li[ng-mousedown]");
    private static readonly By BlockingOverlayBy = By.CssSelector("div.block-ui-overlay");
    private static readonly By SearchRateLimitBy = By.XPath(
        "//*[contains(normalize-space(.), 'Ви перевищили ліміт пошукових запитів.')]");
    private static readonly By EmptyResultBy = By.XPath(
        "//*[@role='alert']//*[contains(normalize-space(.), " +
        "'За Вашим пошуковим запитом нічого не знайдено.')]");
    private static readonly By ResultSummaryBy = By.XPath(
        "//*[starts-with(normalize-space(.), 'Знайдено по ')]");
    private static readonly By StandardResultBy =
        By.CssSelector("[ng-repeat='item in searchresult.Items']");
    private static readonly By StartsWithSummaryBy = By.XPath(
        "//*[contains(normalize-space(.), 'За цим кодом знайдено брендів:')]");
    private static readonly By StartsWithCodesBy = By.XPath(
        "//table//tbody//tr/td[3]//a[normalize-space(.)]");

    public bool SupportsPerformanceLog => driver is ChromiumDriver;

    public bool SupportsClipboardPaste => driver is ChromiumDriver;

    public string Query => SearchInput.GetAttribute("value") ?? string.Empty;

    public string SearchPlaceholder => SearchInput.GetAttribute("placeholder") ?? string.Empty;

    public string ResultSummary => NormalizeWhitespace(VisibleElement(ResultSummaryBy).Text);

    public bool HasEmptyResult => IsVisible(driver, EmptyResultBy);

    public bool IsInputUsable => SearchInput.Displayed && SearchInput.Enabled;

    public bool IsLoading => IsVisible(driver, BlockingOverlayBy);

    public bool IsStartsWithEnabled =>
        driver.FindElement(StartsWithCheckboxBy).Selected;

    public IReadOnlyList<string> ProductCodes => driver.FindElements(
            By.CssSelector("a.searchProdCard"))
        .Where(element => element.Displayed)
        .Select(element => NormalizeWhitespace(element.Text))
        .Where(text => text.Length > 0)
        .ToArray();

    public IReadOnlyList<string> ProductDescriptions => driver.FindElements(
            By.CssSelector(".searchDescrip"))
        .Where(element => element.Displayed)
        .Select(element => NormalizeWhitespace(element.Text))
        .Where(text => text.Length > 0)
        .ToArray();

    public IReadOnlyList<string> ProductBrands => driver.FindElements(
            By.CssSelector(".brandSearch"))
        .Where(element => element.Displayed)
        .Select(element => NormalizeWhitespace(element.Text))
        .Where(text => text.Length > 0)
        .ToArray();

    public IReadOnlyList<string> StartsWithCodes => driver.FindElements(StartsWithCodesBy)
        .Where(element => element.Displayed)
        .Select(element => NormalizeWhitespace(element.Text))
        .Where(text => text.Length > 0)
        .ToArray();

    public void Search(string query)
    {
        WaitUntilPageIsReady();

        var previousResult = FindVisibleStateElement();

        ReplaceQuery(query);
        WaitForSearchSlot();
        SearchInput.SendKeys(Keys.Enter);

        WaitForNewResult(previousResult);
    }

    public void SearchRapidly(string firstQuery, string secondQuery)
    {
        WaitUntilPageIsReady();
        var previousResult = FindVisibleStateElement();

        ReplaceQuery(firstQuery);
        WaitForSearchSlot();
        SearchInput.SendKeys(Keys.Enter);
        ReplaceQuery(secondQuery);
        SearchInput.SendKeys(Keys.Enter);
        _lastSearchStartedUtc = DateTime.UtcNow;

        WaitForNewResult(previousResult);
        _wait.Until(_ => string.Equals(Query, secondQuery, StringComparison.Ordinal));
    }

    public void TypeQuery(string query)
    {
        WaitUntilPageIsReady();
        ReplaceQuery(query);
    }

    public void ReplaceWithCtrlAAndSearch(string query)
    {
        WaitUntilPageIsReady();
        var previousResult = FindVisibleStateElement();
        ReplaceQuery(query);
        WaitForSearchSlot();
        SearchInput.SendKeys(Keys.Enter);
        WaitForNewResult(previousResult);
    }

    public void SubmitWithoutWaiting(string query)
    {
        WaitUntilPageIsReady();
        ReplaceQuery(query);
        if (query.Trim().Length > 0)
        {
            WaitForSearchSlot();
        }
        SearchInput.SendKeys(Keys.Enter);
    }

    public void WaitForIdle(TimeSpan timeout)
    {
        new WebDriverWait(driver, timeout).Until(d => !IsVisible(d, BlockingOverlayBy));
    }

    public void ClearQueryWithButton()
    {
        var clearButton = _wait.Until(d => d.FindElements(ClearSearchBy)
            .FirstOrDefault(element => element.Displayed && element.Enabled));
        clearButton.Click();
        _wait.Until(_ => Query.Length == 0);
    }

    public void SetStartsWith(bool enabled)
    {
        WaitUntilPageIsReady();
        CloseHistory();
        if (IsStartsWithEnabled == enabled) return;

        var label = _wait.Until(d => d.FindElements(StartsWithLabelBy)
            .FirstOrDefault(element => element.Displayed && element.Enabled));
        label.Click();
        _wait.Until(_ => IsStartsWithEnabled == enabled);
    }

    public void CloseHistory()
    {
        if (!IsVisible(driver, HistoryContainerBy)) return;

        var button = _wait.Until(d => d.FindElements(HistoryButtonBy)
            .FirstOrDefault(element => element.Displayed && element.Enabled));
        button.Click();
        _wait.Until(d => !IsVisible(d, HistoryContainerBy));
    }

    public IReadOnlyList<string> OpenHistory()
    {
        if (!IsVisible(driver, HistoryContainerBy))
        {
            var button = _wait.Until(d => d.FindElements(HistoryButtonBy)
                .FirstOrDefault(element => element.Displayed && element.Enabled));
            button.Click();
        }

        _wait.Until(d => IsVisible(d, HistoryContainerBy));
        _wait.Until(_ => VisibleHistoryItems().Count > 0);
        return VisibleHistoryItems();
    }

    public void SelectHistoryItem(string query)
    {
        OpenHistory();
        var itemBy = By.XPath(
            $"//li[@ng-mousedown='onHistoryItemClick(phrase)' and " +
            $"normalize-space(.)={ToXPathLiteral(query)}]");
        var item = VisibleElement(itemBy);
        WaitForSearchSlot();
        item.Click();

        _wait.Until(_ => string.Equals(Query, query, StringComparison.OrdinalIgnoreCase));
        _wait.Until(d => !IsVisible(d, BlockingOverlayBy) && HasCompletedOutcome(d));
    }

    public void ClearPerformanceLog()
    {
        if (!SupportsPerformanceLog) return;
        _ = driver.Manage().Logs.GetLog(LogType.Performance);
    }

    public IReadOnlyList<string> SearchRequestSignaturesContaining(string query)
    {
        if (!SupportsPerformanceLog) return [];

        return driver.Manage().Logs.GetLog(LogType.Performance)
            .Select(entry => RequestSignature(entry.Message, query))
            .Where(signature => signature is not null)
            .Select(signature => signature!)
            .ToArray();
    }

    public void PasteAndSearch(string query)
    {
        if (driver is not ChromiumDriver chromiumDriver)
        {
            throw new NotSupportedException("Clipboard automation requires a Chromium driver.");
        }

        var origin = new Uri(driver.Url).GetLeftPart(UriPartial.Authority);
        chromiumDriver.ExecuteCdpCommand(
            "Browser.grantPermissions",
            new Dictionary<string, object?>
            {
                ["origin"] = origin,
                ["permissions"] = new[] { "clipboardReadWrite", "clipboardSanitizedWrite" }
            });

        var script = (IJavaScriptExecutor)driver;
        var copied = script.ExecuteAsyncScript(
            "const value = arguments[0]; const done = arguments[arguments.length - 1];" +
            "navigator.clipboard.writeText(value).then(() => done(true)).catch(() => done(false));",
            query);
        if (copied is not true)
        {
            throw new InvalidOperationException("The browser clipboard could not be prepared.");
        }

        var previousResult = FindVisibleStateElement();
        ReplaceQuery(string.Empty);
        SearchInput.Click();
        new Actions(driver).KeyDown(Keys.Control).SendKeys("v").KeyUp(Keys.Control).Perform();
        _wait.Until(_ => string.Equals(Query, query, StringComparison.Ordinal));
        WaitForSearchSlot();
        SearchInput.SendKeys(Keys.Enter);
        WaitForNewResult(previousResult);
    }

    public bool HasCompletedOutcome() => HasCompletedOutcome(driver);

    public string ResultSignature()
    {
        var summary = IsVisible(driver, ResultSummaryBy) ? ResultSummary : string.Empty;
        return string.Join("|", summary, HasEmptyResult, string.Join(",", ProductCodes));
    }

    private void WaitForNewResult(IWebElement? previousResult)
    {
        if (previousResult is not null)
        {
            try
            {
                new WebDriverWait(driver, TimeSpan.FromSeconds(1)).Until(d =>
                    IsStale(previousResult) || IsVisible(d, BlockingOverlayBy));
            }
            catch (WebDriverTimeoutException)
            {
                // Angular may reuse the result element and finish before the overlay is observed.
            }
        }

        _wait.Until(d =>
        {
            ThrowIfSearchRateLimited(d);
            return !IsVisible(d, BlockingOverlayBy) && HasCompletedOutcome(d);
        });
    }

    private void WaitForSearchSlot()
    {
        var remaining = MinimumSearchInterval - (DateTime.UtcNow - _lastSearchStartedUtc);
        if (remaining > TimeSpan.Zero)
        {
            Thread.Sleep(remaining);
        }

        _lastSearchStartedUtc = DateTime.UtcNow;
    }

    public ProductResult GetProduct(string code)
    {
        var productCodeBy = By.XPath(
            $"//a[contains(concat(' ', normalize-space(@class), ' '), ' searchProdCard ') " +
            $"and normalize-space(.)={ToXPathLiteral(code)}]");
        var codeElement = VisibleElement(productCodeBy);
        var result = codeElement.FindElement(By.XPath(
            "ancestor::*[@ng-repeat='item in searchresult.Items'][1]"));

        var brand = result.FindElements(By.CssSelector(".brandSearch"))
            .Select(element => NormalizeWhitespace(element.Text))
            .First(text => text.Length > 0);

        return new ProductResult(
            Code: NormalizeWhitespace(codeElement.Text),
            Card: NormalizeWhitespace(result.FindElement(By.CssSelector(".searchCard span")).Text),
            Description: NormalizeWhitespace(result.FindElement(By.CssSelector(".searchDescrip")).Text),
            Brand: brand);
    }

    public bool IsProductDisplayed(string code)
    {
        var by = By.XPath(
            $"//a[contains(concat(' ', normalize-space(@class), ' '), ' searchProdCard ') " +
            $"and normalize-space(.)={ToXPathLiteral(code)}]");
        return IsVisible(driver, by);
    }

    private IWebElement SearchInput => _wait.Until(d =>
        d.FindElements(SearchInputBy)
            .FirstOrDefault(element => element.Displayed && element.Enabled));

    private IWebElement VisibleElement(By by) => _wait.Until(d =>
        d.FindElements(by).FirstOrDefault(element => element.Displayed));

    private void WaitUntilPageIsReady() =>
        _wait.Until(d =>
        {
            ThrowIfSearchRateLimited(d);
            return !IsVisible(d, BlockingOverlayBy);
        });

    private IWebElement? FindVisibleStateElement() =>
        driver.FindElements(StandardResultBy)
            .Concat(driver.FindElements(EmptyResultBy))
            .Concat(driver.FindElements(StartsWithCodesBy))
            .FirstOrDefault(element => element.Displayed);

    private void ReplaceQuery(string query)
    {
        var input = SearchInput;
        input.SendKeys(Keys.Control + "a");
        input.SendKeys(Keys.Backspace);
        if (query.Length > 0)
        {
            input.SendKeys(query);
        }
    }

    private static string? RequestSignature(string message, string query)
    {
        try
        {
            using var root = JsonDocument.Parse(message);
            var innerMessage = root.RootElement.GetProperty("message");
            if (!string.Equals(
                    innerMessage.GetProperty("method").GetString(),
                    "Network.requestWillBeSent",
                    StringComparison.Ordinal))
            {
                return null;
            }

            var request = innerMessage.GetProperty("params").GetProperty("request");
            var url = request.GetProperty("url").GetString() ?? string.Empty;
            var postData = request.TryGetProperty("postData", out var postDataElement)
                ? postDataElement.GetString() ?? string.Empty
                : string.Empty;
            var signature = $"{request.GetProperty("method").GetString()} {url} {postData}";

            return signature.Contains(query, StringComparison.OrdinalIgnoreCase)
                ? signature
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private IReadOnlyList<string> VisibleHistoryItems() =>
        driver.FindElements(HistoryItemsBy)
            .Select(element => NormalizeWhitespace(element.Text))
            .Where(text => text.Length > 0)
            .ToArray();

    private static bool HasCompletedOutcome(IWebDriver webDriver) =>
        IsVisible(webDriver, ResultSummaryBy) ||
        IsVisible(webDriver, EmptyResultBy) ||
        IsVisible(webDriver, StartsWithSummaryBy) ||
        IsVisible(webDriver, StartsWithCodesBy);

    private static void ThrowIfSearchRateLimited(IWebDriver webDriver)
    {
        if (IsVisible(webDriver, SearchRateLimitBy))
        {
            throw new InvalidOperationException(
                "Тестовый сервер отклонил поиск: превышен лимит поисковых запросов. " +
                "Дождитесь сброса лимита перед следующим E2E-прогоном.");
        }
    }

    private static bool IsVisible(IWebDriver webDriver, By by)
    {
        try
        {
            return webDriver.FindElements(by).Any(element => element.Displayed);
        }
        catch (StaleElementReferenceException)
        {
            return false;
        }
    }

    private static bool IsStale(IWebElement element)
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

    private static string NormalizeWhitespace(string value) =>
        Regex.Replace(value.Trim(), @"\s+", " ");

    private static string ToXPathLiteral(string value)
    {
        if (!value.Contains('\'')) return $"'{value}'";
        if (!value.Contains('"')) return $"\"{value}\"";

        var parts = value.Split('\'');
        return $"concat('{string.Join("', \"'\", '", parts)}')";
    }
}

public sealed record ProductResult(
    string Code,
    string Card,
    string Description,
    string Brand);
