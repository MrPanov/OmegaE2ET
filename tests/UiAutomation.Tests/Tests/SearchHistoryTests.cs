using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests;

[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Search")]
[Category("SearchControls")]
[Category(TestCategories.MutatesUserState)]
public sealed class SearchHistoryTests : AuthenticatedUiTestFixture
{
    private SearchResultsPage _search = null!;

    private string LowercaseProductCode => Settings.SearchData.ProductCode.ToLowerInvariant();

    protected override void OnAuthenticated()
    {
        _search = new SearchResultsPage(
            Driver,
            Timeout,
            TimeSpan.FromSeconds(Settings.SearchMinimumIntervalSeconds));
    }

    [SetUp]
    public void ResetSearchState() => _search.Reset(Settings.BaseUrl);

    [Test]
    [Category("P1")]
    [Property("TestCaseId", "SEARCH-BAR-025")]
    public void SearchHistoryContainsRecentQueriesWithoutDuplicates()
    {
        _search.Search(LowercaseProductCode);
        _search.Search(Settings.SearchData.ProductCard);

        var history = _search.OpenHistory();
        var historySnapshot = string.Join(" | ", history.Select(item => $"'{item}'"));

        Assert.Multiple(() =>
        {
            Assert.That(history.Count(item =>
                string.Equals(item, LowercaseProductCode, StringComparison.Ordinal)), Is.EqualTo(1),
                $"History: {historySnapshot}");
            Assert.That(history.Count(item =>
                string.Equals(item, Settings.SearchData.ProductCard, StringComparison.Ordinal)), Is.EqualTo(1),
                $"History: {historySnapshot}");
            Assert.That(history.All(item => item.Length > 0), Is.True);
        });
    }

    [Test]
    [Category("P1")]
    [Property("TestCaseId", "SEARCH-BAR-026")]
    public void QueryCanBeSelectedFromSearchHistory()
    {
        _search.Search(LowercaseProductCode);
        _search.Search(Settings.SearchData.ProductCard);

        _search.SelectHistoryItem(LowercaseProductCode);

        Assert.Multiple(() =>
        {
            Assert.That(_search.Query, Is.EqualTo(LowercaseProductCode).IgnoreCase);
            Assert.That(_search.IsProductDisplayed(Settings.SearchData.ProductCode), Is.True);
            var history = _search.OpenHistory();
            Assert.That(history.Count(item =>
                string.Equals(item, LowercaseProductCode, StringComparison.Ordinal)), Is.EqualTo(1),
                $"History: {string.Join(" | ", history.Select(item => $"'{item}'"))}");
        });
    }
}
