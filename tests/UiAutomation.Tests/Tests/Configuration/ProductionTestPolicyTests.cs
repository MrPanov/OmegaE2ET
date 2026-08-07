using NUnit.Framework;
using UiAutomation.Tests.Infrastructure;

namespace UiAutomation.Tests.Tests.Configuration;

[TestFixture]
[Category("Unit")]
public sealed class ProductionTestPolicyTests
{
    [Test]
    public void ProductionSafeCategoryIsAllowed() =>
        Assert.That(
            ProductionTestPolicy.IsAllowed([TestCategories.ProductionSafe]),
            Is.True);

    [Test]
    public void MissingProductionSafeCategoryIsBlocked() =>
        Assert.That(ProductionTestPolicy.IsAllowed(["Smoke"]), Is.False);

    [Test]
    public void MutatingCategoryOverridesProductionSafeCategory() =>
        Assert.That(
            ProductionTestPolicy.IsAllowed(
                [TestCategories.ProductionSafe, TestCategories.MutatesUserState]),
            Is.False);

    [Test]
    public void TestClientMutationRequiresExplicitConfirmation() =>
        Assert.That(
            ProductionTestPolicy.IsAllowed(
                [TestCategories.ProductionTestClient, TestCategories.MutatesUserState]),
            Is.False);

    [Test]
    public void ConfirmedTestClientMutationIsAllowed() =>
        Assert.That(
            ProductionTestPolicy.IsAllowed(
                [TestCategories.ProductionTestClient, TestCategories.MutatesUserState],
                allowProductionMutations: true),
            Is.True);

    [Test]
    public void TestClientCategoryWithoutMutationMarkerIsBlocked() =>
        Assert.That(
            ProductionTestPolicy.IsAllowed(
                [TestCategories.ProductionTestClient],
                allowProductionMutations: true),
            Is.False);

    [Test]
    public void ProductionBlockedOverridesEveryConfirmation() =>
        Assert.That(
            ProductionTestPolicy.IsAllowed(
                [
                    TestCategories.ProductionSafe,
                    TestCategories.ProductionTestClient,
                    TestCategories.MutatesUserState,
                    TestCategories.ProductionBlocked
                ],
                allowProductionMutations: true),
            Is.False);
}
