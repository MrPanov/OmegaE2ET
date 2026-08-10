using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests.MainMenu;

/// <summary>
/// Главное меню авторизованной части: кнопка меню, раскрытие и сворачивание,
/// состав разделов и пунктов, переход по каждому пункту.
/// </summary>
/// <remarks>
/// Набор помечен <see cref="TestCategories.ProductionSafe"/>: он только читает
/// разметку и ходит по ссылкам, ничего не создавая и не меняя. Поэтому он же
/// служит проверкой того, что боевой сервер вообще отвечает.
///
/// Одна сессия на всю фикстуру (<c>SingleInstance</c>): отдельный вход на каждую
/// проверку сервер не выдерживает. Из-за этого проверки идут по одному и тому же
/// браузеру, и каждая начинается с открытия меню — предыдущая могла увести
/// на другую страницу или свернуть панель.
///
/// Пункты проверяются оптом, а не по случаю на каждый: это общая проверка того,
/// что меню в целом работает. Разбор отдельных разделов появится собственными
/// наборами, когда до них дойдут руки, и там проверять будут уже содержимое
/// открывшейся страницы, а не сам факт перехода.
/// </remarks>
[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("MainMenu")]
[Category(TestCategories.ProductionSafe)]
public sealed class MainMenuTests : AuthenticatedUiTestFixture
{
    private MainMenuPage _mainMenu = null!;
    private AppTabs _tabs = null!;

    /// <summary>Заголовки, на которые разбито меню.</summary>
    /// <remarks>
    /// В разметке есть и четвёртый заголовок, «Адмін», но он и его пункты
    /// скрыты для обычной учётной записи, поэтому в эталон не входят.
    /// </remarks>
    private static readonly string[] MenuSections =
    [
        "Звіти",
        "Журнали",
        "Інше"
    ];

    /// <summary>
    /// Эталонный состав меню: имя пункта и маршрут, на который он ведёт.
    /// Закомментированные строки — пункты, которые в меню есть, но проверять их
    /// решено не здесь; состав оставлен записанным, чтобы их не пришлось искать
    /// заново.
    /// </summary>
    /// <remarks>
    /// Список ограничен тем, что видит учётная запись прогона. В самой панели
    /// лежит вдвое больше ссылок: раздел «Адмін» (24 пункта), подменю «Autodoc»
    /// (6 пунктов), «Ваші акції» <c>#/app/bonus</c>, «Звіт та донати козака»
    /// <c>#/app/donationReport</c>, «Редактор The power of motion»
    /// <c>#/app/powerEditor</c>. Все они есть в разметке, но скрыты по правам —
    /// проверено на живом сайте под учётной записью прогона. Поэтому обход идёт
    /// по списку, а не по всем ссылкам панели подряд: иначе набор ломился бы
    /// в чужие страницы и падал бы на правах, а не на дефектах меню.
    ///
    /// Под учётной записью с большими правами эти пункты видны. Если такой набор
    /// понадобится, заводить его надо отдельной фикстурой со своим списком,
    /// а не расширением этого: здесь состав обязан совпадать с правами того,
    /// кем ходит прогон.
    ///
    /// Маршруты записаны без UUID: у половины пунктов href выглядит как
    /// <c>#/app/receivablesList/06a8f93c-…</c>, и этот UUID меняется от загрузки
    /// к загрузке. Проверяется только устойчивая часть.
    ///
    /// Четыре пункта ведут на один и тот же <c>#/app/simplesearch</c> и по
    /// маршруту неразличимы. Их различает <c>MainMenuPage</c>: перед кликом он
    /// сверяет href самой ссылки, поэтому пункт, ведущий не туда, виден и здесь.
    /// </remarks>
    private static readonly (string Name, string Route)[] MenuItems =
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
        ("Кошик повернень", "#/app/claimsBasket")

        // Проверять здесь не нужно — решение владельца набора.
        // ("Прайс-листи", "#/app/prices"),
        // ("Документи", "#/app/documents"),
        //
        // «EДО» ссылки не имеет вовсе: это dropdown-toggle с javascript:void(0);,
        // раскрывающий четыре вложенных пункта — «Підписати ЕЦП»,
        // «Підключення до ЕДО», «Проблема з ЕДО», «Запитання ЕДО». У них тоже
        // нет href, переход вешает обработчик. Написано латинской E
        // и кириллическими ДО — так стоит в разметке сайта.
        // ("EДО", null)
    ];

    /// <summary>
    /// Пункты для обхода, по случаю на каждый. Имя случая задаётся вручную:
    /// иначе они различаются в отчёте только номером аргумента, и отдельный
    /// пункт не запустить фильтром.
    /// </summary>
    public static IEnumerable<TestCaseData> RoutedMenuItems =>
        MenuItems.Select(item =>
            new TestCaseData(item.Name, item.Route).SetName($"MenuOpens_{item.Name}"));

    protected override void OnAuthenticated()
    {
        _mainMenu = new MainMenuPage(Driver, Timeout);
        _tabs = new AppTabs(Driver, Timeout);
    }

    /// <summary>
    /// Ручной сценарий: войти на сайт и осмотреть шапку.
    /// Ожидаемый результат: кнопка главного меню на месте и доступна для нажатия.
    /// </summary>
    /// <remarks>
    /// Проверка идёт первой намеренно: без этой кнопки меню не открыть, и все
    /// остальные упали бы на ожидании разметки, не объясняя причины.
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
    /// панели: свёрнутая панель остаётся в разметке, у неё лишь добавляется
    /// <c>ng-hide</c>, а пункты перестают отрисовываться.
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
    /// Ручной сценарий: раскрыть меню и сверить его состав с эталонным.
    /// Ожидаемый результат: показаны все три раздела и все 19 проверяемых пунктов.
    /// </summary>
    /// <remarks>
    /// Присутствие отделено от перехода: пропавший пункт и пункт, ведущий не
    /// туда, — разные дефекты, и по тому, какая из двух проверок упала, сразу
    /// видно, какой именно.
    /// </remarks>
    [Test]
    [Category("Smoke")]
    public void MainMenuShowsEverySectionAndItem()
    {
        var missingSections = MenuSections
            .Where(section => !_mainMenu.IsSectionDisplayed(section))
            .ToArray();
        var missingItems = MenuItems
            .Where(item => !_mainMenu.IsMenuItemDisplayed(item.Name))
            .Select(item => item.Name)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                missingSections,
                Is.Empty,
                $"Разделы меню не показаны: {string.Join(", ", missingSections)}.");
            Assert.That(
                missingItems,
                Is.Empty,
                $"Пункты меню не показаны: {string.Join(", ", missingItems)}.");
        });
    }

    /// <summary>
    /// Ручной сценарий: раскрыть меню, нажать пункт, убедиться, что открылся его
    /// раздел, и закрыть вкладку раздела.
    /// Ожидаемый результат: пункт уводит на свой маршрут, а его вкладка после
    /// закрытия исчезает из панели. Повторяется для каждого проверяемого пункта.
    /// </summary>
    /// <remarks>
    /// По случаю на пункт, а не единым обходом: так падение одного пункта видно
    /// по имени прямо в отчёте, а отдельный пункт можно прогнать фильтром —
    /// <c>--filter "FullyQualifiedName~MenuOpens_Заявки"</c>. Вход всё равно один
    /// на всю фикстуру, лишних сессий это не создаёт.
    ///
    /// Проверка того, что открылось, ограничена маршрутом: содержимое каждого
    /// раздела — предмет отдельных наборов, которые появятся позже. Здесь
    /// отвечают только на вопрос «работают ли пункты меню».
    ///
    /// Клик ждёт исчезновения <c>block-ui-overlay</c>: пока приложение грузит
    /// предыдущий раздел, нажатие перехватывает оверлей, а не пункт меню.
    ///
    /// Вкладка закрывается сразу после проверки и её закрытие сверяется по числу
    /// вкладок. Приложение держит вкладки на сервере, привязанными к учётной
    /// записи: незакрытая вкладка переживает не только соседние проверки,
    /// но и весь прогон, и следующий прогон споткнётся уже об неё. Проверено
    /// на живом сайте — раздел, открытый в чужой сессии того же пользователя,
    /// в сессии прогона не открывается вовсе, и ожидание адреса выходит
    /// по таймауту.
    /// </remarks>
    [TestCaseSource(nameof(RoutedMenuItems))]
    [Category("Smoke")]
    public void MenuItemOpensItsSection(string itemName, string expectedRoute)
    {
        var tabsBeforeOpen = _tabs.Count;

        _mainMenu.OpenMenuItem(itemName, expectedRoute);

        Assert.That(
            Driver.Url,
            Does.Contain(expectedRoute).IgnoreCase,
            $"Пункт «{itemName}» не открыл свой раздел.");

        _tabs.CloseActive();

        Assert.That(
            _tabs.Count,
            Is.EqualTo(tabsBeforeOpen),
            $"Вкладка раздела «{itemName}» осталась открытой.");
    }
}
