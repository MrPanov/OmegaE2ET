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
public sealed class BasketAddProductTests : AuthenticatedUiTestFixture
{
    private BasketPage _basket = null!;
    private string? _addedCard;

    protected override void OnAuthenticated() => _basket = new BasketPage(Driver, Timeout);

    [SetUp]
    public void OpenBasket()
    {
        _addedCard = null;
        _basket.Open(Settings.BaseUrl);
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
        _basket.RemoveProduct(cardNumber);

        _addedCard = cardNumber;
        _basket.AddProduct(cardNumber);

        Assert.Multiple(() =>
        {
            Assert.That(_basket.HasProduct(cardNumber), Is.True);
            Assert.That(_basket.ProductRowCount(cardNumber), Is.EqualTo(1));
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
            _basket.RemoveProduct(_addedCard);
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
