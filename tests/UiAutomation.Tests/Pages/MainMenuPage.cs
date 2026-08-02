using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace UiAutomation.Tests.Pages;

public sealed class MainMenuPage(IWebDriver driver, TimeSpan waitTimeout)
{
    private readonly WebDriverWait _wait = new(driver, waitTimeout);

    private static readonly By MenuButtonBy =
        By.CssSelector("a.wrapper-tab-link.menu-button");

    private static readonly By VisibleMenuMarkerBy =
        By.XPath("//a[normalize-space(.)='Деб. заборгованість']");

    private static readonly By BlockingOverlayBy =
        By.CssSelector("div.block-ui-overlay");

    private IWebElement MenuButton => _wait.Until(d =>
        d.FindElements(MenuButtonBy)
            .FirstOrDefault(element => element.Displayed && element.Enabled));

    public bool IsMenuButtonDisplayed => MenuButton.Displayed && MenuButton.Enabled;

    public bool IsMenuExpanded => IsVisible(driver, VisibleMenuMarkerBy);

    public void OpenMenu()
    {
        if (!IsMenuExpanded)
        {
            ClickWhenPageIsReady(MenuButtonBy);
        }

        _wait.Until(d => IsVisible(d, VisibleMenuMarkerBy));
    }

    public void CloseMenu()
    {
        if (IsMenuExpanded)
        {
            ClickWhenPageIsReady(MenuButtonBy);
        }

        _wait.Until(d => !IsVisible(d, VisibleMenuMarkerBy));
    }

    public bool IsSectionDisplayed(string sectionName)
    {
        OpenMenu();
        return driver.FindElements(By.XPath(
                $"//*[normalize-space(.)={ToXPathLiteral(sectionName)}]"))
            .Any(element => element.Displayed);
    }

    public bool IsMenuItemDisplayed(string itemName)
    {
        OpenMenu();
        return driver.FindElements(MenuItemBy(itemName))
            .Any(element => element.Displayed && element.Enabled);
    }

    public void OpenMenuItem(string itemName, string expectedRoute)
    {
        OpenMenu();
        var item = VisibleMenuItem(itemName);

        AssertHrefMatchesRoute(item, itemName, expectedRoute);
        ClickWhenPageIsReady(MenuItemBy(itemName));

        _wait.Until(d =>
            d.Url.Contains(expectedRoute, StringComparison.OrdinalIgnoreCase));
    }

    public void OpenSubmenu(string itemName)
    {
        OpenMenu();
        var item = VisibleMenuItem(itemName);

        if (!string.Equals(
                item.GetAttribute("aria-expanded"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            ClickWhenPageIsReady(MenuItemBy(itemName));
        }

        _wait.Until(_ => string.Equals(
            VisibleMenuItem(itemName).GetAttribute("aria-expanded"),
            "true",
            StringComparison.OrdinalIgnoreCase));
    }

    public bool IsSubmenuItemDisplayed(string itemName)
    {
        return driver.FindElements(By.XPath(
                $"//*[normalize-space(.)={ToXPathLiteral(itemName)}]"))
            .Any(element => element.Displayed);
    }

    private IWebElement VisibleMenuItem(string itemName) => _wait.Until(d =>
        d.FindElements(MenuItemBy(itemName))
            .FirstOrDefault(element => element.Displayed && element.Enabled));

    private void ClickWhenPageIsReady(By by)
    {
        _wait.Until(d =>
        {
            if (IsVisible(d, BlockingOverlayBy)) return false;

            var element = d.FindElements(by)
                .FirstOrDefault(candidate => candidate.Displayed && candidate.Enabled);
            if (element is null) return false;

            try
            {
                element.Click();
                return true;
            }
            catch (ElementClickInterceptedException)
            {
                return false;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });
    }

    private static void AssertHrefMatchesRoute(
        IWebElement item,
        string itemName,
        string expectedRoute)
    {
        var href = item.GetAttribute("href") ?? string.Empty;
        if (!href.Contains(expectedRoute, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Menu item '{itemName}' has unexpected href '{href}'. " +
                $"Expected it to contain '{expectedRoute}'.");
        }
    }

    private static By MenuItemBy(string itemName) =>
        By.XPath($"//a[normalize-space(.)={ToXPathLiteral(itemName)}]");

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

    private static string ToXPathLiteral(string value)
    {
        if (!value.Contains('\'')) return $"'{value}'";
        if (!value.Contains('"')) return $"\"{value}\"";

        var parts = value.Split('\'');
        return $"concat('{string.Join("', \"'\", '", parts)}')";
    }
}
