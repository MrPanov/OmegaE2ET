using NUnit.Framework;
using UiAutomation.Tests.Configuration;

namespace UiAutomation.Tests.Infrastructure;

internal static class ProductionTestPolicy
{
    public static void EnsureCurrentTestIsAllowed(TestSettings settings)
    {
        if (!settings.IsProduction) return;

        var categories = CurrentCategories();

        if (IsAllowed(categories)) return;

        Assert.Fail(
            $"Test '{TestContext.CurrentContext.Test.Name}' is blocked in Production. " +
            $"Use [{TestCategories.ProductionSafe}] for read-only tests or " +
            $"[{TestCategories.ProductionTestClient}] for controlled test-client changes. " +
            $"[{TestCategories.ProductionBlocked}] tests never run in Production. " +
            $"Detected categories: [{string.Join(", ", categories)}].");
    }

    private static string[] CurrentCategories()
    {
        var test = TestContext.CurrentContext.Test;
        var categories = test.Properties["Category"]
            .Select(value => value?.ToString() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var fixtureType = typeof(ProductionTestPolicy).Assembly.GetType(test.ClassName ?? string.Empty);
        if (fixtureType is null) return categories.ToArray();

        AddCategories(fixtureType.GetCustomAttributes(typeof(CategoryAttribute), inherit: true));
        if (!string.IsNullOrWhiteSpace(test.MethodName))
        {
            foreach (var method in fixtureType.GetMethods()
                         .Where(candidate => candidate.Name == test.MethodName))
            {
                AddCategories(method.GetCustomAttributes(typeof(CategoryAttribute), inherit: true));
            }
        }

        return categories.ToArray();

        void AddCategories(IEnumerable<object> attributes)
        {
            foreach (var attribute in attributes.Cast<CategoryAttribute>())
            {
                categories.Add(attribute.Name);
            }
        }
    }

    internal static bool IsAllowed(IEnumerable<string> categories)
    {
        var categorySet = categories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (categorySet.Contains(TestCategories.ProductionBlocked)) return false;

        var mutatesUserState = categorySet.Contains(TestCategories.MutatesUserState);
        if (categorySet.Contains(TestCategories.ProductionSafe) && !mutatesUserState)
        {
            return true;
        }

        return mutatesUserState &&
               categorySet.Contains(TestCategories.ProductionTestClient);
    }
}
