using System.Globalization;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Pages;

public sealed class BasketPage(IWebDriver driver, TimeSpan waitTimeout)
{
    private static readonly By BlockingOverlayBy = By.CssSelector("div.block-ui-overlay");
    private static readonly By AddPositionBy = By.Id("inputBasketAddCardNumber");
    private static readonly By BasketRowsBy = By.CssSelector(".item-basket");
    private static readonly By RowCheckboxLabelBy = By.CssSelector("label:has(input[type='checkbox'])");
    private static readonly By ReserveButtonBy = By.Id("buttonBasketReservationInvoice");
    private static readonly By WarehouseDialogBy = By.XPath(
        "//*[@role='dialog'][.//*[contains(normalize-space(.), 'Виберіть склад')]]");
    private static readonly By WarehouseLabelsBy = By.CssSelector("label:has(input[name='warehouse'])");
    private static readonly By ConfirmWarehouseBy = By.XPath(
        "//*[@role='dialog']//button[normalize-space(.)='Вибрати']");
    private static readonly By InvoiceCreatedToastBy = By.XPath(
        "//*[contains(normalize-space(.), 'Створено рахунок')]");
    private static readonly By ActiveInvoiceFormBy = By.XPath(
        "//form[.//*[contains(normalize-space(.), 'Деталі рахунку')]]");

    private readonly WebDriverWait _wait = new(driver, waitTimeout);

    public bool IsLoaded => driver.IsVisible(AddPositionBy);

    public bool IsInvoiceJournalVisible => driver.FindElements(By.XPath(
            "//a[contains(normalize-space(.), 'Журнал рахунків')]") )
        .Any(element => element.Displayed);

    public IReadOnlyList<string> ProductCards => driver.FindElements(By.CssSelector(".item-basket .basketCard a"))
        .Where(element => element.Displayed)
        .Select(element => UiText.NormalizeWhitespace(element.Text))
        .Where(text => text.Length > 0)
        .ToArray();

    public void Open(string baseUrl)
    {
        driver.Navigate().GoToUrl(new Uri(new Uri(baseUrl), "#/app/basket"));
        _wait.Until(d => d.FindElements(AddPositionBy)
            .Any(element => element.Displayed && element.Enabled));
    }

    public void AddProduct(string cardNumber)
    {
        var input = _wait.Until(d => d.FindElements(AddPositionBy)
            .FirstOrDefault(element => element.Displayed && element.Enabled));
        input.Clear();
        input.SendKeys(cardNumber + Keys.Enter);

        _wait.Until(_ => ProductRow(cardNumber) is not null);
    }

    public bool HasProduct(string cardNumber) => ProductRow(cardNumber) is not null;

    public BasketProductDetails ProductDetails(string cardNumber)
    {
        var row = RequiredProductRow(cardNumber);
        var links = row.FindElements(By.CssSelector("a"))
            .Where(element => element.Displayed)
            .Select(element => UiText.NormalizeWhitespace(element.Text))
            .Where(text => text.Length > 0)
            .ToArray();
        var quantity = row.FindElement(By.CssSelector("input[type='number']"));
        var priceTexts = row.FindElements(By.CssSelector(".price, [class*='price'], ul"))
            .Where(element => element.Displayed)
            .Select(element => UiText.NormalizeWhitespace(element.Text));

        return new BasketProductDetails(
            Card: cardNumber,
            Code: links.First(text => !string.Equals(text, cardNumber, StringComparison.Ordinal) &&
                                      !string.Equals(text, "Аналоги", StringComparison.OrdinalIgnoreCase)),
            Text: UiText.NormalizeWhitespace(row.Text),
            Price: priceTexts.Select(ParseAmount).First(amount => amount > 0),
            Quantity: int.Parse(quantity.GetAttribute("value") ?? "0", CultureInfo.InvariantCulture));
    }

    public WarehouseStockTable WarehouseStocks(string cardNumber)
    {
        var (headerCells, valueCells) = StockCells(cardNumber);
        var headers = headerCells.Select(element => UiText.NormalizeWhitespace(element.Text)).ToArray();
        var values = valueCells.Select(element => UiText.NormalizeWhitespace(element.Text)).ToArray();
        var warehouseCount = Math.Min(headers.Length, values.Length) - 1;
        return new WarehouseStockTable(headers[..warehouseCount], values[..warehouseCount]);
    }

    public int ProductQuantity(string cardNumber) => int.Parse(
        RequiredProductRow(cardNumber).FindElement(By.CssSelector("input[type='number']"))
            .GetAttribute("value") ?? "0",
        CultureInfo.InvariantCulture);

    public void IncreaseQuantity(string cardNumber) =>
        ChangeQuantity(cardNumber, "Increment", ProductQuantity(cardNumber) + 1);

    public void DecreaseQuantity(string cardNumber) =>
        ChangeQuantity(cardNumber, "Decrement", ProductQuantity(cardNumber) - 1);

    public void SetQuantity(string cardNumber, string value)
    {
        var input = RequiredProductRow(cardNumber).FindElement(By.CssSelector("input[type='number']"));
        input.SendKeys(Keys.Control + "a");
        input.SendKeys(value + Keys.Tab);
    }

    public decimal SelectedTotal => ParseAmount(_wait.Until(d => d.FindElements(By.XPath(
            "//*[contains(normalize-space(.), 'Загальна сума обраних в кошику')]/following::strong[1]"))
        .First(element => element.Displayed)).Text);

    public IReadOnlyList<bool> SelectionStates => driver.FindElements(BasketRowsBy)
        .Where(row => row.Displayed)
        .Select(row => row.FindElements(By.CssSelector("input[type='checkbox']")).FirstOrDefault()?.Selected == true)
        .ToArray();

    public void RestoreSelectionStates(IReadOnlyList<bool> states)
    {
        var rows = driver.FindElements(BasketRowsBy).Where(row => row.Displayed).ToArray();
        if (rows.Length != states.Count)
        {
            throw new InvalidOperationException(
                $"Cannot restore basket selection: expected {states.Count} rows, found {rows.Length}.");
        }

        for (var index = 0; index < rows.Length; index++)
        {
            var checkbox = rows[index].FindElement(By.CssSelector("input[type='checkbox']"));
            if (checkbox.Selected == states[index]) continue;
            driver.ClickRobustly(rows[index].FindElements(By.CssSelector("label")).First());
            var expected = states[index];
            _wait.Until(_ => checkbox.IsStale() || checkbox.Selected == expected);
        }
    }

    public void SetSelectAll(bool selected)
    {
        var label = _wait.Until(d => d.FindElements(By.XPath(
                "//label[contains(normalize-space(.), 'Вибрати всі')]") )
            .First(element => element.Displayed));
        var checkbox = label.FindElement(By.CssSelector("input[type='checkbox']"));
        if (checkbox.Selected != selected) driver.ClickRobustly(label);
        _wait.Until(_ => SelectionStates.All(state => state == selected));
    }

    public void RemoveProduct(string cardNumber)
    {
        var row = ProductRow(cardNumber);
        if (row is null) return;

        var action = row.FindElements(By.CssSelector("[ng-click]"))
            .FirstOrDefault(element =>
            {
                var handler = element.GetAttribute("ng-click") ?? string.Empty;
                return handler.Contains("remove", StringComparison.OrdinalIgnoreCase) ||
                       handler.Contains("delete", StringComparison.OrdinalIgnoreCase);
            });
        action ??= row.FindElements(By.CssSelector(".fa-close, .fa-remove")).FirstOrDefault();
        if (action is null) throw new InvalidOperationException($"Remove action is absent for '{cardNumber}'.");

        driver.ClickRobustly(action);
        try
        {
            driver.SwitchTo().Alert().Accept();
        }
        catch (NoAlertPresentException)
        {
        }

        _wait.Until(_ => ProductRow(cardNumber) is null);
    }

    public WarehouseChoice PositiveStockWarehouse(string cardNumber)
    {
        var (headers, values) = StockCells(cardNumber);
        var warehouseCount = Math.Min(headers.Count, values.Count) - 1;

        for (var index = 0; index < warehouseCount; index++)
        {
            var stockText = UiText.NormalizeWhitespace(values[index].Text);
            if (IsPositiveStock(stockText))
            {
                return new WarehouseChoice(
                    Index: index,
                    ColumnName: UiText.NormalizeWhitespace(headers[index].Text),
                    Stock: stockText);
            }
        }

        throw new InvalidOperationException(
            $"Product '{cardNumber}' has no positive stock in the visible warehouse columns.");
    }

    public void SelectOnlyProduct(string cardNumber)
    {
        var rows = driver.FindElements(BasketRowsBy).Where(element => element.Displayed).ToArray();
        foreach (var row in rows)
        {
            var checkbox = row.FindElements(By.CssSelector("input[type='checkbox']")).FirstOrDefault();
            if (checkbox is null) continue;

            var shouldBeSelected = ContainsExactText(row, cardNumber);
            if (checkbox.Selected == shouldBeSelected) continue;

            var label = row.FindElements(RowCheckboxLabelBy).First();
            driver.ClickRobustly(label);
            _wait.Until(_ => checkbox.IsStale() || checkbox.Selected == shouldBeSelected);
        }

        _wait.Until(_ => SelectedProductCards().SequenceEqual([cardNumber]));
    }

    public string ReserveFromWarehouse(WarehouseChoice warehouse)
    {
        ClickWhenReady(ReserveButtonBy);
        var dialog = _wait.Until(d => d.FindElements(WarehouseDialogBy)
            .FirstOrDefault(element => element.Displayed));
        var warehouseLabels = dialog.FindElements(WarehouseLabelsBy)
            .Where(element => element.Displayed)
            .ToArray();
        if (warehouse.Index >= warehouseLabels.Length)
        {
            throw new InvalidOperationException(
                $"Warehouse column '{warehouse.ColumnName}' has no corresponding reserve option.");
        }

        var selectedWarehouse = UiText.NormalizeWhitespace(warehouseLabels[warehouse.Index].Text);
        driver.ClickRobustly(warehouseLabels[warehouse.Index]);
        ClickWhenReady(ConfirmWarehouseBy);

        _wait.Until(d => d.IsVisible(InvoiceCreatedToastBy));
        _wait.Until(d => d.FindElements(ActiveInvoiceFormBy).Any(element => element.Displayed));
        return selectedWarehouse;
    }

    public IReadOnlyList<string> ActiveInvoiceProductCards()
    {
        var invoice = _wait.Until(d => d.FindElements(ActiveInvoiceFormBy)
            .FirstOrDefault(element => element.Displayed));

        return invoice.FindElements(By.CssSelector(".basketCard a, a"))
            .Select(element => UiText.NormalizeWhitespace(element.Text))
            .Where(text => text.All(char.IsDigit) && text.Length >= 6)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public bool ActiveInvoiceContainsWarehouse(string warehouse) =>
        UiText.NormalizeWhitespace(_wait.Until(d => d.FindElements(ActiveInvoiceFormBy)
            .First(element => element.Displayed)).Text)
            .Contains(warehouse, StringComparison.OrdinalIgnoreCase);

    private IWebElement? ProductRow(string cardNumber) =>
        driver.FindElements(BasketRowsBy)
            .FirstOrDefault(row => row.Displayed && ContainsExactText(row, cardNumber));

    private IWebElement RequiredProductRow(string cardNumber) => ProductRow(cardNumber) ??
        throw new InvalidOperationException($"Product '{cardNumber}' is absent from the basket.");

    private (IReadOnlyList<IWebElement> Headers, IReadOnlyList<IWebElement> Values) StockCells(
        string cardNumber)
    {
        var row = RequiredProductRow(cardNumber);
        var itemRows = row.FindElements(By.CssSelector("table tr"));
        if (itemRows.Count == 0)
        {
            throw new InvalidOperationException($"Stock table is absent for product '{cardNumber}'.");
        }

        if (itemRows.Count >= 2)
        {
            return (
                itemRows[0].FindElements(By.CssSelector("th,td")),
                itemRows[1].FindElements(By.CssSelector("th,td")));
        }

        var headerRow = driver.FindElements(By.CssSelector("table tr"))
            .FirstOrDefault(candidate => candidate.Displayed &&
                candidate.FindElements(By.CssSelector("th,td"))
                    .Any(cell => UiText.NormalizeWhitespace(cell.Text) == "Всі скл."));
        if (headerRow is null)
        {
            throw new InvalidOperationException("Warehouse header row is absent.");
        }

        return (
            headerRow.FindElements(By.CssSelector("th,td")),
            itemRows[0].FindElements(By.CssSelector("th,td")));
    }

    private void ChangeQuantity(string cardNumber, string actionName, int expected)
    {
        var row = RequiredProductRow(cardNumber);
        var action = row.FindElements(By.CssSelector("[ng-click]"))
            .First(element => (element.GetAttribute("ng-click") ?? string.Empty)
                .Contains(actionName, StringComparison.OrdinalIgnoreCase));
        driver.ClickRobustly(action);
        _wait.Until(_ => ProductQuantity(cardNumber) == expected);
    }

    private IReadOnlyList<string> SelectedProductCards() =>
        driver.FindElements(BasketRowsBy)
            .Where(row => row.Displayed && row.FindElements(By.CssSelector("input[type='checkbox']"))
                .FirstOrDefault()?.Selected == true)
            .SelectMany(row => row.FindElements(By.CssSelector(".basketCard a")))
            .Select(element => UiText.NormalizeWhitespace(element.Text))
            .Where(text => text.Length > 0)
            .ToArray();

    private void ClickWhenReady(By by)
    {
        _wait.Until(d =>
        {
            if (d.IsVisible(BlockingOverlayBy)) return false;
            var element = d.FindElements(by)
                .FirstOrDefault(candidate => candidate.Displayed && candidate.Enabled);
            if (element is null) return false;

            try
            {
                d.ClickRobustly(element);
                return true;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });
    }

    private static bool ContainsExactText(IWebElement root, string text) =>
        root.FindElements(By.XPath($".//*[normalize-space(.)={XPathHelpers.Literal(text)}]"))
            .Any(element => element.Displayed);

    private static bool IsPositiveStock(string value)
    {
        var numeric = new string(value.Where(character => char.IsDigit(character) || character is '.' or ',').ToArray())
            .Replace(',', '.');
        return decimal.TryParse(numeric, NumberStyles.Number, CultureInfo.InvariantCulture, out var stock) &&
               stock > 0;
    }

    private static decimal ParseAmount(string value)
    {
        var normalized = new string(value.Where(character =>
                char.IsDigit(character) || character is '.' or ',' or '-').ToArray())
            .Replace(" ", string.Empty)
            .Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : 0;
    }
}

public sealed record WarehouseChoice(int Index, string ColumnName, string Stock);

public sealed record WarehouseStockTable(
    IReadOnlyList<string> Headers,
    IReadOnlyList<string> Values);

public sealed record BasketProductDetails(
    string Card,
    string Code,
    string Text,
    decimal Price,
    int Quantity);
