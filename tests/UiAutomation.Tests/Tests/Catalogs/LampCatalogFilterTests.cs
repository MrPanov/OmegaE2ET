using NUnit.Framework;

namespace UiAutomation.Tests.Tests.Catalogs;

[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
[Category("Lamps")]
public sealed class LampCatalogFilterTests : CatalogFilterTestBase
{
    protected override CatalogDefinition Catalog => CatalogDefinitions.Lamps;

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingLamps() => AssertBrandFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void PowerFilterNarrowsLampResults() => AssertNarrowingFacet("Потужність");

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableLamps() => AssertInStockFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsLampSelection() => AssertFilterReset();
}
