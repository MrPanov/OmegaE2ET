using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Pages;

internal sealed class SearchHistoryComponent(IWebDriver driver, WebDriverWait wait)
{
    private static readonly By HistoryButtonBy =
        By.CssSelector("a[ng-click='onclickBut(1)']");
    private static readonly By HistoryContainerBy =
        By.CssSelector(".history-allSearch-container");
    private static readonly By HistoryItemsBy =
        By.CssSelector(".history-allSearch-container #search.active li[ng-mousedown]");

    public void Close()
    {
        if (!driver.IsVisible(HistoryContainerBy)) return;

        HistoryButton().Click();
        wait.Until(d => !d.IsVisible(HistoryContainerBy));
    }

    public IReadOnlyList<string> Open()
    {
        if (!driver.IsVisible(HistoryContainerBy))
        {
            HistoryButton().Click();
        }

        wait.Until(d => d.IsVisible(HistoryContainerBy));
        wait.Until(_ => VisibleItems().Count > 0);
        return VisibleItems();
    }

    public IWebElement Item(string query)
    {
        Open();
        var itemBy = By.XPath(
            $"//li[@ng-mousedown='onHistoryItemClick(phrase)' and " +
            $"normalize-space(.)={XPathHelpers.Literal(query)}]");
        return wait.Until(d => d.FindElements(itemBy).FirstOrDefault(element => element.Displayed));
    }

    private IWebElement HistoryButton() => wait.Until(d => d.FindElements(HistoryButtonBy)
        .FirstOrDefault(element => element.Displayed && element.Enabled));

    private IReadOnlyList<string> VisibleItems() => driver.VisibleTexts(HistoryItemsBy);
}
