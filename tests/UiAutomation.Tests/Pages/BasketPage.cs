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
    private static readonly By ClearBasketBy = By.XPath("//a[contains(@ng-click,'clearBasket')]");
    private static readonly By HeaderCartBy = By.Id("navbarBasket");
    private static readonly By InvoiceJournalBy = By.XPath(
        "//a[contains(normalize-space(.), 'Журнал рахунків')]");
    private static readonly By BlockingOverlayBy = By.CssSelector("div.block-ui-overlay");

    private static readonly TimeSpan ContentSettleTime = TimeSpan.FromMilliseconds(1500);

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

    /// <summary>Виден ли переход в журнал счетов — признак полностью отрисованной корзины.</summary>
    public bool IsInvoiceJournalVisible => driver.IsVisible(InvoiceJournalBy);

    /// <summary>
    /// Открывает корзину прямой ссылкой. Так делают все сценарии, кроме BASKET-001:
    /// им важно состояние корзины, а не способ, которым в неё попали.
    /// </summary>
    public void Open(string baseUrl)
    {
        driver.Navigate().GoToUrl(new Uri(new Uri(baseUrl), "#/app/basket"));
        _wait.Until(_ => IsLoaded);
        WaitUntilContentSettled();
    }

    /// <summary>
    /// Открывает корзину кликом по иконке тележки в шапке. Проверяет сам маршрут
    /// перехода, поэтому используется только в BASKET-001.
    /// </summary>
    public void OpenFromHeader()
    {
        ClickWhenReady(HeaderCartBy);
        _wait.Until(_ => IsLoaded);
        WaitUntilContentSettled();
    }

    /// <summary>
    /// Перезагружает страницу целиком. Переход по тому же хешу перезагрузки не даёт,
    /// поэтому состояние, оставшееся после анимаций Angular, снимается только так.
    /// </summary>
    public void Reload()
    {
        driver.Navigate().Refresh();
        _wait.Until(_ => IsLoaded);
        WaitUntilContentSettled();
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

    /// <summary>
    /// Кнопка очистки корзины отсутствует в разметке, пока корзина пуста,
    /// поэтому её видимость — самостоятельный признак непустой корзины.
    /// </summary>
    public bool HasClearButton => driver.IsVisible(ClearBasketBy);

    /// <summary>Удаляет из корзины все позиции, включая чужие. Подтверждения нет.</summary>
    public void ClearBasket()
    {
        ClickWhenReady(ClearBasketBy);
        _wait.Until(_ => ProductCards.Count == 0);
        WaitUntilIdle();
    }

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

    /// <summary>
    /// Ждёт, пока состав корзины перестанет меняться. Позиции подгружаются отдельным
    /// запросом уже после того, как поле ввода отрисовано и оверлей снят, поэтому
    /// чтение сразу после загрузки страницы возвращает пустой список.
    /// </summary>
    private void WaitUntilContentSettled()
    {
        string? previousSignature = null;
        DateTime? stableSince = null;

        _wait.Until(_ =>
        {
            if (driver.IsVisible(BlockingOverlayBy))
            {
                previousSignature = null;
                stableSince = null;
                return false;
            }

            var signature = string.Join("|", ProductCards);
            if (!string.Equals(signature, previousSignature, StringComparison.Ordinal))
            {
                previousSignature = signature;
                stableSince = DateTime.UtcNow;
                return false;
            }

            return stableSince is not null && DateTime.UtcNow - stableSince >= ContentSettleTime;
        });
    }
}
