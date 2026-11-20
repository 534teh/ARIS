using AdaptiveUiRecovery.Core.Interception;

namespace AdaptiveUiRecovery.Core.Pipeline;

/// <summary>
/// Contract for diagnosing failed locator actions and proposing verified replacement selectors.
/// </summary>
public interface ILocatorRecoveryPipeline
{
    Task<HealingResult?> TryRecoverAsync(
        RecoveryRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight delegate-backed recovery pipeline useful for integration testing and staged extensions.
/// </summary>
public sealed class DelegateRecoveryPipeline : ILocatorRecoveryPipeline
{
    private readonly Func<RecoveryRequest, CancellationToken, Task<HealingResult?>> _handler;

    public DelegateRecoveryPipeline(
        Func<RecoveryRequest, CancellationToken, Task<HealingResult?>> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public DelegateRecoveryPipeline(Func<RecoveryRequest, HealingResult?> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = (request, _) => Task.FromResult(handler(request));
    }

    public DelegateRecoveryPipeline(
        Func<CallerContext, string, Exception, CancellationToken, Task<HealingResult?>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = (request, ct) => handler(request.Caller, request.FailedSelector, request.Failure, ct);
    }

    public DelegateRecoveryPipeline(Func<CallerContext, string, Exception, HealingResult?> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = (request, _) => Task.FromResult(handler(request.Caller, request.FailedSelector, request.Failure));
    }

    public Task<HealingResult?> TryRecoverAsync(
        RecoveryRequest request,
        CancellationToken cancellationToken = default)
        => _handler(request, cancellationToken);
}
