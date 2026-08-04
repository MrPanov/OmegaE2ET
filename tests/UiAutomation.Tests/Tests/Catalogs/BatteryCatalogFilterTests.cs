using NUnit.Framework;

namespace UiAutomation.Tests.Tests.Catalogs;

[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
[Category("Batteries")]
public sealed class BatteryCatalogFilterTests : CatalogFilterTestBase
{
    protected override CatalogDefinition Catalog => CatalogDefinitions.Batteries;

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingBatteries() => AssertBrandFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void CapacityFilterNarrowsBatteryResults() => AssertNarrowingFacet("Ємність");

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableBatteries() => AssertInStockFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsBatterySelection() => AssertFilterReset();
}
