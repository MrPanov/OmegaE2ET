using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Pages;

/// <summary>
/// Facade for faceted catalog pages. Facet controls and result rendering are
/// implemented by separate components so their waits and assertions stay focused.
/// </summary>
public sealed class CatalogFilterPage
{
    private readonly CatalogFacetsComponent _facets;
    private readonly CatalogResultsComponent _results;
    private readonly DomMutationTracker _mutations;

    public CatalogFilterPage(IWebDriver driver, TimeSpan waitTimeout)
    {
        var wait = new WebDriverWait(driver, waitTimeout);
        _facets = new CatalogFacetsComponent(driver, wait);
        _results = new CatalogResultsComponent(driver, wait);
        _mutations = new DomMutationTracker(driver);
    }

    public IReadOnlyList<string> ProductCodes => _results.ProductCodes;

    public IReadOnlyList<string> ProductDescriptions => _results.ProductDescriptions;

    public IReadOnlyList<string> PrimaryProductDescriptions =>
        _results.PrimaryProductDescriptions;

    public IReadOnlyList<string> ProductBrands => _results.ProductBrands;

    public int ResultCount => _results.ResultCount;

    public int PrimaryResultCount => _results.PrimaryResultCount;

    public bool HasActiveFilters => _facets.HasActiveFilters;

    public int SelectedFacetValuesCount => _facets.SelectedFacetValuesCount;

    public bool IsInStockOnlyEnabled => _facets.IsInStockOnlyEnabled;

    public bool IsSaleOnlyEnabled => _facets.IsSaleOnlyEnabled;

    public bool IsPromotionalOnlyEnabled => _facets.IsPromotionalOnlyEnabled;

    public bool IsPassengerFormFactorSelected => _facets.IsPassengerFormFactorSelected;

    public string ResultSignature => _results.Signature();

    public bool HasAppliedFilter(string value) => _results.HasAppliedFilter(value);

    public void WaitUntilLoaded() => _results.WaitUntilLoaded();

    public FacetOption SelectMostRestrictiveFacetOption(string facetTitle) =>
        _facets.SelectMostRestrictiveFacetOption(facetTitle);

    public FacetOption SelectFirstListedFacetOption(string facetTitle) =>
        _facets.SelectFirstListedFacetOption(facetTitle);

    public FacetOption SelectFirstAvailableFacetOption(string facetTitle) =>
        _facets.SelectFirstAvailableFacetOption(facetTitle);

    public FacetOption SelectFacetOption(string facetTitle, string optionValue) =>
        _facets.SelectFacetOption(facetTitle, optionValue);

    public void SwitchToListView() => _facets.SwitchToListView();

    public IReadOnlyList<string> ProductsWithoutStock(int productLimit) =>
        _results.ProductsWithoutStock(productLimit);

    public IReadOnlyList<string> PrimaryProductsWithoutBrand(string expectedBrand) =>
        _results.PrimaryProductsWithoutBrand(expectedBrand);

    public IReadOnlyList<string> ProductsWithoutSaleMarker() =>
        _results.ProductsWithoutSaleMarker();

    public void EnableInStockOnly() => _facets.EnableInStockOnly();

    public void EnableSaleOnly() => _facets.EnableSaleOnly();

    public void EnablePromotionalOnly() => _facets.EnablePromotionalOnly();

    public void SelectPassengerFormFactor() => _facets.SelectPassengerFormFactor();

    public void ApplyFilters(bool requireResultChange = true)
    {
        ApplyFilters(expectedAppliedFilter: null, requireResultChange);
    }

    public void ApplyFilters(
        string? expectedAppliedFilter,
        bool requireResultChange = true)
    {
        var previousSignature = ResultSignature;
        var mutationVersion = _mutations.Snapshot();
        _facets.Apply();
        _results.WaitForChangedAndSettled(
            previousSignature,
            _mutations,
            mutationVersion,
            requireResultChange,
            expectedAppliedFilter);
    }

    public void ResetFilters()
    {
        var previousSignature = ResultSignature;
        var mutationVersion = _mutations.Snapshot();
        _facets.Reset();
        _results.WaitForChangedAndSettled(
            previousSignature,
            _mutations,
            mutationVersion,
            requireResultChange: true);
    }
}
