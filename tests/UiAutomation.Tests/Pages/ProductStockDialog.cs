using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Pages;

/// <summary>
/// Окно «Залишки», которое открывается ссылкой «див. наяв.» в выдаче поиска.
/// Для товара без остатка на складах в нём есть блок «Під замовлення на склад Омеги»
/// со списком поставок; каждую можно положить в корзину.
/// </summary>
/// <remarks>
/// Рядом с кнопкой добавления в корзину стоит «Відвантажити» (<c>shipOrderProduct</c>),
/// которая создаёт реальный документ отгрузки. Этот класс её намеренно не умеет нажимать.
/// </remarks>
public sealed class ProductStockDialog(IWebDriver driver, TimeSpan waitTimeout)
{
    private static readonly By StockLinkBy = By.XPath("//span[@ng-click='showBalanceDetails(item)']");
    private static readonly By DialogBy = By.CssSelector("[role=dialog], .modal, .popup, .ui-dialog");
    private static readonly By AddToBasketBy = By.CssSelector("div.cartAdd");
    private static readonly By CloseBy = By.CssSelector("span.close-icon-ticket");

    private readonly WebDriverWait _wait = new(driver, waitTimeout);

    public bool IsOpen => Dialog is not null;

    /// <summary>Весь текст окна: код продукта, наименование, заголовок блока и строки поставок.</summary>
    public string Text => UiText.NormalizeWhitespace(RequiredDialog.Text);

    /// <summary>Сколько поставок под заказ предложено — столько же кнопок «в корзину».</summary>
    public int BackorderOptionCount => RequiredDialog.FindElements(AddToBasketBy)
        .Count(element => element.Displayed);

    /// <summary>Открывает окно остатков для первой позиции в выдаче поиска.</summary>
    public void OpenForFirstSearchResult()
    {
        var link = _wait.Until(_ => driver.FindElements(StockLinkBy)
            .FirstOrDefault(element => element.Displayed));
        driver.ClickRobustly(link);
        _wait.Until(_ => IsOpen && BackorderOptionCount > 0);
    }

    /// <summary>
    /// Кладёт в корзину первую поставку под заказ. Кнопка не имеет обработчика
    /// в разметке — он навешан директивой, поэтому ищется по классу.
    /// </summary>
    public void AddFirstOptionToBasket()
    {
        var button = _wait.Until(_ => RequiredDialog.FindElements(AddToBasketBy)
            .FirstOrDefault(element => element.Displayed));
        driver.ClickRobustly(button);
    }

    public void Close()
    {
        var close = driver.FindElements(CloseBy).FirstOrDefault(element => element.Displayed);
        if (close is null) return;

        driver.ClickRobustly(close);
        _wait.Until(_ => !IsOpen);
    }

    private IWebElement RequiredDialog => Dialog
        ?? throw new InvalidOperationException("Окно «Залишки» не открыто.");

    private IWebElement? Dialog => driver.FindElements(DialogBy)
        .LastOrDefault(element => element.Displayed);
}
