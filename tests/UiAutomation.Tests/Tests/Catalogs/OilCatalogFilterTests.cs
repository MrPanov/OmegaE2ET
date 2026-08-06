using NUnit.Framework;

namespace UiAutomation.Tests.Tests.Catalogs;

/// <summary>
/// Covers every control in the oil catalog's "Основні фільтри" block. Each test
/// starts from a fresh oil result set and applies one filter. Facet tests verify
/// the active tag, changed result set and advertised count, while stock and sale
/// tests inspect every visible product card directly.
/// </summary>
[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
[Category("Oils")]
public sealed class OilCatalogFilterTests : CatalogFilterTestBase
{
    protected override CatalogDefinition Catalog => CatalogDefinitions.Oils;

    /// <summary>
    /// Выбирает бренд в фильтре масел, применяет его и проверяет, что в выдаче
    /// остались только товары выбранного бренда.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingOils() => AssertBrandFilter();

    /// <summary>
    /// Выбирает автомобиль ACURA и проверяет, что фильтр отображается среди
    /// применённых, сужает выдачу и оставляет хотя бы одно масло.
    /// </summary>
    [Test]
    [Property("TestCaseId", "OIL-002")]
    public void VehicleFilterNarrowsOilResults() => AssertNarrowingFacet("Авто", "ACURA");

    /// <summary>
    /// Выбирает назначение «Автопромивна олія» и проверяет применение фильтра,
    /// изменение выдачи и соответствие количества результатов счётчику фасета.
    /// </summary>
    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void PurposeFilterNarrowsOilResults() =>
        AssertNarrowingFacet("Призначення", "Автопромивна олія");

    /// <summary>
    /// Выбирает вязкость 10W, применяет фильтр и проверяет, что обозначение 10W
    /// присутствует в описании каждого показанного масла.
    /// </summary>
    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void DensityFilterKeepsOnlyOilsWithSelectedViscosity()
    {
        var density = AssertNarrowingFacet("Густина", "10W");
        var mismatches = Filter.ProductDescriptions
            .Where(description => !description.Contains(
                density.Value,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.That(mismatches, Is.Empty,
            $"Oils without viscosity '{density.Value}' were shown: " +
            string.Join(" | ", mismatches));
    }

    /// <summary>
    /// Выбирает объём 0,5 литра, применяет фильтр и проверяет выбранное значение
    /// в описании каждого товара с учётом точки и запятой в десятичной дроби.
    /// </summary>
    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void VolumeFilterKeepsOnlyOilsWithSelectedVolume()
    {
        var volume = AssertNarrowingFacet("Об `єм", "0.5");
        var expected = NormalizeDecimal(volume.Value);
        var mismatches = Filter.ProductDescriptions
            .Where(description => !NormalizeDecimal(description).Contains(
                expected,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.That(mismatches, Is.Empty,
            $"Oils without volume '{volume.Value}' were shown: " +
            string.Join(" | ", mismatches));
    }

    /// <summary>
    /// Выбирает применение «Промивні» и проверяет, что фильтр применяется,
    /// изменяет выдачу и возвращает непустой ограниченный набор товаров.
    /// </summary>
    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void ApplicationFilterNarrowsOilResults() =>
        AssertNarrowingFacet("Застосування", "Промивні");

    /// <summary>
    /// Выбирает тип «Мінеральна олива» и проверяет applied tag, изменение
    /// выдачи, непустой результат и соответствие счётчику фасета.
    /// </summary>
    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void OilTypeFilterNarrowsOilResults() =>
        AssertNarrowingFacet("Тип", "Мінеральна олива");

    /// <summary>
    /// Выбирает спецификацию API CF-4 и проверяет, что фильтр применяется,
    /// сужает выдачу и оставляет хотя бы один подходящий товар.
    /// </summary>
    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void ApiSpecificationFilterNarrowsOilResults() =>
        AssertNarrowingFacet("Специфікації API", "CF-4");

    /// <summary>
    /// Выбирает спецификацию ACEA C1 и проверяет applied tag, изменение
    /// выдачи и соответствие количества результатов значению фасета.
    /// </summary>
    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void AceaSpecificationFilterNarrowsOilResults() =>
        AssertNarrowingFacet("Специфікації ACEA", "C1");

    /// <summary>
    /// Выбирает спецификацию ILSAC GF-3 и проверяет, что она применяется,
    /// сужает выдачу и возвращает непустой набор масел.
    /// </summary>
    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void IlsacSpecificationFilterNarrowsOilResults() =>
        AssertNarrowingFacet("Специфікації LSAC", "GF-3");

    /// <summary>
    /// Выбирает спецификацию JASO DH-1-17 и проверяет applied tag, изменение
    /// выдачи, непустой результат и ограничение по счётчику фасета.
    /// </summary>
    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void JasoSpecificationFilterNarrowsOilResults() =>
        AssertNarrowingFacet("Специфікації JASO", "DH-1-17");

    /// <summary>
    /// Выбирает допуск OEM Allison TES295 и проверяет, что фильтр применяется,
    /// сужает выдачу и оставляет хотя бы одно подходящее масло.
    /// </summary>
    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void OemSpecificationFilterNarrowsOilResults() =>
        AssertNarrowingFacet("Специфікації OEM", "Allison TES295");

    /// <summary>
    /// Выбирает стандарт ГОСТ Afnor 48600 и проверяет applied tag, изменение
    /// выдачи и соответствие количества результатов счётчику фасета.
    /// </summary>
    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void GostSpecificationFilterNarrowsOilResults() =>
        AssertNarrowingFacet("ГОСТ", "Afnor 48600");

    /// <summary>
    /// Включает фильтр «Тільки товар у наявності» и проверяет, что первые
    /// показанные масла имеют положительный остаток на выбранном складе.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableOils() => AssertInStockFilter();

    /// <summary>
    /// Включает фильтр распродажи и проверяет изменение непустой выдачи,
    /// applied tag и наличие признака распродажи у каждого показанного масла.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void SaleFilterShowsOnlyMarkedDownOils() => AssertSaleFilter();

    /// <summary>
    /// Включает фильтр акционных товаров и проверяет, что он остаётся выбранным,
    /// отображается среди применённых и изменяет непустую выдачу масел.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void PromotionalFilterChangesResultsAndRemainsApplied() =>
        AssertPromotionalFilter();

    /// <summary>
    /// Применяет фильтр масел, сбрасывает его и проверяет очистку выбранных
    /// значений и восстановление первоначальной выдачи.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsOilSelection() => AssertFilterReset();

    private static string NormalizeDecimal(string value) => value.Replace(',', '.');
}
