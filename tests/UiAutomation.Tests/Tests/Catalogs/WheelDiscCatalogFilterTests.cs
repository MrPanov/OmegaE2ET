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
                @"\d+(?:\.\d+)?\s*X\s*11\.75(?!\d)"),
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
    /// Выбирает разболтовку 10х335, проверяет применение фильтра и изменение
    /// выдачи, а также наличие выбранного PCD хотя бы в одном наименовании.
    /// Разные варианты символа умножения и пробелы приводятся к одному формату.
    /// </summary>
    [Test]
    [Property("TestCaseId", "WHEEL-001")]
    public void PcdFilterKeepsOnlyWheelDiscsWithSelectedBoltPattern()
    {
        AssertNarrowingFacet("PCD", "10х335");
        var displayTexts = Filter.ProductDisplayTexts;

        Assert.That(
            displayTexts.Any(description =>
                Regex.IsMatch(
                    NormalizeTechnicalText(description),
                    @"(?<!\d)10(?:X|[^\p{L}\p{N}])*335(?!\d)")),
            Is.True,
            "No visible wheel disc name contains PCD '10x335' after filtering. " +
            "Read product rows: " + string.Join(" | ", displayTexts.Take(10)));
    }

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
                @"ET\s*102(?!\d)"),
            "offset 'ET 102'");

    /// <summary>
    /// Выбирает центральное отверстие DIA 108.5, проверяет применение фильтра
    /// и изменение выдачи, а также наличие значения 108.5 хотя бы в одном
    /// наименовании. Префикс DIA и конкретный символ-разделитель необязательны.
    /// </summary>
    [Test]
    [Property("TestCaseId", "WHEEL-001")]
    public void HubDiameterFilterKeepsOnlyWheelDiscsWithSelectedDia()
    {
        AssertNarrowingFacet("DIA", "108.5");

        Assert.That(
            Filter.PrimaryProductDescriptions.Any(description =>
                Regex.IsMatch(
                    NormalizeTechnicalText(description),
                    @"(?<!\d)108[^\p{L}\p{N}]*5(?!\d)")),
            Is.True,
            "No primary wheel disc name contains hub diameter '108.5' " +
            "after filtering.");
    }

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
    /// Применяет фильтр дисков, сбрасывает его и проверяет восстановление
    /// первоначальной выдачи и очистку выбранных параметров.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsWheelDiscSelection() => AssertFilterReset();

    private static string NormalizeTechnicalText(string value)
    {
        var normalized = value
            .Replace(',', '.')
            .Replace('х', 'X')
            .Replace('Х', 'X')
            .Replace('×', 'X')
            .ToUpperInvariant();

        return Regex.Replace(normalized, @"\s+", " ").Trim();
    }
}
