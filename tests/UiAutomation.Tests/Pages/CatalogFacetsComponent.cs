using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Pages;

internal sealed class CatalogFacetsComponent(IWebDriver driver, WebDriverWait wait)
{
    private static readonly By ApplyButtonBy = By.CssSelector("button.btn-apply-filter");
    private static readonly By ResetButtonBy =
        By.CssSelector("a[ng-click='filterModel.resetModel()']");
    private static readonly By OptionLabelBy =
        By.CssSelector("span.text[ng-click*='selectedLabelClick']");
    private static readonly By SelectedFacetCheckboxBy =
        By.CssSelector("div.accordion-newFilter input[type='checkbox']:checked");
    private static readonly By InStockCheckboxBy =
        By.CssSelector("input[ng-model='filterModel.rest']");
    private static readonly By ListViewToggleBy = By.CssSelector("a[ng-click='setView(1)']");

    public bool HasActiveFilters
    {
        get
        {
            var reset = driver.FindElements(ResetButtonBy).FirstOrDefault(element => element.Displayed);
            if (reset is null) return false;
            var classes = (reset.GetAttribute("class") ?? string.Empty).Split(' ');
            return !classes.Contains("disabledbutton");
        }
    }

    public int SelectedFacetValuesCount =>
        driver.FindElements(SelectedFacetCheckboxBy).Count(element => element.Selected);

    public bool IsInStockOnlyEnabled =>
        driver.FindElements(InStockCheckboxBy).FirstOrDefault()?.Selected == true;

    public FacetOption SelectMostRestrictiveFacetOption(string facetTitle)
    {
        var panel = FacetPanel(facetTitle);
        ExpandPanel(panel);

        var optionLabel = wait.Until(_ => panel.FindElements(OptionLabelBy)
            .Where(element => element.Displayed && element.Text.Trim().Length > 0)
            .Select(element => (Element: element, Option: ParseOption(
                UiText.NormalizeWhitespace(element.Text))))
            .Where(item => item.Option.Count > 0)
            .OrderBy(item => item.Option.Count)
            .Select(item => item.Element)
            .FirstOrDefault());

        return SelectOption(
            optionLabel,
            ParseOption(UiText.NormalizeWhitespace(optionLabel.Text)));
    }

    public FacetOption SelectFacetOption(string facetTitle, string optionValue)
    {
        var panel = FacetPanel(facetTitle);
        ExpandPanel(panel);

        var option = wait.Until(_ => panel.FindElements(OptionLabelBy)
            .FirstOrDefault(element => element.Displayed && string.Equals(
                ParseOption(UiText.NormalizeWhitespace(element.Text)).Value,
                optionValue,
                StringComparison.OrdinalIgnoreCase)));

        return SelectOption(option, ParseOption(UiText.NormalizeWhitespace(option.Text)));
    }

    public void SwitchToListView()
    {
        var toggle = wait.Until(d => d.FindElements(ListViewToggleBy)
            .FirstOrDefault(element => element.Displayed && element.Enabled));
        driver.ClickRobustly(toggle);
        wait.Until(d => d.FindElements(InStockCheckboxBy).Count > 0);
    }

    public void EnableInStockOnly()
    {
        var checkbox = wait.Until(d => d.FindElements(InStockCheckboxBy).FirstOrDefault());
        if (checkbox.Selected) return;

        driver.ClickRobustly(checkbox);
        wait.Until(_ => checkbox.Selected);
    }

    public void Apply()
    {
        var apply = wait.Until(d => d.FindElements(ApplyButtonBy)
            .FirstOrDefault(element => element.Displayed && element.Enabled));
        driver.ClickRobustly(apply);
    }

    public void Reset()
    {
        var reset = wait.Until(d => d.FindElements(ResetButtonBy).FirstOrDefault(element =>
            element.Displayed &&
            !(element.GetAttribute("class") ?? string.Empty)
                .Split(' ')
                .Contains("disabledbutton")));
        driver.ClickRobustly(reset);
        wait.Until(_ => !HasActiveFilters && SelectedFacetValuesCount == 0);
    }

    private FacetOption SelectOption(IWebElement optionLabel, FacetOption parsed)
    {
        driver.ClickRobustly(optionLabel);

        var checkbox = optionLabel
            .FindElement(By.XPath(
                "ancestor::div[contains(concat(' ', normalize-space(@class), ' '), ' checkbox ')][1]"))
            .FindElement(By.CssSelector("input[type='checkbox']"));
        wait.Until(_ => checkbox.Selected);

        return parsed;
    }

    private IWebElement FacetPanel(string title) => wait.Until(d =>
        d.FindElements(By.XPath(FacetPanelXPath(title)))
            .FirstOrDefault(element => element.Displayed));

    private void ExpandPanel(IWebElement panel)
    {
        var body = panel.FindElement(By.CssSelector(".panel-collapse"));
        if (body.Displayed) return;

        driver.ClickRobustly(panel.FindElement(By.CssSelector(".panel-heading a")));
        wait.Until(_ => body.Displayed);
    }

    private static string FacetPanelXPath(string title) =>
        "//div[contains(concat(' ', normalize-space(@class), ' '), ' accordion-newFilter ')]" +
        "[.//div[contains(concat(' ', normalize-space(@class), ' '), ' panel-heading ')]" +
        $"[contains(normalize-space(.), {XPathHelpers.Literal(title)})]]";

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
}

public sealed record FacetOption(string Value, string Core, int Count);
