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

    // Production пока использует старый id, а обновлённая Test-среда оставила
    // полю только стабильный пользовательский placeholder.
    private static readonly By AddCardInputBy = By.CssSelector(
        "#inputBasketAddCardNumber, input[placeholder='Додати позицію']");
    private static readonly By AddCardConfirmBy = By.CssSelector(
        "#buttonBasketGo[ng-click*='addSearchItemToBasketTable'], " +
        "[ng-click*='addSearchItemToBasketTable']");
    private static readonly By BasketRowsBy = By.CssSelector(".item-basket, .basket-row");
    private static readonly By ProductCardLinkBy = By.CssSelector(
        ".basketCard a, .b-cart-no a");
    private static readonly By RemoveButtonBy = By.CssSelector(
        "a.basketDel, .b-delete, [ng-click*='delete'][ng-click*='item']");
    private static readonly By ClearBasketBy = By.CssSelector("[tooltip='Очистити кошик']");
    private static readonly By AddQuantityInputBy = By.XPath(
        "//*[button[contains(@class,'claim-plus-btn')]]//input[@type='number'] | " +
        "(//input[@placeholder='Додати позицію']/following::input[@type='number'])[1]");
    private static readonly By AddQuantityPlusBy = By.XPath(
        "//button[contains(@class,'claim-plus-btn')] | " +
        "(//input[@placeholder='Додати позицію']/following::input[@type='number'])[1]" +
        "/following-sibling::button[1]");
    private static readonly By AddQuantityMinusBy = By.XPath(
        "//button[contains(@class,'claim-minus-btn')] | " +
        "(//input[@placeholder='Додати позицію']/following::input[@type='number'])[1]" +
        "/preceding-sibling::button[1]");
    private static readonly By RowQuantityInputBy = By.CssSelector("input[type='number']");
    private static readonly By RowCheckboxBy = By.CssSelector("input[type='checkbox']");
    private static readonly By WarehouseNameBy = By.CssSelector(
        "tr.hidden-lg td[ng-repeat='war in availablewarehouses'] span[data-content], " +
        ".wh-dropdown__option .wh-name");
    private static readonly By WarehouseStockBy = By.CssSelector(
        "td[ng-repeat='war in item.availablewarehouses'] span, " +
        ".wh-dropdown__option .wh-rest");
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
    private static readonly By SaveInvoiceBy = By.CssSelector(
        "#buttonBasketCreateInvoice[ng-click=\"basketLength() == 0 || createInvoice('Save')\"]");
    private static readonly By ReserveInvoiceBy = By.CssSelector(
        "#buttonBasketReservationInvoice[ng-click=\"basketLength() == 0 || createInvoice('Apply')\"]");
    private static readonly By VisibleModalBy = By.CssSelector(".modal-content");
    private static readonly By ModalTitleBy = By.CssSelector(".create-ticket-title");
    private static readonly By ModalOptionLabelBy = By.CssSelector(".radio label");
    private static readonly By ModalConfirmBy = By.CssSelector("button[ng-click='ok()']");
    private static readonly By MatrixReserveBy = By.CssSelector(
        "#buttonMatrixReadyInvoice[ng-click=\"reserve('matrixApply')\"]");
    private static readonly By NotificationBy = By.CssSelector(
        "#toast-container .toast, #toast-container .toast-title, #toast-container .toast-message");
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
        => AddByIdentifier(catalogCode, expectedCard, expectedQuantity, quantity);

    /// <summary>
    /// Добавляет товар по номеру карточки или каталожному коду и проверяет,
    /// что изменилась именно ожидаемая строка корзины.
    /// </summary>
    public void AddByIdentifier(
        string identifier,
        string expectedCard,
        int expectedQuantity,
        int quantity = 1)
    {
        SubmitAddForm(identifier, quantity);
        var notificationsAfterResponse = driver.VisibleTexts(NotificationBy);

        try
        {
            var resultWait = new WebDriverWait(driver, waitTimeout);
            resultWait.Until(_ =>
                HasProduct(expectedCard) && ProductQuantity(expectedCard) == expectedQuantity);
        }
        catch (WebDriverTimeoutException exception)
        {
            var notifications = notificationsAfterResponse
                .Concat(driver.VisibleTexts(NotificationBy))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var notificationText = notifications.Length == 0
                ? "уведомлений нет"
                : string.Join(" | ", notifications);
            var expectedCardMarkup = FindCardRowMarkup(expectedCard);

            throw new InvalidOperationException(
                $"После клика '#buttonBasketGo' идентификатор '{identifier}' " +
                $"не дал карточку '{expectedCard}' с количеством {expectedQuantity}. " +
                $"Видимые карточки: [{string.Join(", ", ProductCards)}]; {notificationText}. " +
                $"Разметка ожидаемой карточки: {expectedCardMarkup}",
                exception);
        }

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

    /// <summary>
    /// Устанавливает состояние флажка конкретной позиции. Для создания счёта
    /// нельзя пользоваться только «Вибрати всі»: этот флажок не управляет
    /// товарами под заказ и может оставить в выборе чужую строку.
    /// </summary>
    public void SetProductSelected(string cardNumber, bool selected)
    {
        var checkbox = RequiredProductRow(cardNumber).FindElement(RowCheckboxBy);
        if (checkbox.Selected == selected) return;

        driver.ClickRobustly(checkbox);
        _wait.Until(_ => IsProductSelected(cardNumber) == selected);
        WaitUntilIdle();
    }

    /// <summary>Снимает выбор со всех видимых складских и заказных позиций.</summary>
    public void DeselectAllProducts()
    {
        foreach (var cardNumber in ProductCards.Distinct(StringComparer.Ordinal).ToArray())
        {
            SetProductSelected(cardNumber, false);
        }
    }

    /// <summary>Карточки всех позиций, которые сейчас войдут в будущий счёт.</summary>
    public IReadOnlyList<string> SelectedProductCards => driver.FindElements(BasketRowsBy)
        .Where(IsVisibleRow)
        .Where(row => row.FindElements(RowCheckboxBy).FirstOrDefault()?.Selected == true)
        .SelectMany(row => row.FindElements(ProductCardLinkBy))
        .Select(link => UiText.NormalizeWhitespace(link.Text))
        .Where(text => text.Length > 0)
        .ToArray();

    /// <summary>
    /// Склады с положительным остатком в том же порядке, в котором они показаны
    /// в строке товара. Названия берутся из заголовков таблицы, а не из окна
    /// выбора: так тест выбирает склад, где товар действительно был доступен.
    /// </summary>
    public IReadOnlyList<string> WarehousesWithStock(string cardNumber)
    {
        string[] names = [];
        string[] stocks = [];
        try
        {
            _wait.Until(_ =>
            {
                try
                {
                    var row = RequiredProductRow(cardNumber);
                    names = row.FindElements(WarehouseNameBy)
                        .Select(element => UiText.NormalizeWhitespace(
                            element.GetAttribute("data-content") ?? ElementText(element)))
                        .ToArray();
                    stocks = row.FindElements(WarehouseStockBy)
                        .Select(element => UiText.NormalizeWhitespace(ElementText(element)))
                        .ToArray();

                    return names.Length > 0 && names.Length == stocks.Length;
                }
                catch (StaleElementReferenceException)
                {
                    names = [];
                    stocks = [];
                    return false;
                }
            });
        }
        catch (WebDriverTimeoutException exception)
        {
            throw new InvalidOperationException(
                $"Не удалось дождаться складов и остатков для карточки '{cardNumber}': " +
                $"складов {names.Length}, значений {stocks.Length}.",
                exception);
        }

        return names.Zip(stocks)
            .Where(item => HasStock(item.Second))
            .Select(item => item.First)
            .ToArray();
    }

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
    public void SetQuantity(string cardNumber, string value, int? expected = null)
    {
        var input = RequiredProductRow(cardNumber).FindElement(RowQuantityInputBy);
        input.SendKeys(Keys.Control + "a");
        input.SendKeys(value + Keys.Tab);

        if (expected is not null)
        {
            _wait.Until(_ => ProductQuantity(cardNumber) == expected);
        }

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

    /// <summary>
    /// Очищает корзину, только если в ней есть позиции. На пустой корзине кнопки
    /// <c>tooltip="Очистити кошик"</c> в DOM нет, и это штатное состояние.
    /// </summary>
    /// <returns><see langword="true"/>, если кнопка очистки была нажата.</returns>
    public bool ClearIfNotEmpty()
    {
        if (ProductCards.Count == 0) return false;

        ClickWhenReady(ClearBasketBy);
        _wait.Until(_ => driver.VisibleTexts(NotificationBy).Any(text =>
            text.Contains("Кошик очищено", StringComparison.OrdinalIgnoreCase)));
        _wait.Until(_ => ProductCards.Count == 0);
        WaitUntilIdle();

        var animationWait = new WebDriverWait(
            driver,
            TimeSpan.FromSeconds(Math.Min(3, Math.Max(1, waitTimeout.TotalSeconds))));
        try
        {
            animationWait.Until(_ => !HasClearButton);
        }
        catch (WebDriverTimeoutException)
        {
            // В headless Angular иногда оставляет кнопку в состоянии ng-leave.
            // Настоящая перезагрузка подтверждает пустую корзину по данным сервера.
            Reload();
            _wait.Until(_ => !HasClearButton && ProductCards.Count == 0);
        }

        return true;
    }

    /// <summary>Требует непустую корзину и удаляет из неё все позиции.</summary>
    public void ClearBasket()
    {
        if (!ClearIfNotEmpty())
        {
            throw new InvalidOperationException("Нельзя нажать очистку: корзина уже пуста.");
        }
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

    /// <summary>
    /// Нажимает «У резерв» и, если приложение запросило склад или сервис,
    /// выбирает первый доступный вариант. После подтверждения ждёт завершения
    /// всей цепочки Angular-запросов, включая создание счёта.
    /// </summary>
    public void ReserveSelectedProducts(
        string preferredWarehouse,
        string? preferredService = null)
    {
        var selectedCard = RequiredSingleSelectedCard();

        var checkpoint = driver.CaptureAngularRequestCheckpoint();
        ClickWhenReady(ReserveInvoiceBy);

        var dialog = WaitForReservationDialogIfPresent();
        if (dialog is not null)
        {
            SelectReservationOption(dialog, preferredWarehouse, preferredService);
        }

        driver.WaitUntilAngularRequestsCompleteAfter(checkpoint, waitTimeout);
        ConfirmProductDeliveryMatrixIfPresent(selectedCard);
        WaitUntilIdle();
    }

    /// <summary>
    /// Создаёт счёт в статусе «Збережений». Такой промежуточный шаг нужен,
    /// когда перед резервированием следует изменить вид доставки или оплату.
    /// </summary>
    public void SaveSelectedProducts(string preferredWarehouse)
    {
        _ = RequiredSingleSelectedCard();

        var checkpoint = driver.CaptureAngularRequestCheckpoint();
        ClickWhenReady(SaveInvoiceBy);

        var dialog = WaitForReservationDialogIfPresent();
        if (dialog is not null)
        {
            SelectReservationOption(dialog, preferredWarehouse, preferredService: null);
        }

        driver.WaitUntilAngularRequestsCompleteAfter(checkpoint, waitTimeout);
        WaitUntilIdle();
    }

    /// <summary>Заполняет панель добавления и подтверждает ввод.</summary>
    private void SubmitAddForm(string identifier, int quantity)
    {
        var input = _wait.Until(_ => VisibleElement(AddCardInputBy));
        input.Clear();
        input.SendKeys(identifier);
        _wait.Until(_ => string.Equals(
            VisibleElement(AddCardInputBy)?.GetAttribute("value"),
            identifier,
            StringComparison.Ordinal));

        SetAddQuantity(quantity);

        // Нажимаем именно кнопку подтверждения формы. Результат операции ждут
        // AddProduct/AddByIdentifier по карточке и количеству. Ожидать здесь
        // глобальный pendingRequests == 0 нельзя: Production держит фоновые
        // запросы расчёта доставки, не относящиеся к добавлению позиции.
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

    private static string ElementText(IWebElement element) =>
        string.IsNullOrWhiteSpace(element.Text)
            ? element.GetAttribute("textContent") ?? string.Empty
            : element.Text;

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

    private string RequiredSingleSelectedCard()
    {
        var selectedCards = SelectedProductCards;
        return selectedCards.Count switch
        {
            1 => selectedCards[0],
            0 => throw new InvalidOperationException(
                "Для создания счёта не выбрана ни одна позиция."),
            _ => throw new InvalidOperationException(
                $"Для создания счёта выбрано несколько позиций: " +
                $"{string.Join(", ", selectedCards)}.")
        };
    }

    private IWebElement? ProductRow(string cardNumber) => driver.FindElements(BasketRowsBy)
        .FirstOrDefault(row => IsVisibleRow(row) && RowBelongsToCard(row, cardNumber));

    private string FindCardRowMarkup(string cardNumber)
    {
        var card = driver.FindElements(By.XPath(
                $"//*[normalize-space(text())='{cardNumber}']"))
            .FirstOrDefault(element => element.Displayed);
        if (card is null) return "<карточка не найдена в DOM>";

        var markup = ((IJavaScriptExecutor)driver)
            .ExecuteScript(
                """
                var element = arguments[0];
                for (var current = element; current; current = current.parentElement) {
                  if (current.querySelector &&
                      current.querySelector("input[type='checkbox']") &&
                      current.querySelector("input, button")) {
                    return JSON.stringify(Array.from(current.querySelectorAll("input, button, a"))
                      .map(function (control) {
                        return {
                          tag: control.tagName,
                          type: control.getAttribute("type"),
                          className: control.getAttribute("class"),
                          value: control.value,
                          text: (control.innerText || "").trim(),
                          ngClick: control.getAttribute("ng-click")
                        };
                      }));
                  }
                }
                return element.outerHTML;
                """,
                card)?.ToString()
            ?? "<outerHTML недоступен>";
        return markup.Length <= 5000 ? markup : markup[..5000] + "…";
    }

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

    private IWebElement? WaitForReservationDialogIfPresent()
    {
        var dialogWait = new WebDriverWait(
            driver,
            TimeSpan.FromSeconds(Math.Min(5, Math.Max(1, waitTimeout.TotalSeconds))));

        try
        {
            return dialogWait.Until(_ => driver.FindElements(VisibleModalBy)
                .FirstOrDefault(element => element.Displayed));
        }
        catch (WebDriverTimeoutException)
        {
            // Для товара с уже определённым виртуальным складом счёт создаётся
            // сразу, без промежуточного диалога.
            return null;
        }
    }

    private void SelectReservationOption(
        IWebElement dialog,
        string preferredWarehouse,
        string? preferredService)
    {
        var title = UiText.NormalizeWhitespace(
            dialog.FindElements(ModalTitleBy).FirstOrDefault()?.Text ?? string.Empty);
        if (!title.Contains("склад", StringComparison.OrdinalIgnoreCase) &&
            !title.Contains("сервіс", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"После резервирования открыт неизвестный диалог '{title}'.");
        }

        var labels = dialog.FindElements(ModalOptionLabelBy)
            .Where(element => element.Displayed && element.Enabled)
            .ToArray();
        var isWarehouseDialog = title.Contains("склад", StringComparison.OrdinalIgnoreCase);
        var requestedOption = isWarehouseDialog ? preferredWarehouse : preferredService;
        var optionLabel = requestedOption is null
            ? labels.FirstOrDefault()
            : labels.FirstOrDefault(label => OptionMatches(label.Text, requestedOption));
        if (optionLabel is null)
        {
            var availableOptions = labels
                .Select(label => UiText.NormalizeWhitespace(label.Text))
                .Where(text => text.Length > 0)
                .ToArray();
            throw new InvalidOperationException(
                $"В диалоге '{title}' нет доступного варианта '{requestedOption}'. " +
                $"Доступны: [{string.Join(", ", availableOptions)}].");
        }

        var option = optionLabel.FindElement(By.CssSelector("input[type='radio']"));
        driver.ClickRobustly(optionLabel);
        _wait.Until(_ => option.Selected);

        var confirm = dialog.FindElements(ModalConfirmBy)
            .FirstOrDefault(element => element.Displayed && element.Enabled)
            ?? throw new InvalidOperationException(
                $"В диалоге '{title}' не найдена кнопка подтверждения.");

        driver.ClickRobustly(confirm);
        _wait.Until(_ => !driver.IsVisible(VisibleModalBy));
    }

    private static bool OptionMatches(string actual, string expected)
    {
        var normalizedActual = UiText.NormalizeWhitespace(actual);
        var normalizedExpected = UiText.NormalizeWhitespace(expected);
        return normalizedActual.Contains(normalizedExpected, StringComparison.OrdinalIgnoreCase) ||
               normalizedExpected.Contains(normalizedActual, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// После выбора склада Test-среда открывает матрицу распределения. Кнопка
    /// имеет дублирующийся id, поэтому дополнительно фиксируем обработчик
    /// <c>reserve('matrixApply')</c>, чтобы не нажать «Відвантажити».
    /// Production может сразу создать резерв без этого окна.
    /// </summary>
    private void ConfirmProductDeliveryMatrixIfPresent(string expectedCard)
    {
        var matrixWait = new WebDriverWait(
            driver,
            TimeSpan.FromSeconds(Math.Min(10, Math.Max(1, waitTimeout.TotalSeconds))));
        IWebElement? reserve;
        try
        {
            reserve = matrixWait.Until(_ => driver.FindElements(MatrixReserveBy)
                .FirstOrDefault(element => element.Displayed && element.Enabled));
        }
        catch (WebDriverTimeoutException)
        {
            // В Production матрица для выбранного виртуального склада не нужна.
            return;
        }

        var matrix = reserve!.FindElements(By.XPath("./ancestor::*[contains(@class,'modal')][1]"))
            .FirstOrDefault();
        if (matrix is not null &&
            !UiText.NormalizeWhitespace(matrix.Text).Contains(expectedCard, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Матрица резервирования не содержит карточку '{expectedCard}'.");
        }

        var checkpoint = driver.CaptureAngularRequestCheckpoint();
        driver.ClickRobustly(reserve);
        driver.WaitUntilAngularRequestsCompleteAfter(checkpoint, waitTimeout);
        _wait.Until(_ => !driver.IsVisible(MatrixReserveBy));
    }

    private static bool HasStock(string stock)
    {
        var normalized = stock.Replace(">", string.Empty, StringComparison.Ordinal).Trim();
        return decimal.TryParse(
                   normalized.Replace(',', '.'),
                   NumberStyles.Number,
                   CultureInfo.InvariantCulture,
                   out var value) && value > 0;
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
