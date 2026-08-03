using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests;

/// <summary>
/// Filter applicability across the faceted catalog pages, data-driven per catalog:
/// an applied brand filter must keep only matching products, applying another facet
/// must narrow the list, resetting must clear the selection, and the in-stock filter
/// must leave only products available in a warehouse. Covers CAT-COM-005/008.
/// The tyres catalog has its own detailed suite in <see cref="TyreCatalogFilterTests"/>.
/// </summary>
[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
public sealed class CatalogFilterTests : AuthenticatedUiTestFixture
{
    private const string BrandFacet = "Бренд";

    private CatalogMenuPage _catalogMenu = null!;
    private CatalogFilterPage _filter = null!;

    /// <param name="Brand">Has a "Бренд" facet (brand + reset tests apply).</param>
    /// <param name="NarrowFacet">A safe non-brand facet to exercise, or null.</param>
    /// <param name="Stock">Supports the table view with warehouse stock columns.</param>
    private sealed record CatalogSpec(
        string Name,
        int Index,
        string Route,
        bool Brand,
        string? NarrowFacet,
        bool Stock);

    // Шини is covered in depth by TyreCatalogFilterTests, so it only takes part in the
    // brand test here (Stock/NarrowFacet left off to avoid duplicating that suite).
    private static readonly CatalogSpec[] Catalogs =
    [
        new("Шини", 0, "#/app/simplesearchTires", Brand: true, NarrowFacet: null, Stock: false),
        new("Колісні диски", 13, "#/app/simplesearchWheelDisc", true, "Діаметр", true),
        new("Камери", 23, "#/app/simplesearchCameras", true, "Діаметр", true),
        new("Оливи", 3, "#/app/simplesearchOil", true, "Густина", true),
        new("Тех. рідини", 11, "#/app/simplesearchTechnicalFluids", true, "Призначення", true),
        new("ЗЧ до сільгосптехніки", 21, "#/app/simplesearchAgro", true, null, true),
        new("АКБ", 2, "#/app/simplesearchAccum", true, "Ємність", true),
        new("Кузов та оптика", 1, "#/app/simplesearchOptic", true, "Група", true),
        new("Лампи", 4, "#/app/simplesearchLamps", true, "Потужність", true),
        // Підшипники has no facet filters, but still supports the in-stock table view.
        new("Підшипники", 15, "#/app/simplesearchPodshipnik", Brand: false, NarrowFacet: null, Stock: true),
        new("Ремені Агро техніка", 20, "#/app/simplesearchBelts", true, "Тип", true),
        new("Аварійні з'єднувачі", 25, "#/app/simplesearchPneumo", true, "Матеріал", true)
    ];

    public static IEnumerable<TestCaseData> BrandCases =>
        Catalogs.Where(c => c.Brand)
            .Select(c => new TestCaseData(c.Index, c.Route).SetArgDisplayNames(c.Name));

    public static IEnumerable<TestCaseData> NarrowFacetCases =>
        Catalogs.Where(c => c.NarrowFacet is not null)
            .Select(c => new TestCaseData(c.Index, c.Route, c.NarrowFacet!).SetArgDisplayNames(c.Name));

    public static IEnumerable<TestCaseData> StockCases =>
        Catalogs.Where(c => c.Stock)
            .Select(c => new TestCaseData(c.Index, c.Route).SetArgDisplayNames(c.Name));

    protected override void OnAuthenticated()
    {
        _catalogMenu = new CatalogMenuPage(Driver, Timeout);
        _filter = new CatalogFilterPage(Driver, Timeout);
    }

    [TestCaseSource(nameof(BrandCases))]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingProducts(int categoryIndex, string route)
    {
        _catalogMenu.OpenSimpleSearchCatalog(categoryIndex, route);
        _filter.WaitUntilLoaded();

        var brand = _filter.SelectFirstFacetOption(BrandFacet);
        _filter.ApplyFilters();

        var brands = _filter.ProductBrands;

        Assert.Multiple(() =>
        {
            Assert.That(_filter.ResultCount, Is.GreaterThan(0),
                $"No products remained after filtering '{route}' by brand '{brand.Core}'.");
            Assert.That(brands, Is.Not.Empty);
            Assert.That(
                brands.All(b => b.Contains(brand.Core, StringComparison.OrdinalIgnoreCase)),
                Is.True,
                $"Some results are not brand '{brand.Core}': " +
                $"{string.Join(", ", brands.Distinct())}");
            Assert.That(_filter.ResultCount, Is.LessThanOrEqualTo(brand.Count),
                $"More products shown ({_filter.ResultCount}) than the facet promised ({brand.Count}).");
        });
    }

    [TestCaseSource(nameof(NarrowFacetCases))]
    [Property("TestCaseId", "CAT-COM-005")]
    public void FacetFilterNarrowsResults(int categoryIndex, string route, string facetTitle)
    {
        _catalogMenu.OpenSimpleSearchCatalog(categoryIndex, route);
        _filter.WaitUntilLoaded();

        var option = _filter.SelectFirstFacetOption(facetTitle);
        _filter.ApplyFilters();

        Assert.Multiple(() =>
        {
            Assert.That(_filter.HasActiveFilters, Is.True,
                $"'{facetTitle}' filter was not registered as active for '{route}'.");
            Assert.That(_filter.ResultCount, Is.GreaterThan(0),
                $"'{facetTitle}' = '{option.Value}' returned no products for '{route}'.");
            Assert.That(_filter.ResultCount, Is.LessThanOrEqualTo(option.Count),
                $"More products shown ({_filter.ResultCount}) than the facet promised ({option.Count}).");
        });
    }

    [TestCaseSource(nameof(StockCases))]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyProductsAvailableInWarehouses(int categoryIndex, string route)
    {
        _catalogMenu.OpenSimpleSearchCatalog(categoryIndex, route);
        _filter.WaitUntilLoaded();

        _filter.SwitchToListView();
        _filter.EnableInStockOnly();
        _filter.ApplyFilters();

        var withoutStock = _filter.ProductsWithoutStock();

        Assert.Multiple(() =>
        {
            Assert.That(_filter.HasActiveFilters, Is.True,
                $"The in-stock filter was not registered as active for '{route}'.");
            Assert.That(_filter.ResultCount, Is.GreaterThan(0),
                $"No products remained after the in-stock filter for '{route}'.");
            Assert.That(withoutStock, Is.Empty,
                $"Products shown with zero stock in all warehouses for '{route}': " +
                string.Join(" | ", withoutStock));
        });
    }

    [TestCaseSource(nameof(BrandCases))]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsTheAppliedSelection(int categoryIndex, string route)
    {
        _catalogMenu.OpenSimpleSearchCatalog(categoryIndex, route);
        _filter.WaitUntilLoaded();

        _filter.SelectFirstFacetOption(BrandFacet);
        _filter.ApplyFilters();
        Assert.That(_filter.HasActiveFilters, Is.True,
            $"Filter was not registered as active for '{route}'.");

        _filter.ResetFilters();

        Assert.That(_filter.HasActiveFilters, Is.False,
            $"Filters were not cleared after reset for '{route}'.");
    }
}
