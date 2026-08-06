using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests;

[TestFixture]
[NonParallelizable]
[Category("Basket")]
public sealed class BasketTests : AuthenticatedUiTestFixture
{
    private const string ProductCard = "5614799817";
    private BasketPage _basket = null!;

    protected override void OnAuthenticated()
    {
        _basket = new BasketPage(Driver, Timeout);
        _basket.Open(Settings.BaseUrl);
    }

    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "BASKET-001")]
    public void AvailableProductCanBeReservedWithoutOtherBasketItems()
    {
        _basket.AddProduct(ProductCard);
        var warehouse = _basket.PositiveStockWarehouse(ProductCard);

        _basket.SelectOnlyProduct(ProductCard);
        var selectedWarehouse = _basket.ReserveFromWarehouse(warehouse);

        Assert.Multiple(() =>
        {
            Assert.That(warehouse.Stock, Is.Not.EqualTo("0"));
            Assert.That(_basket.ActiveInvoiceProductCards(), Is.EqualTo(new[] { ProductCard }));
            Assert.That(_basket.ActiveInvoiceContainsWarehouse(selectedWarehouse), Is.True);
        });
    }
}
