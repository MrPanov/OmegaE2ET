using System.Text.RegularExpressions;
using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests;

/// <summary>
/// Filter applicability for the tyres catalog (Шини). Each test opens the catalog
/// fresh, applies one filter and checks the result list against it.
///
/// Діаметр and Типорозмір are printed in the product description (e.g.
/// "Шина 155/65R13 …"), so those results are checked exactly. The in-stock filter
/// is checked in the table view against the per-warehouse columns (КВ-Ш, ХРК-Ш …):
/// every shown product must have ≥ 1 pc in at least one warehouse. Сезонність and
/// Призначення are not shown on the card, so those tests verify that the filter is
/// applied and returns a non-empty, bounded set.
/// </summary>
[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
public sealed class TyreCatalogFilterTests : AuthenticatedUiTestFixture
{
    private const int CategoryIndex = 0;
    private const string Route = "#/app/simplesearchTires";

    private CatalogMenuPage _catalogMenu = null!;
    private CatalogFilterPage _filter = null!;

    protected override void OnAuthenticated()
    {
        _catalogMenu = new CatalogMenuPage(Driver, Timeout);
        _filter = new CatalogFilterPage(Driver, Timeout);
    }

    [SetUp]
    public void OpenTyreCatalog()
    {
        _catalogMenu.OpenSimpleSearchCatalog(CategoryIndex, Route);
        _filter.WaitUntilLoaded();
    }

    [Test]
    [Property("TestCaseId", "TYRE-001")]
    public void DiameterFilterKeepsOnlyTyresOfThatDiameter()
    {
        var diameter = _filter.SelectFacetOption("Діаметр", "16");
        _filter.ApplyFilters();

        var descriptions = _filter.ProductDescriptions;
        var pattern = new Regex($@"R{Regex.Escape(diameter.Value)}(?!\d)");

        Assert.Multiple(() =>
        {
            Assert.That(_filter.ResultCount, Is.GreaterThan(0),
                $"No tyres left after filtering by diameter R{diameter.Value}.");
            Assert.That(descriptions.All(description => pattern.IsMatch(description)), Is.True,
                $"Some tyres are not diameter R{diameter.Value}: " +
                $"{string.Join(" | ", descriptions.Where(description => !pattern.IsMatch(description)))}");
        });
    }

    [Test]
    [Property("TestCaseId", "TYRE-001")]
    public void SizeFilterKeepsOnlyTyresOfThatSize()
    {
        var size = _filter.SelectFacetOption("Типорозмір", "155/65R14");
        _filter.ApplyFilters();

        var descriptions = _filter.ProductDescriptions;

        Assert.Multiple(() =>
        {
            Assert.That(_filter.ResultCount, Is.GreaterThan(0),
                $"No tyres left after filtering by size '{size.Value}'.");
            Assert.That(
                descriptions.All(description =>
                    description.Contains(size.Value, StringComparison.OrdinalIgnoreCase)),
                Is.True,
                $"Some tyres are not size '{size.Value}': " +
                $"{string.Join(" | ", descriptions.Where(description => !description.Contains(size.Value, StringComparison.OrdinalIgnoreCase)))}");
        });
    }

    [Test]
    [Property("TestCaseId", "TYRE-002")]
    public void SeasonFilterIsAppliedAndReturnsBoundedResults() =>
        AssertNarrowingFacet("Сезонність", "Зима");

    [Test]
    [Property("TestCaseId", "TYRE-002")]
    public void PurposeFilterIsAppliedAndReturnsBoundedResults() =>
        AssertNarrowingFacet("Призначення", "Легкова");

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyProductsAvailableInWarehouses()
    {
        // The warehouse columns (КВ-Ш, ХРК-Ш …) and the in-stock checkbox live in
        // the table view; a product is available if any warehouse shows ≥ 1 pc.
        _filter.SwitchToListView();
        _filter.EnableInStockOnly();
        _filter.ApplyFilters();

        var withoutStock = _filter.ProductsWithoutStock();

        Assert.Multiple(() =>
        {
            Assert.That(_filter.HasActiveFilters, Is.True,
                "The in-stock filter was not registered as active.");
            Assert.That(_filter.ResultCount, Is.GreaterThan(0),
                "No products remained after the in-stock filter.");
            Assert.That(withoutStock, Is.Empty,
                "Products shown with zero stock in all warehouses (КВ-Ш/ХРК-Ш): " +
                string.Join(" | ", withoutStock));
        });
    }

    private void AssertNarrowingFacet(string facetTitle, string optionValue)
    {
        var option = _filter.SelectFacetOption(facetTitle, optionValue);
        _filter.ApplyFilters();

        Assert.Multiple(() =>
        {
            Assert.That(_filter.HasActiveFilters, Is.True,
                $"'{facetTitle}' filter was not registered as active.");
            Assert.That(_filter.ResultCount, Is.GreaterThan(0),
                $"'{facetTitle}' = '{option.Value}' returned no products.");
            Assert.That(_filter.ResultCount, Is.LessThanOrEqualTo(option.Count),
                $"More products shown ({_filter.ResultCount}) than the facet promised ({option.Count}).");
        });
    }
}
