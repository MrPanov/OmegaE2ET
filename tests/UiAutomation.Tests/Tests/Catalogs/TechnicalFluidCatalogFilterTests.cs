using NUnit.Framework;

namespace UiAutomation.Tests.Tests.Catalogs;

/// <summary>
/// Проверки всех фильтров блока «Основні фільтри» каталога технических
/// жидкостей. Каждый тест начинает работу с исходной выдачи.
/// </summary>
[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
[Category("TechnicalFluids")]
public sealed class TechnicalFluidCatalogFilterTests : CatalogFilterTestBase
{
    protected override CatalogDefinition Catalog => CatalogDefinitions.TechnicalFluids;

    /// <summary>
    /// Выбирает бренд технической жидкости и проверяет, что в выдаче остались
    /// только товары выбранного бренда.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingTechnicalFluids() => AssertBrandFilter();

    /// <summary>
    /// Выбирает назначение «Охолоджувальні рідини» и проверяет, что фильтр
    /// применяется, сужает выдачу и оставляет хотя бы один товар.
    /// </summary>
    [Test]
    [Property("TestCaseId", "FLUID-001")]
    public void PurposeFilterNarrowsTechnicalFluidResults() =>
        AssertNarrowingFacet("Призначення", "Охолоджувальні рідини");

    /// <summary>
    /// Включает фильтр наличия и проверяет, что каждая техническая жидкость
    /// имеет положительный остаток хотя бы на одном доступном складе.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableTechnicalFluids() => AssertInStockFilter();

    /// <summary>
    /// Включает распродажу и проверяет изменение выдачи и признак распродажи
    /// у каждого показанного товара.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void SaleFilterShowsOnlyMarkedDownTechnicalFluids() => AssertSaleFilter();

    /// <summary>
    /// Применяет фильтр технических жидкостей, сбрасывает его и проверяет
    /// очистку параметров и восстановление исходной выдачи.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsTechnicalFluidSelection() => AssertFilterReset();
}
