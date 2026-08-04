using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Pages;

public sealed class CatalogMenuPage(IWebDriver driver, TimeSpan waitTimeout)
{
    private readonly WebDriverWait _wait = new(driver, waitTimeout);

    private static readonly By CatalogButtonBy =
        By.XPath("//a[contains(normalize-space(.), 'Каталоги')]");

    private IWebElement CatalogButton => _wait.Until(d =>
        d.FindElements(CatalogButtonBy)
            .FirstOrDefault(element => element.Displayed && element.Enabled));

    public bool IsCatalogButtonDisplayed => CatalogButton.Displayed && CatalogButton.Enabled;

    public bool IsMenuExpanded =>
        string.Equals(CatalogButton.GetAttribute("aria-expanded"), "true", StringComparison.OrdinalIgnoreCase);

    public void OpenMenu()
    {
        if (!IsMenuExpanded)
        {
            CatalogButton.Click();
        }

        _wait.Until(_ => IsMenuExpanded);
    }

    public void CloseMenu()
    {
        if (IsMenuExpanded)
        {
            CatalogButton.Click();
        }

        _wait.Until(_ => !IsMenuExpanded);
    }

    public bool IsGroupDisplayed(string groupName)
    {
        OpenMenu();
        return driver.IsVisible(By.XPath(
            $"//*[normalize-space(.)={XPathHelpers.Literal(groupName)}]"));
    }

    public bool IsCatalogItemDisplayed(string itemName)
    {
        OpenMenu();
        return driver.IsVisible(CatalogItemBy(itemName));
    }

    public void OpenCatalog(string itemName, string expectedRoute)
    {
        OpenMenu();
        _wait.Until(d => d.FindElement(CatalogItemBy(itemName))).Click();
        _wait.Until(d => d.Url.Contains(expectedRoute, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Opens a "simplesearch" catalog by its Angular <c>categoryClick(index)</c>
    /// handler. These menu items are <c>href="javascript:void(0)"</c> entries in a
    /// hover mega-menu, so a native click is not reliable — the handler is invoked
    /// through the browser instead. Navigating by index also avoids matching the
    /// menu text (some items contain a backtick, e.g. "Аварійні з`єднувачі").
    /// </summary>
    public void OpenSimpleSearchCatalog(int categoryIndex, string expectedRoute)
    {
        var link = _wait.Until(d =>
            d.FindElements(By.CssSelector($"a[ng-click='categoryClick({categoryIndex});']"))
                .FirstOrDefault());
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", link);
        _wait.Until(d => d.Url.Contains(expectedRoute, StringComparison.OrdinalIgnoreCase));
    }

    public bool SelectCatalog(string itemName)
    {
        OpenMenu();
        var urlBeforeSelection = driver.Url;
        var item = _wait.Until(d =>
            d.FindElements(CatalogItemBy(itemName))
                .FirstOrDefault(element => element.Displayed && element.Enabled));

        item.Click();

        return _wait.Until(d =>
            !string.Equals(d.Url, urlBeforeSelection, StringComparison.OrdinalIgnoreCase) ||
            !IsCatalogMenuExpanded(d));
    }

    private static bool IsCatalogMenuExpanded(IWebDriver webDriver)
    {
        var button = webDriver.FindElements(CatalogButtonBy)
            .FirstOrDefault(element => element.Displayed);

        return button is not null &&
               string.Equals(
                   button.GetAttribute("aria-expanded"),
                   "true",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static By CatalogItemBy(string itemName)
    {
        var name = XPathHelpers.Literal(itemName);
        var nameWithPrefix = XPathHelpers.Literal($"- {itemName}");
        return By.XPath($"//a[normalize-space(.)={name} or normalize-space(.)={nameWithPrefix}]");
    }
}
