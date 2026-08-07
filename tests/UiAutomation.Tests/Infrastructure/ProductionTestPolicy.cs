using NUnit.Framework;
using UiAutomation.Tests.Configuration;

namespace UiAutomation.Tests.Infrastructure;

internal static class ProductionTestPolicy
{
    public static void EnsureCurrentTestIsAllowed(TestSettings settings)
    {
        if (!settings.IsProduction) return;

        var categories = TestContext.CurrentContext.Test.Properties["Category"]
            .Select(value => value?.ToString() ?? string.Empty)
            .ToArray();

        if (IsAllowed(categories, settings.AllowProductionMutations)) return;

        Assert.Fail(
            $"Test '{TestContext.CurrentContext.Test.Name}' is blocked in Production. " +
            $"Use [{TestCategories.ProductionSafe}] for read-only tests or " +
            $"[{TestCategories.ProductionTestClient}] with " +
            $"ALLOW_PRODUCTION_MUTATIONS=true for controlled test-client changes. " +
            $"[{TestCategories.ProductionBlocked}] tests never run in Production.");
    }

    internal static bool IsAllowed(
        IEnumerable<string> categories,
        bool allowProductionMutations = false)
    {
        var categorySet = categories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (categorySet.Contains(TestCategories.ProductionBlocked)) return false;

        var mutatesUserState = categorySet.Contains(TestCategories.MutatesUserState);
        if (categorySet.Contains(TestCategories.ProductionSafe) && !mutatesUserState)
        {
            return true;
        }

        return allowProductionMutations &&
               mutatesUserState &&
               categorySet.Contains(TestCategories.ProductionTestClient);
    }
}
