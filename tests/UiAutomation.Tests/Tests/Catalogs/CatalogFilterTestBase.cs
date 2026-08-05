using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests.Catalogs;

/// <summary>
/// Shared setup and assertions for the individual catalog filter fixtures.
/// Each concrete fixture names its catalog and exposes only the scenarios that
/// are supported by that catalog.
/// </summary>
public abstract class CatalogFilterTestBase : AuthenticatedUiTestFixture
{
    private const string BrandFacet = "Бренд";

    private CatalogMenuPage _catalogMenu = null!;

    protected CatalogFilterPage Filter { get; private set; } = null!;

    protected abstract CatalogDefinition Catalog { get; }

    protected override void OnAuthenticated()
    {
        _catalogMenu = new CatalogMenuPage(Driver, Timeout);
        Filter = new CatalogFilterPage(Driver, Timeout);
    }

    [SetUp]
    public void OpenCatalog()
    {
        _catalogMenu.OpenSimpleSearchCatalog(Catalog.MenuIndex, Catalog.Route);
        Filter.WaitUntilLoaded();
    }

    protected void AssertBrandFilter()
    {
        var unfilteredSignature = Filter.ResultSignature;

        var brand = Filter.SelectFirstFacetOption(BrandFacet);
        Filter.ApplyFilters(brand.Value);

        var brands = Filter.ProductBrands;
        var filteredSignature = Filter.ResultSignature;

        Assert.Multiple(() =>
        {
            Assert.That(Filter.ResultCount, Is.GreaterThan(0),
                $"No products remained after filtering '{Catalog.Name}' by brand '{brand.Core}'.");
            Assert.That(filteredSignature, Is.Not.EqualTo(unfilteredSignature),
                $"Applying brand '{brand.Core}' did not change products for '{Catalog.Name}'.");
            Assert.That(Filter.SelectedFacetValuesCount, Is.GreaterThan(0),
                $"Brand '{brand.Core}' is not selected in '{Catalog.Name}'.");
            Assert.That(Filter.HasAppliedFilter(brand.Value), Is.True,
                $"Brand '{brand.Value}' is absent from the applied filters for '{Catalog.Name}'.");
            Assert.That(brands, Is.Not.Empty);
            Assert.That(
                brands.All(value => value.Contains(brand.Core, StringComparison.OrdinalIgnoreCase)),
                Is.True,
                $"Some '{Catalog.Name}' results are not brand '{brand.Core}': " +
                string.Join(", ", brands.Distinct()));
            Assert.That(Filter.ResultCount, Is.LessThanOrEqualTo(brand.Count),
                $"More products shown ({Filter.ResultCount}) than the facet promised ({brand.Count}).");
        });
    }

    protected FacetOption AssertNarrowingFacet(string facetTitle, string? optionValue = null)
    {
        var unfilteredSignature = Filter.ResultSignature;
        var option = optionValue is null
            ? Filter.SelectFirstFacetOption(facetTitle)
            : Filter.SelectFacetOption(facetTitle, optionValue);

        Filter.ApplyFilters(option.Value);
        var filteredSignature = Filter.ResultSignature;

        Assert.Multiple(() =>
        {
            Assert.That(Filter.HasActiveFilters, Is.True,
                $"'{facetTitle}' filter was not registered as active for '{Catalog.Name}'.");
            Assert.That(Filter.SelectedFacetValuesCount, Is.GreaterThan(0),
                $"'{facetTitle}' = '{option.Value}' is not selected for '{Catalog.Name}'.");
            Assert.That(Filter.HasAppliedFilter(option.Value), Is.True,
                $"'{facetTitle}' = '{option.Value}' is absent from the applied filters " +
                $"for '{Catalog.Name}'.");
            Assert.That(filteredSignature, Is.Not.EqualTo(unfilteredSignature),
                $"'{facetTitle}' = '{option.Value}' did not change products for '{Catalog.Name}'.");
            Assert.That(Filter.ResultCount, Is.GreaterThan(0),
                $"'{facetTitle}' = '{option.Value}' returned no products for '{Catalog.Name}'.");
            Assert.That(Filter.ResultCount, Is.LessThanOrEqualTo(option.Count),
                $"More products shown ({Filter.ResultCount}) than the facet promised ({option.Count}).");
        });

        return option;
    }

    protected FacetOption AssertDescriptionsMatchAfterFacet(
        string facetTitle,
        string optionValue,
        Func<string, bool> matches,
        string expectedDescription)
    {
        var option = AssertNarrowingFacet(facetTitle, optionValue);
        var descriptions = Filter.ProductDescriptions;
        var mismatches = descriptions.Where(description => !matches(description)).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(descriptions, Is.Not.Empty,
                $"No product descriptions were rendered for '{Catalog.Name}'.");
            Assert.That(mismatches, Is.Empty,
                $"Products without {expectedDescription} were shown for '{Catalog.Name}': " +
                string.Join(" | ", mismatches));
        });

        return option;
    }

    protected void AssertInStockFilter()
    {
        Filter.SwitchToListView();
        Filter.EnableInStockOnly();
        Filter.ApplyFilters(
            "Тільки товар у наявності",
            requireResultChange: false);

        var withoutStock = Filter.ProductsWithoutStock();

        Assert.Multiple(() =>
        {
            Assert.That(Filter.HasActiveFilters, Is.True,
                $"The in-stock filter was not registered as active for '{Catalog.Name}'.");
            Assert.That(Filter.IsInStockOnlyEnabled, Is.True,
                $"The in-stock checkbox is not selected for '{Catalog.Name}'.");
            Assert.That(Filter.HasAppliedFilter("Тільки товар у наявності"), Is.True,
                $"The in-stock filter is absent from the applied filters for '{Catalog.Name}'.");
            Assert.That(Filter.ResultCount, Is.GreaterThan(0),
                $"No products remained after the in-stock filter for '{Catalog.Name}'.");
            Assert.That(withoutStock, Is.Empty,
                $"Products shown with zero stock in all warehouses for '{Catalog.Name}': " +
                string.Join(" | ", withoutStock));
        });
    }

    protected void AssertSaleFilter()
    {
        var unfilteredSignature = Filter.ResultSignature;

        Filter.EnableSaleOnly();
        Filter.ApplyFilters("Розпродаж");

        var withoutSaleMarker = Filter.ProductsWithoutSaleMarker();

        Assert.Multiple(() =>
        {
            Assert.That(Filter.IsSaleOnlyEnabled, Is.True,
                $"The sale checkbox is not selected for '{Catalog.Name}'.");
            Assert.That(Filter.HasAppliedFilter("Розпродаж"), Is.True,
                $"The sale filter is absent from the applied filters for '{Catalog.Name}'.");
            Assert.That(Filter.ResultSignature, Is.Not.EqualTo(unfilteredSignature),
                $"The sale filter did not change products for '{Catalog.Name}'.");
            Assert.That(Filter.ResultCount, Is.GreaterThan(0),
                $"No products remained after the sale filter for '{Catalog.Name}'.");
            Assert.That(withoutSaleMarker, Is.Empty,
                $"Products without a sale marker were shown for '{Catalog.Name}': " +
                string.Join(" | ", withoutSaleMarker));
        });
    }

    protected void AssertPromotionalFilter()
    {
        var unfilteredSignature = Filter.ResultSignature;

        Filter.EnablePromotionalOnly();
        Filter.ApplyFilters("Акційний товар");

        Assert.Multiple(() =>
        {
            Assert.That(Filter.IsPromotionalOnlyEnabled, Is.True,
                $"The promotional-products checkbox is not selected for '{Catalog.Name}'.");
            Assert.That(Filter.HasAppliedFilter("Акційний товар"), Is.True,
                $"The promotional-products filter is absent from the applied filters " +
                $"for '{Catalog.Name}'.");
            Assert.That(Filter.ResultSignature, Is.Not.EqualTo(unfilteredSignature),
                $"The promotional-products filter did not change products for '{Catalog.Name}'.");
            Assert.That(Filter.ResultCount, Is.GreaterThan(0),
                $"No products remained after the promotional-products filter for '{Catalog.Name}'.");
        });
    }

    protected void AssertFilterReset()
    {
        var unfilteredSignature = Filter.ResultSignature;

        var brand = Filter.SelectFirstFacetOption(BrandFacet);
        Filter.ApplyFilters(brand.Value);
        var filteredSignature = Filter.ResultSignature;

        Assert.That(Filter.HasActiveFilters, Is.True,
            $"Filter was not registered as active for '{Catalog.Name}'.");
        Assert.That(filteredSignature, Is.Not.EqualTo(unfilteredSignature),
            $"Applied filter did not change products for '{Catalog.Name}'.");

        Filter.ResetFilters();

        Assert.Multiple(() =>
        {
            Assert.That(Filter.HasActiveFilters, Is.False,
                $"Filters were not cleared after reset for '{Catalog.Name}'.");
            Assert.That(Filter.SelectedFacetValuesCount, Is.Zero,
                $"A facet value remains selected after reset for '{Catalog.Name}'.");
            Assert.That(Filter.ResultSignature, Is.EqualTo(unfilteredSignature),
                $"The original products were not restored after reset for '{Catalog.Name}'.");
        });
    }
}
