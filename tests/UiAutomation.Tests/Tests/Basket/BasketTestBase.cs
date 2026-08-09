using NUnit.Framework;
using OpenQA.Selenium;
using UiAutomation.Tests.Configuration;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests.Basket;

/// <summary>
/// База для сценариев корзины. Браузер и вход общие на весь набор — их держит
/// <see cref="BasketSession"/>; здесь остаются только те проверки и хуки,
/// которые обязаны выполняться для каждого теста отдельно.
/// </summary>
public abstract class BasketTestBase
{
    protected IWebDriver Driver => BasketSession.Driver;

    protected TestSettings Settings => BasketSession.Settings;

    protected TimeSpan Timeout => BasketSession.Timeout;

    /// <summary>Корзина, открытая заново перед каждым тестом.</summary>
    protected BasketPage Basket { get; private set; } = null!;

    /// <summary>
    /// Политика Production смотрит категории текущего теста, поэтому проверяется
    /// на каждом тесте, а не однократно при входе.
    /// </summary>
    [SetUp]
    public void PrepareTest()
    {
        TestContext.Out.WriteLine(
            $"E2E environment: {Settings.EnvironmentName}; Base URL: {Settings.BaseUrl}");
        ProductionTestPolicy.EnsureCurrentTestIsAllowed(Settings);

        Basket = new BasketPage(Driver, Timeout);
        OnBasketReady();
    }

    /// <summary>
    /// Вызывается после создания страницы корзины. Сценарии, которым нужна
    /// открытая корзина, открывают её здесь; остальные — нет.
    /// </summary>
    protected virtual void OnBasketReady() => Basket.Open(Settings.BaseUrl);

    [TearDown]
    public void SaveScreenshotWhenTestFails() => Screenshots.SaveWhenCurrentTestFailed(Driver);
}
