using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Pages;

internal sealed class SearchResultComponent(IWebDriver driver, WebDriverWait wait)
{
    private static readonly By SearchRateLimitBy = By.XPath(
        "//*[contains(normalize-space(.), 'Ви перевищили ліміт пошукових запитів.')]");
    private static readonly By EmptyResultBy = By.XPath(
        "//*[@role='alert']//*[contains(normalize-space(.), " +
        "'За Вашим пошуковим запитом нічого не знайдено.')]");
    private static readonly By ResultSummaryBy = By.XPath(
        "//*[starts-with(normalize-space(.), 'Знайдено по ')]");
    private static readonly By StartsWithSummaryBy = By.XPath(
        "//*[contains(normalize-space(.), 'За цим кодом знайдено брендів:')]");
    private static readonly By StartsWithCodesBy = By.XPath(
        "//table//tbody//tr/td[3]//a[normalize-space(.)]");
    private static readonly By ProductCodeBy = By.CssSelector("a.searchProdCard");
    private static readonly By ProductDescriptionBy = By.CssSelector(".searchDescrip");
    private static readonly By ProductBrandBy = By.CssSelector(".brandSearch");

    public string Summary => UiText.NormalizeWhitespace(
        wait.Until(d => d.FindElements(ResultSummaryBy).FirstOrDefault(element => element.Displayed)).Text);

    public bool HasEmptyResult => driver.IsVisible(EmptyResultBy);

    public IReadOnlyList<string> ProductCodes => driver.VisibleTexts(ProductCodeBy);

    public IReadOnlyList<string> ProductDescriptions => driver.VisibleTexts(ProductDescriptionBy);

    public IReadOnlyList<string> ProductBrands => driver.VisibleTexts(ProductBrandBy);

    public IReadOnlyList<string> StartsWithCodes => driver.VisibleTexts(StartsWithCodesBy);

    public bool HasCompletedOutcome =>
        driver.IsVisible(ResultSummaryBy) ||
        driver.IsVisible(EmptyResultBy) ||
        driver.IsVisible(StartsWithSummaryBy) ||
        driver.IsVisible(StartsWithCodesBy);

    public string Signature()
    {
        var summary = driver.IsVisible(ResultSummaryBy) ? Summary : string.Empty;
        return string.Join("|", summary, HasEmptyResult, string.Join(",", ProductCodes));
    }

    public ProductResult GetProduct(string code)
    {
        var productCodeBy = By.XPath(
            $"//a[contains(concat(' ', normalize-space(@class), ' '), ' searchProdCard ') " +
            $"and normalize-space(.)={XPathHelpers.Literal(code)}]");
        var codeElement = wait.Until(d =>
            d.FindElements(productCodeBy).FirstOrDefault(element => element.Displayed));
        var result = codeElement.FindElement(By.XPath(
            "ancestor::*[@ng-repeat='item in searchresult.Items'][1]"));
        var brand = result.FindElements(ProductBrandBy)
            .Select(element => UiText.NormalizeWhitespace(element.Text))
            .First(text => text.Length > 0);

        return new ProductResult(
            Code: UiText.NormalizeWhitespace(codeElement.Text),
            Card: UiText.NormalizeWhitespace(result.FindElement(By.CssSelector(".searchCard span")).Text),
            Description: UiText.NormalizeWhitespace(result.FindElement(ProductDescriptionBy).Text),
            Brand: brand);
    }

    public bool IsProductDisplayed(string code)
    {
        var by = By.XPath(
            $"//a[contains(concat(' ', normalize-space(@class), ' '), ' searchProdCard ') " +
            $"and normalize-space(.)={XPathHelpers.Literal(code)}]");
        return driver.IsVisible(by);
    }

    public void ThrowIfRateLimited()
    {
        if (driver.IsVisible(SearchRateLimitBy))
        {
            throw new InvalidOperationException(
                "Сервер отклонил поиск: превышен лимит поисковых запросов. " +
                "Дождитесь сброса лимита перед следующим E2E-прогоном.");
        }
    }
}

public sealed record ProductResult(
    string Code,
    string Card,
    string Description,
    string Brand);
