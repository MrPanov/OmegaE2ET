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

    /// <summary>
    /// Ручной сценарий: авторизоваться, открыть корзину и проверить, что страница
    /// загрузилась и на ней доступен переход в журнал счетов.
    /// Ожидаемый результат: корзина открыта, ссылка «Журнал рахунків» отображается.
    /// </summary>
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

    /// <summary>
    /// Ручной сценарий: ввести номер карточки товара в поле добавления корзины,
    /// подтвердить ввод и повторить добавление той же карточки.
    /// Ожидаемый результат: товар присутствует в корзине только в одной строке.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-002")]
    public void ProductCanBeAddedByCardWithoutDuplicate()
    {
        _basket.AddProduct(ProductCard);

        Assert.That(_basket.ProductCards.Count(card => card == ProductCard), Is.EqualTo(1));
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

    /// <summary>
    /// Ручной сценарий: добавить товар, оставить выбранным только его и сравнить
    /// общую сумму выбранных позиций с ценой, умноженной на количество.
    /// Ожидаемый результат: итог учитывает только выбранный товар.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-006")]
    public void SelectedTotalIncludesOnlyCheckedProduct()
    {
        _basket.AddProduct(ProductCard);
        var product = _basket.ProductDetails(ProductCard);

        _basket.SelectOnlyProduct(ProductCard);

        Assert.That(_basket.SelectedTotal, Is.EqualTo(product.Price * product.Quantity));
    }

    /// <summary>
    /// Ручной сценарий: добавить товар, снять флажок «Вибрати всі», затем установить его снова.
    /// Ожидаемый результат: флажки всех строк корзины синхронно снимаются и устанавливаются.
    /// </summary>
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

    /// <summary>
    /// Ручной сценарий: запомнить существующие позиции, добавить эталонный товар и удалить его.
    /// Ожидаемый результат: удаляется только эталонный товар, остальные строки не изменяются.
    /// </summary>
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
