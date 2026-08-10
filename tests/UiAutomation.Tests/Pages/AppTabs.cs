using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Pages;

/// <summary>
/// Панель вкладок приложения: каждый открытый раздел остаётся в ней отдельной
/// вкладкой, пока её не закроют крестиком.
/// </summary>
/// <remarks>
/// Вкладки копятся на всю сессию, а сессия у фикстуры одна. Набор, который
/// открывает разделы подряд и не убирает за собой, к концу прогона работает уже
/// не на чистом приложении: панель переполняется и перехватывает клики по тому,
/// что под ней. Поэтому проверка, открывшая раздел, обязана закрыть свою вкладку.
///
/// Закрывается всегда активная вкладка — только что открытая. По названию искать
/// нельзя: в заголовке вкладки стоит имя страницы («Журнал заявок АМ»), а не имя
/// пункта меню, которым её открыли («Заявки АМ»).
/// </remarks>
public sealed class AppTabs(IWebDriver driver, TimeSpan waitTimeout)
{
    private static readonly By TabBy =
        By.CssSelector("ul.wrapper-tab-list-newDashboard > li");

    private static readonly By ActiveTabBy =
        By.CssSelector("ul.wrapper-tab-list-newDashboard > li.active");

    private static readonly By CloseIconBy = By.CssSelector("i.close-icon-tab");

    private static readonly By LabelBy = By.CssSelector("span.tab-label");

    private readonly WebDriverWait _wait = new(driver, waitTimeout);

    /// <summary>Сколько вкладок открыто сейчас.</summary>
    public int Count => driver.FindElements(TabBy).Count(element => element.Displayed);

    /// <summary>Заголовок активной вкладки; пустая строка, если вкладок нет.</summary>
    public string ActiveLabel
    {
        get
        {
            var active = ActiveTab;
            if (active is null) return string.Empty;

            var label = active.FindElements(LabelBy).FirstOrDefault();
            return label is null ? string.Empty : UiText.NormalizeWhitespace(label.Text);
        }
    }

    /// <summary>
    /// Закрывает активную вкладку и ждёт, пока она пропадёт из панели.
    /// </summary>
    /// <returns>
    /// <c>false</c>, если закрывать было нечего: раздел мог открыться без вкладки,
    /// и это не повод ронять проверку, которая уже сделала своё дело.
    /// </returns>
    public bool CloseActive()
    {
        var active = ActiveTab;
        if (active is null) return false;

        var closeIcon = active.FindElements(CloseIconBy)
            .FirstOrDefault(element => element.Displayed);
        if (closeIcon is null) return false;

        var countBeforeClose = Count;
        driver.ClickRobustly(closeIcon);
        _wait.Until(_ => Count < countBeforeClose);
        return true;
    }

    private IWebElement? ActiveTab =>
        driver.FindElements(ActiveTabBy).FirstOrDefault(element => element.Displayed);
}
