namespace AdaptiveUiRecovery.Core.Interception;

/// <summary>
/// Controls runtime behavior of healable locator interception.
/// </summary>
public enum HealMode
{
    Off,
    Record,
    Heal
}

/// <summary>
/// Runtime configuration for locator interception and fast-fail polling.
/// </summary>
public sealed record InterceptionOptions
{
    public int FastFailTimeoutMs { get; init; } = 1500;

    public HealMode Mode { get; init; } = HealMode.Heal;

    /// <summary>
    /// Reads interception settings from <c>HEAL_MODE</c> and <c>HEAL_FAST_FAIL_MS</c> environment variables.
    /// </summary>
    public static InterceptionOptions FromEnvironment()
    {
        string? rawMode = Environment.GetEnvironmentVariable("HEAL_MODE");
        HealMode mode = rawMode?.Trim().ToUpperInvariant() switch
        {
            "OFF" => HealMode.Off,
            "RECORD" => HealMode.Record,
            _ => HealMode.Heal
        };

        int fastFailMs = 1500;
        string? rawTimeout = Environment.GetEnvironmentVariable("HEAL_FAST_FAIL_MS");
        if (int.TryParse(rawTimeout, out int parsed) && parsed > 0)
        {
            fastFailMs = parsed;
        }

        return new InterceptionOptions
        {
            Mode = mode,
            FastFailTimeoutMs = fastFailMs
        };
    }
}
