using System.Globalization;
using System.Text.RegularExpressions;
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
    /// <summary>Раздел корзины с позициями, доступными на складе.</summary>
    public const string StockSection = "Товари зі складу";

    /// <summary>Раздел корзины с позициями под заказ.</summary>
    public const string BackorderSection = "Товари під замовлення";

    private static readonly By AddCardInputBy = By.Id("inputBasketAddCardNumber");
    private static readonly By AddCardConfirmBy = By.Id("buttonBasketGo");
    private static readonly By BasketRowsBy = By.CssSelector(".item-basket");
    private static readonly By ProductCardLinkBy = By.CssSelector(".basketCard a");
    private static readonly By RemoveButtonBy = By.CssSelector("a.basketDel");
    private static readonly By ClearBasketBy = By.XPath("//a[contains(@ng-click,'clearBasket')]");
    private static readonly By AddQuantityInputBy = By.XPath(
        "//*[button[contains(@class,'claim-plus-btn')]]//input[@type='number']");
    private static readonly By AddQuantityPlusBy = By.CssSelector("button.claim-plus-btn");
    private static readonly By AddQuantityMinusBy = By.CssSelector("button.claim-minus-btn");
    private static readonly By RowQuantityInputBy = By.CssSelector("input[type='number']");
    private static readonly By RowCheckboxBy = By.CssSelector("input[type='checkbox']");
    private static readonly By SelectAllLabelBy = By.XPath(
        "//label[contains(normalize-space(.), 'Вибрати всі')]");
    // Сама сумма лежит не в узле с подписью, а в ближайшем следующем <strong>.
    private static readonly By SelectedTotalBy = By.XPath(
        "//*[contains(normalize-space(.), 'Загальна сума обраних в кошику')]/following::strong[1]");
    // Ищется от строки товара вверх по документу: ось preceding нумерует
    // элементы в обратном порядке, поэтому [1] — ближайший заголовок сверху.
    private static readonly By SectionHeadingBy = By.XPath(
        $"./preceding::*[normalize-space(text())='{StockSection}'" +
        $" or normalize-space(text())='{BackorderSection}'][1]");
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

    /// <summary>
    /// Вводит номер карточки в поле добавления и ждёт появления позиции в корзине.
    /// Количество задаётся счётчиком до подтверждения — после него счётчик уже не влияет.
    /// </summary>
    public void AddProduct(string cardNumber, int quantity = 1)
    {
        SubmitAddForm(cardNumber, quantity);
        _wait.Until(_ => HasProduct(cardNumber));
        WaitUntilIdle();
    }

    /// <summary>
    /// Добавляет товар по каталожному коду. Код и номер карточки — разные
    /// идентификаторы одного товара, поэтому строку ждём по карточке,
    /// а завершение операции — по ожидаемому количеству в ней.
    /// </summary>
    public void AddByCode(string catalogCode, string expectedCard, int expectedQuantity, int quantity = 1)
    {
        SubmitAddForm(catalogCode, quantity);
        _wait.Until(_ => HasProduct(expectedCard) && ProductQuantity(expectedCard) == expectedQuantity);
        WaitUntilIdle();
    }

    public bool HasProduct(string cardNumber) => ProductRow(cardNumber) is not null;

    /// <summary>
    /// Раздел корзины, в котором отображается позиция. Обе таблицы лежат в одном
    /// контейнере и различаются только заголовком перед строками, поэтому раздел
    /// определяется ближайшим предшествующим заголовком, а не предком.
    /// </summary>
    public string SectionOf(string cardNumber)
    {
        var heading = RequiredProductRow(cardNumber).FindElements(SectionHeadingBy).FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"Не удалось определить раздел корзины для позиции '{cardNumber}'.");

        return UiText.NormalizeWhitespace(heading.Text);
    }

    /// <summary>Количество в строке товара.</summary>
    public int ProductQuantity(string cardNumber) => ParseInt(
        RequiredProductRow(cardNumber).FindElement(RowQuantityInputBy).GetAttribute("value"));

    /// <summary>Отмечена ли позиция флажком. Добавленный товар отмечается автоматически.</summary>
    public bool IsProductSelected(string cardNumber) =>
        RequiredProductRow(cardNumber).FindElement(RowCheckboxBy).Selected;

    /// <summary>Состояния флажков всех видимых позиций.</summary>
    public IReadOnlyList<bool> SelectionStates => driver.FindElements(BasketRowsBy)
        .Where(IsVisibleRow)
        .Select(row => row.FindElements(RowCheckboxBy).FirstOrDefault())
        .Where(checkbox => checkbox is not null)
        .Select(checkbox => checkbox!.Selected)
        .ToArray();

    /// <summary>Сумма по отмеченным позициям.</summary>
    public decimal SelectedTotal => ParseAmount(
        _wait.Until(_ => driver.FindElements(SelectedTotalBy)
            .FirstOrDefault(element => element.Displayed))!.Text);

    public void IncreaseQuantity(string cardNumber) =>
        ChangeQuantity(cardNumber, "itemAmountIncrement", ProductQuantity(cardNumber) + 1);

    public void DecreaseQuantity(string cardNumber, int? expected = null) =>
        ChangeQuantity(cardNumber, "itemAmountDecrement", expected ?? ProductQuantity(cardNumber) - 1);

    /// <summary>
    /// Вводит количество вручную. Недопустимое значение приложение возвращает
    /// к минимально допустимому, поэтому ожидание строится не на введённом тексте.
    /// </summary>
    public void SetQuantity(string cardNumber, string value)
    {
        var input = RequiredProductRow(cardNumber).FindElement(RowQuantityInputBy);
        input.SendKeys(Keys.Control + "a");
        input.SendKeys(value + Keys.Tab);
        WaitUntilIdle();
    }

    /// <summary>Переключает флажок «Вибрати всі» и ждёт, пока состояние применится.</summary>
    public void SetSelectAll(bool selected)
    {
        var label = _wait.Until(_ => driver.FindElements(SelectAllLabelBy)
            .FirstOrDefault(element => element.Displayed));
        if (label!.FindElement(RowCheckboxBy).Selected == selected) return;

        driver.ClickRobustly(label);
        _wait.Until(_ => driver.FindElements(SelectAllLabelBy)
            .First(element => element.Displayed)
            .FindElement(RowCheckboxBy).Selected == selected);
        WaitUntilIdle();
    }

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

    /// <summary>Заполняет панель добавления и подтверждает ввод.</summary>
    private void SubmitAddForm(string identifier, int quantity)
    {
        var input = _wait.Until(_ => VisibleElement(AddCardInputBy));
        input.Clear();
        input.SendKeys(identifier);
        SetAddQuantity(quantity);
        ClickWhenReady(AddCardConfirmBy);
    }

    /// <summary>
    /// Доводит счётчик панели добавления до нужного значения кнопками «−» и «+»,
    /// как это делает пользователь.
    /// </summary>
    private void SetAddQuantity(int quantity)
    {
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Минимум 1.");

        for (var guard = 0; guard < 50; guard++)
        {
            var current = ParseInt(
                _wait.Until(_ => VisibleElement(AddQuantityInputBy))!.GetAttribute("value"));
            if (current == quantity) return;

            ClickWhenReady(current < quantity ? AddQuantityPlusBy : AddQuantityMinusBy);
            _wait.Until(_ => ParseInt(VisibleElement(AddQuantityInputBy)?.GetAttribute("value")) != current);
        }

        throw new InvalidOperationException($"Счётчик добавления не удалось привести к {quantity}.");
    }

    private void ChangeQuantity(string cardNumber, string handler, int expected)
    {
        var control = RequiredProductRow(cardNumber).FindElements(By.CssSelector("[ng-click]"))
            .FirstOrDefault(element =>
                (element.GetAttribute("ng-click") ?? string.Empty)
                .Contains(handler, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Контрол '{handler}' не найден для '{cardNumber}'.");

        driver.ClickRobustly(control);
        _wait.Until(_ => ProductQuantity(cardNumber) == expected);
        WaitUntilIdle();
    }

    private static int ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;

    /// <summary>Достаёт денежную сумму из текста вида «... в кошику: 159.06 грн.».</summary>
    private static decimal ParseAmount(string text)
    {
        var match = Regex.Match(UiText.NormalizeWhitespace(text), @"([\d\s ]+[.,]\d{2})");
        if (!match.Success) return 0;

        var normalized = match.Groups[1].Value
            .Replace(" ", string.Empty)
            .Replace(" ", string.Empty)
            .Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : 0;
    }

    private IWebElement RequiredProductRow(string cardNumber) => ProductRow(cardNumber)
        ?? throw new InvalidOperationException($"Позиция '{cardNumber}' отсутствует в корзине.");

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
