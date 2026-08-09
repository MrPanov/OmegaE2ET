using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests.Basket;

/// <summary>
/// Очистка корзины удаляет все позиции разом, включая чужие, и подтверждения
/// не запрашивает. Восстановить состав невозможно: строки раздела
/// «Товари під замовлення» после повторного добавления получают текущие склад
/// и дату, а не исходные. Поэтому фикстура помечена <see cref="TestCategories.ProductionBlocked"/>
/// и в Production не выполняется никогда.
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Basket")]
[Category("Smoke")]
[Category(TestCategories.ProductionBlocked)]
[Category(TestCategories.MutatesUserState)]
public sealed class BasketClearTests : AuthenticatedUiTestFixture
{
    private const string Card = "4610495";

    private BasketPage _basket = null!;

    protected override void OnAuthenticated() => _basket = new BasketPage(Driver, Timeout);

    [SetUp]
    public void OpenBasket() => _basket.Open(Settings.BaseUrl);

    /// <summary>
    /// Ручной сценарий: убедиться, что кнопка очистки отображается, и нажать её.
    /// Ожидаемый результат: корзина пуста, кнопка очистки исчезает вместе
    /// с последней позицией.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-012")]
    public void ClearBasketRemovesEveryRow()
    {
        // Кнопки очистки нет в разметке, пока корзина пуста, поэтому сначала
        // гарантируем непустое состояние.
        _basket.AddProduct(Card);
        Assert.That(_basket.HasClearButton, Is.True, "Кнопка очистки не появилась при непустой корзине.");

        _basket.ClearBasket();
        Assert.That(_basket.ProductCards, Is.Empty);

        // Сразу после очистки кнопка остаётся в разметке с классами ng-leave:
        // Angular запускает анимацию исчезновения, которая в headless-сессии
        // не доигрывает. Поэтому проверяем отсутствие кнопки после перезагрузки.
        _basket.Reload();

        Assert.Multiple(() =>
        {
            Assert.That(_basket.ProductCards, Is.Empty);
            Assert.That(_basket.HasClearButton, Is.False, "Кнопка очистки осталась при пустой корзине.");
        });
    }
}
