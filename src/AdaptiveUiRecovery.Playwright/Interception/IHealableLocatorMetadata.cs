using AdaptiveUiRecovery.Core.Interception;
using AdaptiveUiRecovery.Core.Pipeline;
using Microsoft.Playwright;

namespace AdaptiveUiRecovery.Playwright.Interception;

/// <summary>
/// Exposes caller metadata, native locator resolution, and healing telemetry on a healable <see cref="ILocator"/> proxy.
/// </summary>
public interface IHealableLocatorMetadata
{
    CallerContext Caller { get; }

    ILocator RawLocator { get; }

    ILocator CurrentNativeLocator { get; }

    bool WasHealed { get; }

    HealingResult? LastHealingResult { get; }

    double? LastInterceptionElapsedMs { get; }

    Task<ILocator> ResolveNativeLocatorAsync();
}
