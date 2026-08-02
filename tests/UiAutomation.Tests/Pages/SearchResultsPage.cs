using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Text.RegularExpressions;

namespace UiAutomation.Tests.Pages;

public sealed class SearchResultsPage(IWebDriver driver, TimeSpan waitTimeout)
{
    private readonly WebDriverWait _wait = new(driver, waitTimeout);

    private static readonly By SearchInputBy = By.Id("headerInputSearch");
    private static readonly By BlockingOverlayBy = By.CssSelector("div.block-ui-overlay");
    private static readonly By EmptyResultBy = By.XPath(
        "//*[@role='alert']//*[contains(normalize-space(.), " +
        "'За Вашим пошуковим запитом нічого не знайдено.')]");
    private static readonly By ResultSummaryBy = By.XPath(
        "//*[starts-with(normalize-space(.), 'Знайдено по ')]");

    public string Query => SearchInput.GetAttribute("value") ?? string.Empty;

    public string ResultSummary => NormalizeWhitespace(VisibleElement(ResultSummaryBy).Text);

    public bool HasEmptyResult => IsVisible(driver, EmptyResultBy);

    public void Search(string query)
    {
        WaitUntilPageIsReady();

        var previousResult = driver
            .FindElements(By.CssSelector("[ng-repeat='item in searchresult.Items']"))
            .FirstOrDefault(element => element.Displayed);

        var input = SearchInput;
        input.SendKeys(Keys.Control + "a");
        input.SendKeys(query);
        input.SendKeys(Keys.Enter);

        if (previousResult is not null)
        {
            _wait.Until(d => IsStale(previousResult) || IsVisible(d, BlockingOverlayBy));
        }

        _wait.Until(d =>
            !IsVisible(d, BlockingOverlayBy) &&
            (IsVisible(d, ResultSummaryBy) || IsVisible(d, EmptyResultBy)));
    }

    public ProductResult GetProduct(string code)
    {
        var productCodeBy = By.XPath(
            $"//a[contains(concat(' ', normalize-space(@class), ' '), ' searchProdCard ') " +
            $"and normalize-space(.)={ToXPathLiteral(code)}]");
        var codeElement = VisibleElement(productCodeBy);
        var result = codeElement.FindElement(By.XPath(
            "ancestor::*[@ng-repeat='item in searchresult.Items'][1]"));

        var brand = result.FindElements(By.CssSelector(".brandSearch"))
            .Select(element => NormalizeWhitespace(element.Text))
            .First(text => text.Length > 0);

        return new ProductResult(
            Code: NormalizeWhitespace(codeElement.Text),
            Card: NormalizeWhitespace(result.FindElement(By.CssSelector(".searchCard span")).Text),
            Description: NormalizeWhitespace(result.FindElement(By.CssSelector(".searchDescrip")).Text),
            Brand: brand);
    }

    public bool IsProductDisplayed(string code)
    {
        var by = By.XPath(
            $"//a[contains(concat(' ', normalize-space(@class), ' '), ' searchProdCard ') " +
            $"and normalize-space(.)={ToXPathLiteral(code)}]");
        return IsVisible(driver, by);
    }

    private IWebElement SearchInput => _wait.Until(d =>
        d.FindElements(SearchInputBy)
            .FirstOrDefault(element => element.Displayed && element.Enabled));

    private IWebElement VisibleElement(By by) => _wait.Until(d =>
        d.FindElements(by).FirstOrDefault(element => element.Displayed));

    private void WaitUntilPageIsReady() =>
        _wait.Until(d => !IsVisible(d, BlockingOverlayBy));

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

public sealed record ProductResult(
    string Code,
    string Card,
    string Description,
    string Brand);
