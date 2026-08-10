using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests.Basket;

/// <summary>
/// Очистка корзины удаляет все позиции разом и подтверждения не запрашивает.
/// </summary>
/// <remarks>
/// Сценарий выполняется и в Production: учётная запись там — такой же тестовый
/// клиент, поэтому очищается не корзина реального покупателя. Но действие
/// необратимо, и строки раздела «Товари під замовлення» после повторного
/// добавления получат текущие склад и дату, а не исходные, — прогон затрёт
/// состояние, если кто-то готовил его для ручной проверки.
/// </remarks>
[TestFixture]
[NonParallelizable]
[Category("Basket")]
[Category("Smoke")]
[Category(TestCategories.ProductionTestClient)]
[Category(TestCategories.MutatesUserState)]
public sealed class BasketClearTests : BasketTestBase
{
    private const string Card = "4610495";


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
        Basket.AddProduct(Card);
        Assert.That(Basket.HasClearButton, Is.True, "Кнопка очистки не появилась при непустой корзине.");

        Basket.ClearBasket();
        Assert.That(Basket.ProductCards, Is.Empty);

        // Сразу после очистки кнопка остаётся в разметке с классами ng-leave:
        // Angular запускает анимацию исчезновения, которая в headless-сессии
        // не доигрывает. Поэтому проверяем отсутствие кнопки после перезагрузки.
        Basket.Reload();

        Assert.Multiple(() =>
        {
            Assert.That(Basket.ProductCards, Is.Empty);
            Assert.That(Basket.HasClearButton, Is.False, "Кнопка очистки осталась при пустой корзине.");
        });
    }
}
