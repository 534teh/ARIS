namespace AdaptiveUiRecovery.Core.Interception;

/// <summary>
/// Captures compile-time caller metadata and expression context for a healable locator.
/// </summary>
public sealed record CallerContext(
    string RelativeFilePath,
    string ClassName,
    string MemberName,
    int LineNumber,
    string LocatorExpression)
{
    /// <summary>
    /// Canonical identifier keyed by normalized repository-relative file path, class, and member name.
    /// </summary>
    public string CanonicalKey => $"{RelativeFilePath}:{ClassName}.{MemberName}";

    /// <summary>
    /// Disambiguated identifier that differentiates multiple healable calls inside the same member.
    /// </summary>
    public string DisambiguatedKey => string.IsNullOrWhiteSpace(LocatorExpression)
        ? $"{CanonicalKey}@L{LineNumber}"
        : $"{CanonicalKey}[{LocatorExpression}]";
}
