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

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingOils() => AssertBrandFilter();

    [Test]
    [Property("TestCaseId", "OIL-002")]
    public void VehicleFilterNarrowsOilResults() => AssertNarrowingFacet("Авто", "ACURA");

    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void PurposeFilterNarrowsOilResults() =>
        AssertNarrowingFacet("Призначення", "Автопромивна олія");

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

    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void ApplicationFilterNarrowsOilResults() =>
        AssertNarrowingFacet("Застосування", "Промивні");

    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void OilTypeFilterNarrowsOilResults() =>
        AssertNarrowingFacet("Тип", "Мінеральна олива");

    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void ApiSpecificationFilterNarrowsOilResults() =>
        AssertNarrowingFacet("Специфікації API", "CF-4");

    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void AceaSpecificationFilterNarrowsOilResults() =>
        AssertNarrowingFacet("Специфікації ACEA", "C1");

    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void IlsacSpecificationFilterNarrowsOilResults() =>
        AssertNarrowingFacet("Специфікації LSAC", "GF-3");

    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void JasoSpecificationFilterNarrowsOilResults() =>
        AssertNarrowingFacet("Специфікації JASO", "DH-1-17");

    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void OemSpecificationFilterNarrowsOilResults() =>
        AssertNarrowingFacet("Специфікації OEM", "Allison TES295");

    [Test]
    [Property("TestCaseId", "OIL-001")]
    public void GostSpecificationFilterNarrowsOilResults() =>
        AssertNarrowingFacet("ГОСТ", "Afnor 48600");

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableOils() => AssertInStockFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void SaleFilterShowsOnlyMarkedDownOils()
    {
        var unfilteredSignature = Filter.ResultSignature;

        Filter.EnableSaleOnly();
        Filter.ApplyFilters();

        var withoutSaleMarker = Filter.ProductsWithoutSaleMarker();

        Assert.Multiple(() =>
        {
            Assert.That(Filter.IsSaleOnlyEnabled, Is.True,
                "The sale checkbox is not selected.");
            Assert.That(Filter.HasAppliedFilter("Розпродаж"), Is.True,
                "The sale filter is absent from the applied filters.");
            Assert.That(Filter.ResultSignature, Is.Not.EqualTo(unfilteredSignature),
                "The sale filter did not change the oil results.");
            Assert.That(Filter.ResultCount, Is.GreaterThan(0),
                "No oils remained after applying the sale filter.");
            Assert.That(withoutSaleMarker, Is.Empty,
                "Products without a sale marker were shown: " +
                string.Join(" | ", withoutSaleMarker));
        });
    }

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void PromotionalFilterChangesResultsAndRemainsApplied()
    {
        var unfilteredSignature = Filter.ResultSignature;

        Filter.EnablePromotionalOnly();
        Filter.ApplyFilters();

        Assert.Multiple(() =>
        {
            Assert.That(Filter.IsPromotionalOnlyEnabled, Is.True,
                "The promotional-products checkbox is not selected.");
            Assert.That(Filter.HasAppliedFilter("Акційний товар"), Is.True,
                "The promotional-products filter is absent from the applied filters.");
            Assert.That(Filter.ResultSignature, Is.Not.EqualTo(unfilteredSignature),
                "The promotional-products filter did not change the oil results.");
            Assert.That(Filter.ResultCount, Is.GreaterThan(0),
                "No oils remained after applying the promotional-products filter.");
        });
    }

    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsOilSelection() => AssertFilterReset();

    private static string NormalizeDecimal(string value) => value.Replace(',', '.');
}
