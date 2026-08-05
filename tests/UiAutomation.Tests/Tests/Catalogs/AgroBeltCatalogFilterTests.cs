using NUnit.Framework;

namespace UiAutomation.Tests.Tests.Catalogs;

[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
[Category("AgroBelts")]
public sealed class AgroBeltCatalogFilterTests : CatalogFilterTestBase
{
    protected override CatalogDefinition Catalog => CatalogDefinitions.AgroBelts;

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingAgroBelts() => AssertBrandFilter();

    /// <summary>
    /// Открывает фильтр «Тип», выбирает первый вариант в порядке отображения
    /// списка и проверяет, что он применяется и сужает выдачу агроремней.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void TypeFilterNarrowsAgroBeltResults() =>
        AssertFirstListedNarrowingFacet("Тип");

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableAgroBelts() => AssertInStockFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsAgroBeltSelection() => AssertFilterReset();
}
