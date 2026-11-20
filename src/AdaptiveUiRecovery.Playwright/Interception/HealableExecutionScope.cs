using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using AdaptiveUiRecovery.Core.Interception;
using AdaptiveUiRecovery.Core.Pipeline;

namespace AdaptiveUiRecovery.Playwright.Interception;

/// <summary>
/// Ambient test-scoped context connecting <see cref="HealableLocatorExtensions.Heal"/> calls to active options and recovery pipelines.
/// </summary>
public sealed class HealableExecutionScope : IDisposable
{
    private static readonly AsyncLocal<HealableExecutionScope?> CurrentScope = new();

    private readonly ConcurrentDictionary<string, HealingResult> _healedLocators = new(StringComparer.Ordinal);
    private readonly HealableExecutionScope? _previous;
    private bool _disposed;

    public HealableExecutionScope(InterceptionOptions? options = null, ILocatorRecoveryPipeline? pipeline = null)
    {
        Options = options ?? InterceptionOptions.FromEnvironment();
        Pipeline = pipeline;
        _previous = CurrentScope.Value;
        CurrentScope.Value = this;
    }

    public static HealableExecutionScope? Current => CurrentScope.Value;

    public InterceptionOptions Options { get; set; }

    public ILocatorRecoveryPipeline? Pipeline { get; set; }

    public IReadOnlyDictionary<string, HealingResult> HealedLocators => _healedLocators;

    public bool TryGetCachedHeal(CallerContext caller, [NotNullWhen(true)] out HealingResult? result)
        => _healedLocators.TryGetValue(caller.DisambiguatedKey, out result);

    public void RecordHeal(CallerContext caller, HealingResult result)
        => _healedLocators[caller.DisambiguatedKey] = result;

    public static HealableExecutionScope EnsureScope() => CurrentScope.Value ?? new HealableExecutionScope();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CurrentScope.Value = _previous;
        _disposed = true;
    }
}
