using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests.Basket;

[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Basket")]
[Category("Smoke")]
[Category("P0")]
public sealed class BasketSmokeTests : AuthenticatedUiTestFixture
{
    private const string ProductCard = "5614799817";
    private BasketPage _basket = null!;
    private IReadOnlyList<bool> _originalSelectionStates = [];

    protected override void OnAuthenticated() =>
        _basket = new BasketPage(Driver, Timeout);

    [SetUp]
    public void OpenCleanBasket()
    {
        _basket.Open(Settings.BaseUrl);
        _basket.RemoveProduct(ProductCard);
        _originalSelectionStates = _basket.SelectionStates;
    }

    [TearDown]
    public void RemoveReferenceProduct()
    {
        try
        {
            _basket.Open(Settings.BaseUrl);
            _basket.RemoveProduct(ProductCard);
            _basket.RestoreSelectionStates(_originalSelectionStates);
        }
        catch when (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            TestContext.Progress.WriteLine($"Cleanup could not remove basket product {ProductCard}.");
        }
    }

    [Test]
    [Property("TestCaseId", "BASKET-001")]
    public void BasketOpensAndShowsInvoiceJournal()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_basket.IsLoaded, Is.True);
            Assert.That(_basket.IsInvoiceJournalVisible, Is.True);
        });
    }

    [Test]
    [Property("TestCaseId", "BASKET-002")]
    public void ProductCanBeAddedByCardWithoutDuplicate()
    {
        _basket.AddProduct(ProductCard);

        Assert.That(_basket.ProductCards.Count(card => card == ProductCard), Is.EqualTo(1));
    }

    [Test]
    [Property("TestCaseId", "BASKET-003")]
    public void ProductRowShowsRequiredData()
    {
        _basket.AddProduct(ProductCard);
        var product = _basket.ProductDetails(ProductCard);
        var stocks = _basket.WarehouseStocks(ProductCard);

        Assert.Multiple(() =>
        {
            Assert.That(product.Card, Is.EqualTo(ProductCard));
            Assert.That(product.Code, Is.Not.Empty);
            Assert.That(product.Text, Does.Contain("Фільтр оливний"));
            Assert.That(product.Text, Does.Contain("Дорож"));
            Assert.That(product.Price, Is.GreaterThan(0));
            Assert.That(product.Quantity, Is.EqualTo(1));
            Assert.That(stocks.Values, Is.Not.Empty);
        });
    }

    [Test]
    [Property("TestCaseId", "BASKET-004")]
    public void WarehouseHeadersMatchStockValuesByIndex()
    {
        _basket.AddProduct(ProductCard);
        var stocks = _basket.WarehouseStocks(ProductCard);

        Assert.Multiple(() =>
        {
            Assert.That(stocks.Headers, Has.Count.EqualTo(stocks.Values.Count));
            Assert.That(stocks.Headers, Has.All.Not.Empty);
            Assert.That(stocks.Values, Has.All.Not.Empty);
            Assert.That(stocks.Values.Any(value => value != "0"), Is.True);
        });
    }

    [Test]
    [Property("TestCaseId", "BASKET-005")]
    public void QuantityControlsRecalculateSelectedTotalAndRejectInvalidValue()
    {
        _basket.AddProduct(ProductCard);
        _basket.SelectOnlyProduct(ProductCard);
        var initialTotal = _basket.SelectedTotal;

        _basket.IncreaseQuantity(ProductCard);
        Assert.That(_basket.SelectedTotal, Is.GreaterThan(initialTotal));

        _basket.DecreaseQuantity(ProductCard);
        Assert.That(_basket.SelectedTotal, Is.EqualTo(initialTotal));

        _basket.SetQuantity(ProductCard, "0");
        Assert.That(_basket.ProductQuantity(ProductCard), Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    [Property("TestCaseId", "BASKET-006")]
    public void SelectedTotalIncludesOnlyCheckedProduct()
    {
        _basket.AddProduct(ProductCard);
        var product = _basket.ProductDetails(ProductCard);

        _basket.SelectOnlyProduct(ProductCard);

        Assert.That(_basket.SelectedTotal, Is.EqualTo(product.Price * product.Quantity));
    }

    [Test]
    [Property("TestCaseId", "BASKET-007")]
    public void SelectAllCheckboxControlsEveryBasketRow()
    {
        _basket.AddProduct(ProductCard);

        _basket.SetSelectAll(false);
        Assert.That(_basket.SelectionStates, Has.All.False);

        _basket.SetSelectAll(true);
        Assert.That(_basket.SelectionStates, Has.All.True);
    }

    [Test]
    [Property("TestCaseId", "BASKET-008")]
    public void RemovingProductDoesNotRemoveOtherRows()
    {
        var otherCards = _basket.ProductCards.ToArray();
        _basket.AddProduct(ProductCard);

        _basket.RemoveProduct(ProductCard);

        Assert.Multiple(() =>
        {
            Assert.That(_basket.HasProduct(ProductCard), Is.False);
            Assert.That(_basket.ProductCards, Is.EqualTo(otherCards));
        });
    }
}
