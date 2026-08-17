using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests.Basket;

/// <summary>
/// Создание реального счёта на тестовом клиенте и обязательное удаление документа
/// после проверки. Фикстура непараллельная, потому что корзина и вкладки счетов
/// общие для всех запусков под этой учётной записью.
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Basket")]
[Category("Invoice")]
[Category("P1")]
[Category(TestCategories.ProductionTestClient)]
[Category(TestCategories.MutatesUserState)]
public sealed class BasketInvoiceTests : BasketTestBase
{
    private const string CourierCatalogCode = "OC90";
    private const string CourierCard = "4610495";
    private const string RoutedCard = "5614799817";
    private const string KyivWarehouse = "Київ";
    private const string PlannedDelivery = "Планова доставка";
    private const string Pickup = "Самовивіз";
    private const string CashPayment = "За готівку";

    private BasketInvoicePage _invoice = null!;
    private IReadOnlyCollection<string>? _invoiceNumbersBefore;
    private string? _createdInvoiceNumber;
    private bool _invoiceCreationStarted;

    protected override void OnBasketReady()
    {
        _invoiceNumbersBefore = null;
        _createdInvoiceNumber = null;
        _invoiceCreationStarted = false;

        base.OnBasketReady();
        var cleared = Basket.ClearIfNotEmpty();
        TestContext.Out.WriteLine(cleared
            ? "Корзина очищена перед тестом."
            : "Корзина перед тестом уже была пустой.");
        Assert.That(Basket.ProductCards, Is.Empty, "SetUp не оставил корзину пустой.");

        _invoice = new BasketInvoicePage(Driver, Timeout);
    }

    /// <summary>
    /// Региональный склад без маршрута автоматически переключает резервный счёт
    /// на курьерскую доставку.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-015")]
    public void InvoiceIsReservedWithCourierDelivery()
    {
        Basket.AddByIdentifier(CourierCatalogCode, CourierCard, expectedQuantity: 1);
        AssertSingleProductInBasket(CourierCard);

        var warehousesWithStock = Basket.WarehousesWithStock(CourierCard);
        Assert.That(
            warehousesWithStock,
            Is.Not.Empty,
            $"Для '{CourierCatalogCode}' не найден ни один склад с положительным остатком.");
        var selectedWarehouse = warehousesWithStock[0];
        TestContext.Out.WriteLine(
            $"Для карточки '{CourierCard}' выбран первый склад с остатком " +
            $"'{selectedWarehouse}'.");

        RememberInvoicesBeforeCreation();
        Basket.ReserveSelectedProducts(selectedWarehouse);
        var invoiceNumber = WaitForCreatedInvoice();

        Assert.Multiple(() =>
        {
            Assert.That(
                _invoice.IsReserved(invoiceNumber),
                Is.True,
                $"Счёт '{invoiceNumber}' не получил статус 'Зарезервований'.");
            Assert.That(
                _invoice.HasDeliveryType(
                    invoiceNumber,
                    "Кур'єрська Доставка",
                    "Кур'єрські служби"),
                Is.True,
                $"Счёт '{invoiceNumber}' не переключился на курьерскую доставку.");
            Assert.That(
                _invoice.HasProduct(invoiceNumber, CourierCard),
                Is.True,
                $"В счёте '{invoiceNumber}' нет карточки '{CourierCard}'.");
            Assert.That(
                _invoice.ReservedQuantity(invoiceNumber, CourierCard),
                Is.EqualTo(1),
                $"В счёте '{invoiceNumber}' должна быть зарезервирована одна единица " +
                $"карточки '{CourierCard}'.");
            Assert.That(
                _invoice.HasWarehouse(invoiceNumber, selectedWarehouse),
                Is.True,
                $"Счёт '{invoiceNumber}' создан не на складе '{selectedWarehouse}'.");
        });
    }

    /// <summary>Создаёт резерв с плановой доставкой с киевского склада.</summary>
    [Test]
    [Property("TestCaseId", "BASKET-016")]
    public void InvoiceIsReservedWithPlannedDeliveryFromKyivWarehouse()
    {
        Basket.AddByIdentifier(RoutedCard, RoutedCard, expectedQuantity: 1);
        AssertSingleProductInBasket(RoutedCard);
        var selectedWarehouse = RequiredWarehouseWithStock(RoutedCard, KyivWarehouse);

        RememberInvoicesBeforeCreation();
        Basket.SaveSelectedProducts(selectedWarehouse);
        var invoiceNumber = WaitForCreatedInvoice();
        Assert.That(
            _invoice.IsSaved(invoiceNumber),
            Is.True,
            $"Счёт '{invoiceNumber}' не получил статус 'Збережений'.");

        _invoice.SelectDeliveryType(invoiceNumber, PlannedDelivery);
        _invoice.Reserve(invoiceNumber);

        AssertReservedInvoice(RoutedCard, KyivWarehouse, PlannedDelivery);
    }

    /// <summary>Создаёт резерв с самовывозом с киевского склада.</summary>
    [Test]
    [Property("TestCaseId", "BASKET-017")]
    public void InvoiceIsReservedWithPickupAndCashPaymentFromKyivWarehouse()
    {
        Basket.AddByIdentifier(RoutedCard, RoutedCard, expectedQuantity: 1);
        AssertSingleProductInBasket(RoutedCard);
        var selectedWarehouse = RequiredWarehouseWithStock(RoutedCard, KyivWarehouse);

        RememberInvoicesBeforeCreation();
        Basket.SaveSelectedProducts(selectedWarehouse);
        var invoiceNumber = WaitForCreatedInvoice();
        Assert.That(
            _invoice.IsSaved(invoiceNumber),
            Is.True,
            $"Счёт '{invoiceNumber}' не получил статус 'Збережений'.");

        _invoice.SelectDeliveryType(invoiceNumber, Pickup);
        _invoice.SelectPaymentType(invoiceNumber, CashPayment);
        _invoice.Reserve(invoiceNumber);

        AssertReservedInvoice(RoutedCard, KyivWarehouse, Pickup);
        Assert.That(
            _invoice.HasPaymentType(invoiceNumber, CashPayment),
            Is.True,
            $"В счёте '{invoiceNumber}' не выбран способ оплаты '{CashPayment}'.");
    }

    private void AssertSingleProductInBasket(string card)
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                Basket.ProductCards,
                Is.EqualTo(new[] { card }),
                "После добавления в пустой корзине должна находиться ровно одна карточка.");
            Assert.That(
                Basket.ProductRowCount(card),
                Is.EqualTo(1),
                $"Карточка '{card}' должна отображаться одной строкой.");
            Assert.That(
                Basket.ProductQuantity(card),
                Is.EqualTo(1),
                $"Для карточки '{card}' должно быть добавлено ровно одна единица.");
            Assert.That(
                Basket.SelectedProductCards,
                Is.EqualTo(new[] { card }),
                "В будущий счёт должна входить только добавленная позиция.");
        });
    }

    private string RequiredWarehouseWithStock(string card, string requiredWarehouse)
    {
        var options = Basket.WarehousesWithStock(card);
        var selectedOption = options.FirstOrDefault(item =>
            item.Contains(requiredWarehouse, StringComparison.OrdinalIgnoreCase));

        Assert.That(
            selectedOption,
            Is.Not.Null,
            $"У карточки '{card}' нет положительного остатка на складе " +
            $"'{requiredWarehouse}'. Доступны: [{string.Join(", ", options)}].");
        return selectedOption!;
    }

    private void RememberInvoicesBeforeCreation()
    {
        _invoiceNumbersBefore = _invoice.OpenInvoiceNumbers.ToArray();
        _invoiceCreationStarted = true;
    }

    private string WaitForCreatedInvoice()
    {
        _createdInvoiceNumber = _invoice.WaitForNewInvoiceNumber(_invoiceNumbersBefore!);
        Assert.That(
            _createdInvoiceNumber,
            Is.Not.Empty,
            "После создания не появился номер нового счёта.");
        TestContext.Out.WriteLine(
            $"Создан счёт '{_createdInvoiceNumber}': " +
            _invoice.Description(_createdInvoiceNumber));
        return _createdInvoiceNumber;
    }

    private void AssertReservedInvoice(string card, string warehouse, string deliveryType)
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                _invoice.IsReserved(_createdInvoiceNumber!),
                Is.True,
                $"Счёт '{_createdInvoiceNumber}' не получил статус 'Зарезервований'.");
            Assert.That(
                _invoice.HasDeliveryType(_createdInvoiceNumber!, deliveryType),
                Is.True,
                $"В счёте '{_createdInvoiceNumber}' не выбран вид доставки '{deliveryType}'.");
            Assert.That(
                _invoice.HasProduct(_createdInvoiceNumber!, card),
                Is.True,
                $"В счёте '{_createdInvoiceNumber}' нет карточки '{card}'.");
            Assert.That(
                _invoice.ReservedQuantity(_createdInvoiceNumber!, card),
                Is.EqualTo(1),
                $"В счёте '{_createdInvoiceNumber}' должна быть зарезервирована " +
                $"ровно одна единица карточки '{card}'.");
            Assert.That(
                _invoice.HasWarehouse(_createdInvoiceNumber!, warehouse),
                Is.True,
                $"Счёт '{_createdInvoiceNumber}' создан не на складе '{warehouse}'.");
        });
    }

    /// <summary>
    /// Сначала удаляет созданный документ по точному номеру, затем снова полностью
    /// очищает корзину. Ошибка уборки роняет тест: оставлять настоящий счёт или
    /// товар после зелёного прогона недопустимо.
    /// </summary>
    [TearDown]
    public void DeleteCreatedInvoiceAndClearBasket()
    {
        var cleanupErrors = new List<string>();

        try
        {
            if (_createdInvoiceNumber is null && _invoiceNumbersBefore is not null)
            {
                _createdInvoiceNumber = _invoice.FindNewInvoiceNumber(_invoiceNumbersBefore);
                if (_createdInvoiceNumber is null && _invoiceCreationStarted)
                {
                    _createdInvoiceNumber = _invoice.WaitForNewInvoiceNumber(_invoiceNumbersBefore);
                }
            }

            if (_createdInvoiceNumber is not null)
            {
                _invoice.Delete(_createdInvoiceNumber);
                TestContext.Out.WriteLine($"Удалён тестовый счёт '{_createdInvoiceNumber}'.");
            }
        }
        catch (Exception exception)
        {
            cleanupErrors.Add(
                $"Не удалось удалить счёт '{_createdInvoiceNumber ?? "<номер не определён>"}': " +
                exception.Message);
        }

        try
        {
            Basket.Open(Settings.BaseUrl);
            var cleared = Basket.ClearIfNotEmpty();
            TestContext.Out.WriteLine(cleared
                ? "Корзина очищена после теста."
                : "Корзина после теста уже была пустой.");
            Assert.That(Basket.ProductCards, Is.Empty, "TearDown не оставил корзину пустой.");
        }
        catch (Exception exception)
        {
            cleanupErrors.Add($"Не удалось очистить корзину: {exception.Message}");
        }
        finally
        {
            _createdInvoiceNumber = null;
            _invoiceNumbersBefore = null;
            _invoiceCreationStarted = false;
        }

        if (cleanupErrors.Count > 0)
        {
            Assert.Fail("Ошибка обязательной уборки после теста:\n" + string.Join("\n", cleanupErrors));
        }
    }
}
