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
}
