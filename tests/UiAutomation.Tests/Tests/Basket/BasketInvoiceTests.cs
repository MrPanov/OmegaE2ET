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
    private const string CatalogCode = "OC90";
    private const string Card = "4610495";

    private BasketInvoicePage _invoice = null!;
    private Dictionary<string, bool> _selectionBefore = [];
    private IReadOnlyCollection<string>? _invoiceNumbersBefore;
    private string? _createdInvoiceNumber;
    private int? _originalProductQuantity;
    private bool _reservationStarted;
    private bool _testProductMayExist;

    protected override void OnBasketReady()
    {
        _selectionBefore = [];
        _invoiceNumbersBefore = null;
        _createdInvoiceNumber = null;
        _originalProductQuantity = null;
        _reservationStarted = false;
        _testProductMayExist = false;

        base.OnBasketReady();
        _invoice = new BasketInvoicePage(Driver, Timeout);
    }

    /// <summary>
    /// Добавляет одну позицию, оставляет выбранной только её, создаёт счёт
    /// кнопкой «У резерв» и проверяет номер, статус и состав документа.
    /// </summary>
    [Test]
    [Property("TestCaseId", "BASKET-015")]
    public void SelectedProductCanBeReservedInNewInvoice()
    {
        _selectionBefore = Basket.ProductCards
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(card => card, Basket.IsProductSelected, StringComparer.Ordinal);
        _originalProductQuantity = Basket.HasProduct(Card)
            ? Basket.ProductQuantity(Card)
            : 0;

        Basket.DeselectAllProducts();

        // OC90 уже может находиться в общей корзине. Добавление должно увеличить
        // существующую строку ровно на единицу, а не создавать дубль.
        _testProductMayExist = true;
        Basket.AddByCode(
            CatalogCode,
            Card,
            expectedQuantity: _originalProductQuantity.Value + 1);

        // В резерв отправляется ровно одна единица. Исходное количество общей
        // корзины будет восстановлено в TearDown после удаления счёта.
        Basket.SetQuantity(Card, "1", expected: 1);

        // После добавления Angular повторно применяет начальный выбор и может
        // отметить другие складские строки. Поэтому изоляция выбора выполняется
        // ещё раз непосредственно перед созданием счёта.
        Basket.DeselectAllProducts();
        Basket.SetProductSelected(Card, true);

        Assert.That(
            Basket.SelectedProductCards,
            Is.EqualTo(new[] { Card }),
            "В счёт могла попасть посторонняя позиция корзины.");

        var warehousesWithStock = Basket.WarehousesWithStock(Card);
        Assert.That(
            warehousesWithStock,
            Is.Not.Empty,
            $"Для '{CatalogCode}' не найден ни один склад с положительным остатком.");
        var selectedWarehouse = warehousesWithStock[0];
        TestContext.Out.WriteLine(
            $"Склады с остатком для '{CatalogCode}': {string.Join(", ", warehousesWithStock)}. " +
            $"Выбран первый: {selectedWarehouse}.");

        _invoiceNumbersBefore = _invoice.OpenInvoiceNumbers.ToArray();

        _reservationStarted = true;
        Basket.ReserveSelectedProducts(selectedWarehouse);
        _createdInvoiceNumber = _invoice.WaitForNewInvoiceNumber(_invoiceNumbersBefore);
        TestContext.Out.WriteLine(
            $"Создан счёт '{_createdInvoiceNumber}': " +
            _invoice.Description(_createdInvoiceNumber));

        Assert.Multiple(() =>
        {
            Assert.That(
                _createdInvoiceNumber,
                Is.Not.Empty,
                "После резервирования не появился номер нового счёта.");
            Assert.That(
                _invoice.IsReserved(_createdInvoiceNumber),
                Is.True,
                $"Счёт '{_createdInvoiceNumber}' не получил статус 'Зарезервований'.");
            Assert.That(
                _invoice.HasProduct(_createdInvoiceNumber, Card),
                Is.True,
                $"В счёте '{_createdInvoiceNumber}' нет карточки '{Card}'.");
            Assert.That(
                _invoice.HasWarehouse(_createdInvoiceNumber, selectedWarehouse),
                Is.True,
                $"Счёт '{_createdInvoiceNumber}' создан не на складе '{selectedWarehouse}'.");
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
                Basket.RemoveProduct(Card);

                if (_originalProductQuantity > 0)
                {
                    Basket.AddProduct(Card, _originalProductQuantity.Value);
                    Assert.That(
                        Basket.ProductQuantity(Card),
                        Is.EqualTo(_originalProductQuantity.Value),
                        $"Не удалось восстановить исходное количество карточки '{Card}'.");
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
