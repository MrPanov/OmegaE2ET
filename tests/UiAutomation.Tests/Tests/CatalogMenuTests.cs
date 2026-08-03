using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests;

[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Catalogs")]
public sealed class CatalogMenuTests : AuthenticatedUiTestFixture
{
    private CatalogMenuPage _catalogMenu = null!;

    private static readonly string[] CatalogGroups =
    [
        "Підбір по авто",
        "Шини та диски",
        "Оливи і тех. рідини",
        "Спец. каталоги",
        "Інше"
    ];

    private static readonly string[] CatalogItems =
    [
        "VIN",
        "VIN TRUCK",
        "Запчастини для ТО",
        "Для легкових авто",
        "Для вантажних авто",
        "Шини",
        "Колісні диски",
        "Камери",
        "Оливи",
        "Тех. рідини",
        "ЗЧ до сільгосптехніки",
        "АКБ",
        "Кузов та оптика",
        "Лампи",
        "Підшипники",
        "Ремені Агро техніка",
        "Аварійні з'єднувачі",
        "Аксесуари / Обладнання / Інструмент",
        "Товари для господарських потреб / Побутова хімія / Товари для бізнесу / Канцтовари",
        "Туризм і риболовля",
        "Гаражне обладнання",
        "Корисні товари",
        "Подарунки"
    ];

    protected override void OnAuthenticated()
    {
        _catalogMenu = new CatalogMenuPage(Driver, Timeout);
        _catalogMenu.OpenMenu();
    }

    [Test]
    [Category("Smoke")]
    public void CatalogButtonIsDisplayedAfterLogin()
    {
        Assert.That(_catalogMenu.IsCatalogButtonDisplayed, Is.True);
    }

    [Test]
    [Category("Smoke")]
    public void CatalogMenuCanBeExpandedAndCollapsed()
    {
        _catalogMenu.OpenMenu();
        Assert.That(_catalogMenu.IsMenuExpanded, Is.True);

        _catalogMenu.CloseMenu();
        Assert.That(_catalogMenu.IsMenuExpanded, Is.False);
    }

    [TestCaseSource(nameof(CatalogGroups))]
    [Category("Smoke")]
    public void CatalogMenuContainsExpectedGroup(string groupName)
    {
        Assert.That(
            _catalogMenu.IsGroupDisplayed(groupName),
            Is.True,
            $"Catalog group '{groupName}' is not displayed.");
    }

    [TestCaseSource(nameof(CatalogItems))]
    [Category("Smoke")]
    public void CatalogMenuContainsExpectedItem(string itemName)
    {
        Assert.That(
            _catalogMenu.IsCatalogItemDisplayed(itemName),
            Is.True,
            $"Catalog item '{itemName}' is not displayed.");
    }

    [TestCaseSource(nameof(CatalogItems))]
    [Category("Smoke")]
    public void CatalogItemCanBeSelected(string itemName)
    {
        Assert.That(
            _catalogMenu.SelectCatalog(itemName),
            Is.True,
            $"Catalog item '{itemName}' did not trigger navigation or close the menu.");
    }

    [TestCase("VIN", "#/app/modelsearch")]
    [TestCase("VIN TRUCK", "#/app/modelsearchtruck")]
    [TestCase("Запчастини для ТО", "#/app/maintenancesearch")]
    [TestCase("Для легкових авто", "#/app/carsCatalog")]
    [TestCase("Для вантажних авто", "#/app/trucksCatalog")]
    [TestCase("Подарунки", "#/app/catalogGifts")]
    [Category("Smoke")]
    public void DirectCatalogCanBeOpened(string itemName, string expectedRoute)
    {
        _catalogMenu.OpenCatalog(itemName, expectedRoute);

        Assert.That(Driver.Url, Does.Contain(expectedRoute).IgnoreCase);
    }
}
