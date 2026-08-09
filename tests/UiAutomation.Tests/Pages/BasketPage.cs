using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Pages;

/// <summary>
/// Корзина: добавление товара по номеру карточки и проверка состава корзины.
/// Поле ввода одно и то же для любого товара, различаются только номера карточек.
/// </summary>
public sealed class BasketPage(IWebDriver driver, TimeSpan waitTimeout)
{
    private static readonly By AddCardInputBy = By.Id("inputBasketAddCardNumber");
    private static readonly By AddCardConfirmBy = By.Id("buttonBasketGo");
    private static readonly By BasketRowsBy = By.CssSelector(".item-basket");
    private static readonly By ProductCardLinkBy = By.CssSelector(".basketCard a");
    private static readonly By RemoveButtonBy = By.CssSelector("a.basketDel");
    private static readonly By BlockingOverlayBy = By.CssSelector("div.block-ui-overlay");

    private readonly WebDriverWait _wait = new(driver, waitTimeout);

    /// <summary>Признак того, что корзина отрисована и готова принимать ввод.</summary>
    public bool IsLoaded => driver.IsVisible(AddCardInputBy);

    /// <summary>Номера карточек всех видимых позиций корзины в порядке отображения.</summary>
    public IReadOnlyList<string> ProductCards => driver.FindElements(BasketRowsBy)
        .Where(IsVisibleRow)
        .SelectMany(row => row.FindElements(ProductCardLinkBy))
        .Select(link => UiText.NormalizeWhitespace(link.Text))
        .Where(text => text.Length > 0)
        .ToArray();

    public void Open(string baseUrl)
    {
        driver.Navigate().GoToUrl(new Uri(new Uri(baseUrl), "#/app/basket"));
        _wait.Until(_ => IsLoaded);
        WaitUntilIdle();
    }

    /// <summary>Вводит номер карточки в поле добавления и ждёт появления позиции в корзине.</summary>
    public void AddProduct(string cardNumber)
    {
        var input = _wait.Until(_ => VisibleElement(AddCardInputBy));
        input.Clear();
        input.SendKeys(cardNumber);
        ClickWhenReady(AddCardConfirmBy);

        _wait.Until(_ => HasProduct(cardNumber));
        WaitUntilIdle();
    }

    public bool HasProduct(string cardNumber) => ProductRow(cardNumber) is not null;

    /// <summary>Сколько строк корзины относится к указанной карточке.</summary>
    public int ProductRowCount(string cardNumber) =>
        ProductCards.Count(card => string.Equals(card, cardNumber, StringComparison.Ordinal));

    /// <summary>Удаляет позицию, если она есть. Отсутствие позиции не считается ошибкой.</summary>
    public void RemoveProduct(string cardNumber)
    {
        var row = ProductRow(cardNumber);
        if (row is null) return;

        driver.ClickRobustly(RemoveControl(row, cardNumber));
        AcceptConfirmationIfPresent();

        _wait.Until(_ => !HasProduct(cardNumber));
        WaitUntilIdle();
    }

    private IWebElement? ProductRow(string cardNumber) => driver.FindElements(BasketRowsBy)
        .FirstOrDefault(row => IsVisibleRow(row) && RowBelongsToCard(row, cardNumber));

    private static bool RowBelongsToCard(IWebElement row, string cardNumber)
    {
        try
        {
            return row.FindElements(ProductCardLinkBy)
                .Any(link => string.Equals(
                    UiText.NormalizeWhitespace(link.Text),
                    cardNumber,
                    StringComparison.Ordinal));
        }
        catch (StaleElementReferenceException)
        {
            return false;
        }
    }

    private static bool IsVisibleRow(IWebElement row)
    {
        try
        {
            return row.Displayed;
        }
        catch (StaleElementReferenceException)
        {
            return false;
        }
    }

    /// <summary>Кнопка удаления строки — `a.basketDel` с обработчиком `delete(item)`.</summary>
    private static IWebElement RemoveControl(IWebElement row, string cardNumber) =>
        row.FindElements(RemoveButtonBy).FirstOrDefault()
        ?? throw new InvalidOperationException(
            $"Кнопка удаления не найдена для карточки '{cardNumber}'.");

    private void AcceptConfirmationIfPresent()
    {
        try
        {
            driver.SwitchTo().Alert().Accept();
        }
        catch (NoAlertPresentException)
        {
        }
    }

    private IWebElement? VisibleElement(By by) => driver.FindElements(by)
        .FirstOrDefault(element => element.Displayed && element.Enabled);

    private void ClickWhenReady(By by) => _wait.Until(_ =>
    {
        if (driver.IsVisible(BlockingOverlayBy)) return false;

        var element = VisibleElement(by);
        if (element is null) return false;

        try
        {
            driver.ClickRobustly(element);
            return true;
        }
        catch (StaleElementReferenceException)
        {
            return false;
        }
    });

    /// <summary>Ждёт, пока исчезнет блокирующий оверлей после запроса к серверу.</summary>
    private void WaitUntilIdle() => _wait.Until(_ => !driver.IsVisible(BlockingOverlayBy));
}
