using System.Text.RegularExpressions;
using NUnit.Framework;

namespace UiAutomation.Tests.Tests.Catalogs;

/// <summary>
/// Filter applicability for the tyres catalog. Diameter and size can be checked
/// directly against product descriptions; the other common checks reuse the
/// catalog filter assertions from <see cref="CatalogFilterTestBase"/>.
/// </summary>
[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
[Category("CatalogFilters")]
[Category("Tyres")]
public sealed class TyreCatalogFilterTests : CatalogFilterTestBase
{
    protected override CatalogDefinition Catalog => CatalogDefinitions.Tyres;

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingTyres() => AssertBrandFilter();

    [Test]
    [Property("TestCaseId", "TYRE-001")]
    public void DiameterFilterKeepsOnlyTyresOfThatDiameter()
    {
        var unfilteredSignature = Filter.ResultSignature;
        var diameter = Filter.SelectFacetOption("Діаметр", "16");
        Filter.ApplyFilters();

        var descriptions = Filter.ProductDescriptions;
        var pattern = new Regex($@"R{Regex.Escape(diameter.Value)}(?!\d)");

        Assert.Multiple(() =>
        {
            Assert.That(Filter.ResultCount, Is.GreaterThan(0),
                $"No tyres left after filtering by diameter R{diameter.Value}.");
            Assert.That(Filter.ResultSignature, Is.Not.EqualTo(unfilteredSignature),
                $"Diameter R{diameter.Value} did not change the tyre results.");
            Assert.That(descriptions.All(description => pattern.IsMatch(description)), Is.True,
                $"Some tyres are not diameter R{diameter.Value}: " +
                string.Join(" | ", descriptions.Where(description => !pattern.IsMatch(description))));
        });
    }

    [Test]
    [Property("TestCaseId", "TYRE-001")]
    public void SizeFilterKeepsOnlyTyresOfThatSize()
    {
        var unfilteredSignature = Filter.ResultSignature;
        var size = Filter.SelectFacetOption("Типорозмір", "155/65R14");
        Filter.ApplyFilters();

        var descriptions = Filter.ProductDescriptions;

        Assert.Multiple(() =>
        {
            Assert.That(Filter.ResultCount, Is.GreaterThan(0),
                $"No tyres left after filtering by size '{size.Value}'.");
            Assert.That(Filter.ResultSignature, Is.Not.EqualTo(unfilteredSignature),
                $"Size '{size.Value}' did not change the tyre results.");
            Assert.That(
                descriptions.All(description =>
                    description.Contains(size.Value, StringComparison.OrdinalIgnoreCase)),
                Is.True,
                $"Some tyres are not size '{size.Value}': " +
                string.Join(
                    " | ",
                    descriptions.Where(description =>
                        !description.Contains(size.Value, StringComparison.OrdinalIgnoreCase))));
        });
    }

    [Test]
    [Property("TestCaseId", "TYRE-002")]
    public void SeasonFilterIsAppliedAndReturnsBoundedResults() =>
        AssertNarrowingFacet("Сезонність", "Зима");

    [Test]
    [Property("TestCaseId", "TYRE-002")]
    public void PurposeFilterIsAppliedAndReturnsBoundedResults() =>
        AssertNarrowingFacet("Призначення", "Легкова");

    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableTyres() => AssertInStockFilter();

    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsTyreSelection() => AssertFilterReset();
}
