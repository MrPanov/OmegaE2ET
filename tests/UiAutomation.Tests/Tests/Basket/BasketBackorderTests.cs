using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests.Basket;

/// <summary>
/// Товар без остатка на складах нельзя положить в корзину полем «Додати позицію» —
/// он добавляется из окна «Залишки», которое открывается ссылкой «див. наяв.»
/// в выдаче поиска.
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Basket")]
[Category("Smoke")]
[Category("P0")]
[Category(TestCategories.ProductionTestClient)]
[Category(TestCategories.MutatesUserState)]
public sealed class BasketBackorderTests : AuthenticatedUiTestFixture
{
    /// <summary>Товар без складского остатка: «Фільтр масляний (Вир-во MEYLE)».</summary>
    private const string ProductCode = "614 065 0004";

    /// <summary>Номер карточки, под которым этот товар попадает в корзину.</summary>
    private const string ExpectedCard = "46001179890";

    private SearchResultsPage _search = null!;
    private ProductStockDialog _stock = null!;
    private BasketPage _basket = null!;
    private bool _added;

    protected override void OnAuthenticated()
    {
        _search = new SearchResultsPage(
            Driver,
            Timeout,
            TimeSpan.FromSeconds(Settings.SearchMinimumIntervalSeconds));
        _stock = new ProductStockDialog(Driver, Timeout);
        _basket = new BasketPage(Driver, Timeout);
    }

    /// <summary>
    /// Ручной сценарий: найти товар без остатка, открыть «див. наяв.», в блоке
    /// «Під замовлення на склад Омеги» положить первую поставку в корзину,
    /// закрыть окно и перейти в корзину.
    /// Ожидаемый результат: позиция появилась в корзине под своим номером карточки.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-013")]
    public void BackorderedProductReachesBasketFromStockDialog()
    {
        _basket.Open(Settings.BaseUrl);
        _basket.RemoveProduct(ExpectedCard);

        _search.Reset(Settings.BaseUrl);
        _search.Search(ProductCode);
        _stock.OpenForFirstSearchResult();

        Assert.Multiple(() =>
        {
            Assert.That(_stock.Text, Does.Contain(ProductCode));
            Assert.That(_stock.Text, Does.Contain("Під замовлення на склад Омеги"));
            Assert.That(_stock.BackorderOptionCount, Is.GreaterThan(0));
        });

        _added = true;
        _stock.AddFirstOptionToBasket();
        _stock.Close();

        _basket.Open(Settings.BaseUrl);

        Assert.Multiple(() =>
        {
            Assert.That(_basket.HasProduct(ExpectedCard), Is.True,
                "Позиция под заказ не появилась в корзине.");
            Assert.That(_basket.ProductRowCount(ExpectedCard), Is.EqualTo(1));
        });
    }

    /// <summary>Убирает добавленную позицию. Сбой уборки не роняет прошедший тест.</summary>
    [TearDown]
    public void RemoveBackorderedProduct()
    {
        if (!_added) return;

        try
        {
            _basket.Open(Settings.BaseUrl);
            _basket.RemoveProduct(ExpectedCard);
        }
        catch (Exception exception)
        {
            TestContext.Error.WriteLine(
                $"Не удалось убрать позицию '{ExpectedCard}' из корзины: {exception.Message}");
        }
        finally
        {
            _added = false;
        }
    }
}
