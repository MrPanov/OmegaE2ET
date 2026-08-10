using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Pages;

/// <summary>
/// Панель главного меню — <c>div.side-menu</c>, раскрываемая кнопкой в шапке.
/// </summary>
/// <remarks>
/// Все поиски ограничены этой панелью намеренно. В разметке страницы те же
/// подписи встречаются ещё в трёх местах: во второй, мобильной копии меню
/// (<c>div.mobile_menu</c>, скрытой на десктопе), в выпадающем списке
/// «Каталоги» — там есть свой раздел «Інше» — и в подвале страницы, где лежат
/// «Контакти». Незаземлённый поиск по всей странице нашёл бы их и выдал
/// отсутствующий пункт меню за присутствующий.
/// </remarks>
public sealed class MainMenuPage(IWebDriver driver, TimeSpan waitTimeout)
{
    private readonly WebDriverWait _wait = new(driver, waitTimeout);

    private const string SideMenuPath = "//div[contains(@class,'side-menu')]";

    private static readonly By MenuButtonBy =
        By.CssSelector("a.wrapper-tab-link.menu-button");

    private static readonly By VisibleMenuMarkerBy =
        By.XPath($"{SideMenuPath}//a[normalize-space(.)='Деб. заборгованість']");

    private static readonly By BlockingOverlayBy =
        By.CssSelector("div.block-ui-overlay");

    private IWebElement MenuButton => _wait.Until(d =>
        d.FindElements(MenuButtonBy)
            .FirstOrDefault(element => element.Displayed && element.Enabled));

    public bool IsMenuButtonDisplayed => MenuButton.Displayed && MenuButton.Enabled;

    public bool IsMenuExpanded => driver.IsVisible(VisibleMenuMarkerBy);

    public void OpenMenu()
    {
        if (!IsMenuExpanded)
        {
            ClickWhenPageIsReady(MenuButtonBy);
        }

        _wait.Until(d => d.IsVisible(VisibleMenuMarkerBy));
    }

    public void CloseMenu()
    {
        if (IsMenuExpanded)
        {
            ClickWhenPageIsReady(MenuButtonBy);
        }

        _wait.Until(d => !d.IsVisible(VisibleMenuMarkerBy));
    }

    public bool IsSectionDisplayed(string sectionName)
    {
        OpenMenu();
        return driver.FindElements(By.XPath(
                $"{SideMenuPath}//div[contains(@class,'side-menu-title')]" +
                $"[normalize-space(.)={XPathHelpers.Literal(sectionName)}]"))
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

    private IWebElement VisibleMenuItem(string itemName) => _wait.Until(d =>
        d.FindElements(MenuItemBy(itemName))
            .FirstOrDefault(element => element.Displayed && element.Enabled));

    private void ClickWhenPageIsReady(By by)
    {
        _wait.Until(d =>
        {
            if (d.IsVisible(BlockingOverlayBy)) return false;

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

    /// <summary>
    /// Пункт меню ищется по точному тексту ссылки внутри панели: у пунктов нет
    /// ни идентификаторов, ни собственных классов, а href у половины содержит
    /// UUID аккаунта, который меняется от загрузки к загрузке.
    /// </summary>
    private static By MenuItemBy(string itemName) =>
        By.XPath($"{SideMenuPath}//a[normalize-space(.)={XPathHelpers.Literal(itemName)}]");
}
