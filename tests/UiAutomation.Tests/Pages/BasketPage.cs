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

    public WarehouseChoice PositiveStockWarehouse(string cardNumber)
    {
        var row = ProductRow(cardNumber) ??
                  throw new InvalidOperationException($"Product '{cardNumber}' is absent from the basket.");
        var tableRows = row.FindElements(By.CssSelector("table tr"));
        if (tableRows.Count < 2)
        {
            throw new InvalidOperationException($"Stock table is absent for product '{cardNumber}'.");
        }

        var headers = tableRows[0].FindElements(By.CssSelector("th,td"));
        var values = tableRows[1].FindElements(By.CssSelector("th,td"));
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
}

public sealed record WarehouseChoice(int Index, string ColumnName, string Stock);
