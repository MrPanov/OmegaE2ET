using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;
using UiAutomation.Tests.Pages;

namespace UiAutomation.Tests.Tests.Basket;

public abstract class BasketMutatingTestBase : AuthenticatedUiTestBase
{
    private readonly HashSet<string> _addedCards = new(StringComparer.Ordinal);
    private IReadOnlyList<BasketSelectionState> _originalSelectionStates = [];

    protected BasketPage Basket { get; private set; } = null!;

    [SetUp]
    public void OpenBasketAndRememberState()
    {
        _addedCards.Clear();
        Basket = new BasketPage(Driver, Timeout);
        Basket.Open(Settings.BaseUrl);
        _originalSelectionStates = Basket.SelectionStatesByCard;
    }

    protected void AddTrackedProduct(string cardNumber)
    {
        Assert.That(
            Basket.HasProduct(cardNumber),
            Is.False,
            $"Precondition failed: basket already contains test card '{cardNumber}'. " +
            "The test will not remove a product it did not add.");

        _addedCards.Add(cardNumber);
        Basket.AddProduct(cardNumber);
    }

    [TearDown]
    public void RestoreBasketState()
    {
        var failures = new List<string>();

        try
        {
            Basket.Open(Settings.BaseUrl);
        }
        catch (Exception exception)
        {
            var openFailure = $"Basket cleanup could not open the basket: {exception.Message}";
            if (TestContext.CurrentContext.Result.Outcome.Status ==
                NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                TestContext.Error.WriteLine(openFailure);
                return;
            }

            Assert.Fail(openFailure);
            return;
        }

        foreach (var card in _addedCards)
        {
            try
            {
                Basket.RemoveProduct(card);
            }
            catch (Exception exception)
            {
                failures.Add($"{card}: {exception.Message}");
            }
        }

        try
        {
            Basket.RestoreSelectionStates(_originalSelectionStates);
        }
        catch (Exception exception)
        {
            failures.Add($"selection: {exception.Message}");
        }

        if (failures.Count == 0) return;

        var message = "Basket cleanup failed: " + string.Join(" | ", failures);
        if (TestContext.CurrentContext.Result.Outcome.Status ==
            NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            TestContext.Error.WriteLine(message);
            return;
        }

        Assert.Fail(message);
    }
}
