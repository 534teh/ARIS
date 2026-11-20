namespace AdaptiveUiRecovery.Core.Pipeline;

/// <summary>
/// Result produced by the locator recovery pipeline after diagnosing and relocating a broken UI element.
/// </summary>
public sealed record HealingResult(
    string HealedSelector,
    double Confidence,
    string Tier,
    TimeSpan PureHealDuration,
    string Reasoning);
