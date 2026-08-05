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

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingLamps() => AssertBrandFilter();

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

    [Test]
    [Property("TestCaseId", "LAMP-001")]
    public void VehicleFilterNarrowsLampResults() =>
        AssertNarrowingFacet("Авто", "ALFA ROMEO");

    [Test]
    [Property("TestCaseId", "LAMP-001")]
    public void PowerFilterKeepsOnlyLampsWithSelectedPower() =>
        AssertDescriptionsContainAfterFacet("Потужність", "0.5", "0.5W");

    [Test]
    [Property("TestCaseId", "LAMP-001")]
    public void SocketFilterKeepsOnlyLampsWithSelectedSocket() =>
        AssertDescriptionsContainAfterFacet("Цоколь", "B10d", "B10D");

    [Test]
    [Property("TestCaseId", "LAMP-001")]
    public void VoltageFilterKeepsOnlyLampsWithSelectedVoltage() =>
        AssertDescriptionsContainAfterFacet("Напруга", "24", "24V", "24В");

    [Test]
    [Property("TestCaseId", "LAMP-001")]
    public void XenonFilterKeepsOnlyLampsWithSelectedSocketType() =>
        AssertDescriptionsContainAfterFacet("Ксенон", "D1R", "D1R");

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableLamps() => AssertInStockFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void SaleFilterShowsOnlyMarkedDownLamps() => AssertSaleFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void PromotionalFilterChangesResultsAndRemainsApplied() =>
        AssertPromotionalFilter();

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
