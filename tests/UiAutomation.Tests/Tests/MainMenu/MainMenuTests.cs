using NUnit.Framework;
using OpenQA.Selenium;
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

    /// <summary>
    /// Сколько вкладок было открыто до того, как проверка открыла свою.
    /// </summary>
    private int _tabsBeforeOpen;

    /// <summary>
    /// Пункт меню: имя, устойчивая часть маршрута и признак того, что раздел
    /// догрузился, вместе с человеческим названием этого признака для сообщения
    /// об ошибке.
    /// </summary>
    public sealed record MenuItem(
        string Name,
        string Route,
        By ReadyMarker,
        string ReadyMarkerName)
    {
        public MenuItem(string name, string route, By headingMarker)
            : this(name, route, headingMarker, "заголовок раздела")
        {
        }
    }

    /// <summary>
    /// Заголовок раздела — элемент с этим текстом целиком. Сравнение точное:
    /// «Журнал заявок» не должен совпасть с «Журнал заявок АМ».
    /// </summary>
    private static By Heading(string text) =>
        By.XPath($"//*[normalize-space(text())={XPathHelpers.Literal(text)}]");

    /// <summary>Выбор адреса доставки над выдачей — он приезжает последним.</summary>
    private static readonly By AddressPicker =
        By.CssSelector(".dropdown-account-address-text");

    private const string AddressPickerName = "выбор адреса доставки";

    /// <summary>Карточки товаров в выдаче.</summary>
    private static readonly By ProductCards =
        By.CssSelector("searchlist-control a.searchProdCard");

    private const string ProductCardsName = "карточки товаров";

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
    private static readonly MenuItem[] MenuItems =
    [
        new("Деб. заборгованість", "#/app/receivablesList",
            Heading("Дебіторська заборгованість")),
        new("Взаєморозрахунки", "#/app/mutualSettlementsList",
            Heading("Взаєморозрахунки")),
        new("Новий товар", "#/app/simplesearch", AddressPicker, AddressPickerName),
        new("Розпродаж", "#/app/simplesearch", AddressPicker, AddressPickerName),
        new("Вибрані товари", "#/app/simplesearch", ProductCards, ProductCardsName),
        new("Відвантажені товари", "#/app/simplesearch", AddressPicker, AddressPickerName),
        new("Залишки по ВЗ", "#/app/safeStorage",
            Heading("Залишки по відповідальному зберіганню")),
        new("Рахунки", "#/app/basket", Heading("Товари зі складу")),
        new("Видаткові накладні", "#/app/expenseList",
            Heading("Журнал видаткових накладних")),
        new("Податкові накладні", "#/app/taxInvoiceList",
            Heading("Журнал податкових накладних")),
        new("Коригування до податкових накладних", "#/app/taxInvoiceChangeList",
            Heading("Журнал корегування ПН")),
        new("Посилки", "#/app/sendbox", Heading("Журнал посилок")),
        new("Повернення", "#/app/claimsList", Heading("Мої повернення")),
        new("Заявки АМ", "#/app/assortmentMatrixList", Heading("Журнал заявок АМ")),
        new("Облік закупівель", "#/app/purchase", Heading("Облік закупівель")),
        new("Зворотний звʼязок", "#/app/ticket", Heading("Мої відгуки")),
        new("Запити", "#/app/requestList", Heading("Журнал заявок")),
        new("Аукціон", "#/app/auction", Heading("Аукціон")),
        new("Кошик повернень", "#/app/claimsBasket",
            Heading("Повернення якісного товару:"))

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
        MenuItems.Select(item => new TestCaseData(item).SetName($"MenuOpens_{item.Name}"));

    protected override void OnAuthenticated()
    {
        _mainMenu = new MainMenuPage(Driver, Timeout);
        _tabs = new AppTabs(Driver, Timeout);
    }

    /// <summary>
    /// Запоминает, сколько вкладок было до проверки, чтобы уборка знала, что
    /// именно эта проверка открыла.
    /// </summary>
    [SetUp]
    public void RememberOpenTabs() => _tabsBeforeOpen = _tabs.Count;

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
    /// Ручной сценарий: раскрыть меню, нажать пункт, дождаться, пока раздел
    /// догрузится, и закрыть его вкладку.
    /// Ожидаемый результат: пункт уводит на свой маршрут и раздел прогружается
    /// до своего опознавательного элемента. Повторяется для каждого пункта.
    /// </summary>
    /// <remarks>
    /// Смена адреса — ещё не открытый раздел: маршрут переключается сразу,
    /// а содержимое приезжает позже, и всё это время страница закрыта оверлеем
    /// загрузки. Поэтому готовность подтверждают три вещи вместе — адрес,
    /// снятый оверлей вместе с завершёнными запросами и опознавательный элемент
    /// самого раздела из <c>MenuItems</c>.
    ///
    /// Вкладку раздела проверка не требует: часть разделов её не заводит вовсе —
    /// «Взаєморозрахунки» открывается без вкладки. Поэтому вкладка только
    /// закрывается, и только если появилась.
    ///
    /// Опознавательный элемент у каждого раздела свой. У четырнадцати это
    /// заголовок страницы. У четырёх пунктов, ведущих в общую выдачу
    /// <c>#/app/simplesearch</c>, заголовка нет вовсе, и ждать приходится того,
    /// что приезжает последним: выбора адреса доставки, а у «Вибрані товари» —
    /// карточек товаров, потому что там выдача непустая по определению.
    ///
    /// По случаю на пункт, а не единым обходом: так падение одного пункта видно
    /// по имени прямо в отчёте, а отдельный пункт можно прогнать фильтром —
    /// <c>--filter "FullyQualifiedName~MenuOpens_Заявки"</c>. Вход всё равно один
    /// на всю фикстуру, лишних сессий это не создаёт.
    ///
    /// Дальше опознавательного элемента проверка не идёт: содержимое каждого
    /// раздела — предмет отдельных наборов, которые появятся позже. Здесь
    /// отвечают только на вопрос «работают ли пункты меню».
    ///
    /// Клик ждёт исчезновения <c>block-ui-overlay</c>: пока приложение грузит
    /// предыдущий раздел, нажатие перехватывает оверлей, а не пункт меню.
    ///
    /// Ожидание завершения запросов здесь не для красоты. Пока его не было,
    /// набор стабильно падал на «Заявки АМ»: следующий пункт нажимался, когда
    /// предыдущий раздел ещё догружал данные, и приложение проглатывало переход —
    /// адрес не менялся до самого таймаута. Поодиночке тот же пункт проходил
    /// за секунду, потому что перед ним ничего не грузилось.
    /// </remarks>
    [TestCaseSource(nameof(RoutedMenuItems))]
    [Category("Smoke")]
    public void MenuItemOpensItsSection(MenuItem item)
    {
        _mainMenu.OpenMenuItem(item.Name, item.Route);

        var isSectionReady = Driver.WaitUntilVisible(item.ReadyMarker, Timeout);

        Assert.Multiple(() =>
        {
            Assert.That(
                Driver.Url,
                Does.Contain(item.Route).IgnoreCase,
                $"Пункт «{item.Name}» не открыл свой раздел.");
            Assert.That(
                isSectionReady,
                Is.True,
                $"Раздел «{item.Name}» не догрузился: не появился {item.ReadyMarkerName}.");
        });
    }

    /// <summary>
    /// Закрывает вкладку, открытую проверкой, и убеждается, что панель вернулась
    /// к прежнему числу вкладок.
    /// </summary>
    /// <remarks>
    /// Уборка стоит именно здесь, а не в конце теста: при упавшей проверке тело
    /// теста до закрытия не доходит, и вкладка остаётся. Разбираться потом
    /// приходится не с первой причиной, а с её последствиями.
    ///
    /// Вкладки живут всю сессию и хранятся на сервере, поэтому незакрытая
    /// вкладка достаётся и следующему прогону. На открытие разделов это, как
    /// выяснилось, не влияет — набор проходит и при открытых в другой сессии
    /// разделах, — но панель разрастается и перехватывает клики по тому,
    /// что под ней.
    /// </remarks>
    [TearDown]
    public void CloseSectionTabOpenedByTest()
    {
        if (_tabs.Count <= _tabsBeforeOpen) return;

        _tabs.CloseActive();

        Assert.That(
            _tabs.Count,
            Is.EqualTo(_tabsBeforeOpen),
            "Вкладка раздела осталась открытой и помешает следующим проверкам.");
    }
}
