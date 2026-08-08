using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests;

[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("MainMenu")]
[Category(TestCategories.ProductionSafe)]
public sealed class MainMenuTests : AuthenticatedUiTestFixture
{
    private MainMenuPage _mainMenu = null!;

    private static readonly string[] MenuSections =
    [
        "Звіти",
        "Журнали",
        "Інше"
    ];

    private static readonly (string Name, string? Route)[] MenuItems =
    [
        ("Деб. заборгованість", "#/app/receivablesList"),
        ("Взаєморозрахунки", "#/app/mutualSettlementsList"),
        ("Новий товар", "#/app/simplesearch"),
        ("Розпродаж", "#/app/simplesearch"),
        ("Вибрані товари", "#/app/simplesearch"),
        ("Відвантажені товари", "#/app/simplesearch"),
        ("Залишки по ВЗ", "#/app/safeStorage"),
        ("Рахунки", "#/app/basket"),
        ("Видаткові накладні", "#/app/expenseList"),
        ("Податкові накладні", "#/app/taxInvoiceList"),
        ("Коригування до податкових накладних", "#/app/taxInvoiceChangeList"),
        ("Посилки", "#/app/sendbox"),
        ("Повернення", "#/app/claimsList"),
        ("Заявки АМ", "#/app/assortmentMatrixList"),
        ("Облік закупівель", "#/app/purchase"),
        ("Зворотний звʼязок", "#/app/ticket"),
        ("Запити", "#/app/requestList"),
        ("Аукціон", "#/app/auction"),
        ("Кошик повернень", "#/app/claimsBasket"),
        ("Прайс-листи", "#/app/prices"),
        ("Документи", "#/app/documents"),
        ("EДО", null)
    ];

    private static readonly string[] EdoMenuItems =
    [
        "Підписати ЕЦП",
        "Підключення до ЕДО",
        "Проблема з ЕДО",
        "Запитання ЕДО"
    ];

    public static IEnumerable<TestCaseData> MenuItemNames =>
        MenuItems.Select(item =>
            new TestCaseData(item.Name).SetName($"MenuContains_{item.Name}"));

    public static IEnumerable<TestCaseData> RoutedMenuItems =>
        MenuItems
            .Where(item => item.Route is not null)
            .Select(item =>
                new TestCaseData(item.Name, item.Route!)
                    .SetName($"MenuOpens_{item.Name}"));

    protected override void OnAuthenticated()
    {
        _mainMenu = new MainMenuPage(Driver, Timeout);
    }

    [Test]
    [Category("Smoke")]
    public void MainMenuButtonIsDisplayedAfterLogin()
    {
        Assert.That(_mainMenu.IsMenuButtonDisplayed, Is.True);
    }

    [Test]
    [Category("Smoke")]
    public void MainMenuCanBeExpandedAndCollapsed()
    {
        _mainMenu.OpenMenu();
        Assert.That(_mainMenu.IsMenuExpanded, Is.True);

        _mainMenu.CloseMenu();
        Assert.That(_mainMenu.IsMenuExpanded, Is.False);
    }

    [TestCaseSource(nameof(MenuSections))]
    [Category("Smoke")]
    public void MainMenuContainsExpectedSection(string sectionName)
    {
        Assert.That(
            _mainMenu.IsSectionDisplayed(sectionName),
            Is.True,
            $"Main menu section '{sectionName}' is not displayed.");
    }

    [TestCaseSource(nameof(MenuItemNames))]
    [Category("Smoke")]
    public void MainMenuContainsExpectedItem(string itemName)
    {
        Assert.That(
            _mainMenu.IsMenuItemDisplayed(itemName),
            Is.True,
            $"Main menu item '{itemName}' is not displayed.");
    }

    [TestCaseSource(nameof(RoutedMenuItems))]
    [Category("Smoke")]
    public void MainMenuItemCanBeOpened(string itemName, string expectedRoute)
    {
        _mainMenu.OpenMenuItem(itemName, expectedRoute);

        Assert.That(Driver.Url, Does.Contain(expectedRoute).IgnoreCase);
    }

    [Test]
    [Category("Smoke")]
    public void EdoMenuCanBeExpanded()
    {
        _mainMenu.OpenSubmenu("EДО");

        Assert.Multiple(() =>
        {
            foreach (var itemName in EdoMenuItems)
            {
                Assert.That(
                    _mainMenu.IsSubmenuItemDisplayed(itemName),
                    Is.True,
                    $"EДО submenu item '{itemName}' is not displayed.");
            }
        });
    }
}
