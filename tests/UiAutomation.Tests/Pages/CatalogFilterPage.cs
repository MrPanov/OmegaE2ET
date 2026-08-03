using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Text.RegularExpressions;

namespace UiAutomation.Tests.Pages;

/// <summary>
/// Faceted-filter catalog pages (<c>#/app/simplesearch*</c>: Шини, Оливи, АКБ …).
/// All of these share the same markup: a left accordion of facet panels
/// (<c>div.accordion-newFilter</c>), an apply button and a reset link, and a
/// result list that reuses the standard search-result classes
/// (<c>a.searchProdCard</c>, <c>.searchDescrip</c>, <c>.brandSearch</c>).
/// </summary>
public sealed class CatalogFilterPage(IWebDriver driver, TimeSpan waitTimeout)
{
    private readonly WebDriverWait _wait = new(driver, waitTimeout);

    private static readonly By FacetPanelBy = By.CssSelector("div.accordion-newFilter");
    private static readonly By OverlayBy = By.CssSelector("div.block-ui-overlay");
    private static readonly By ApplyButtonBy = By.CssSelector("button.btn-apply-filter");
    private static readonly By ResetButtonBy =
        By.CssSelector("a[ng-click='filterModel.resetModel()']");
    private static readonly By OptionLabelBy =
        By.CssSelector("span.text[ng-click*='selectedLabelClick']");
    private static readonly By InStockCheckboxBy =
        By.CssSelector("input[ng-model='filterModel.rest']");
    private static readonly By ListViewToggleBy = By.CssSelector("a[ng-click='setView(1)']");
    // Per-warehouse immediate quantity cell (КВ-Ш / ХРК-Ш …), list view only.
    private static readonly By StockQuantityBy = By.CssSelector("span[ng-style*='war.rest']");
    private static readonly By ProductCardBy = By.CssSelector("a.searchProdCard");
    private static readonly By ProductDescriptionBy = By.CssSelector(".searchDescrip");
    private static readonly By ProductBrandBy = By.CssSelector(".brandSearch");

    public IReadOnlyList<string> ProductDescriptions => VisibleTexts(ProductDescriptionBy);

    public IReadOnlyList<string> ProductBrands => VisibleTexts(ProductBrandBy);

    public int ResultCount => driver.FindElements(ProductCardBy).Count(e => e.Displayed);

    /// <summary>At least one facet value is selected (the reset link is enabled).</summary>
    public bool HasActiveFilters
    {
        get
        {
            var reset = driver.FindElements(ResetButtonBy).FirstOrDefault(e => e.Displayed);
            if (reset is null) return false;
            var classes = (reset.GetAttribute("class") ?? string.Empty).Split(' ');
            return !classes.Contains("disabledbutton");
        }
    }

    public void WaitUntilLoaded()
    {
        _wait.Until(d => !IsVisible(d, OverlayBy));
        _wait.Until(d => d.FindElements(FacetPanelBy).Any(e => e.Displayed));
    }

    /// <summary>
    /// Selects the first available value of the facet whose heading matches
    /// <paramref name="facetTitle"/> (e.g. "Бренд") and returns the selected option.
    /// The caller applies it with <see cref="ApplyFilters"/>.
    /// </summary>
    public FacetOption SelectFirstFacetOption(string facetTitle)
    {
        var panel = FacetPanel(facetTitle);
        ExpandPanel(panel);

        var option = _wait.Until(_ =>
            panel.FindElements(OptionLabelBy)
                .FirstOrDefault(e => e.Displayed && e.Text.Trim().Length > 0));

        return SelectOption(option);
    }

    /// <summary>
    /// Selects the facet value whose visible label equals <paramref name="optionValue"/>
    /// (ignoring the trailing "(count)"), e.g. facet "Діаметр" value "16".
    /// </summary>
    public FacetOption SelectFacetOption(string facetTitle, string optionValue)
    {
        var panel = FacetPanel(facetTitle);
        ExpandPanel(panel);

        var option = _wait.Until(_ =>
            panel.FindElements(OptionLabelBy)
                .FirstOrDefault(e => e.Displayed && string.Equals(
                    ParseOption(NormalizeWhitespace(e.Text)).Value,
                    optionValue,
                    StringComparison.OrdinalIgnoreCase)));

        return SelectOption(option);
    }

    private FacetOption SelectOption(IWebElement optionLabel)
    {
        var parsed = ParseOption(NormalizeWhitespace(optionLabel.Text));
        ClickRobustly(optionLabel);

        var checkbox = optionLabel
            .FindElement(By.XPath(
                "ancestor::div[contains(concat(' ', normalize-space(@class), ' '), ' checkbox ')][1]"))
            .FindElement(By.CssSelector("input[type='checkbox']"));
        _wait.Until(_ => checkbox.Selected);

        return parsed;
    }

    /// <summary>
    /// Switches the result list to the table ("списком") view. The per-warehouse
    /// stock columns (КВ-Ш, ХРК-Ш …) and the in-stock checkbox exist only there.
    /// </summary>
    public void SwitchToListView()
    {
        var toggle = _wait.Until(d =>
            d.FindElements(ListViewToggleBy).FirstOrDefault(e => e.Displayed && e.Enabled));
        ClickRobustly(toggle);
        // The in-stock checkbox exists only in the table view; its real <input> is
        // visually hidden (custom control), so wait for presence rather than display.
        _wait.Until(d => d.FindElements(InStockCheckboxBy).Count > 0);
    }

    /// <summary>
    /// Product codes shown with zero stock in every warehouse column. A product
    /// counts as available if any warehouse (e.g. КВ-Ш or ХРК-Ш) shows ≥ 1 pc.
    /// Requires the table view (see <see cref="SwitchToListView"/>).
    /// </summary>
    public IReadOnlyList<string> ProductsWithoutStock()
    {
        var missing = new List<string>();
        foreach (var card in driver.FindElements(ProductCardBy).Where(e => e.Displayed))
        {
            var row = card.FindElement(By.XPath(
                "ancestor::*[.//span[contains(@ng-style, 'war.rest')]][1]"));
            var quantities = row.FindElements(StockQuantityBy)
                .Select(cell => LeadingInt(cell.Text));
            if (!quantities.Any(quantity => quantity >= 1))
            {
                missing.Add(NormalizeWhitespace(card.Text));
            }
        }

        return missing;
    }

    /// <summary>Ticks the "Тільки товар у наявності" (in-stock only) checkbox.</summary>
    public void EnableInStockOnly()
    {
        // The real <input> is visually hidden, so match by presence and let
        // ClickRobustly fall back to a scripted click.
        var checkbox = _wait.Until(d => d.FindElements(InStockCheckboxBy).FirstOrDefault());
        if (checkbox.Selected) return;

        ClickRobustly(checkbox);
        _wait.Until(_ => checkbox.Selected);
    }

    public void ApplyFilters()
    {
        var previous = driver.FindElements(ProductCardBy).FirstOrDefault(e => e.Displayed);

        var apply = _wait.Until(d =>
            d.FindElements(ApplyButtonBy).FirstOrDefault(e => e.Displayed && e.Enabled));
        ClickRobustly(apply);

        if (previous is not null)
        {
            try
            {
                new WebDriverWait(driver, TimeSpan.FromSeconds(2)).Until(d =>
                    IsStale(previous) || IsVisible(d, OverlayBy));
            }
            catch (WebDriverTimeoutException)
            {
                // The list may be rebuilt faster than the overlay is observed.
            }
        }

        _wait.Until(d => !IsVisible(d, OverlayBy));
        // The overlay clears before Angular finishes re-rendering the result list,
        // so wait until the filtered products are actually on the page.
        _wait.Until(d => d.FindElements(ProductCardBy).Any(e => e.Displayed));
    }

    public void ResetFilters()
    {
        var reset = _wait.Until(d =>
            d.FindElements(ResetButtonBy).FirstOrDefault(e =>
                e.Displayed &&
                !(e.GetAttribute("class") ?? string.Empty).Split(' ').Contains("disabledbutton")));
        ClickRobustly(reset);
        _wait.Until(_ => !HasActiveFilters);
    }

    // A floating "scroll to top" button can overlap controls near the bottom of the
    // viewport, so scroll the target to the centre first and fall back to a scripted
    // click if the native click is still intercepted.
    private void ClickRobustly(IWebElement element)
    {
        ((IJavaScriptExecutor)driver).ExecuteScript(
            "arguments[0].scrollIntoView({block: 'center'});", element);
        try
        {
            element.Click();
        }
        catch (Exception ex)
            when (ex is ElementClickInterceptedException or ElementNotInteractableException)
        {
            // Overlapped, or a custom control whose real input is visually hidden.
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
        }
    }

    private IWebElement FacetPanel(string title) => _wait.Until(d =>
        d.FindElements(By.XPath(FacetPanelXPath(title))).FirstOrDefault(e => e.Displayed));

    private void ExpandPanel(IWebElement panel)
    {
        var body = panel.FindElement(By.CssSelector(".panel-collapse"));
        if (body.Displayed) return;

        ClickRobustly(panel.FindElement(By.CssSelector(".panel-heading a")));
        _wait.Until(_ => body.Displayed);
    }

    private IReadOnlyList<string> VisibleTexts(By by) =>
        driver.FindElements(by)
            .Where(e => e.Displayed)
            .Select(e => NormalizeWhitespace(e.Text))
            .Where(text => text.Length > 0)
            .ToArray();

    private static string FacetPanelXPath(string title) =>
        "//div[contains(concat(' ', normalize-space(@class), ' '), ' accordion-newFilter ')]" +
        "[.//div[contains(concat(' ', normalize-space(@class), ' '), ' panel-heading ')]" +
        $"[contains(normalize-space(.), {ToXPathLiteral(title)})]]";

    private static int LeadingInt(string text)
    {
        var match = Regex.Match(text, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    private static FacetOption ParseOption(string raw)
    {
        var count = 0;
        var value = raw;
        var match = Regex.Match(raw, @"\((\d+)\)\s*$");
        if (match.Success)
        {
            count = int.Parse(match.Groups[1].Value);
            value = raw[..match.Index].Trim();
        }

        var core = value.Contains('(') ? value[..value.IndexOf('(')].Trim() : value;
        return new FacetOption(value, core, count);
    }

    private static bool IsVisible(IWebDriver webDriver, By by)
    {
        try
        {
            return webDriver.FindElements(by).Any(element => element.Displayed);
        }
        catch (StaleElementReferenceException)
        {
            return false;
        }
    }

    private static bool IsStale(IWebElement element)
    {
        try
        {
            _ = element.Enabled;
            return false;
        }
        catch (StaleElementReferenceException)
        {
            return true;
        }
    }

    private static string NormalizeWhitespace(string value) =>
        Regex.Replace(value.Trim(), @"\s+", " ");

    private static string ToXPathLiteral(string value)
    {
        if (!value.Contains('\'')) return $"'{value}'";
        if (!value.Contains('"')) return $"\"{value}\"";

        var parts = value.Split('\'');
        return $"concat('{string.Join("', \"'\", '", parts)}')";
    }
}

/// <summary>A single facet value, e.g. "DUNLOP (9)" → Value "DUNLOP", Core "DUNLOP", Count 9.</summary>
public sealed record FacetOption(string Value, string Core, int Count);
