using OpenQA.Selenium;

namespace UiAutomation.Tests.Infrastructure;

internal sealed class DomMutationTracker(IWebDriver driver)
{
    private const string EnsureObserverScript = """
        if (!window.__omegaE2eMutationObserver) {
            window.__omegaE2eMutationVersion = 0;
            window.__omegaE2eMutationObserver = new MutationObserver(function () {
                window.__omegaE2eMutationVersion++;
            });
            window.__omegaE2eMutationObserver.observe(document.body, {
                subtree: true,
                childList: true,
                characterData: true,
                attributes: true
            });
        }
        return window.__omegaE2eMutationVersion;
        """;

    public long Snapshot() => Convert.ToInt64(
        ((IJavaScriptExecutor)driver).ExecuteScript(EnsureObserverScript),
        System.Globalization.CultureInfo.InvariantCulture);

    public bool HasChangedSince(long version) => Snapshot() > version;
}
