using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Pages;

/// <summary>
/// Раздел «Пошук за VIN кодом». Запрос по VIN или по государственному номеру
/// уводит из товарной выдачи сюда — на страницу подобранных модификаций автомобиля.
/// </summary>
/// <remarks>
/// Заголовок и сам VIN лежат в разных узлах: подпись «Знайдені модифікації
/// автомобіля по VIN» идёт без двоеточия и без номера, номер отображается рядом.
/// Поэтому проверяются они по отдельности, а не одной строкой.
/// </remarks>
public sealed class VinSearchPage(IWebDriver driver, TimeSpan waitTimeout)
{
    /// <summary>Маршрут раздела. Относительный: на боевом и тестовом он одинаков.</summary>
    public const string Route = "#/app/vin";

    /// <summary>Подпись над списком подобранных модификаций.</summary>
    public const string FoundModificationsTitle = "Знайдені модифікації автомобіля по VIN";

    private static readonly By FoundModificationsBy = By.XPath(
        $"//*[normalize-space(text())={XPathHelpers.Literal(FoundModificationsTitle)}]");

    private readonly WebDriverWait _wait = new(driver, waitTimeout);

    public bool IsOpen => driver.Url.Contains(Route, StringComparison.OrdinalIgnoreCase);

    public bool IsFoundModificationsTitleVisible => driver.IsVisible(FoundModificationsBy);

    /// <summary>Отображается ли на странице указанный VIN.</summary>
    public bool ShowsVin(string vin) => driver.IsVisible(
        By.XPath($"//*[normalize-space(text())={XPathHelpers.Literal(vin)}]"));

    /// <summary>
    /// Ждёт перехода в раздел и появления подписи с подобранными модификациями.
    /// </summary>
    /// <remarks>
    /// Ожидание строится на адресе и подписи, а не на исчезновении оверлея выдачи:
    /// товарные результаты здесь не отрисовываются вовсе.
    /// </remarks>
    public void WaitUntilOpened() =>
        _wait.Until(_ => IsOpen && IsFoundModificationsTitleVisible);
}
