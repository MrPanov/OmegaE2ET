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
    private Dictionary<string, bool> _selectionBefore = [];
    private IReadOnlyCollection<string>? _invoiceNumbersBefore;
    private string? _createdInvoiceNumber;
    private string? _testCard;
    private int? _originalProductQuantity;
    private bool _reservationStarted;
    private bool _testProductMayExist;

    protected override void OnBasketReady()
    {
        _selectionBefore = [];
        _invoiceNumbersBefore = null;
        _createdInvoiceNumber = null;
        _testCard = null;
        _originalProductQuantity = null;
        _reservationStarted = false;
        _testProductMayExist = false;

        base.OnBasketReady();
        _invoice = new BasketInvoicePage(Driver, Timeout);
    }

    /// <summary>
    /// Региональный склад без маршрута автоматически переключает счёт на
    /// курьерскую доставку. Из-за незаполненных реквизитов доставки Test-клиента
    /// сайт оставляет такой документ в штатном fallback-статусе «Збережений».
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-015")]
    public void InvoiceWithoutShipmentRouteIsCreatedWithCourierDelivery()
    {
        PrepareSingleProduct(CourierCatalogCode, CourierCard);
        var warehousesWithStock = Basket.WarehousesWithStock(CourierCard);
        Assert.That(
            warehousesWithStock,
            Is.Not.Empty,
            $"Для '{CourierCatalogCode}' не найден ни один склад с положительным остатком.");
        var selectedWarehouse = warehousesWithStock[0];
        CreateInvoice(CourierCard, selectedWarehouse);

        Assert.Multiple(() =>
        {
            Assert.That(
                _invoice.IsSaved(_createdInvoiceNumber!),
                Is.True,
                $"Счёт '{_createdInvoiceNumber}' не получил fallback-статус 'Збережений'.");
            Assert.That(
                _invoice.HasDeliveryType(_createdInvoiceNumber!, "Кур'єрська Доставка"),
                Is.True,
                $"Счёт '{_createdInvoiceNumber}' не переключился на курьерскую доставку.");
            Assert.That(
                _invoice.HasProduct(_createdInvoiceNumber!, CourierCard),
                Is.True,
                $"В счёте '{_createdInvoiceNumber}' нет карточки '{CourierCard}'.");
            Assert.That(
                _invoice.HasWarehouse(_createdInvoiceNumber!, selectedWarehouse),
                Is.True,
                $"Счёт '{_createdInvoiceNumber}' создан не на складе '{selectedWarehouse}'.");
        });
    }

    /// <summary>Создаёт резерв с плановой доставкой с киевского склада.</summary>
    [Test]
    [Property("TestCaseId", "BASKET-016")]
    public void InvoiceCanBeReservedWithPlannedDeliveryFromKyivWarehouse()
    {
        PrepareSingleProduct(RoutedCard, RoutedCard);
        var selectedWarehouse = RequiredWarehouseWithStock(RoutedCard, KyivWarehouse);
        CreateInvoice(RoutedCard, selectedWarehouse, PlannedDelivery);

        AssertReservedInvoice(RoutedCard, selectedWarehouse, PlannedDelivery);
    }

    /// <summary>Создаёт резерв с самовывозом с киевского склада.</summary>
    [Test]
    [Property("TestCaseId", "BASKET-017")]
    public void InvoiceCanBeReservedWithPickupFromKyivWarehouse()
    {
        PrepareSingleProduct(RoutedCard, RoutedCard);
        var selectedWarehouse = RequiredWarehouseWithStock(RoutedCard, KyivWarehouse);
        CreateSavedInvoice(RoutedCard, selectedWarehouse);
        _invoice.ConfigureAndReserve(_createdInvoiceNumber!, Pickup, CashPayment);

        AssertReservedInvoice(RoutedCard, selectedWarehouse, Pickup);
        Assert.That(
            _invoice.HasPaymentType(_createdInvoiceNumber!, CashPayment),
            Is.True,
            $"В счёте '{_createdInvoiceNumber}' не выбран способ оплаты '{CashPayment}'.");
    }

    private void PrepareSingleProduct(string identifier, string card)
    {
        _selectionBefore = Basket.ProductCards
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(item => item, Basket.IsProductSelected, StringComparer.Ordinal);
        _testCard = card;
        _originalProductQuantity = Basket.HasProduct(card)
            ? Basket.ProductQuantity(card)
            : 0;

        Basket.DeselectAllProducts();

        _testProductMayExist = true;
        Basket.AddByIdentifier(
            identifier,
            card,
            expectedQuantity: _originalProductQuantity.Value + 1);

        Basket.SetQuantity(card, "1", expected: 1);
        Basket.DeselectAllProducts();
        Basket.SetProductSelected(card, true);

        Assert.That(
            Basket.SelectedProductCards,
            Is.EqualTo(new[] { card }),
            "В счёт могла попасть посторонняя позиция корзины.");
    }

    private string RequiredWarehouseWithStock(string card, string expectedWarehouse)
    {
        var warehouses = Basket.WarehousesWithStock(card);
        var warehouse = warehouses.FirstOrDefault(item =>
            item.Contains(expectedWarehouse, StringComparison.OrdinalIgnoreCase));

        Assert.That(
            warehouse,
            Is.Not.Null,
            $"У карточки '{card}' нет положительного остатка на складе '{expectedWarehouse}'. " +
            $"Доступны: [{string.Join(", ", warehouses)}].");
        return warehouse!;
    }

    private void CreateInvoice(string card, string warehouse, string? service = null)
    {
        TestContext.Out.WriteLine(
            $"Для карточки '{card}' выбран склад '{warehouse}'" +
            (service is null ? "." : $" и сервис '{service}'."));

        _invoiceNumbersBefore = _invoice.OpenInvoiceNumbers.ToArray();
        _reservationStarted = true;
        Basket.ReserveSelectedProducts(warehouse, service);
        _createdInvoiceNumber = _invoice.WaitForNewInvoiceNumber(_invoiceNumbersBefore);

        Assert.That(
            _createdInvoiceNumber,
            Is.Not.Empty,
            "После создания не появился номер нового счёта.");
        TestContext.Out.WriteLine(
            $"Создан счёт '{_createdInvoiceNumber}': " +
            _invoice.Description(_createdInvoiceNumber));
    }

    private void CreateSavedInvoice(string card, string warehouse)
    {
        TestContext.Out.WriteLine(
            $"Для карточки '{card}' создаётся сохранённый счёт на складе '{warehouse}'.");

        _invoiceNumbersBefore = _invoice.OpenInvoiceNumbers.ToArray();
        _reservationStarted = true;
        Basket.SaveSelectedProducts(warehouse);
        _createdInvoiceNumber = _invoice.WaitForNewInvoiceNumber(_invoiceNumbersBefore);

        Assert.That(
            _invoice.IsSaved(_createdInvoiceNumber),
            Is.True,
            $"Счёт '{_createdInvoiceNumber}' не получил статус 'Збережений'.");
        TestContext.Out.WriteLine(
            $"Создан сохранённый счёт '{_createdInvoiceNumber}': " +
            _invoice.Description(_createdInvoiceNumber));
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
    /// Сначала удаляет созданный документ по точному номеру, затем убирает тестовый
    /// товар из корзины и возвращает исходные флажки остальных строк. Ошибка уборки
    /// роняет тест: оставлять настоящий счёт после зелёного прогона недопустимо.
    /// </summary>
    [TearDown]
    public void DeleteCreatedInvoiceAndRestoreBasket()
    {
        var cleanupErrors = new List<string>();

        try
        {
            if (_createdInvoiceNumber is null && _invoiceNumbersBefore is not null)
            {
                _createdInvoiceNumber = _invoice.FindNewInvoiceNumber(_invoiceNumbersBefore);
                if (_createdInvoiceNumber is null && _reservationStarted)
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

            if (_testProductMayExist)
            {
                Basket.RemoveProduct(_testCard!);

                if (_originalProductQuantity > 0)
                {
                    Basket.AddProduct(_testCard!, _originalProductQuantity.Value);
                    Assert.That(
                        Basket.ProductQuantity(_testCard!),
                        Is.EqualTo(_originalProductQuantity.Value),
                        $"Не удалось восстановить исходное количество карточки '{_testCard}'.");
                }
            }

            foreach (var (card, selected) in _selectionBefore)
            {
                if (Basket.HasProduct(card))
                {
                    Basket.SetProductSelected(card, selected);
                }
            }
        }
        catch (Exception exception)
        {
            cleanupErrors.Add($"Не удалось восстановить корзину: {exception.Message}");
        }
        finally
        {
            _createdInvoiceNumber = null;
            _testCard = null;
            _invoiceNumbersBefore = null;
            _originalProductQuantity = null;
            _selectionBefore = [];
            _reservationStarted = false;
            _testProductMayExist = false;
        }

        if (cleanupErrors.Count > 0)
        {
            Assert.Fail("Ошибка обязательной уборки после теста:\n" + string.Join("\n", cleanupErrors));
        }
    }
}
