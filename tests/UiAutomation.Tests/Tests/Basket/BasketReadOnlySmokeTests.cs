using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests.Basket;

[TestFixture]
[NonParallelizable]
[Category("Basket")]
[Category("Smoke")]
[Category("P0")]
[Category(TestCategories.ProductionSafe)]
public sealed class BasketReadOnlySmokeTests : AuthenticatedUiTestFixture
{
    private BasketPage _basket = null!;

    protected override void OnAuthenticated() =>
        _basket = new BasketPage(Driver, Timeout);

    [SetUp]
    public void OpenBasket() => _basket.Open(Settings.BaseUrl);

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
}
