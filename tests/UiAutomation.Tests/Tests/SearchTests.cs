using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests;

[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Search")]
[Category(TestCategories.ProductionTestClient)]
[Category(TestCategories.MutatesUserState)]
public sealed class SearchTests : AuthenticatedUiTestFixture
{
    private SearchResultsPage _search = null!;

    private string LowercaseProductCode => Settings.SearchData.ProductCode.ToLowerInvariant();

    private string PartialProductCode => Settings.SearchData.ProductCode[..^1];

    protected override void OnAuthenticated()
    {
        Assert.That(
            Settings.SearchData.IsConfigured,
            Is.True,
            $"Search reference data are not configured for environment '{Settings.EnvironmentName}'.");
        _search = new SearchResultsPage(
            Driver,
            Timeout,
            TimeSpan.FromSeconds(Settings.SearchMinimumIntervalSeconds));
    }

    [SetUp]
    public void ResetSearchState() => _search.Reset(Settings.BaseUrl);

    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-BAR-001")]
    public void SearchByLowercaseProductCodeReturnsExpectedProduct()
    {
        _search.Search(LowercaseProductCode);
        var product = _search.GetProduct(Settings.SearchData.ProductCode);

        Assert.Multiple(() =>
        {
            Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по коду: 1"));
            Assert.That(product.Code, Is.EqualTo(Settings.SearchData.ProductCode));
            Assert.That(product.Card, Is.EqualTo(Settings.SearchData.ProductCard));
            Assert.That(product.Description, Is.EqualTo(Settings.SearchData.ProductDescription));
            Assert.That(product.Brand, Is.EqualTo(Settings.SearchData.ProductBrand));
        });
    }

    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-BAR-002")]
    public void SearchByCardReturnsExpectedProduct()
    {
        _search.Search(Settings.SearchData.ProductCard);
        var product = _search.GetProduct(Settings.SearchData.ProductCode);

        Assert.Multiple(() =>
        {
            Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по картці: 1"));
            Assert.That(product.Code, Is.EqualTo(Settings.SearchData.ProductCode));
            Assert.That(product.Card, Is.EqualTo(Settings.SearchData.ProductCard));
        });
    }

    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-BAR-003")]
    public void ProductCodeSearchIsCaseInsensitive()
    {
        _search.Search(LowercaseProductCode);
        var lowercaseResult = _search.GetProduct(Settings.SearchData.ProductCode);

        _search.Search(Settings.SearchData.ProductCode.ToUpperInvariant());
        var uppercaseResult = _search.GetProduct(Settings.SearchData.ProductCode);

        Assert.That(uppercaseResult, Is.EqualTo(lowercaseResult));
    }

    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-BAR-004")]
    public void FullProductDescriptionReturnsBothReferenceProducts()
    {
        _search.Search(Settings.SearchData.ProductDescription);

        Assert.Multiple(() =>
        {
            Assert.That(_search.ResultSummary, Does.StartWith("Знайдено по частин"));
            Assert.That(_search.ProductCodes, Does.Contain(Settings.SearchData.ProductCode));
            Assert.That(_search.ProductCodes, Does.Contain(Settings.SearchData.AlternativeProductCode));
        });
    }

    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-BAR-005")]
    public void PartialProductDescriptionReturnsRelevantProductsFromDifferentBrands()
    {
        _search.Search(Settings.SearchData.PartialDescription);

        Assert.Multiple(() =>
        {
            Assert.That(_search.ProductCodes, Does.Contain(Settings.SearchData.ProductCode));
            Assert.That(_search.ProductDescriptions.Any(description =>
                description.Contains(
                    Settings.SearchData.LatinExpectedText,
                    StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(_search.ProductBrands.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                Is.GreaterThan(1));
        });
    }

    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-BAR-006")]
    public void MissingProductShowsEmptyResultAndClearsPreviousProducts()
    {
        _search.Search(LowercaseProductCode);
        Assert.That(_search.IsProductDisplayed(Settings.SearchData.ProductCode), Is.True);

        _search.Search(Settings.SearchData.MissingProductQuery);

        Assert.Multiple(() =>
        {
            Assert.That(_search.HasEmptyResult, Is.True);
            Assert.That(_search.IsProductDisplayed(Settings.SearchData.ProductCode), Is.False);
            Assert.That(_search.IsInputUsable, Is.True);
        });
    }

    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-BAR-007")]
    public void NewSearchReplacesPreviousResult()
    {
        _search.Search(LowercaseProductCode);
        Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по коду: 1"));

        _search.Search(Settings.SearchData.ProductCard);

        Assert.Multiple(() =>
        {
            Assert.That(_search.Query, Is.EqualTo(Settings.SearchData.ProductCard));
            Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по картці: 1"));
            Assert.That(
                _search.GetProduct(Settings.SearchData.ProductCode).Card,
                Is.EqualTo(Settings.SearchData.ProductCard));
        });
    }

    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-BAR-008")]
    public void EnterDoesNotDuplicateSearchRequests()
    {
        Assume.That(_search.SupportsPerformanceLog, Is.True);
        _search.ClearPerformanceLog();

        _search.Search(LowercaseProductCode);
        var matchingRequests = _search.SearchRequestSignaturesContaining(LowercaseProductCode);

        Assert.Multiple(() =>
        {
            Assert.That(matchingRequests, Is.Not.Empty);
            Assert.That(matchingRequests.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(matchingRequests.Count));
            Assert.That(_search.IsProductDisplayed(Settings.SearchData.ProductCode), Is.True);
            Assert.That(_search.IsInputUsable, Is.True);
        });
    }

    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-BAR-009")]
    public void RapidQueriesFinishWithTheLastAcceptedQuery()
    {
        _search.SearchRapidly(
            LowercaseProductCode,
            Settings.SearchData.ProductCard,
            "Знайдено по картці: 1");

        Assert.Multiple(() =>
        {
            Assert.That(_search.Query, Is.EqualTo(Settings.SearchData.ProductCard));
            Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по картці: 1"));
            Assert.That(
                _search.GetProduct(Settings.SearchData.ProductCode).Card,
                Is.EqualTo(Settings.SearchData.ProductCard));
            Assert.That(_search.IsLoading, Is.False);
        });
    }

    [Test]
    [Category("SearchInput")]
    [Category("P1")]
    [Property("TestCaseId", "SEARCH-BAR-011")]
    public void OuterWhitespaceDoesNotChangeTheProductResult()
    {
        _search.Search(LowercaseProductCode);
        var expected = _search.GetProduct(Settings.SearchData.ProductCode);

        foreach (var query in new[]
                 {
                     $" {LowercaseProductCode}",
                     $"{LowercaseProductCode} ",
                     $"  {LowercaseProductCode}  "
                 })
        {
            _search.Search(query);
            Assert.That(_search.GetProduct(Settings.SearchData.ProductCode), Is.EqualTo(expected),
                $"Unexpected result for query '{query}'.");
        }
    }

    [Test]
    [Category("SearchInput")]
    [Category("P1")]
    [Property("TestCaseId", "SEARCH-BAR-012")]
    public void WhitespaceOnlyQueryDoesNotStartAnUnboundedSearch()
    {
        _search.Search(LowercaseProductCode);
        var previousCount = _search.ProductCodes.Count;

        _search.SubmitWithoutWaiting("   ");
        var stableResult = _search.ResultSignature();
        _search.WaitForStableResult(stableResult, TimeSpan.FromMilliseconds(500));

        Assert.Multiple(() =>
        {
            Assert.That(_search.Query.Trim(), Is.Empty);
            Assert.That(_search.ProductCodes.Count, Is.LessThanOrEqualTo(previousCount));
            Assert.That(_search.IsLoading, Is.False);
            Assert.That(_search.IsInputUsable, Is.True);
        });
    }

    [Test]
    [Category("SearchInput")]
    [Category("P1")]
    [Property("TestCaseId", "SEARCH-BAR-013")]
    public void EmptyQueryDoesNotStartAnUnboundedSearch()
    {
        _search.Search(LowercaseProductCode);
        var previousCount = _search.ProductCodes.Count;

        _search.SubmitWithoutWaiting(string.Empty);
        var stableResult = _search.ResultSignature();
        _search.WaitForStableResult(stableResult, TimeSpan.FromMilliseconds(500));

        Assert.Multiple(() =>
        {
            Assert.That(_search.Query, Is.Empty);
            Assert.That(_search.ProductCodes.Count, Is.LessThanOrEqualTo(previousCount));
            Assert.That(_search.IsLoading, Is.False);
            Assert.That(_search.IsInputUsable, Is.True);
        });
    }

    [Test]
    [Category("SearchInput")]
    [Category("P1")]
    [Property("TestCaseId", "SEARCH-BAR-014")]
    public void MultipleInternalSpacesAreNormalized()
    {
        _search.Search(Settings.SearchData.PartialDescription);
        var expectedCodes = _search.ProductCodes;

        _search.Search(string.Join(
            "   ",
            Settings.SearchData.PartialDescription.Split(' ', StringSplitOptions.RemoveEmptyEntries)));

        Assert.That(_search.ProductCodes, Is.EqualTo(expectedCodes));
    }

    [Test]
    [Category("SearchInput")]
    [Category("P1")]
    [Property("TestCaseId", "SEARCH-BAR-015")]
    public void CyrillicAndLatinQueriesRemainSearchable()
    {
        _search.Search(Settings.SearchData.CyrillicQuery);
        var cyrillicDescriptions = _search.ProductDescriptions;

        _search.Search(Settings.SearchData.LatinQuery);
        var latinDescriptions = _search.ProductDescriptions;

        Assert.Multiple(() =>
        {
            Assert.That(cyrillicDescriptions.Any(description =>
                description.Contains(
                    Settings.SearchData.CyrillicExpectedText,
                    StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(latinDescriptions.Any(description =>
                description.Contains(
                    Settings.SearchData.LatinExpectedText,
                    StringComparison.OrdinalIgnoreCase)), Is.True);
        });
    }

    [Test]
    [Category("SearchInput")]
    [Category("P1")]
    [Property("TestCaseId", "SEARCH-BAR-016")]
    public void MixedCaseDescriptionReturnsTheSameProducts()
    {
        _search.Search(Settings.SearchData.PartialDescription);
        var expectedCodes = _search.ProductCodes;

        _search.Search(ToAlternatingCase(Settings.SearchData.PartialDescription));

        Assert.That(_search.ProductCodes, Is.EqualTo(expectedCodes));
    }

    [Test]
    [Category("SearchInput")]
    [Category("P1")]
    [Property("TestCaseId", "SEARCH-BAR-017")]
    public void ProductCodeWithPunctuationIsPreserved()
    {
        var punctuatedCode = Settings.SearchData.PunctuatedProductCode;
        _search.Search(punctuatedCode);

        Assert.Multiple(() =>
        {
            Assert.That(_search.Query, Is.EqualTo(punctuatedCode));
            Assert.That(_search.IsProductDisplayed(punctuatedCode), Is.True);
            Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по коду: 1"));
        });
    }

    [Test]
    [Category("SearchInput")]
    [Category("P1")]
    [Property("TestCaseId", "SEARCH-BAR-018")]
    public void PartialCodeIsNotReportedAsExactOc90Match()
    {
        _search.Search(PartialProductCode);

        Assert.Multiple(() =>
        {
            Assert.That(_search.HasCompletedOutcome(), Is.True);
            Assert.That(_search.IsProductDisplayed(Settings.SearchData.ProductCode), Is.False);
            Assert.That(_search.IsInputUsable, Is.True);
        });
    }

    [Test]
    [Category("SearchInput")]
    [Category("P1")]
    [Property("TestCaseId", "SEARCH-BAR-020")]
    public void PastedProductCodeReturnsTheSameProduct()
    {
        Assume.That(_search.SupportsClipboardPaste, Is.True);

        _search.PasteAndSearch(LowercaseProductCode);

        Assert.Multiple(() =>
        {
            Assert.That(_search.Query, Is.EqualTo(LowercaseProductCode));
            Assert.That(_search.IsProductDisplayed(Settings.SearchData.ProductCode), Is.True);
        });
    }

    [Test]
    [Category("SearchControls")]
    [Category("P1")]
    [Property("TestCaseId", "SEARCH-BAR-021")]
    public void ClearButtonRemovesTheTypedQuery()
    {
        _search.TypeQuery(LowercaseProductCode);
        _search.ClearQueryWithButton();

        Assert.Multiple(() =>
        {
            Assert.That(_search.Query, Is.Empty);
            Assert.That(_search.SearchPlaceholder,
                Is.EqualTo(Settings.SearchData.SearchPlaceholder));
            Assert.That(_search.IsInputUsable, Is.True);
        });
    }

    [Test]
    [Category("SearchControls")]
    [Category("P1")]
    [Property("TestCaseId", "SEARCH-BAR-022")]
    public void CtrlAReplacesTheExistingQuery()
    {
        _search.TypeQuery(LowercaseProductCode);
        _search.ReplaceWithCtrlAAndSearch(Settings.SearchData.ProductCard);

        Assert.Multiple(() =>
        {
            Assert.That(_search.Query, Is.EqualTo(Settings.SearchData.ProductCard));
            Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по картці: 1"));
        });
    }

    [Test]
    [Category("SearchControls")]
    [Category("P1")]
    [Property("TestCaseId", "SEARCH-BAR-023")]
    public void StartsWithOptionReturnsCodesBeginningWithTheQuery()
    {
        _search.SetStartsWith(true);
        _search.Search(PartialProductCode);

        Assert.Multiple(() =>
        {
            Assert.That(_search.IsStartsWithEnabled, Is.True);
            Assert.That(_search.StartsWithCodes, Is.Not.Empty);
            Assert.That(_search.StartsWithCodes.All(code =>
                code.Replace(" ", string.Empty).StartsWith(
                    PartialProductCode,
                    StringComparison.OrdinalIgnoreCase)),
                Is.True);
        });
    }

    [Test]
    [Category("SearchControls")]
    [Category("P1")]
    [Property("TestCaseId", "SEARCH-BAR-024")]
    public void DisablingStartsWithRestoresNormalSearch()
    {
        _search.SetStartsWith(true);
        _search.Search(PartialProductCode);
        Assert.That(_search.StartsWithCodes, Is.Not.Empty);

        _search.SetStartsWith(false);
        _search.Search(PartialProductCode);

        Assert.Multiple(() =>
        {
            Assert.That(_search.IsStartsWithEnabled, Is.False);
            Assert.That(_search.ProductCodes, Is.Not.Empty);
            Assert.That(_search.ResultSummary, Does.StartWith("Знайдено по "));
        });
    }

    private static string ToAlternatingCase(string value) =>
        string.Concat(value.Select((character, index) =>
            index % 2 == 0
                ? char.ToLowerInvariant(character)
                : char.ToUpperInvariant(character)));
}
