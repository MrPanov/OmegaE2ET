using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests.MainMenu;

/// <summary>
/// Главное меню авторизованной части: кнопка меню, раскрытие и сворачивание,
/// состав разделов и пунктов, переход по каждому пункту и подменю ЕДО.
/// Шесть методов дают 49 проверок — по одной на каждый пункт эталонного состава.
/// </summary>
/// <remarks>
/// Набор помечен <see cref="TestCategories.ProductionSafe"/>: он только читает
/// разметку и ходит по ссылкам, ничего не создавая и не меняя. Поэтому он же
/// служит проверкой того, что боевой сервер вообще отвечает.
///
/// Одна сессия на всю фикстуру (<c>SingleInstance</c>): 49 отдельных входов
/// на сайт сервер не выдерживает. Из-за этого проверки идут по одному и тому же
/// браузеру, и каждая начинается с открытия меню — предыдущая могла увести
/// на другую страницу или свернуть панель.
/// </remarks>
[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("MainMenu")]
[Category(TestCategories.ProductionSafe)]
public sealed class MainMenuTests : AuthenticatedUiTestFixture
{
    private MainMenuPage _mainMenu = null!;

    /// <summary>Заголовки, на которые разбито меню.</summary>
    private static readonly string[] MenuSections =
    [
        "Звіти",
        "Журнали",
        "Інше"
    ];

    /// <summary>
    /// Эталонный состав меню: имя пункта и маршрут, на который он ведёт.
    /// <c>null</c> вместо маршрута — у пункта нет ссылки, он только раскрывает
    /// подменю, поэтому такой пункт проверяется на присутствие, но не на переход.
    /// </summary>
    /// <remarks>
    /// «EДО» написано латинской <c>E</c> и кириллическими <c>ДО</c> — так оно
    /// стоит в разметке сайта. Поиск идёт по точному совпадению текста ссылки,
    /// поэтому «правка опечатки» на однородную кириллицу сломает и этот пункт,
    /// и раскрытие подменю.
    ///
    /// Четыре пункта ведут на один и тот же <c>#/app/simplesearch</c> и по
    /// маршруту неразличимы. Их различает <c>MainMenuPage</c>: перед кликом он
    /// сверяет href самой ссылки, поэтому пункт, ведущий не туда, виден и здесь.
    /// </remarks>
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

    /// <summary>Пункты, которые обязаны появиться после раскрытия «EДО».</summary>
    private static readonly string[] EdoMenuItems =
    [
        "Підписати ЕЦП",
        "Підключення до ЕДО",
        "Проблема з ЕДО",
        "Запитання ЕДО"
    ];

    /// <summary>
    /// Все пункты меню для проверки присутствия. Имя случая задаётся вручную,
    /// иначе в отчёте они различаются только номером аргумента.
    /// </summary>
    public static IEnumerable<TestCaseData> MenuItemNames =>
        MenuItems.Select(item =>
            new TestCaseData(item.Name).SetName($"MenuContains_{item.Name}"));

    /// <summary>
    /// Только пункты со ссылкой — те, по которым есть куда переходить.
    /// </summary>
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

    /// <summary>
    /// Ручной сценарий: войти на сайт и осмотреть шапку.
    /// Ожидаемый результат: кнопка главного меню на месте и доступна для нажатия.
    /// </summary>
    /// <remarks>
    /// Проверка идёт первой намеренно: без этой кнопки меню не открыть, и все
    /// остальные 48 проверок упали бы на ожидании разметки, не объясняя причины.
    /// </remarks>
    [Test]
    [Category("Smoke")]
    public void MainMenuButtonIsDisplayedAfterLogin()
    {
        Assert.That(_mainMenu.IsMenuButtonDisplayed, Is.True);
    }

    /// <summary>
    /// Ручной сценарий: нажать кнопку меню, затем нажать её ещё раз.
    /// Ожидаемый результат: панель раскрывается и сворачивается обратно.
    /// </summary>
    /// <remarks>
    /// Раскрытость определяется по видимости первого пункта, а не по классам
    /// панели: панель остаётся в разметке и свёрнутой, а пункт исчезает.
    /// </remarks>
    [Test]
    [Category("Smoke")]
    public void MainMenuCanBeExpandedAndCollapsed()
    {
        _mainMenu.OpenMenu();
        Assert.That(_mainMenu.IsMenuExpanded, Is.True);

        _mainMenu.CloseMenu();
        Assert.That(_mainMenu.IsMenuExpanded, Is.False);
    }

    /// <summary>
    /// Ручной сценарий: раскрыть меню и найти в нём заголовок раздела.
    /// Ожидаемый результат: раздел показан. Повторяется для «Звіти», «Журнали»
    /// и «Інше».
    /// </summary>
    [TestCaseSource(nameof(MenuSections))]
    [Category("Smoke")]
    public void MainMenuContainsExpectedSection(string sectionName)
    {
        Assert.That(
            _mainMenu.IsSectionDisplayed(sectionName),
            Is.True,
            $"Main menu section '{sectionName}' is not displayed.");
    }

    /// <summary>
    /// Ручной сценарий: раскрыть меню и найти в нём пункт.
    /// Ожидаемый результат: пункт показан и доступен для нажатия. Повторяется
    /// для всех 22 пунктов эталонного состава.
    /// </summary>
    /// <remarks>
    /// Проверка отделена от перехода: пропавший пункт и пункт, ведущий не туда, —
    /// разные дефекты, и по имени упавшего случая должно быть видно, какой из них.
    /// </remarks>
    [TestCaseSource(nameof(MenuItemNames))]
    [Category("Smoke")]
    public void MainMenuContainsExpectedItem(string itemName)
    {
        Assert.That(
            _mainMenu.IsMenuItemDisplayed(itemName),
            Is.True,
            $"Main menu item '{itemName}' is not displayed.");
    }

    /// <summary>
    /// Ручной сценарий: раскрыть меню и нажать пункт.
    /// Ожидаемый результат: приложение переходит по маршруту этого пункта.
    /// Повторяется для всех 21 пункта со ссылкой.
    /// </summary>
    /// <remarks>
    /// Перед кликом <c>MainMenuPage</c> сверяет href ссылки с ожидаемым маршрутом
    /// и падает с отдельным сообщением, если они разошлись. Без этого четыре
    /// пункта, ведущие на общий <c>#/app/simplesearch</c>, прошли бы проверку
    /// даже перепутанными между собой — по адресу они неразличимы.
    ///
    /// Клик ждёт исчезновения оверлея <c>block-ui-overlay</c>: пока приложение
    /// грузит предыдущий раздел, нажатие перехватывается им, а не пунктом меню.
    /// </remarks>
    [TestCaseSource(nameof(RoutedMenuItems))]
    [Category("Smoke")]
    public void MainMenuItemCanBeOpened(string itemName, string expectedRoute)
    {
        _mainMenu.OpenMenuItem(itemName, expectedRoute);

        Assert.That(Driver.Url, Does.Contain(expectedRoute).IgnoreCase);
    }

    /// <summary>
    /// Ручной сценарий: раскрыть меню и нажать «EДО».
    /// Ожидаемый результат: пункт раскрывается в подменю, и в нём видны все
    /// четыре вложенных пункта.
    /// </summary>
    /// <remarks>
    /// Раскрытость берётся из <c>aria-expanded</c> самого пункта, поэтому уже
    /// раскрытое подменю не закрывается повторным кликом.
    ///
    /// Все четыре пункта проверяются одним <c>Assert.Multiple</c>, а не
    /// отдельными случаями: подменю раскрывается один раз, и разбивать это
    /// на четыре прохода означало бы четыре лишних открытия меню.
    /// </remarks>
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
