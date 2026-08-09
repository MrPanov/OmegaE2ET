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
public sealed class BasketRemoveProductTests : AuthenticatedUiTestFixture
{
    private const string Card = "4610495";

    private BasketPage _basket = null!;

    protected override void OnAuthenticated() => _basket = new BasketPage(Driver, Timeout);

    [SetUp]
    public void OpenBasket() => _basket.Open(Settings.BaseUrl);

    /// <summary>
    /// Ручной сценарий: запомнить состав корзины, добавить товар и удалить его
    /// крестиком в строке.
    /// Ожидаемый результат: строка исчезает без подтверждения, остальные позиции
    /// остаются нетронутыми.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-011")]
    public void RemovingAddedProductKeepsOtherRowsIntact()
    {
        // Корзина общая: снимаем возможный след прошлого прогона до замера состава.
        _basket.RemoveProduct(Card);
        var otherCards = _basket.ProductCards.ToArray();

        _basket.AddProduct(Card);
        _basket.RemoveProduct(Card);

        Assert.Multiple(() =>
        {
            Assert.That(_basket.HasProduct(Card), Is.False);
            Assert.That(_basket.ProductCards, Is.EqualTo(otherCards));
        });
    }
}
