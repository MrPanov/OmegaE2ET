using NUnit.Framework;

namespace UiAutomation.Tests.Tests.Catalogs;

/// <summary>
/// Проверки доступных фильтров каталога кузова и оптики. Зависимый фасет
/// «Модель» не покрывается согласно принятому ограничению набора тестов.
/// </summary>
[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
[Category("BodyAndOptics")]
public sealed class BodyAndOpticsCatalogFilterTests : CatalogFilterTestBase
{
    protected override CatalogDefinition Catalog => CatalogDefinitions.BodyAndOptics;

    /// <summary>
    /// Выбирает бренд кузовной детали или оптики и проверяет, что выдача
    /// содержит только товары выбранного бренда.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingBodyAndOpticsProducts() => AssertBrandFilter();

    /// <summary>
    /// Выбирает автомобиль ALFA ROMEO и проверяет, что фильтр применяется,
    /// изменяет выдачу и оставляет хотя бы один подходящий товар.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BODY-001")]
    public void VehicleFilterNarrowsBodyAndOpticsResults() =>
        AssertNarrowingFacet("Авто", "ALFA ROMEO");

    /// <summary>
    /// Выбирает группу «Дзеркала» и проверяет, что она отображается среди
    /// применённых фильтров и сужает непустую выдачу.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BODY-001")]
    public void GroupFilterNarrowsBodyAndOpticsResults() =>
        AssertNarrowingFacet("Група товарів", "Дзеркала");

    /// <summary>
    /// Выбирает сторону «ліворуч» и проверяет применение фильтра, изменение
    /// выдачи и соответствие количества счётчику фасета.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BODY-001")]
    public void SideFilterNarrowsBodyAndOpticsResults() =>
        AssertNarrowingFacet("Сторона", "ліворуч");

    /// <summary>
    /// Включает фильтр наличия и проверяет положительный остаток каждого товара
    /// хотя бы на одном доступном складе.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableBodyAndOpticsProducts() => AssertInStockFilter();

    /// <summary>
    /// Включает распродажу и проверяет изменение выдачи и наличие признака
    /// распродажи у каждого показанного товара.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void SaleFilterShowsOnlyMarkedDownBodyAndOpticsProducts() => AssertSaleFilter();

    /// <summary>
    /// Включает акционный товар и проверяет, что фильтр применился и изменил
    /// непустую выдачу кузовных деталей и оптики.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void PromotionalFilterChangesResultsAndRemainsApplied() =>
        AssertPromotionalFilter();

    /// <summary>
    /// Применяет фильтр, сбрасывает его и проверяет очистку параметров и
    /// восстановление первоначальной выдачи каталога.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsBodyAndOpticsSelection() => AssertFilterReset();
}
