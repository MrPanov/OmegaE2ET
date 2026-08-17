using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Text.RegularExpressions;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Pages;

/// <summary>
/// Счета, открытые во вложенных вкладках корзины. Страница намеренно работает
/// только с точным номером счёта: тестовый клиент общий, поэтому удаление
/// «последней строки» или «активного счёта» могло бы затронуть чужой документ.
/// </summary>
public sealed class BasketInvoicePage(IWebDriver driver, TimeSpan waitTimeout)
{
    private static readonly By InvoiceTabsBy = By.CssSelector("li[ng-repeat='tab in tabs']");
    private static readonly By ActivePaneBy = By.CssSelector(".tab-pane.active");
    private static readonly By InvoiceProductCardBy = By.CssSelector(
        "a[ng-click='openProductCard(item.Product)']");
    private static readonly By InvoiceJournalTabBy = By.CssSelector(
        "li[select=\"selecttab('journal')\"]");
    private static readonly By JournalRowsBy = By.CssSelector("table tbody tr");
    private static readonly By JournalInvoiceLinkBy = By.CssSelector("a[ng-click='activate(item)']");
    private static readonly By JournalInvoiceDeleteBy = By.CssSelector(
        "#buttonBasketRemoveInvoice[ng-click='deleteInvoice($event, item)']");
    private static readonly By ShipmentTypeBy = By.XPath(
        ".//*[@id='slctshipmentType'] | " +
        ".//omega-selectize[@options-selectize='shipmentTypeList'] | " +
        ".//div[contains(concat(' ', normalize-space(@class), ' '), ' ui-select-container ')][" +
        "contains(normalize-space(.), \"Кур'єрські служби\") or " +
        "contains(normalize-space(.), 'Планова доставка') or " +
        "contains(normalize-space(.), 'Самовивіз') or " +
        "contains(normalize-space(.), 'Експрес доставка')]");
    private static readonly By WarehouseBy = By.Id("slctWarehouse");
    private static readonly By PaymentTypeBy = By.CssSelector(
        "omega-selectize[options-selectize='lists.paymentList']");
    private static readonly By SelectizeDescriptionBy = By.CssSelector(".description-label");
    private static readonly By SelectizeOptionBy = By.CssSelector(
        ".selectize-dropdown-content .option[data-selectable]");
    private static readonly By UiSelectToggleBy = By.CssSelector(
        ".ui-select-toggle, .ui-select-match");
    private static readonly By UiSelectValueBy = By.CssSelector(
        ".ui-select-match-text, .ui-select-match");
    private static readonly By UiSelectOptionBy = By.CssSelector(
        ".ui-select-choices-row");
    private static readonly By ActiveInvoiceReserveBy = By.CssSelector(
        "#buttonBasketReservationInvoice[ng-click=\"save($event,'Apply')\"]");

    private readonly WebDriverWait _wait = new(driver, waitTimeout);

    /// <summary>Номера счетов, для которых в корзине уже открыты вкладки.</summary>
    public IReadOnlyList<string> OpenInvoiceNumbers => driver.FindElements(InvoiceTabsBy)
        .Where(IsVisible)
        .Select(element => UiText.NormalizeWhitespace(element.Text))
        .Where(text => text.Length > 0)
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    /// <summary>
    /// Ждёт ровно одну новую вкладку относительно снимка до создания и возвращает
    /// её номер. Более одного нового номера считается небезопасной неоднозначностью.
    /// </summary>
    public string WaitForNewInvoiceNumber(IReadOnlyCollection<string> numbersBefore)
    {
        var baseline = numbersBefore.ToHashSet(StringComparer.Ordinal);

        return _wait.Until(_ =>
        {
            var added = OpenInvoiceNumbers
                .Where(number => !baseline.Contains(number))
                .ToArray();

            return added.Length switch
            {
                0 => null,
                1 => added[0],
                _ => throw new InvalidOperationException(
                    $"После создания появились несколько новых счетов: {string.Join(", ", added)}.")
            };
        })!;
    }

    /// <summary>Возвращает единственный новый номер, если он уже появился.</summary>
    public string? FindNewInvoiceNumber(IReadOnlyCollection<string> numbersBefore)
    {
        var baseline = numbersBefore.ToHashSet(StringComparer.Ordinal);
        var added = OpenInvoiceNumbers
            .Where(number => !baseline.Contains(number))
            .ToArray();

        return added.Length switch
        {
            0 => null,
            1 => added[0],
            _ => throw new InvalidOperationException(
                $"Нельзя однозначно определить созданный счёт: {string.Join(", ", added)}.")
        };
    }

    public bool IsReserved(string invoiceNumber) => HasAnyText(
        invoiceNumber,
        "Зарезервований",
        "Зарезервирован");

    public bool IsSaved(string invoiceNumber) => HasAnyText(
        invoiceNumber,
        "Збережений",
        "Сохранён");

    public bool HasProduct(string invoiceNumber, string cardNumber)
    {
        var pane = OpenInvoice(invoiceNumber);
        return pane.FindElements(InvoiceProductCardBy)
            .Any(link => string.Equals(
                UiText.NormalizeWhitespace(link.Text),
                cardNumber,
                StringComparison.Ordinal));
    }

    /// <summary>
    /// Возвращает значение колонки «Резерв» для конкретной карточки. Индекс
    /// колонки определяется по заголовку таблицы, поэтому добавление новых
    /// колонок в счёт не сдвинет проверку на чужое значение.
    /// </summary>
    public int ReservedQuantity(string invoiceNumber, string cardNumber)
    {
        var pane = OpenInvoice(invoiceNumber);
        var cardLink = pane.FindElements(InvoiceProductCardBy)
            .FirstOrDefault(link => string.Equals(
                UiText.NormalizeWhitespace(link.Text),
                cardNumber,
                StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"В счёте '{invoiceNumber}' нет карточки '{cardNumber}'.");

        var row = cardLink.FindElement(By.XPath("./ancestor::tr[1]"));
        var table = row.FindElement(By.XPath("./ancestor::table[1]"));
        var headers = table.FindElements(By.CssSelector("thead th"))
            .Select(header => UiText.NormalizeWhitespace(header.Text))
            .ToArray();
        var reserveColumn = Array.FindIndex(headers, header =>
            string.Equals(header, "Резерв", StringComparison.OrdinalIgnoreCase));
        if (reserveColumn < 0)
        {
            throw new InvalidOperationException(
                $"В счёте '{invoiceNumber}' не найдена колонка 'Резерв'. " +
                $"Колонки: [{string.Join(", ", headers)}].");
        }

        var cells = row.FindElements(By.CssSelector("td"));
        if (reserveColumn >= cells.Count)
        {
            throw new InvalidOperationException(
                $"В строке карточки '{cardNumber}' нет ячейки колонки 'Резерв'.");
        }

        var value = UiText.NormalizeWhitespace(cells[reserveColumn].Text);
        if (!int.TryParse(value, out var quantity))
        {
            throw new InvalidOperationException(
                $"Значение резерва карточки '{cardNumber}' не является числом: '{value}'.");
        }

        return quantity;
    }

    public string WarehouseName(string invoiceNumber)
    {
        var pane = OpenInvoice(invoiceNumber);
        var warehouse = pane.FindElements(WarehouseBy)
            .FirstOrDefault(element => element.Displayed);
        if (warehouse is not null)
        {
            var description = warehouse.FindElements(SelectizeDescriptionBy)
                .FirstOrDefault(element => element.Displayed);
            if (description is not null)
            {
                return UiText.NormalizeWhitespace(description.Text);
            }
        }

        var match = Regex.Match(
            UiText.NormalizeWhitespace(pane.Text),
            @"(?:^|\s)Склад\s+(.+?)\s+Замітка(?:\s|$)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? UiText.NormalizeWhitespace(match.Groups[1].Value) : string.Empty;
    }

    public bool HasWarehouse(string invoiceNumber, string warehouseName) =>
        NormalizeWarehouse(WarehouseName(invoiceNumber))
            .Contains(NormalizeWarehouse(warehouseName), StringComparison.Ordinal);

    public bool HasDeliveryType(string invoiceNumber, params string[] deliveryTypes)
    {
        var invoiceText = ActiveInvoiceText(invoiceNumber);
        return deliveryTypes.Any(deliveryType => Regex.IsMatch(
            invoiceText,
            $@"(?:Вид\s+доставки\s+)?{Regex.Escape(deliveryType)}(?:\s|$)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
    }

    public bool HasPaymentType(string invoiceNumber, string paymentType)
    {
        var invoiceText = ActiveInvoiceText(invoiceNumber);
        return Regex.IsMatch(
            invoiceText,
            $@"Оплата\s+{Regex.Escape(paymentType)}(?:\s|$)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    /// <summary>Выбирает вид доставки в сохранённом счёте.</summary>
    public void SelectDeliveryType(string invoiceNumber, string deliveryType) =>
        SelectOption(invoiceNumber, ShipmentTypeBy, deliveryType, "Вид доставки");

    /// <summary>Выбирает способ оплаты в сохранённом счёте.</summary>
    public void SelectPaymentType(string invoiceNumber, string paymentType) =>
        SelectOption(invoiceNumber, PaymentTypeBy, paymentType, "Оплата");

    /// <summary>Переводит заранее настроенный сохранённый счёт в резерв.</summary>
    public void Reserve(string invoiceNumber)
    {
        var pane = OpenInvoice(invoiceNumber);
        var reserve = pane.FindElements(ActiveInvoiceReserveBy)
            .FirstOrDefault(element => element.Displayed && element.Enabled)
            ?? throw new InvalidOperationException(
                $"У сохранённого счёта '{invoiceNumber}' недоступна кнопка резервирования.");

        var checkpoint = driver.CaptureAngularRequestCheckpoint();
        driver.ClickRobustly(reserve);
        driver.WaitUntilAngularRequestsCompleteAfter(checkpoint, waitTimeout);
        _wait.Until(_ => IsReserved(invoiceNumber));
    }

    public string Description(string invoiceNumber) => ActiveInvoiceText(invoiceNumber);

    /// <summary>
    /// Удаляет только указанный счёт через «Журнал рахунків». Кнопка ищется
    /// внутри строки точного номера, поэтому соседний документ удалить нельзя.
    /// </summary>
    public void Delete(string invoiceNumber)
    {
        OpenJournal();
        var row = _wait.Until(_ => JournalRow(invoiceNumber));
        var journalDelete = row.FindElements(JournalInvoiceDeleteBy)
            .FirstOrDefault(IsVisible)
            ?? throw new InvalidOperationException(
                $"В журнале у счёта '{invoiceNumber}' нет доступной кнопки удаления.");

        DeleteWithConfirmation(journalDelete);
        WaitUntilDeletedFromJournal(invoiceNumber);
    }

    private IWebElement OpenInvoice(string invoiceNumber)
    {
        var tab = InvoiceTab(invoiceNumber)
            ?? throw new InvalidOperationException(
                $"Вкладка счёта '{invoiceNumber}' не открыта.");

        var classes = tab.GetAttribute("class") ?? string.Empty;
        if (!classes.Contains("active", StringComparison.OrdinalIgnoreCase))
        {
            driver.ClickRobustly(tab);
        }

        return _wait.Until(_ => ActiveInvoicePane(invoiceNumber));
    }

    private string ActiveInvoiceText(string invoiceNumber) =>
        UiText.NormalizeWhitespace(OpenInvoice(invoiceNumber).Text);

    private void SelectOption(
        string invoiceNumber,
        By fieldBy,
        string expectedValue,
        string fieldName)
    {
        var field = RequiredVisibleField(OpenInvoice(invoiceNumber), fieldBy, fieldName);
        var isUiSelect = HasCssClass(field, "ui-select-container");
        var currentValueElement = isUiSelect
            ? field.FindElements(UiSelectValueBy).FirstOrDefault(element => element.Displayed)
            : field.FindElements(SelectizeDescriptionBy).FirstOrDefault(element => element.Displayed);
        if (currentValueElement is null)
        {
            throw new InvalidOperationException(
                $"В счёте '{invoiceNumber}' не удалось прочитать поле '{fieldName}'.");
        }

        var currentValue = UiText.NormalizeWhitespace(currentValueElement.Text);
        if (OptionMatches(currentValue, expectedValue))
        {
            return;
        }

        if (isUiSelect &&
            (!field.Enabled ||
             field.GetAttribute("disabled") is not null ||
             HasCssClass(field, "disabled")))
        {
            throw new InvalidOperationException(
                $"В счёте '{invoiceNumber}' поле '{fieldName}' заблокировано. " +
                $"Текущее значение: '{currentValue}', ожидается: '{expectedValue}'.");
        }

        if (isUiSelect)
        {
            var toggle = field.FindElements(UiSelectToggleBy)
                .FirstOrDefault(element => element.Displayed && element.Enabled)
                ?? throw new InvalidOperationException(
                    $"В счёте '{invoiceNumber}' не удалось открыть поле '{fieldName}'.");
            driver.ClickRobustly(toggle);
        }
        else
        {
            // Клик по вложенной иконке вызывает openlist() и на самой иконке, и
            // на родителе. Текст значения всплывает только к одному обработчику.
            driver.ClickRobustly(currentValueElement);
        }

        var optionWait = new WebDriverWait(
            driver,
            TimeSpan.FromSeconds(Math.Min(10, Math.Max(1, waitTimeout.TotalSeconds))));
        var option = optionWait.Until(_ =>
        {
            var refreshedField = RequiredVisibleField(
                OpenInvoice(invoiceNumber),
                fieldBy,
                fieldName);
            var optionBy = HasCssClass(refreshedField, "ui-select-container")
                ? UiSelectOptionBy
                : SelectizeOptionBy;
            return refreshedField.FindElements(optionBy)
                .FirstOrDefault(element =>
                    element.Displayed &&
                    OptionMatches(element.Text, expectedValue));
        });

        var checkpoint = driver.CaptureAngularRequestCheckpoint();
        driver.ClickRobustly(option);
        driver.WaitUntilAngularRequestsCompleteAfter(checkpoint, waitTimeout);

        _wait.Until(_ => OptionMatches(
            CurrentOptionValue(RequiredVisibleField(
                OpenInvoice(invoiceNumber),
                fieldBy,
                fieldName)),
            expectedValue));
    }

    private static string CurrentOptionValue(IWebElement field)
    {
        var valueBy = HasCssClass(field, "ui-select-container")
            ? UiSelectValueBy
            : SelectizeDescriptionBy;
        var value = field.FindElements(valueBy)
            .FirstOrDefault(element => element.Displayed);
        return UiText.NormalizeWhitespace(value?.Text ?? string.Empty);
    }

    private static bool HasCssClass(IWebElement element, string className) =>
        (element.GetAttribute("class") ?? string.Empty)
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Contains(className, StringComparer.OrdinalIgnoreCase);

    private static bool OptionMatches(string actual, string expected)
    {
        var normalizedActual = UiText.NormalizeWhitespace(actual);
        var normalizedExpected = UiText.NormalizeWhitespace(expected);
        return normalizedActual.Contains(normalizedExpected, StringComparison.OrdinalIgnoreCase) ||
               normalizedExpected.Contains(normalizedActual, StringComparison.OrdinalIgnoreCase);
    }

    private static IWebElement RequiredVisibleField(
        IWebElement pane,
        By fieldBy,
        string fieldName)
    {
        var field = pane.FindElements(fieldBy)
            .FirstOrDefault(element => element.Displayed);
        if (field is not null) return field;

        var candidates = pane.FindElements(By.CssSelector(
                "omega-selectize, select, [class*='dropdown']"))
            .Where(element => element.Displayed)
            .Select(element =>
                $"tag={element.TagName}; id={element.GetAttribute("id")}; " +
                $"class={element.GetAttribute("class")}; " +
                $"label={element.GetAttribute("label")}; " +
                $"options={element.GetAttribute("options-selectize")}; " +
                $"text={UiText.NormalizeWhitespace(element.Text)}")
            .ToArray();
        throw new InvalidOperationException(
            $"В активном счёте не найдено поле '{fieldName}'. " +
            $"Доступные select-компоненты: [{string.Join(" | ", candidates)}].");
    }

    private bool HasAnyText(string invoiceNumber, params string[] values)
    {
        var invoiceText = ActiveInvoiceText(invoiceNumber);
        return values.Any(value =>
            invoiceText.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private IWebElement? InvoiceTab(string invoiceNumber) => driver.FindElements(InvoiceTabsBy)
        .FirstOrDefault(element =>
            IsVisible(element) &&
            string.Equals(
                UiText.NormalizeWhitespace(element.Text),
                invoiceNumber,
                StringComparison.Ordinal));

    private IWebElement? ActiveInvoicePane(string invoiceNumber) => driver.FindElements(ActivePaneBy)
        .FirstOrDefault(element =>
            IsVisible(element) &&
            UiText.NormalizeWhitespace(element.Text)
                .Contains(invoiceNumber, StringComparison.Ordinal));

    private void OpenJournal()
    {
        var tab = _wait.Until(_ => driver.FindElements(InvoiceJournalTabBy)
            .FirstOrDefault(IsVisible));
        driver.ClickRobustly(tab);
        _wait.Until(_ => driver.FindElements(JournalRowsBy).Any(IsVisible));
    }

    private IWebElement? JournalRow(string invoiceNumber) => driver.FindElements(JournalRowsBy)
        .FirstOrDefault(row =>
            IsVisible(row) &&
            row.FindElements(JournalInvoiceLinkBy).Any(link =>
                string.Equals(
                    UiText.NormalizeWhitespace(link.Text),
                    invoiceNumber,
                    StringComparison.Ordinal)));

    private void DeleteWithConfirmation(IWebElement delete)
    {
        driver.ClickRobustly(delete);

        var alert = _wait.Until(_ =>
        {
            try
            {
                return driver.SwitchTo().Alert();
            }
            catch (NoAlertPresentException)
            {
                return null;
            }
        });
        alert.Accept();
        // Вызывающие методы ждут исчезновения точной вкладки или строки счёта.
        // Это надёжнее глобального pendingRequests == 0: Production параллельно
        // держит фоновые запросы расчёта доставки.
    }

    private void WaitUntilDeletedFromJournal(string invoiceNumber)
    {
        _wait.Until(_ =>
        {
            var row = JournalRow(invoiceNumber);
            return row is null || UiText.NormalizeWhitespace(row.Text)
                .Contains("Вилучений", StringComparison.OrdinalIgnoreCase);
        });

        driver.Navigate().Refresh();
        OpenJournal();
        _wait.Until(_ => JournalRow(invoiceNumber) is null);
    }

    private static bool IsVisible(IWebElement element)
    {
        try
        {
            return element.Displayed;
        }
        catch (StaleElementReferenceException)
        {
            return false;
        }
    }

    /// <summary>
    /// В корзине склад показан как «Київ (Правий Північ) РС», а в заголовке
    /// счёта скобки и служебная пометка «РС» могут отсутствовать. Для сравнения
    /// оставляем только смысловые буквы и цифры.
    /// </summary>
    private static string NormalizeWarehouse(string value)
    {
        var withoutDisplayMarkers = Regex.Replace(
            UiText.NormalizeWhitespace(value).ToLowerInvariant(),
            @"\b(?:рс|берег)\b",
            string.Empty,
            RegexOptions.CultureInvariant);
        return Regex.Replace(
            withoutDisplayMarkers,
            @"[^\p{L}\p{Nd}]+",
            string.Empty,
            RegexOptions.CultureInvariant);
    }
}
