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

        if (IsAllowed(categories)) return;

        Assert.Fail(
            $"Test '{TestContext.CurrentContext.Test.Name}' is blocked in Production. " +
            $"Only [{TestCategories.ProductionSafe}] tests that do not have " +
            $"[{TestCategories.MutatesUserState}] may run there.");
    }

    internal static bool IsAllowed(IEnumerable<string> categories)
    {
        var categorySet = categories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return categorySet.Contains(TestCategories.ProductionSafe) &&
               !categorySet.Contains(TestCategories.MutatesUserState);
    }
}
