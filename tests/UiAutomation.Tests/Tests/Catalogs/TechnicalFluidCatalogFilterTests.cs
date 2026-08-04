using NUnit.Framework;

namespace UiAutomation.Tests.Tests.Catalogs;

[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
[Category("TechnicalFluids")]
public sealed class TechnicalFluidCatalogFilterTests : CatalogFilterTestBase
{
    protected override CatalogDefinition Catalog => CatalogDefinitions.TechnicalFluids;

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingTechnicalFluids() => AssertBrandFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void PurposeFilterNarrowsTechnicalFluidResults() => AssertNarrowingFacet("Призначення");

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableTechnicalFluids() => AssertInStockFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsTechnicalFluidSelection() => AssertFilterReset();
}
