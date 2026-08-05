using System.Text.RegularExpressions;
using NUnit.Framework;

namespace UiAutomation.Tests.Tests.Catalogs;

/// <summary>
/// Проверки всех доступных фильтров каталога аккумуляторов. Напряжение,
/// технология, ёмкость, пусковой ток и габариты дополнительно сверяются
/// с описанием каждого показанного аккумулятора.
/// </summary>
[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
[Category("Batteries")]
public sealed class BatteryCatalogFilterTests : CatalogFilterTestBase
{
    protected override CatalogDefinition Catalog => CatalogDefinitions.Batteries;

    /// <summary>
    /// Выбирает бренд аккумулятора и проверяет, что в выдаче остались только
    /// товары выбранного бренда.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingBatteries() => AssertBrandFilter();

    /// <summary>
    /// Выбирает напряжение 12 В и проверяет, что значение 12 V указано
    /// в описании каждого аккумулятора после фильтрации.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BATTERY-001")]
    public void VoltageFilterKeepsOnlyTwelveVoltBatteries() =>
        AssertBatteryDescriptions(
            "Напруга",
            "12 В",
            @"(?<!\d)12V(?!\d)",
            "voltage '12 V'");

    /// <summary>
    /// Выбирает технологию EFB и проверяет, что обозначение EFB присутствует
    /// в описании каждого аккумулятора в выдаче.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BATTERY-001")]
    public void TypeFilterKeepsOnlyEfbBatteries() =>
        AssertBatteryDescriptions("Тип", "EFB", @"EFB", "type 'EFB'");

    /// <summary>
    /// Выбирает ёмкость 100 А/ч и проверяет, что значение 100 Ah указано
    /// в описании каждого показанного аккумулятора.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BATTERY-001")]
    public void CapacityFilterKeepsOnlyHundredAmpHourBatteries() =>
        AssertBatteryDescriptions(
            "Ємність",
            "100 А/ч",
            @"(?<!\d)100AH(?!\d)",
            "capacity '100 Ah'");

    /// <summary>
    /// Выбирает пусковой ток 1000 А и проверяет обозначение EN1000
    /// в описании каждого аккумулятора после применения фильтра.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BATTERY-001")]
    public void StartingCurrentFilterKeepsOnlyThousandAmpBatteries() =>
        AssertBatteryDescriptions(
            "Пусковий струм",
            "1000 А",
            @"EN1000(?!\d)",
            "starting current '1000 A'");

    /// <summary>
    /// Выбирает длину 113 мм и проверяет первое значение в размерной группе
    /// каждого аккумулятора.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BATTERY-001")]
    public void LengthFilterKeepsOnlyBatteriesOfSelectedLength() =>
        AssertBatteryDescriptions(
            "Довжина",
            "113 мм",
            @"\(113X",
            "length '113 mm'");

    /// <summary>
    /// Выбирает ширину 100 мм и проверяет второе значение в размерной группе
    /// каждого аккумулятора.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BATTERY-001")]
    public void WidthFilterKeepsOnlyBatteriesOfSelectedWidth() =>
        AssertBatteryDescriptions(
            "Ширина",
            "100 мм",
            @"\(\d+(?:\.\d+)?X100X",
            "width '100 mm'");

    /// <summary>
    /// Выбирает высоту 104 мм и проверяет третье значение в размерной группе
    /// каждого аккумулятора.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BATTERY-001")]
    public void HeightFilterKeepsOnlyBatteriesOfSelectedHeight() =>
        AssertBatteryDescriptions(
            "Висота",
            "104 мм",
            @"\(\d+(?:\.\d+)?X\d+(?:\.\d+)?X104\)",
            "height '104 mm'");

    /// <summary>
    /// Выбирает правую обратную полярность и проверяет, что фильтр применяется,
    /// изменяет выдачу и не превышает заявленный счётчик фасета.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BATTERY-001")]
    public void PolarityFilterNarrowsBatteryResults() =>
        AssertNarrowingFacet(
            "Розташування полюсних виводів",
            "R+правий (зворотний (0)");

    /// <summary>
    /// Выбирает стандартные клеммы и проверяет, что фильтр отображается среди
    /// применённых и сужает непустую выдачу аккумуляторов.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BATTERY-001")]
    public void TerminalFilterNarrowsBatteryResults() =>
        AssertNarrowingFacet("Клеми", "1 - стандартні клеми");

    /// <summary>
    /// Выбирает европейский тип корпуса и проверяет применение фильтра,
    /// изменение выдачи и соответствие количества счётчику фасета.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BATTERY-001")]
    public void CaseTypeFilterNarrowsBatteryResults() =>
        AssertNarrowingFacet("Тип корпусу", "Євро");

    /// <summary>
    /// Выбирает крепление B13 и проверяет, что фильтр применяется и сужает
    /// непустую выдачу аккумуляторов.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BATTERY-001")]
    public void MountingTypeFilterNarrowsBatteryResults() =>
        AssertNarrowingFacet("Тип кріплення", "B13");

    /// <summary>
    /// Включает фильтр наличия и проверяет положительный остаток каждого
    /// аккумулятора хотя бы на одном доступном складе.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableBatteries() => AssertInStockFilter();

    /// <summary>
    /// Включает распродажу и проверяет изменение выдачи и наличие признака
    /// распродажи у каждого показанного аккумулятора.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void SaleFilterShowsOnlyMarkedDownBatteries() => AssertSaleFilter();

    /// <summary>
    /// Включает акционный товар и проверяет, что фильтр применился и изменил
    /// непустую выдачу аккумуляторов.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void PromotionalFilterChangesResultsAndRemainsApplied() =>
        AssertPromotionalFilter();

    /// <summary>
    /// Применяет фильтр аккумуляторов, сбрасывает его и проверяет очистку
    /// параметров и восстановление первоначальной выдачи.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsBatterySelection() => AssertFilterReset();

    private void AssertBatteryDescriptions(
        string facetTitle,
        string optionValue,
        string pattern,
        string expectedDescription) =>
        AssertDescriptionsMatchAfterFacet(
            facetTitle,
            optionValue,
            description => Regex.IsMatch(NormalizeBatteryText(description), pattern),
            expectedDescription);

    private static string NormalizeBatteryText(string value) => value
        .Replace(',', '.')
        .Replace('х', 'X')
        .Replace('Х', 'X')
        .Replace('×', 'X')
        .Replace(" ", string.Empty)
        .ToUpperInvariant();
}
