using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

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
        return FindVisible(By.XPath($"//*[normalize-space(.)={ToXPathLiteral(groupName)}]"));
    }

    public bool IsCatalogItemDisplayed(string itemName)
    {
        OpenMenu();
        return FindVisible(CatalogItemBy(itemName));
    }

    public void OpenCatalog(string itemName, string expectedRoute)
    {
        OpenMenu();
        _wait.Until(d => d.FindElement(CatalogItemBy(itemName))).Click();
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

    private bool FindVisible(By by) =>
        driver.FindElements(by).Any(element => element.Displayed);

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
        var name = ToXPathLiteral(itemName);
        var nameWithPrefix = ToXPathLiteral($"- {itemName}");
        return By.XPath($"//a[normalize-space(.)={name} or normalize-space(.)={nameWithPrefix}]");
    }

    private static string ToXPathLiteral(string value)
    {
        if (!value.Contains('\'')) return $"'{value}'";
        if (!value.Contains('"')) return $"\"{value}\"";

        var parts = value.Split('\'');
        return $"concat('{string.Join("', \"'\", '", parts)}')";
    }
}
