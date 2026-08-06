using System.Text.RegularExpressions;
using NUnit.Framework;

namespace UiAutomation.Tests.Tests.Catalogs;

/// <summary>
/// Проверки всех доступных фильтров каталога колёсных дисков. Геометрические
/// параметры и цвет дополнительно сверяются с описанием каждого товара.
/// </summary>
[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
[Category("WheelDiscs")]
public sealed class WheelDiscCatalogFilterTests : CatalogFilterTestBase
{
    protected override CatalogDefinition Catalog => CatalogDefinitions.WheelDiscs;

    /// <summary>
    /// Выбирает бренд дисков и проверяет, что в выдаче остались только товары
    /// выбранного бренда.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingWheelDiscs() => AssertBrandFilter();

    /// <summary>
    /// Выбирает ширину 11.75 и проверяет, что она указана вторым размером
    /// в описании каждого показанного диска.
    /// </summary>
    [Test]
    [Property("TestCaseId", "WHEEL-001")]
    public void WidthFilterKeepsOnlyWheelDiscsOfThatWidth() =>
        AssertDescriptionsMatchAfterFacet(
            "Ширина",
            "11.75",
            description => Regex.IsMatch(
                NormalizeTechnicalText(description),
                @"\d+(?:\.\d+)?X11\.75(?!\d)"),
            "width '11.75'");

    /// <summary>
    /// Выбирает диаметр 17.5, проверяет применение фильтра и изменение выдачи,
    /// а также наличие выбранного диаметра хотя бы в одном наименовании товара.
    /// Диаметр хранится в атрибутах и не всегда дублируется в каждом наименовании.
    /// </summary>
    [Test]
    [Property("TestCaseId", "WHEEL-001")]
    public void DiameterFilterKeepsOnlyWheelDiscsOfThatDiameter()
    {
        AssertNarrowingFacet("Діаметр", "17.5");

        Assert.That(
            Filter.PrimaryProductDescriptions.Any(description =>
                Regex.IsMatch(
                    NormalizeTechnicalText(description),
                    @"(?<!\d)17\.5(?!\d)")),
            Is.True,
            "No primary wheel disc name contains diameter '17.5' after filtering.");
    }

    /// <summary>
    /// Выбирает разболтовку 10х335 и проверяет, что значение PCD присутствует
    /// в описании каждого показанного диска.
    /// </summary>
    [Test]
    [Property("TestCaseId", "WHEEL-001")]
    public void PcdFilterKeepsOnlyWheelDiscsWithSelectedBoltPattern() =>
        AssertDescriptionsMatchAfterFacet(
            "PCD",
            "10х335",
            description => NormalizeTechnicalText(description).Contains("10X335"),
            "PCD '10x335'");

    /// <summary>
    /// Выбирает вылет ET 102 и проверяет, что ET 102 указан в описании каждого
    /// диска после применения фильтра.
    /// </summary>
    [Test]
    [Property("TestCaseId", "WHEEL-001")]
    public void OffsetFilterKeepsOnlyWheelDiscsWithSelectedEt() =>
        AssertDescriptionsMatchAfterFacet(
            "ET",
            "102",
            description => Regex.IsMatch(
                NormalizeTechnicalText(description),
                @"ET102(?!\d)"),
            "offset 'ET 102'");

    /// <summary>
    /// Выбирает центральное отверстие DIA 108.5 и проверяет это значение
    /// в описании каждого диска из отфильтрованной выдачи.
    /// </summary>
    [Test]
    [Property("TestCaseId", "WHEEL-001")]
    public void HubDiameterFilterKeepsOnlyWheelDiscsWithSelectedDia() =>
        AssertDescriptionsMatchAfterFacet(
            "DIA",
            "108.5",
            description => Regex.IsMatch(
                NormalizeTechnicalText(description),
                @"DIA108\.5(?!\d)"),
            "hub diameter 'DIA 108.5'");

    /// <summary>
    /// Выбирает чёрный цвет, проверяет применение фильтра и изменение выдачи,
    /// а также наличие слова «чорний» хотя бы в одном наименовании товара.
    /// Цвет хранится в атрибутах и не всегда дублируется в каждом наименовании.
    /// </summary>
    [Test]
    [Property("TestCaseId", "WHEEL-001")]
    public void ColorFilterKeepsOnlyBlackWheelDiscs()
    {
        AssertNarrowingFacet("Color", "чорний");

        Assert.That(
            Filter.PrimaryProductDescriptions.Any(description =>
                NormalizeTechnicalText(description).Contains("ЧОРНИЙ")),
            Is.True,
            "No primary wheel disc name contains color 'чорний' after filtering.");
    }

    /// <summary>
    /// Включает фильтр наличия и проверяет положительный остаток каждого диска
    /// хотя бы на одном доступном складе.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableWheelDiscs() => AssertInStockFilter();

    /// <summary>
    /// Включает распродажу и проверяет изменение выдачи и наличие признака
    /// распродажи у каждого показанного диска.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void SaleFilterShowsOnlyMarkedDownWheelDiscs() => AssertSaleFilter();

    /// <summary>
    /// Включает акционный товар и проверяет, что фильтр применился и изменил
    /// непустую выдачу колёсных дисков.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void PromotionalFilterChangesResultsAndRemainsApplied() =>
        AssertPromotionalFilter();

    /// <summary>
    /// Применяет фильтр дисков, сбрасывает его и проверяет восстановление
    /// первоначальной выдачи и очистку выбранных параметров.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsWheelDiscSelection() => AssertFilterReset();

    private static string NormalizeTechnicalText(string value) => value
        .Replace(',', '.')
        .Replace('х', 'X')
        .Replace('Х', 'X')
        .Replace('×', 'X')
        .Replace(" ", string.Empty)
        .ToUpperInvariant();
}
