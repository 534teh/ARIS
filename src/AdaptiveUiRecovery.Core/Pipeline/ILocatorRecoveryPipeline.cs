using AdaptiveUiRecovery.Core.Interception;

namespace AdaptiveUiRecovery.Core.Pipeline;

/// <summary>
/// Contract for diagnosing failed locator actions and proposing verified replacement selectors.
/// </summary>
public interface ILocatorRecoveryPipeline
{
    Task<HealingResult?> TryRecoverAsync(
        CallerContext caller,
        string failedSelector,
        Exception failure,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight delegate-backed recovery pipeline useful for integration testing and staged extensions.
/// </summary>
public sealed class DelegateRecoveryPipeline : ILocatorRecoveryPipeline
{
    private readonly Func<CallerContext, string, Exception, CancellationToken, Task<HealingResult?>> _handler;

    public DelegateRecoveryPipeline(
        Func<CallerContext, string, Exception, CancellationToken, Task<HealingResult?>> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public DelegateRecoveryPipeline(Func<CallerContext, string, Exception, HealingResult?> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = (caller, selector, failure, _) => Task.FromResult(handler(caller, selector, failure));
    }

    public Task<HealingResult?> TryRecoverAsync(
        CallerContext caller,
        string failedSelector,
        Exception failure,
        CancellationToken cancellationToken = default)
        => _handler(caller, failedSelector, failure, cancellationToken);
}
