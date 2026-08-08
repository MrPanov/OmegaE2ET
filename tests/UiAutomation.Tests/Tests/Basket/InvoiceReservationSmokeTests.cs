using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Tests.Basket;

[TestFixture]
[NonParallelizable]
[Category("Invoice")]
[Category("Smoke")]
[Category("P0")]
[Category(TestCategories.ProductionTestClient)]
[Category(TestCategories.MutatesUserState)]
public sealed class InvoiceReservationSmokeTests : BasketMutatingTestBase
{
    /// <summary>
    /// Добавляет отдельный эталонный товар, выбирает склад с положительным остатком
    /// и создаёт резервный счёт. Проверяет, что в счёт попал только этот товар и
    /// склад в деталях соответствует выбранному варианту.
    /// </summary>
    [Test]
    [Property("TestCaseId", "INVOICE-001")]
    public void AvailableProductCreatesSingleItemReservedInvoice()
    {
        var card = BasketTestCards.InvoiceReservation;
        AddTrackedProduct(card);
        var warehouse = Basket.PositiveStockWarehouse(card);

        Basket.SelectOnlyProduct(card);
        var selectedWarehouse = Basket.ReserveFromWarehouse(warehouse);

        Assert.Multiple(() =>
        {
            Assert.That(warehouse.Stock, Is.Not.EqualTo("0"));
            Assert.That(Basket.ActiveInvoiceProductCards(), Is.EqualTo(new[] { card }));
            Assert.That(Basket.ActiveInvoiceContainsWarehouse(selectedWarehouse), Is.True);
        });
    }
}
