using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests;

/// <summary>
/// Verifies filter applicability on the faceted catalog pages: after a facet
/// value is applied, the whole result list must match it, and resetting must
/// clear the active filters. Covers the manual scenarios CAT-COM-005/008 for
/// Шини, Оливи and АКБ (which all share the same filter/result markup).
/// </summary>
[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
public sealed class CatalogFilterTests : AuthenticatedUiTestFixture
{
    private CatalogMenuPage _catalogMenu = null!;
    private CatalogFilterPage _filter = null!;

    // Faceted catalog pages that expose a "Бренд" facet, keyed by the Angular
    // categoryClick(index) that opens them. "Підшипники"
    // (#/app/simplesearchPodshipnik) is intentionally omitted: it has no facet
    // filters in the test catalog, so there is no applicability to verify.
    private static readonly (string Name, int Index, string Route)[] Catalogs =
    [
        ("Шини", 0, "#/app/simplesearchTires"),
        ("Колісні диски", 13, "#/app/simplesearchWheelDisc"),
        ("Камери", 23, "#/app/simplesearchCameras"),
        ("Оливи", 3, "#/app/simplesearchOil"),
        ("Тех. рідини", 11, "#/app/simplesearchTechnicalFluids"),
        ("Лампи", 4, "#/app/simplesearchLamps"),
        ("Ремені Агро техніка", 20, "#/app/simplesearchBelts"),
        ("Аварійні з'єднувачі", 25, "#/app/simplesearchPneumo"),
        ("АКБ", 2, "#/app/simplesearchAccum")
    ];

    private const string BrandFacet = "Бренд";

    public static IEnumerable<TestCaseData> CatalogCases =>
        Catalogs.Select(catalog =>
            new TestCaseData(catalog.Index, catalog.Route)
                .SetArgDisplayNames(catalog.Name));

    protected override void OnAuthenticated()
    {
        _catalogMenu = new CatalogMenuPage(Driver, Timeout);
        _filter = new CatalogFilterPage(Driver, Timeout);
    }

    [TestCaseSource(nameof(CatalogCases))]
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

    [TestCaseSource(nameof(CatalogCases))]
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
