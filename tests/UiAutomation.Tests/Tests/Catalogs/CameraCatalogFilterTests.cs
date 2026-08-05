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

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingCameras() => AssertBrandFilter();

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

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableCameras() => AssertInStockFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void SaleFilterShowsOnlyMarkedDownCameras() => AssertSaleFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void PromotionalFilterChangesResultsAndRemainsApplied() =>
        AssertPromotionalFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsCameraSelection() => AssertFilterReset();
}
