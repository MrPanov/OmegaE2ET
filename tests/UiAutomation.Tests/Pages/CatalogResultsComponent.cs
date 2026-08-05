using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Pages;

internal sealed class CatalogResultsComponent(IWebDriver driver, WebDriverWait wait)
{
    private static readonly TimeSpan ResultSettleTime = TimeSpan.FromMilliseconds(200);

    private static readonly By OverlayBy = By.CssSelector("div.block-ui-overlay");
    private static readonly By ProductCardBy = By.CssSelector("a.searchProdCard");
    private static readonly By PrimaryProductCardBy = By.XPath(
        "//a[contains(concat(' ', normalize-space(@class), ' '), ' searchProdCard ')]" +
        "[ancestor::*[@ng-repeat][1][@ng-repeat='item in searchresult.Items']]");
    private static readonly By ProductDescriptionBy = By.CssSelector(".searchDescrip");
    private static readonly By PrimaryProductDescriptionBy = By.XPath(
        "//*[contains(concat(' ', normalize-space(@class), ' '), ' searchDescrip ')]" +
        "[ancestor::*[@ng-repeat][1][@ng-repeat='item in searchresult.Items']]");
    private static readonly By ProductBrandBy = By.CssSelector(".brandSearch");
    private static readonly By StockQuantityBy = By.CssSelector("span[ng-style*='war.rest']");
    private static readonly By AppliedFilterTagBy =
        By.CssSelector(".filters-labels-main span.filter-icon");
    private static readonly By SaleMarkerBy =
        By.CssSelector("li[ng-if='productInfo.IsSale > 0']");

    public IReadOnlyList<string> ProductCodes => driver.VisibleTexts(ProductCardBy);

    public IReadOnlyList<string> ProductDescriptions => driver.VisibleTexts(ProductDescriptionBy);

    public IReadOnlyList<string> PrimaryProductDescriptions =>
        driver.VisibleTexts(PrimaryProductDescriptionBy);

    public IReadOnlyList<string> ProductBrands => driver.VisibleTexts(ProductBrandBy);

    public int ResultCount => ProductCodes.Count;

    public int PrimaryResultCount => driver.FindElements(PrimaryProductCardBy)
        .Count(element => element.Displayed);

    public bool HasAppliedFilter(string value) => AppliedFilters
        .Any(tag => FilterValuesMatch(tag, value));

    private IReadOnlyList<string> AppliedFilters => driver.VisibleTexts(AppliedFilterTagBy);

    public string Signature() => string.Join(
        "|",
        string.Join(",", ProductCodes),
        string.Join(",", ProductDescriptions),
        string.Join(",", ProductBrands));

    public void WaitUntilLoaded()
    {
        wait.Until(d => !d.IsVisible(OverlayBy));
        wait.Until(_ => ResultCount > 0);
    }

    public void WaitForChangedAndSettled(
        string previousSignature,
        DomMutationTracker mutations,
        long version,
        bool requireResultChange,
        string? expectedAppliedFilter = null)
    {
        string? lastSignature = null;
        DateTime? stableSince = null;

        try
        {
            wait.Until(d =>
            {
                if (d.IsVisible(OverlayBy) || !mutations.HasChangedSince(version))
                {
                    lastSignature = null;
                    stableSince = null;
                    return false;
                }

                var signature = Signature();
                if (expectedAppliedFilter is not null &&
                    !HasAppliedFilter(expectedAppliedFilter))
                {
                    lastSignature = null;
                    stableSince = null;
                    return false;
                }

                if (requireResultChange &&
                    string.Equals(signature, previousSignature, StringComparison.Ordinal))
                {
                    lastSignature = null;
                    stableSince = null;
                    return false;
                }

                if (!string.Equals(signature, lastSignature, StringComparison.Ordinal))
                {
                    lastSignature = signature;
                    stableSince = DateTime.UtcNow;
                    return false;
                }

                return stableSince is not null &&
                       DateTime.UtcNow - stableSince >= ResultSettleTime;
            });
        }
        catch (WebDriverTimeoutException exception) when (expectedAppliedFilter is not null)
        {
            var applied = AppliedFilters.Count == 0
                ? "<none>"
                : string.Join(", ", AppliedFilters);
            throw new WebDriverTimeoutException(
                $"Timed out waiting for applied filter '{expectedAppliedFilter}'. " +
                $"Current applied filters: {applied}. Visible products: {ResultCount}.",
                exception);
        }
    }

    public IReadOnlyList<string> ProductsWithoutStock(int productLimit)
    {
        var missing = new List<string>();
        foreach (var card in driver.FindElements(ProductCardBy)
                     .Where(element => element.Displayed)
                     .Take(productLimit))
        {
            var row = card.FindElement(By.XPath(
                "ancestor::*[.//span[contains(@ng-style, 'war.rest')]][1]"));
            var quantities = row.FindElements(StockQuantityBy)
                .Select(cell => LeadingInt(cell.Text));
            if (!quantities.Any(quantity => quantity >= 1))
            {
                missing.Add(UiText.NormalizeWhitespace(card.Text));
            }
        }

        return missing;
    }

    public IReadOnlyList<string> PrimaryProductsWithoutBrand(string expectedBrand)
    {
        var mismatches = new List<string>();
        foreach (var card in driver.FindElements(PrimaryProductCardBy)
                     .Where(element => element.Displayed))
        {
            var product = card.FindElement(By.XPath(
                "ancestor::div[" +
                "contains(concat(' ', normalize-space(@class), ' '), ' searchBlock ')][1]"));
            var brandTexts = product.FindElements(ProductBrandBy)
                .Where(element => element.Displayed)
                .Select(element => UiText.NormalizeWhitespace(element.Text));

            if (!brandTexts.Any(value =>
                    value.Contains(expectedBrand, StringComparison.OrdinalIgnoreCase)))
            {
                mismatches.Add(UiText.NormalizeWhitespace(card.Text));
            }
        }

        return mismatches;
    }

    public IReadOnlyList<string> ProductsWithoutSaleMarker()
    {
        var missing = new List<string>();
        foreach (var card in driver.FindElements(ProductCardBy).Where(element => element.Displayed))
        {
            var product = card.FindElement(By.XPath(
                "ancestor::div[" +
                "contains(concat(' ', normalize-space(@class), ' '), ' searchBlock ')][1]"));
            if (product.FindElements(SaleMarkerBy).Count == 0)
            {
                missing.Add(UiText.NormalizeWhitespace(card.Text));
            }
        }

        return missing;
    }

    private static int LeadingInt(string text)
    {
        var match = Regex.Match(text, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    private static bool FilterValuesMatch(string actual, string expected)
    {
        if (string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return actual.StartsWith($"{expected} (", StringComparison.OrdinalIgnoreCase) ||
               expected.StartsWith($"{actual} (", StringComparison.OrdinalIgnoreCase);
    }
}
