using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests.Basket;

[TestFixture]
[NonParallelizable]
[Category("Basket")]
[Category("Smoke")]
[Category("P0")]
[Category(TestCategories.ProductionTestClient)]
[Category(TestCategories.MutatesUserState)]
public sealed class BasketSmokeTests : BasketMutatingTestBase
{
    /// <summary>
    /// Ручной сценарий: ввести номер карточки товара в поле добавления корзины,
    /// подтвердить ввод и повторить добавление той же карточки.
    /// Ожидаемый результат: товар присутствует в корзине только в одной строке.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-002")]
    public void ProductCanBeAddedByCardWithoutDuplicate()
    {
        var card = BasketTestCards.AddProduct;
        AddTrackedProduct(card);

        Assert.That(Basket.ProductCards.Count(item => item == card), Is.EqualTo(1));
    }

    /// <summary>
    /// Ручной сценарий: добавить эталонный товар по номеру карточки и проверить
    /// отображаемые в его строке карточку, код, название, цену, количество и остатки.
    /// Ожидаемый результат: обязательные данные заполнены и относятся к добавленному товару.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-003")]
    public void ProductRowShowsRequiredData()
    {
        var card = BasketTestCards.ProductDetails;
        AddTrackedProduct(card);
        var product = Basket.ProductDetails(card);
        var stocks = Basket.WarehouseStocks(card);

        Assert.Multiple(() =>
        {
            Assert.That(product.Card, Is.EqualTo(card));
            Assert.That(product.Code, Is.Not.Empty);
            Assert.That(product.Text, Does.Contain("Фільтр оливний"));
            Assert.That(product.Text, Does.Contain("KNECHT").IgnoreCase);
            Assert.That(product.Price, Is.GreaterThan(0));
            Assert.That(product.Quantity, Is.EqualTo(1));
            Assert.That(stocks.Values, Is.Not.Empty);
        });
    }

    /// <summary>
    /// Ручной сценарий: добавить товар и сопоставить заголовки складов со значениями
    /// остатков в строке товара слева направо.
    /// Ожидаемый результат: каждому складу соответствует значение остатка и хотя бы
    /// на одном складе товар доступен.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-004")]
    public void WarehouseHeadersMatchStockValuesByIndex()
    {
        var card = BasketTestCards.WarehouseStocks;
        AddTrackedProduct(card);
        var stocks = Basket.WarehouseStocks(card);

        Assert.Multiple(() =>
        {
            Assert.That(stocks.Headers, Has.Count.EqualTo(stocks.Values.Count));
            Assert.That(stocks.Headers, Has.All.Not.Empty);
            Assert.That(stocks.Values, Has.All.Not.Empty);
            Assert.That(stocks.Values.Any(value => value != "0"), Is.True);
        });
    }

    /// <summary>
    /// Ручной сценарий: добавить и выбрать товар, увеличить и уменьшить количество,
    /// затем попытаться установить нулевое количество.
    /// Ожидаемый результат: итоговая сумма пересчитывается, а количество меньше единицы
    /// не принимается.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-005")]
    public void QuantityControlsRecalculateSelectedTotalAndRejectInvalidValue()
    {
        var card = BasketTestCards.Quantity;
        AddTrackedProduct(card);
        Basket.SelectOnlyProduct(card);
        var initialTotal = Basket.SelectedTotal;

        Basket.IncreaseQuantity(card);
        Assert.That(Basket.SelectedTotal, Is.GreaterThan(initialTotal));

        Basket.DecreaseQuantity(card);
        Assert.That(Basket.SelectedTotal, Is.EqualTo(initialTotal));

        Basket.SetQuantity(card, "0");
        Assert.That(Basket.ProductQuantity(card), Is.GreaterThanOrEqualTo(1));
    }

    /// <summary>
    /// Ручной сценарий: добавить товар, оставить выбранным только его и сравнить
    /// общую сумму выбранных позиций с ценой, умноженной на количество.
    /// Ожидаемый результат: итог учитывает только выбранный товар.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-006")]
    public void SelectedTotalIncludesOnlyCheckedProduct()
    {
        var card = BasketTestCards.Selection;
        AddTrackedProduct(card);
        var product = Basket.ProductDetails(card);

        Basket.SelectOnlyProduct(card);

        Assert.That(Basket.SelectedTotal, Is.EqualTo(product.Price * product.Quantity));
    }

    /// <summary>
    /// Ручной сценарий: добавить товар, снять флажок «Вибрати всі», затем установить его снова.
    /// Ожидаемый результат: складские позиции снимаются и выбираются массовым флажком;
    /// позиции «під замовлення», которыми он не управляет, не влияют на результат.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-007")]
    public void SelectAllCheckboxControlsEligibleBasketRows()
    {
        AddTrackedProduct(BasketTestCards.SelectAll);

        Basket.SetSelectAll(false);
        Assert.That(Basket.SelectionStates, Has.All.False);

        Basket.SetSelectAll(true);
        Assert.That(Basket.SelectionStates, Has.Some.True);
    }

    /// <summary>
    /// Ручной сценарий: запомнить существующие позиции, добавить эталонный товар и удалить его.
    /// Ожидаемый результат: удаляется только эталонный товар, остальные строки не изменяются.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-008")]
    public void RemovingProductDoesNotRemoveOtherRows()
    {
        var card = BasketTestCards.Removal;
        var otherCards = Basket.ProductCards.ToArray();
        AddTrackedProduct(card);

        Basket.RemoveProduct(card);

        Assert.Multiple(() =>
        {
            Assert.That(Basket.HasProduct(card), Is.False);
            Assert.That(Basket.ProductCards, Is.EqualTo(otherCards));
        });
    }
}
