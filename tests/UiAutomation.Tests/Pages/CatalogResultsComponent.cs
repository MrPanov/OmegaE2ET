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
    private static readonly By ProductDescriptionBy = By.CssSelector(".searchDescrip");
    private static readonly By ProductBrandBy = By.CssSelector(".brandSearch");
    private static readonly By StockQuantityBy = By.CssSelector("span[ng-style*='war.rest']");

    public IReadOnlyList<string> ProductCodes => driver.VisibleTexts(ProductCardBy);

    public IReadOnlyList<string> ProductDescriptions => driver.VisibleTexts(ProductDescriptionBy);

    public IReadOnlyList<string> ProductBrands => driver.VisibleTexts(ProductBrandBy);

    public int ResultCount => ProductCodes.Count;

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
        bool requireResultChange)
    {
        string? lastSignature = null;
        DateTime? stableSince = null;

        wait.Until(d =>
        {
            if (d.IsVisible(OverlayBy) || !mutations.HasChangedSince(version))
            {
                lastSignature = null;
                stableSince = null;
                return false;
            }

            var signature = Signature();
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

            return stableSince is not null && DateTime.UtcNow - stableSince >= ResultSettleTime;
        });
    }

    public IReadOnlyList<string> ProductsWithoutStock()
    {
        var missing = new List<string>();
        foreach (var card in driver.FindElements(ProductCardBy).Where(element => element.Displayed))
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

    private static int LeadingInt(string text)
    {
        var match = Regex.Match(text, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }
}
