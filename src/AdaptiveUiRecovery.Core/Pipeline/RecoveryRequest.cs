using AdaptiveUiRecovery.Core.Interception;

namespace AdaptiveUiRecovery.Core.Pipeline;

/// <summary>
/// Context passed to <see cref="ILocatorRecoveryPipeline"/> when a locator action or assertion fails stabilization.
/// </summary>
public sealed record RecoveryRequest(
    CallerContext Caller,
    string FailedSelector,
    string ActionName,
    int DomMatchCount,
    Exception Failure);
