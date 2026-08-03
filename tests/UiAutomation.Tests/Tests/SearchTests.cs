using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests;

[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Search")]
public sealed class SearchTests : AuthenticatedUiTestFixture
{
    private const string ProductCode = "OC90";
    private const string ProductCard = "4610495";
    private const string ProductDescription =
        "Фільтр оливний LANOS, AVEO, LACETTI, NUBIRA, NEXIA (вир-во KNECHT-MAHLE)";
    private const string ProductBrand = "KNECHT/MAHLE";
    private const string PartialDescription = "Фільтр оливний LANOS";
    private const string MissingProduct = "zz-no-product-987654321";

    private SearchResultsPage _search = null!;

    protected override void OnAuthenticated()
    {
        _search = new SearchResultsPage(Driver, Timeout);
    }

    [SetUp]
    public void ResetSearchMode()
    {
        _search.CloseHistory();
        _search.SetStartsWith(false);
    }

    [Test]
    [Category("Smoke")]
    [Property("TestCaseId", "SEARCH-BAR-001")]
    public void SearchByLowercaseProductCodeReturnsExpectedProduct()
    {
        _search.Search("oc90");
        var product = _search.GetProduct(ProductCode);

        Assert.Multiple(() =>
        {
            Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по коду: 1"));
            Assert.That(product.Code, Is.EqualTo(ProductCode));
            Assert.That(product.Card, Is.EqualTo(ProductCard));
            Assert.That(product.Description, Is.EqualTo(ProductDescription));
            Assert.That(product.Brand, Is.EqualTo(ProductBrand));
        });
    }

    [Test]
    [Category("Smoke")]
    [Property("TestCaseId", "SEARCH-BAR-002")]
    public void SearchByCardReturnsExpectedProduct()
    {
        _search.Search(ProductCard);
        var product = _search.GetProduct(ProductCode);

        Assert.Multiple(() =>
        {
            Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по картці: 1"));
            Assert.That(product.Code, Is.EqualTo(ProductCode));
            Assert.That(product.Card, Is.EqualTo(ProductCard));
        });
    }

    [Test]
    [Category("Smoke")]
    [Property("TestCaseId", "SEARCH-BAR-003")]
    public void ProductCodeSearchIsCaseInsensitive()
    {
        _search.Search("oc90");
        var lowercaseResult = _search.GetProduct(ProductCode);

        _search.Search("OC90");
        var uppercaseResult = _search.GetProduct(ProductCode);

        Assert.That(uppercaseResult, Is.EqualTo(lowercaseResult));
    }

    [Test]
    [Category("Smoke")]
    [Property("TestCaseId", "SEARCH-BAR-004")]
    public void FullProductDescriptionReturnsBothReferenceProducts()
    {
        _search.Search(ProductDescription);

        Assert.Multiple(() =>
        {
            Assert.That(_search.ResultSummary, Does.StartWith("Знайдено по частин"));
            Assert.That(_search.ProductCodes, Does.Contain("OC90"));
            Assert.That(_search.ProductCodes, Does.Contain("OC90OF"));
        });
    }

    [Test]
    [Category("Smoke")]
    [Property("TestCaseId", "SEARCH-BAR-005")]
    public void PartialProductDescriptionReturnsRelevantProductsFromDifferentBrands()
    {
        _search.Search(PartialDescription);

        Assert.Multiple(() =>
        {
            Assert.That(_search.ProductCodes, Does.Contain(ProductCode));
            Assert.That(_search.ProductDescriptions.Any(description =>
                description.Contains("LANOS", StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(_search.ProductBrands.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                Is.GreaterThan(1));
        });
    }

    [Test]
    [Category("Smoke")]
    [Property("TestCaseId", "SEARCH-BAR-006")]
    public void MissingProductShowsEmptyResultAndClearsPreviousProducts()
    {
        _search.Search("oc90");
        Assert.That(_search.IsProductDisplayed(ProductCode), Is.True);

        _search.Search(MissingProduct);

        Assert.Multiple(() =>
        {
            Assert.That(_search.HasEmptyResult, Is.True);
            Assert.That(_search.IsProductDisplayed(ProductCode), Is.False);
            Assert.That(_search.IsInputUsable, Is.True);
        });
    }

    [Test]
    [Category("Smoke")]
    [Property("TestCaseId", "SEARCH-BAR-007")]
    public void NewSearchReplacesPreviousResult()
    {
        _search.Search("oc90");
        Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по коду: 1"));

        _search.Search(ProductCard);

        Assert.Multiple(() =>
        {
            Assert.That(_search.Query, Is.EqualTo(ProductCard));
            Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по картці: 1"));
            Assert.That(_search.GetProduct(ProductCode).Card, Is.EqualTo(ProductCard));
        });
    }

    [Test]
    [Category("Smoke")]
    [Property("TestCaseId", "SEARCH-BAR-008")]
    public void EnterDoesNotDuplicateSearchRequests()
    {
        Assume.That(_search.SupportsPerformanceLog, Is.True);
        _search.ClearPerformanceLog();

        _search.Search("oc90");
        var matchingRequests = _search.SearchRequestSignaturesContaining("oc90");

        Assert.Multiple(() =>
        {
            Assert.That(matchingRequests, Is.Not.Empty);
            Assert.That(matchingRequests.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(matchingRequests.Count));
            Assert.That(_search.IsProductDisplayed(ProductCode), Is.True);
            Assert.That(_search.IsInputUsable, Is.True);
        });
    }

    [Test]
    [Category("Smoke")]
    [Property("TestCaseId", "SEARCH-BAR-009")]
    public void RapidQueriesFinishWithTheLastAcceptedQuery()
    {
        _search.SearchRapidly("oc90", ProductCard);

        Assert.Multiple(() =>
        {
            Assert.That(_search.Query, Is.EqualTo(ProductCard));
            Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по картці: 1"));
            Assert.That(_search.GetProduct(ProductCode).Card, Is.EqualTo(ProductCard));
            Assert.That(_search.IsLoading, Is.False);
        });
    }

    [Test]
    [Category("SearchInput")]
    [Property("TestCaseId", "SEARCH-BAR-011")]
    public void OuterWhitespaceDoesNotChangeTheProductResult()
    {
        _search.Search("oc90");
        var expected = _search.GetProduct(ProductCode);

        foreach (var query in new[] { " oc90", "oc90 ", "  oc90  " })
        {
            _search.Search(query);
            Assert.That(_search.GetProduct(ProductCode), Is.EqualTo(expected),
                $"Unexpected result for query '{query}'.");
        }
    }

    [Test]
    [Category("SearchInput")]
    [Property("TestCaseId", "SEARCH-BAR-012")]
    public void WhitespaceOnlyQueryDoesNotStartAnUnboundedSearch()
    {
        _search.Search("oc90");
        var previousCount = _search.ProductCodes.Count;

        _search.SubmitWithoutWaiting("   ");
        Thread.Sleep(500);

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
    [Property("TestCaseId", "SEARCH-BAR-013")]
    public void EmptyQueryDoesNotStartAnUnboundedSearch()
    {
        _search.Search("oc90");
        var previousCount = _search.ProductCodes.Count;

        _search.SubmitWithoutWaiting(string.Empty);
        Thread.Sleep(500);

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
    [Property("TestCaseId", "SEARCH-BAR-014")]
    public void MultipleInternalSpacesAreNormalized()
    {
        _search.Search(PartialDescription);
        var expectedCodes = _search.ProductCodes;

        _search.Search("Фільтр   оливний   LANOS");

        Assert.That(_search.ProductCodes, Is.EqualTo(expectedCodes));
    }

    [Test]
    [Category("SearchInput")]
    [Property("TestCaseId", "SEARCH-BAR-015")]
    public void CyrillicAndLatinQueriesRemainSearchable()
    {
        _search.Search("Фільтр оливний");
        var cyrillicDescriptions = _search.ProductDescriptions;

        _search.Search("LANOS");
        var latinDescriptions = _search.ProductDescriptions;

        Assert.Multiple(() =>
        {
            Assert.That(cyrillicDescriptions.Any(description =>
                description.Contains("Фільтр", StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(latinDescriptions.Any(description =>
                description.Contains("LANOS", StringComparison.OrdinalIgnoreCase)), Is.True);
        });
    }

    [Test]
    [Category("SearchInput")]
    [Property("TestCaseId", "SEARCH-BAR-016")]
    public void MixedCaseDescriptionReturnsTheSameProducts()
    {
        _search.Search(PartialDescription);
        var expectedCodes = _search.ProductCodes;

        _search.Search("фІЛьТр ОлИвНиЙ lanos");

        Assert.That(_search.ProductCodes, Is.EqualTo(expectedCodes));
    }

    [Test]
    [Category("SearchInput")]
    [Property("TestCaseId", "SEARCH-BAR-017")]
    public void ProductCodeWithPunctuationIsPreserved()
    {
        const string punctuatedCode = "23.129.02";
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
    [Property("TestCaseId", "SEARCH-BAR-018")]
    public void PartialCodeIsNotReportedAsExactOc90Match()
    {
        _search.Search("OC9");

        Assert.Multiple(() =>
        {
            Assert.That(_search.HasCompletedOutcome(), Is.True);
            Assert.That(_search.IsProductDisplayed(ProductCode), Is.False);
            Assert.That(_search.IsInputUsable, Is.True);
        });
    }

    [Test]
    [Category("SearchInput")]
    [Property("TestCaseId", "SEARCH-BAR-019")]
    public void LongQueriesAreHandledPredictably()
    {
        var queryAtBoundary = new string('a', 255);
        var queryOverBoundary = new string('b', 256);

        _search.Search(queryAtBoundary);
        var firstOutcomeCompleted = _search.HasCompletedOutcome();
        var firstLength = _search.Query.Length;

        _search.Search(queryOverBoundary);

        Assert.Multiple(() =>
        {
            Assert.That(firstOutcomeCompleted, Is.True);
            Assert.That(firstLength, Is.LessThanOrEqualTo(255));
            Assert.That(_search.Query.Length, Is.InRange(1, 256));
            Assert.That(_search.HasCompletedOutcome(), Is.True);
            Assert.That(_search.IsInputUsable, Is.True);
        });
    }

    [Test]
    [Category("SearchInput")]
    [Property("TestCaseId", "SEARCH-BAR-020")]
    public void PastedProductCodeReturnsTheSameProduct()
    {
        Assume.That(_search.SupportsClipboardPaste, Is.True);

        _search.PasteAndSearch("oc90");

        Assert.Multiple(() =>
        {
            Assert.That(_search.Query, Is.EqualTo("oc90"));
            Assert.That(_search.IsProductDisplayed(ProductCode), Is.True);
        });
    }

    [Test]
    [Category("SearchControls")]
    [Property("TestCaseId", "SEARCH-BAR-021")]
    public void ClearButtonRemovesTheTypedQuery()
    {
        _search.TypeQuery("oc90");
        _search.ClearQueryWithButton();

        Assert.Multiple(() =>
        {
            Assert.That(_search.Query, Is.Empty);
            Assert.That(_search.SearchPlaceholder,
                Is.EqualTo("VIN, Держ. номер, OE, найменування, картка, код"));
            Assert.That(_search.IsInputUsable, Is.True);
        });
    }

    [Test]
    [Category("SearchControls")]
    [Property("TestCaseId", "SEARCH-BAR-022")]
    public void CtrlAReplacesTheExistingQuery()
    {
        _search.TypeQuery("oc90");
        _search.ReplaceWithCtrlAAndSearch(ProductCard);

        Assert.Multiple(() =>
        {
            Assert.That(_search.Query, Is.EqualTo(ProductCard));
            Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по картці: 1"));
        });
    }

    [Test]
    [Category("SearchControls")]
    [Property("TestCaseId", "SEARCH-BAR-023")]
    public void StartsWithOptionReturnsCodesBeginningWithTheQuery()
    {
        _search.SetStartsWith(true);
        _search.Search("OC9");

        Assert.Multiple(() =>
        {
            Assert.That(_search.IsStartsWithEnabled, Is.True);
            Assert.That(_search.StartsWithCodes, Is.Not.Empty);
            Assert.That(_search.StartsWithCodes.All(code =>
                code.Replace(" ", string.Empty).StartsWith("OC9", StringComparison.OrdinalIgnoreCase)),
                Is.True);
        });
    }

    [Test]
    [Category("SearchControls")]
    [Property("TestCaseId", "SEARCH-BAR-024")]
    public void DisablingStartsWithRestoresNormalSearch()
    {
        _search.SetStartsWith(true);
        _search.Search("OC9");
        Assert.That(_search.StartsWithCodes, Is.Not.Empty);

        _search.SetStartsWith(false);
        _search.Search("OC9");

        Assert.Multiple(() =>
        {
            Assert.That(_search.IsStartsWithEnabled, Is.False);
            Assert.That(_search.ProductCodes, Is.Not.Empty);
            Assert.That(_search.ResultSummary, Does.StartWith("Знайдено по "));
        });
    }

    [Test]
    [Category("SearchControls")]
    [Property("TestCaseId", "SEARCH-BAR-025")]
    public void SearchHistoryContainsRecentQueriesWithoutDuplicates()
    {
        _search.Search("oc90");
        _search.Search(ProductCard);

        var history = _search.OpenHistory();
        var historySnapshot = string.Join(" | ", history.Select(item => $"'{item}'"));

        Assert.Multiple(() =>
        {
            Assert.That(history.Count(item =>
                string.Equals(item, "oc90", StringComparison.Ordinal)), Is.EqualTo(1),
                $"History: {historySnapshot}");
            Assert.That(history.Count(item =>
                string.Equals(item, ProductCard, StringComparison.Ordinal)), Is.EqualTo(1),
                $"History: {historySnapshot}");
            Assert.That(history.All(item => item.Length > 0), Is.True);
        });
    }

    [Test]
    [Category("SearchControls")]
    [Property("TestCaseId", "SEARCH-BAR-026")]
    public void QueryCanBeSelectedFromSearchHistory()
    {
        _search.Search("oc90");
        _search.Search(ProductCard);

        _search.SelectHistoryItem("oc90");

        Assert.Multiple(() =>
        {
            Assert.That(_search.Query, Is.EqualTo("oc90").IgnoreCase);
            Assert.That(_search.IsProductDisplayed(ProductCode), Is.True);
            var history = _search.OpenHistory();
            Assert.That(history.Count(item =>
                string.Equals(item, "oc90", StringComparison.Ordinal)), Is.EqualTo(1),
                $"History: {string.Join(" | ", history.Select(item => $"'{item}'"))}");
        });
    }
}
