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
public sealed class BasketAddProductTests : BasketTestBase
{
    /// <summary>Карточка и каталожный код одного и того же товара.</summary>
    private const string Card = "4610495";
    private const string CatalogCode = "OC90";

    private string? _addedCard;

    protected override void OnBasketReady()
    {
        _addedCard = null;
        base.OnBasketReady();
    }

    /// <summary>
    /// Ручной сценарий: авторизоваться, открыть корзину, ввести номер карточки товара
    /// в поле добавления и подтвердить ввод.
    /// Ожидаемый результат: товар появляется в корзине ровно одной строкой.
    /// </summary>
    [TestCase("5614799817")]
    [TestCase("651002")]
    [TestCase("4610495")]
    [Property("TestCaseId", "BASKET-002")]
    public void ProductAddedByCardAppearsInBasket(string cardNumber)
    {
        // Корзина живая: убираем возможные следы предыдущего прогона до проверки.
        Basket.RemoveProduct(cardNumber);

        _addedCard = cardNumber;
        Basket.AddProduct(cardNumber);

        Assert.Multiple(() =>
        {
            Assert.That(Basket.HasProduct(cardNumber), Is.True);
            Assert.That(Basket.ProductRowCount(cardNumber), Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Ручной сценарий: добавить товар по номеру карточки, затем ввести каталожный код
    /// того же товара и подтвердить снова.
    /// Ожидаемый результат: вторая строка не создаётся, количество в существующей растёт.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-003")]
    public void CatalogCodeIncrementsExistingRowInsteadOfCreatingSecond()
    {
        Basket.RemoveProduct(Card);
        var rowsBefore = Basket.ProductCards.Count;

        _addedCard = Card;
        Basket.AddProduct(Card);
        Basket.AddByCode(CatalogCode, Card, expectedQuantity: 2);

        Assert.Multiple(() =>
        {
            Assert.That(Basket.ProductRowCount(Card), Is.EqualTo(1), "Появился дубль строки.");
            Assert.That(Basket.ProductQuantity(Card), Is.EqualTo(2));
            Assert.That(Basket.ProductCards, Has.Count.EqualTo(rowsBefore + 1));
        });
    }

    /// <summary>
    /// Ручной сценарий: ввести карточку, выставить счётчик панели добавления в 3
    /// и подтвердить.
    /// Ожидаемый результат: позиция добавлена сразу с количеством 3, а не с 1.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-004")]
    public void ProductIsAddedWithQuantityChosenBeforeConfirmation()
    {
        Basket.RemoveProduct(Card);

        _addedCard = Card;
        Basket.AddProduct(Card, quantity: 3);

        Assert.Multiple(() =>
        {
            Assert.That(Basket.ProductRowCount(Card), Is.EqualTo(1));
            Assert.That(Basket.ProductQuantity(Card), Is.EqualTo(3));
            Assert.That(Basket.SectionOf(Card), Is.EqualTo(BasketPage.StockSection),
                "Складской товар оказался не в том разделе корзины.");
        });
    }

    /// <summary>
    /// Ручной сценарий: добавить товар и осмотреть флажок в его строке.
    /// Ожидаемый результат: позиция отмечена автоматически, отдельный клик не нужен.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-005")]
    public void AddedProductIsSelectedAutomatically()
    {
        Basket.RemoveProduct(Card);

        _addedCard = Card;
        Basket.AddProduct(Card);

        Assert.Multiple(() =>
        {
            Assert.That(Basket.IsProductSelected(Card), Is.True);
            Assert.That(Basket.SelectedTotal, Is.GreaterThan(0));
        });
    }

    /// <summary>
    /// Убирает добавленный товар, чтобы корзина тестового клиента не накапливала позиции.
    /// Сбой уборки не роняет прошедший тест, но попадает в вывод.
    /// </summary>
    [TearDown]
    public void RemoveAddedProduct()
    {
        if (_addedCard is null) return;

        try
        {
            Basket.RemoveProduct(_addedCard);
        }
        catch (Exception exception)
        {
            TestContext.Error.WriteLine(
                $"Не удалось убрать товар '{_addedCard}' из корзины: {exception.Message}");
        }
        finally
        {
            _addedCard = null;
        }
    }
}
