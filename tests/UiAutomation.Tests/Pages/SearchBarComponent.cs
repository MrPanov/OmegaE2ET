using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Pages;

internal sealed class SearchBarComponent(IWebDriver driver, WebDriverWait wait)
{
    private static readonly By SearchInputBy = By.Id("headerInputSearch");
    private static readonly By ClearSearchBy = By.CssSelector(".navbar-input-search .removeIcon");
    private static readonly By StartsWithCheckboxBy = By.Id("searchBeginWith");
    private static readonly By StartsWithLabelBy = By.CssSelector("label.label-search");
    private static readonly By BlockingOverlayBy = By.CssSelector("div.block-ui-overlay");

    public string Query => Input.GetAttribute("value") ?? string.Empty;

    public string Placeholder => Input.GetAttribute("placeholder") ?? string.Empty;

    public bool IsUsable => Input.Displayed && Input.Enabled;

    /// <summary>
    /// Отрисована ли строка поиска. В отличие от <see cref="IsUsable"/> не ждёт
    /// её появления — нужна для быстрой проверки, загрузилась ли страница вообще.
    /// </summary>
    public bool IsRendered => driver.FindElements(SearchInputBy)
        .Any(element => element.Displayed && element.Enabled);

    public bool IsLoading => driver.IsVisible(BlockingOverlayBy);

    public bool IsStartsWithEnabled => driver.FindElement(StartsWithCheckboxBy).Selected;

    public IWebElement Input => wait.Until(d =>
        d.FindElements(SearchInputBy)
            .FirstOrDefault(element => element.Displayed && element.Enabled));

    public void WaitUntilReady(Action throwIfRateLimited) =>
        wait.Until(d =>
        {
            throwIfRateLimited();
            return !d.IsVisible(BlockingOverlayBy);
        });

    public void ReplaceQuery(string query)
    {
        var input = Input;
        input.SendKeys(Keys.Control + "a");
        input.SendKeys(Keys.Backspace);
        if (query.Length > 0)
        {
            input.SendKeys(query);
        }
    }

    public void ClearWithButton()
    {
        var clearButton = wait.Until(d => d.FindElements(ClearSearchBy)
            .FirstOrDefault(element => element.Displayed && element.Enabled));
        clearButton.Click();
        wait.Until(_ => Query.Length == 0);
    }

    public void SetStartsWith(bool enabled)
    {
        if (IsStartsWithEnabled == enabled) return;

        var label = wait.Until(d => d.FindElements(StartsWithLabelBy)
            .FirstOrDefault(element => element.Displayed && element.Enabled));
        label.Click();
        wait.Until(_ => IsStartsWithEnabled == enabled);
    }
}
