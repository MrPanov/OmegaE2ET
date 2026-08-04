using System.Text.RegularExpressions;

namespace UiAutomation.Tests.Infrastructure;

internal static class UiText
{
    public static string NormalizeWhitespace(string value) =>
        Regex.Replace(value.Trim(), @"\s+", " ");
}
