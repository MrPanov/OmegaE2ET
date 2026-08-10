using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests;

/// <summary>
/// Поиск по строке в шапке: по каким идентификаторам находится эталонный товар
/// и куда уводят запросы по VIN и государственному номеру.
/// </summary>
/// <remarks>
/// Сервер ограничивает частоту поиска и при превышении отклоняет запросы целиком,
/// поэтому набор намеренно экономный: один поиск на сценарий, семь на весь прогон.
/// Интервал между запросами выдерживает <c>SearchResultsPage</c> по настройке
/// <c>SEARCH_MIN_INTERVAL_SECONDS</c> — на тестовой среде это 5 секунд, то есть
/// не более двенадцати запросов в минуту при разрешённых тридцати.
/// </remarks>
[TestFixture]
[NonParallelizable]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
[Category("Search")]
[Category(TestCategories.ProductionTestClient)]
[Category(TestCategories.MutatesUserState)]
public sealed class SearchTests : AuthenticatedUiTestFixture
{
    private SearchResultsPage _search = null!;
    private VinSearchPage _vin = null!;

    protected override void OnAuthenticated()
    {
        Assert.That(
            Settings.SearchData.IsConfigured,
            Is.True,
            $"Search reference data are not configured for environment '{Settings.EnvironmentName}'.");
        _search = new SearchResultsPage(
            Driver,
            Timeout,
            TimeSpan.FromSeconds(Settings.SearchMinimumIntervalSeconds));
        _vin = new VinSearchPage(Driver, Timeout);
    }

    /// <summary>
    /// Общее предусловие: открыть сайт и дождаться готовности главной страницы.
    /// Строка поиска очищается, режим «починається з» выключается.
    /// </summary>
    [SetUp]
    public void OpenHomePage() => _search.Reset(Settings.BaseUrl);

    /// <summary>
    /// Ручной сценарий: открыть сайт после авторизации.
    /// Ожидаемый результат: главная страница загрузилась, строка поиска доступна
    /// и показывает подсказку.
    /// </summary>
    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-001")]
    public void HomePageIsReadyForSearch()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_search.IsInputUsable, Is.True, "Строка поиска недоступна.");
            Assert.That(_search.Query, Is.Empty);
            Assert.That(_search.SearchPlaceholder, Is.EqualTo(Settings.SearchData.SearchPlaceholder));
            Assert.That(_search.IsStartsWithEnabled, Is.False);
        });
    }

    /// <summary>
    /// Ручной сценарий: ввести номер карточки товара и нажать Enter.
    /// Ожидаемый результат: найден ровно один товар — эталонный, с ожидаемым кодом.
    /// </summary>
    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-002")]
    public void ProductCardFindsTheReferenceProduct()
    {
        _search.Search(Settings.SearchData.ProductCard);
        var product = _search.GetProduct(Settings.SearchData.ProductCode);

        Assert.Multiple(() =>
        {
            Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по картці: 1"));
            Assert.That(product.Code, Is.EqualTo(Settings.SearchData.ProductCode));
            Assert.That(product.Card, Is.EqualTo(Settings.SearchData.ProductCard));
        });
    }

    /// <summary>
    /// Ручной сценарий: ввести каталожный код товара и нажать Enter.
    /// Ожидаемый результат: найден эталонный товар со всеми реквизитами.
    /// </summary>
    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-003")]
    public void ProductCodeFindsTheReferenceProduct()
    {
        _search.Search(Settings.SearchData.ProductCode);
        var product = _search.GetProduct(Settings.SearchData.ProductCode);

        Assert.Multiple(() =>
        {
            Assert.That(_search.ResultSummary, Is.EqualTo("Знайдено по коду: 1"));
            Assert.That(product.Card, Is.EqualTo(Settings.SearchData.ProductCard));
            Assert.That(product.Description, Is.EqualTo(Settings.SearchData.ProductDescription));
            Assert.That(product.Brand, Is.EqualTo(Settings.SearchData.ProductBrand));
        });
    }

    /// <summary>
    /// Ручной сценарий: ввести полное наименование товара и нажать Enter.
    /// Ожидаемый результат: поиск идёт по частичному совпадению названия и находит
    /// эталонный товар вместе с его аналогом.
    /// </summary>
    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-004")]
    public void FullDescriptionFindsReferenceProductAndItsAnalogue()
    {
        _search.Search(Settings.SearchData.ProductDescription);

        Assert.Multiple(() =>
        {
            Assert.That(_search.ResultSummary, Does.StartWith("Знайдено по частин"));
            Assert.That(_search.ProductCodes, Does.Contain(Settings.SearchData.ProductCode));
            Assert.That(
                _search.ProductCodes,
                Does.Contain(Settings.SearchData.AlternativeProductCode));
        });
    }

    /// <summary>
    /// Ручной сценарий: ввести часть наименования и нажать Enter.
    /// Ожидаемый результат: выдача содержит эталонный товар и позиции разных
    /// производителей — поиск не сужается до одного бренда.
    /// </summary>
    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-005")]
    public void PartialDescriptionSpansSeveralBrands()
    {
        _search.Search(Settings.SearchData.PartialDescription);

        Assert.Multiple(() =>
        {
            Assert.That(_search.ProductCodes, Does.Contain(Settings.SearchData.ProductCode));
            Assert.That(
                _search.ProductBrands.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                Is.GreaterThan(1));
        });
    }

    /// <summary>
    /// Ручной сценарий: ввести наименование вместе с производителем и нажать Enter.
    /// Ожидаемый результат: выдача сужается до товаров этого производителя.
    /// </summary>
    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-006")]
    public void BrandQueryNarrowsResultsToThatBrand()
    {
        _search.Search(Settings.SearchData.BrandQuery);

        Assert.Multiple(() =>
        {
            Assert.That(_search.ProductBrands, Is.Not.Empty, "Выдача пуста.");
            Assert.That(
                _search.ProductBrands.All(brand => brand.Contains("KNECHT", StringComparison.OrdinalIgnoreCase)),
                Is.True,
                $"Среди брендов есть посторонние: {string.Join(", ", _search.ProductBrands.Distinct())}");
        });
    }

    /// <summary>
    /// Ручной сценарий: ввести VIN автомобиля и нажать Enter.
    /// Ожидаемый результат: приложение уходит в раздел «Пошук за VIN кодом»
    /// и показывает подобранные модификации для этого VIN.
    /// </summary>
    /// <remarks>
    /// Запрос уводит из товарной выдачи, поэтому отправляется без ожидания
    /// результатов поиска — готовность подтверждает <see cref="VinSearchPage"/>.
    /// </remarks>
    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-007")]
    public void VinOpensVehicleModificationsSection()
    {
        RequireVehicleSearch();

        _search.SubmitWithoutWaiting(Settings.SearchData.Vin);
        _vin.WaitUntilOpened();

        Assert.Multiple(() =>
        {
            Assert.That(_vin.IsOpen, Is.True, $"Ожидался переход в {VinSearchPage.Route}.");
            Assert.That(_vin.IsFoundModificationsTitleVisible, Is.True);
            Assert.That(_vin.ShowsVin(Settings.SearchData.Vin), Is.True, "VIN не показан на странице.");
        });
    }

    /// <summary>
    /// Ручной сценарий: ввести государственный номер автомобиля и нажать Enter.
    /// Ожидаемый результат: открывается тот же раздел с модификациями, а строка
    /// поиска заменяется найденным по номеру VIN.
    /// </summary>
    /// <remarks>
    /// Подмена запроса на VIN — главное доказательство того, что номер именно
    /// распознан, а не просто открыл раздел.
    /// </remarks>
    [Test]
    [Category("Smoke")]
    [Category("P0")]
    [Property("TestCaseId", "SEARCH-008")]
    public void LicensePlateResolvesToTheSameVehicle()
    {
        RequireVehicleSearch();

        _search.SubmitWithoutWaiting(Settings.SearchData.LicensePlate);
        _vin.WaitUntilOpened();

        Assert.Multiple(() =>
        {
            Assert.That(_vin.IsOpen, Is.True, $"Ожидался переход в {VinSearchPage.Route}.");
            Assert.That(_vin.ShowsVin(Settings.SearchData.Vin), Is.True, "VIN не показан на странице.");
            Assert.That(
                _search.Query,
                Is.EqualTo(Settings.SearchData.Vin),
                "Строка поиска должна замениться найденным VIN.");
        });
    }

    /// <summary>
    /// Поиск по VIN и государственному номеру работает только на боевом сервере,
    /// поэтому на тестовой среде сценарий помечается пропущенным, а не упавшим.
    /// </summary>
    private void RequireVehicleSearch() => Assume.That(
        Settings.IsProduction,
        Is.True,
        "Поиск по VIN и госномеру доступен только в Production.");
}
