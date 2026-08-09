using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests.Basket;

/// <summary>
/// Операции над уже добавленной строкой: количество и выбор. Счётчик в строке
/// и счётчик панели добавления — разные элементы, здесь проверяется первый.
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Basket")]
[Category("Smoke")]
[Category("P0")]
[Category(TestCategories.ProductionTestClient)]
[Category(TestCategories.MutatesUserState)]
public sealed class BasketRowControlsTests : BasketTestBase
{
    private const string Card = "4610495";

    private string? _addedCard;

    protected override void OnBasketReady()
    {
        _addedCard = null;
        base.OnBasketReady();
    }

    /// <summary>
    /// Ручной сценарий: изменить количество кнопками в строке товара и сверить сумму.
    /// Ожидаемый результат: сумма пересчитывается сразу; возврат к исходному количеству
    /// возвращает исходную сумму.
    /// </summary>
    /// <remarks>
    /// Сумма сверяется относительными величинами: цена товара меняется день ото дня,
    /// поэтому эталонного значения не существует.
    /// </remarks>
    [Test]
    [Property("TestCaseId", "BASKET-007")]
    public void RowQuantityControlsRecalculateSelectedTotal()
    {
        Basket.RemoveProduct(Card);
        _addedCard = Card;
        Basket.AddProduct(Card, quantity: 3);

        var totalAtThree = Basket.SelectedTotal;
        Basket.DecreaseQuantity(Card);
        var totalAtTwo = Basket.SelectedTotal;
        Basket.IncreaseQuantity(Card);
        var totalRestored = Basket.SelectedTotal;

        Assert.Multiple(() =>
        {
            Assert.That(Basket.ProductQuantity(Card), Is.EqualTo(3));
            Assert.That(totalAtThree, Is.GreaterThan(0));
            Assert.That(totalAtTwo, Is.LessThan(totalAtThree), "Сумма не уменьшилась.");
            Assert.That(totalRestored, Is.EqualTo(totalAtThree), "Сумма не вернулась к исходной.");
        });
    }

    /// <summary>
    /// Ручной сценарий: ввести нулевое количество, затем попытаться уйти ниже единицы кнопкой.
    /// Ожидаемый результат: ноль не принимается, количество не опускается ниже единицы,
    /// строка не удаляется.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-008")]
    public void QuantityBelowOneIsRejectedAndRowSurvives()
    {
        Basket.RemoveProduct(Card);
        _addedCard = Card;
        Basket.AddProduct(Card, quantity: 2);

        Basket.SetQuantity(Card, "0");
        Assert.That(Basket.ProductQuantity(Card), Is.EqualTo(1), "Ноль был принят.");

        Basket.DecreaseQuantity(Card, expected: 1);

        Assert.Multiple(() =>
        {
            Assert.That(Basket.ProductQuantity(Card), Is.EqualTo(1));
            Assert.That(Basket.HasProduct(Card), Is.True, "Строка исчезла при попытке уйти ниже единицы.");
        });
    }

    /// <summary>
    /// Ручной сценарий: снять флажок «Вибрати всі», затем установить его снова.
    /// Ожидаемый результат: снятие обнуляет сумму выбранных, установка возвращает выбор.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-009")]
    public void SelectAllTogglesSelectionAndTotal()
    {
        Basket.RemoveProduct(Card);
        _addedCard = Card;
        Basket.AddProduct(Card);

        Basket.SetSelectAll(false);
        Assert.Multiple(() =>
        {
            Assert.That(Basket.SelectedTotal, Is.Zero, "Сумма выбранных не обнулилась.");
            Assert.That(Basket.IsProductSelected(Card), Is.False);
        });

        Basket.SetSelectAll(true);
        Assert.Multiple(() =>
        {
            Assert.That(Basket.SelectedTotal, Is.GreaterThan(0));
            Assert.That(Basket.SelectionStates, Has.Some.True);
        });
    }

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
        Basket.RemoveProduct(Card);
        var otherCards = Basket.ProductCards.ToArray();

        Basket.AddProduct(Card);
        Basket.RemoveProduct(Card);

        Assert.Multiple(() =>
        {
            Assert.That(Basket.HasProduct(Card), Is.False);
            Assert.That(Basket.ProductCards, Is.EqualTo(otherCards));
        });
    }

    /// <summary>Убирает добавленный товар. Сбой уборки не роняет прошедший тест.</summary>
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
