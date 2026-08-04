using NUnit.Framework;

namespace UiAutomation.Tests.Tests.Catalogs;

[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
[Category("Oils")]
public sealed class OilCatalogFilterTests : CatalogFilterTestBase
{
    protected override CatalogDefinition Catalog => CatalogDefinitions.Oils;

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingOils() => AssertBrandFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void DensityFilterNarrowsOilResults() => AssertNarrowingFacet("Густина");

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableOils() => AssertInStockFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsOilSelection() => AssertFilterReset();
}
