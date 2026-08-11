using System.Text.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Pages;

public sealed class SearchResultsPage
{
    private static readonly TimeSpan ResultSettleTime = TimeSpan.FromMilliseconds(200);

    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly TimeSpan _minimumSearchInterval;

    /// <summary>Половина общего ожидания: две попытки открыть страницу не должны
    /// суммарно превышать бюджет, заданный в настройках.</summary>
    private readonly TimeSpan _pageAttemptTimeout;

    private readonly SearchBarComponent _bar;
    private readonly SearchHistoryComponent _history;
    private readonly SearchResultComponent _results;
    private DateTime _lastSearchStartedUtc = DateTime.MinValue;

    public SearchResultsPage(
        IWebDriver driver,
        TimeSpan waitTimeout,
        TimeSpan minimumSearchInterval)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, waitTimeout);
        _minimumSearchInterval = minimumSearchInterval;
        _pageAttemptTimeout = TimeSpan.FromMilliseconds(
            Math.Max(1000, waitTimeout.TotalMilliseconds / 2));
        _bar = new SearchBarComponent(driver, _wait);
        _history = new SearchHistoryComponent(driver, _wait);
        _results = new SearchResultComponent(driver, _wait);
    }

    public bool SupportsPerformanceLog => _driver is ChromiumDriver;

    public bool SupportsClipboardPaste => _driver is ChromiumDriver;

    public string Query => _bar.Query;

    public string SearchPlaceholder => _bar.Placeholder;

    public string ResultSummary => _results.Summary;

    public bool HasEmptyResult => _results.HasEmptyResult;

    public bool IsInputUsable => _bar.IsUsable;

    public bool IsLoading => _bar.IsLoading;

    public bool IsStartsWithEnabled => _bar.IsStartsWithEnabled;

    public IReadOnlyList<string> ProductCodes => _results.ProductCodes;

    public IReadOnlyList<string> ProductDescriptions => _results.ProductDescriptions;

    public IReadOnlyList<string> ProductBrands => _results.ProductBrands;

    public IReadOnlyList<string> StartsWithCodes => _results.StartsWithCodes;

    public void Reset(string baseUrl)
    {
        OpenPage(baseUrl);
        _history.Close();
        _bar.SetStartsWith(false);
        _bar.ReplaceQuery(string.Empty);
    }

    /// <summary>
    /// Открывает страницу поиска, при неудаче перезагружая её один раз.
    /// </summary>
    /// <remarks>
    /// Приложение изредка не отрисовывается вовсе — страница остаётся пустой.
    /// Одна перезагрузка это лечит. Если не помогла и она, сбой описывается
    /// отдельным сообщением: иначе неподнявшаяся страница выглядит как падение
    /// самой проверки и портит статистику прогона.
    /// </remarks>
    private void OpenPage(string baseUrl)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            if (attempt == 1)
            {
                _driver.Navigate().GoToUrl(baseUrl);
            }
            else
            {
                _driver.Navigate().Refresh();
            }

            try
            {
                new WebDriverWait(_driver, _pageAttemptTimeout).Until(_ =>
                {
                    _results.ThrowIfRateLimited();
                    return _bar.IsRendered;
                });
                WaitUntilPageIsReady();
                return;
            }
            catch (WebDriverTimeoutException) when (attempt == 1)
            {
            }
        }

        throw new WebDriverTimeoutException(
            "Search page did not render even after a reload — the environment failed, " +
            $"not the scenario. Current URL: '{_driver.Url}'. Page title: '{_driver.Title}'.");
    }

    public void Search(string query)
    {
        WaitUntilPageIsReady();
        _bar.ReplaceQuery(query);
        WaitForSearchSlot();
        var requestCheckpoint = _driver.CaptureAngularRequestCheckpoint();
        _bar.Input.SendKeys(Keys.Enter);
        WaitForSearchCompletion(query, requestCheckpoint, requireQueryMatch: false);
    }

    public void TypeQuery(string query)
    {
        WaitUntilPageIsReady();
        _bar.ReplaceQuery(query);
    }

    public void ReplaceWithCtrlAAndSearch(string query) => Search(query);

    public void SubmitWithoutWaiting(string query)
    {
        WaitUntilPageIsReady();
        _bar.ReplaceQuery(query);
        if (query.Trim().Length > 0)
        {
            WaitForSearchSlot();
        }

        _bar.Input.SendKeys(Keys.Enter);
    }

    /// <summary>
    /// Убеждается, что за отведённое окно поиск не стартовал: индикатор загрузки
    /// не появляется, а число товаров не растёт.
    /// </summary>
    /// <remarks>
    /// Очистка выдачи при этом допустима и ожидаема: пустой запрос законно
    /// сбрасывает список — на живом сайте видно, как товары исчезают примерно
    /// через 150 мс после отправки. Требовать полной неизменности DOM нельзя,
    /// иначе проверка ловит именно этот сброс вместо запрещённого поиска.
    /// </remarks>
    public void EnsureNoSearchStarts(int maxProductCount, TimeSpan window)
    {
        var startedAt = DateTime.UtcNow;
        var watchWait = new WebDriverWait(_driver, window + TimeSpan.FromSeconds(1))
        {
            PollingInterval = TimeSpan.FromMilliseconds(50)
        };

        watchWait.Until(_ =>
        {
            if (_bar.IsLoading)
            {
                throw new InvalidOperationException(
                    "An empty query started loading products.");
            }

            var count = _results.ProductCodes.Count;
            if (count > maxProductCount)
            {
                throw new InvalidOperationException(
                    $"An empty query increased the product count from {maxProductCount} to {count}.");
            }

            return DateTime.UtcNow - startedAt >= window;
        });
    }

    public void WaitForIdle(TimeSpan timeout) =>
        new WebDriverWait(_driver, timeout).Until(_ => !_bar.IsLoading);

    public void ClearQueryWithButton() => _bar.ClearWithButton();

    public void SetStartsWith(bool enabled)
    {
        WaitUntilPageIsReady();
        _history.Close();
        _bar.SetStartsWith(enabled);
    }

    public void CloseHistory() => _history.Close();

    public IReadOnlyList<string> OpenHistory() => _history.Open();

    public void SelectHistoryItem(string query)
    {
        var item = _history.Item(query);
        WaitForSearchSlot();
        var requestCheckpoint = _driver.CaptureAngularRequestCheckpoint();
        item.Click();
        WaitForSearchCompletion(query, requestCheckpoint, requireQueryMatch: true);
    }

    public void ClearPerformanceLog()
    {
        if (!SupportsPerformanceLog) return;
        _ = _driver.Manage().Logs.GetLog(LogType.Performance);
    }

    public IReadOnlyList<string> SearchRequestSignaturesContaining(string query)
    {
        if (!SupportsPerformanceLog) return [];

        return _driver.Manage().Logs.GetLog(LogType.Performance)
            .Select(entry => RequestSignature(entry.Message, query))
            .Where(signature => signature is not null)
            .Select(signature => signature!)
            .ToArray();
    }

    public void PasteAndSearch(string query)
    {
        if (_driver is not ChromiumDriver chromiumDriver)
        {
            throw new NotSupportedException("Clipboard automation requires a Chromium driver.");
        }

        var origin = new Uri(_driver.Url).GetLeftPart(UriPartial.Authority);
        chromiumDriver.ExecuteCdpCommand(
            "Browser.grantPermissions",
            new Dictionary<string, object?>
            {
                ["origin"] = origin,
                ["permissions"] = new[] { "clipboardReadWrite", "clipboardSanitizedWrite" }
            });

        var script = (IJavaScriptExecutor)_driver;
        var copied = script.ExecuteAsyncScript(
            "const value = arguments[0]; const done = arguments[arguments.length - 1];" +
            "navigator.clipboard.writeText(value).then(() => done(true)).catch(() => done(false));",
            query);
        if (copied is not true)
        {
            throw new InvalidOperationException("The browser clipboard could not be prepared.");
        }

        _bar.ReplaceQuery(string.Empty);
        _bar.Input.Click();
        new Actions(_driver).KeyDown(Keys.Control).SendKeys("v").KeyUp(Keys.Control).Perform();
        _wait.Until(_ => string.Equals(Query, query, StringComparison.Ordinal));
        WaitForSearchSlot();
        var requestCheckpoint = _driver.CaptureAngularRequestCheckpoint();
        _bar.Input.SendKeys(Keys.Enter);
        WaitForSearchCompletion(query, requestCheckpoint, requireQueryMatch: true);
    }

    public bool HasCompletedOutcome() => _results.HasCompletedOutcome;

    public string ResultSignature() => _results.Signature();

    public ProductResult GetProduct(string code) => _results.GetProduct(code);

    public bool IsProductDisplayed(string code) => _results.IsProductDisplayed(code);

    private void WaitForSearchCompletion(
        string expectedQuery,
        long requestCheckpoint,
        string? expectedSummary = null,
        bool requireQueryMatch = false)
    {
        _driver.WaitUntilAngularRequestsCompleteAfter(requestCheckpoint, _wait.Timeout);

        string? lastSignature = null;
        DateTime? stableSince = null;

        _wait.Until(_ =>
        {
            _results.ThrowIfRateLimited();
            if (_bar.IsLoading ||
                !_results.HasCompletedOutcome ||
                (requireQueryMatch &&
                 !string.Equals(Query, expectedQuery, StringComparison.Ordinal)))
            {
                lastSignature = null;
                stableSince = null;
                return false;
            }

            if (expectedSummary is not null &&
                !string.Equals(ResultSummary, expectedSummary, StringComparison.Ordinal))
            {
                lastSignature = null;
                stableSince = null;
                return false;
            }

            var signature = _results.Signature();
            if (!string.Equals(signature, lastSignature, StringComparison.Ordinal))
            {
                lastSignature = signature;
                stableSince = DateTime.UtcNow;
                return false;
            }

            return stableSince is not null && DateTime.UtcNow - stableSince >= ResultSettleTime;
        });
    }

    private void WaitUntilPageIsReady()
    {
        _results.ThrowIfRateLimited();
        _bar.WaitUntilReady(_results.ThrowIfRateLimited);
    }

    private void WaitForSearchSlot()
    {
        if (_minimumSearchInterval > TimeSpan.Zero)
        {
            var remaining = _minimumSearchInterval - (DateTime.UtcNow - _lastSearchStartedUtc);
            if (remaining > TimeSpan.Zero)
            {
                var intervalWait = new WebDriverWait(
                    _driver,
                    remaining + TimeSpan.FromSeconds(1))
                {
                    PollingInterval = TimeSpan.FromMilliseconds(50)
                };
                intervalWait.Until(_ =>
                    DateTime.UtcNow - _lastSearchStartedUtc >= _minimumSearchInterval);
            }
        }

        _lastSearchStartedUtc = DateTime.UtcNow;
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
}
