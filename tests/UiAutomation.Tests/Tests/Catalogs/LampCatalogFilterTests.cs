using NUnit.Framework;

namespace UiAutomation.Tests.Tests.Catalogs;

/// <summary>
/// Covers every available control in the lamp catalog's "Основні фільтри"
/// block except the dependent model facet. Power, socket, voltage and xenon are
/// also verified against every visible product description.
/// </summary>
[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
[Category("Lamps")]
public sealed class LampCatalogFilterTests : CatalogFilterTestBase
{
    protected override CatalogDefinition Catalog => CatalogDefinitions.Lamps;

    /// <summary>
    /// Выбирает бренд в фильтре ламп, применяет его и проверяет, что в выдаче
    /// остались только товары выбранного бренда.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingLamps() => AssertBrandFilter();

    /// <summary>
    /// Выбирает тип транспорта «Легкова», применяет фильтр и проверяет, что он
    /// отображается среди применённых фильтров и изменяет непустую выдачу ламп.
    /// </summary>
    [Test]
    [Property("TestCaseId", "LAMP-001")]
    public void PassengerVehicleFormFactorChangesLampResults()
    {
        var unfilteredSignature = Filter.ResultSignature;

        Filter.SelectPassengerFormFactor();
        Filter.ApplyFilters();

        Assert.Multiple(() =>
        {
            Assert.That(Filter.IsPassengerFormFactorSelected, Is.True,
                "The passenger-vehicle form factor is not selected.");
            Assert.That(Filter.HasAppliedFilter("Легкова"), Is.True,
                "The passenger-vehicle form factor is absent from the applied filters.");
            Assert.That(Filter.ResultSignature, Is.Not.EqualTo(unfilteredSignature),
                "The passenger-vehicle form factor did not change the lamp results.");
            Assert.That(Filter.ResultCount, Is.GreaterThan(0),
                "No lamps remained after selecting the passenger-vehicle form factor.");
        });
    }

    /// <summary>
    /// Выбирает автомобиль ALFA ROMEO и проверяет, что фильтр применяется,
    /// сужает выдачу и оставляет хотя бы одну подходящую лампу.
    /// </summary>
    [Test]
    [Property("TestCaseId", "LAMP-001")]
    public void VehicleFilterNarrowsLampResults() =>
        AssertNarrowingFacet("Авто", "ALFA ROMEO");

    /// <summary>
    /// Выбирает мощность 0.5 W и проверяет, что мощность указана в описании
    /// каждой лампы, оставшейся после применения фильтра.
    /// </summary>
    [Test]
    [Property("TestCaseId", "LAMP-001")]
    public void PowerFilterKeepsOnlyLampsWithSelectedPower() =>
        AssertDescriptionsContainAfterFacet("Потужність", "0.5", "0.5W");

    /// <summary>
    /// Выбирает цоколь B10d и проверяет, что этот цоколь указан в описании
    /// каждой лампы в отфильтрованной выдаче.
    /// </summary>
    [Test]
    [Property("TestCaseId", "LAMP-001")]
    public void SocketFilterKeepsOnlyLampsWithSelectedSocket() =>
        AssertDescriptionsContainAfterFacet("Цоколь", "B10d", "B10D");

    /// <summary>
    /// Выбирает напряжение 24 V и проверяет, что значение 24 V или 24 В
    /// присутствует в описании каждой показанной лампы.
    /// </summary>
    [Test]
    [Property("TestCaseId", "LAMP-001")]
    public void VoltageFilterKeepsOnlyLampsWithSelectedVoltage() =>
        AssertDescriptionsContainAfterFacet("Напруга", "24", "24V", "24В");

    /// <summary>
    /// Выбирает ксеноновый тип D1R и проверяет, что D1R указан в описании
    /// каждой лампы, оставшейся в результатах.
    /// </summary>
    [Test]
    [Property("TestCaseId", "LAMP-001")]
    public void XenonFilterKeepsOnlyLampsWithSelectedSocketType() =>
        AssertDescriptionsContainAfterFacet("Ксенон", "D1R", "D1R");

    /// <summary>
    /// Включает фильтр «Тільки товар у наявності» и проверяет, что каждый
    /// показанный товар имеет положительный остаток хотя бы на одном складе.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableLamps() => AssertInStockFilter();

    /// <summary>
    /// Включает фильтр распродажи и проверяет, что выдача изменилась, не пуста,
    /// а у каждой показанной лампы присутствует признак распродажи.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void SaleFilterShowsOnlyMarkedDownLamps() => AssertSaleFilter();

    /// <summary>
    /// Включает фильтр акционных товаров и проверяет, что он остаётся выбранным,
    /// отображается среди применённых и изменяет непустую выдачу ламп.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void PromotionalFilterChangesResultsAndRemainsApplied() =>
        AssertPromotionalFilter();

    /// <summary>
    /// Применяет фильтр ламп, затем сбрасывает его и проверяет, что выбранные
    /// параметры очищены, а исходная выдача восстановлена.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsLampSelection() => AssertFilterReset();

    private void AssertDescriptionsContainAfterFacet(
        string facetTitle,
        string optionValue,
        params string[] expectedTokens)
    {
        AssertNarrowingFacet(facetTitle, optionValue);

        var normalizedTokens = expectedTokens.Select(NormalizeTechnicalText).ToArray();
        var mismatches = Filter.ProductDescriptions
            .Where(description =>
            {
                var normalized = NormalizeTechnicalText(description);
                return normalizedTokens.All(token => !normalized.Contains(token));
            })
            .ToArray();

        Assert.That(mismatches, Is.Empty,
            $"Lamps without '{optionValue}' for facet '{facetTitle}' were shown: " +
            string.Join(" | ", mismatches));
    }

    private static string NormalizeTechnicalText(string value) => value
        .Replace(',', '.')
        .Replace(" ", string.Empty)
        .ToUpperInvariant();
}
