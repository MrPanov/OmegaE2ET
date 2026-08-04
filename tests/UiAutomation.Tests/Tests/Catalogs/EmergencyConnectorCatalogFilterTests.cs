using NUnit.Framework;

namespace UiAutomation.Tests.Tests.Catalogs;

[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
[Category("EmergencyConnectors")]
public sealed class EmergencyConnectorCatalogFilterTests : CatalogFilterTestBase
{
    protected override CatalogDefinition Catalog => CatalogDefinitions.EmergencyConnectors;

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingEmergencyConnectors() => AssertBrandFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void MaterialFilterNarrowsEmergencyConnectorResults() => AssertNarrowingFacet("Матеріал");

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableEmergencyConnectors() => AssertInStockFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsEmergencyConnectorSelection() => AssertFilterReset();
}
