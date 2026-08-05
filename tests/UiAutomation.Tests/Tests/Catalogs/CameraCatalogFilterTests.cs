using System.Text.RegularExpressions;
using NUnit.Framework;

namespace UiAutomation.Tests.Tests.Catalogs;

/// <summary>
/// Covers every available control in the camera catalog's "Основні фільтри"
/// block. The diameter is checked directly against every visible camera size.
/// </summary>
[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
[Category("Cameras")]
public sealed class CameraCatalogFilterTests : CatalogFilterTestBase
{
    protected override CatalogDefinition Catalog => CatalogDefinitions.Cameras;

    /// <summary>
    /// Выбирает бренд в фильтре камер, применяет его и проверяет, что в выдаче
    /// остались только камеры выбранного бренда.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingCameras() => AssertBrandFilter();

    /// <summary>
    /// Выбирает диаметр 10, применяет фильтр и проверяет по описанию каждого
    /// товара, что в выдаче остались только камеры выбранного диаметра.
    /// </summary>
    [Test]
    [Property("TestCaseId", "TUBE-001")]
    public void DiameterFilterKeepsOnlyCamerasOfThatDiameter()
    {
        var diameter = AssertNarrowingFacet("Діаметр", "10");
        var pattern = new Regex(
            $@"-{Regex.Escape(diameter.Value)}(?![\d.,])",
            RegexOptions.IgnoreCase);
        var mismatches = Filter.ProductDescriptions
            .Where(description => !pattern.IsMatch(description))
            .ToArray();

        Assert.That(mismatches, Is.Empty,
            $"Cameras outside diameter '{diameter.Value}' were shown: " +
            string.Join(" | ", mismatches));
    }

    /// <summary>
    /// Включает фильтр «Тільки товар у наявності» и проверяет, что каждая
    /// показанная камера имеет положительный остаток хотя бы на одном складе.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableCameras() => AssertInStockFilter();

    /// <summary>
    /// Включает фильтр распродажи и проверяет, что выдача изменилась, не пуста,
    /// а у каждой показанной камеры присутствует признак распродажи.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void SaleFilterShowsOnlyMarkedDownCameras() => AssertSaleFilter();

    /// <summary>
    /// Включает фильтр акционных товаров и проверяет, что он остаётся выбранным,
    /// отображается среди применённых и изменяет непустую выдачу камер.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void PromotionalFilterChangesResultsAndRemainsApplied() =>
        AssertPromotionalFilter();

    /// <summary>
    /// Применяет фильтр камер, затем сбрасывает его и проверяет, что выбранные
    /// параметры очищены, а исходная выдача восстановлена.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsCameraSelection() => AssertFilterReset();
}
