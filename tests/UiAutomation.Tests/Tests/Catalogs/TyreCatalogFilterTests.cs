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

    /// <summary>
    /// Выбирает бренд в фильтре шин, применяет его и проверяет, что в выдаче
    /// остались только шины выбранного бренда.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void AppliedBrandFilterKeepsOnlyMatchingTyres() => AssertBrandFilter();

    /// <summary>
    /// Выбирает диаметр R16, применяет фильтр и проверяет, что выдача изменилась,
    /// не пуста, а в описании каждой показанной шины указан диаметр R16.
    /// </summary>
    [Test]
    [Property("TestCaseId", "TYRE-001")]
    public void DiameterFilterKeepsOnlyTyresOfThatDiameter()
    {
        var unfilteredSignature = Filter.ResultSignature;
        var diameter = Filter.SelectFacetOption("Діаметр", "16");
        Filter.ApplyFilters(diameter.Value);

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

    /// <summary>
    /// Выбирает типоразмер 155/65R14, применяет фильтр и проверяет, что выдача
    /// изменилась, а выбранный типоразмер указан в описании каждой шины.
    /// </summary>
    [Test]
    [Property("TestCaseId", "TYRE-001")]
    public void SizeFilterKeepsOnlyTyresOfThatSize()
    {
        var unfilteredSignature = Filter.ResultSignature;
        var size = Filter.SelectFacetOption("Типорозмір", "155/65R14");
        Filter.ApplyFilters(size.Value);

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

    /// <summary>
    /// Выбирает сезон «Зима» и проверяет, что фильтр отображается среди
    /// применённых, сужает выдачу и оставляет хотя бы одну шину.
    /// </summary>
    [Test]
    [Property("TestCaseId", "TYRE-002")]
    public void SeasonFilterIsAppliedAndReturnsBoundedResults() =>
        AssertNarrowingFacet("Сезонність", "Зима");

    /// <summary>
    /// Выбирает назначение «Легкова» и проверяет, что фильтр применяется,
    /// сужает выдачу и оставляет хотя бы одну подходящую шину.
    /// </summary>
    [Test]
    [Property("TestCaseId", "TYRE-002")]
    public void PurposeFilterIsAppliedAndReturnsBoundedResults() =>
        AssertNarrowingFacet("Призначення", "Легкова");

    /// <summary>
    /// Включает фильтр «Тільки товар у наявності» и проверяет, что каждая
    /// показанная шина имеет положительный остаток хотя бы на одном складе.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-005")]
    public void InStockFilterShowsOnlyAvailableTyres() => AssertInStockFilter();

    /// <summary>
    /// Применяет фильтр шин, затем сбрасывает его и проверяет, что выбранные
    /// параметры очищены, а исходная выдача восстановлена.
    /// </summary>
    [Test]
    [Property("TestCaseId", "CAT-COM-008")]
    public void ResettingFiltersClearsTyreSelection() => AssertFilterReset();
}
