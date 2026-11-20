namespace AdaptiveUiRecovery.Core.Interception;

/// <summary>
/// Normalizes compiler-captured source file paths into portable repository-relative paths.
/// </summary>
public static class PathNormalizer
{
    /// <summary>
    /// Converts an absolute or machine-specific caller file path to a forward-slash repository-relative path.
    /// </summary>
    public static string NormalizeToRepoRelative(string callerFilePath, string? repoRootOverride = null)
    {
        if (string.IsNullOrWhiteSpace(callerFilePath))
        {
            return string.Empty;
        }

        string normalizedInput = NormalizeSlashes(callerFilePath);
        string? repoRoot = !string.IsNullOrWhiteSpace(repoRootOverride)
            ? NormalizeSlashes(Path.GetFullPath(repoRootOverride)).TrimEnd('/')
            : FindRepositoryRoot(Path.GetDirectoryName(callerFilePath))
              ?? FindRepositoryRoot(AppContext.BaseDirectory);

        if (string.IsNullOrEmpty(repoRoot))
        {
            return normalizedInput;
        }

        string prefix = repoRoot + "/";
        if (normalizedInput.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedInput[prefix.Length..];
        }

        return normalizedInput;
    }

    private static string? FindRepositoryRoot(string? startDirectory)
    {
        if (string.IsNullOrWhiteSpace(startDirectory))
        {
            return null;
        }

        DirectoryInfo? current = Directory.Exists(startDirectory)
            ? new DirectoryInfo(startDirectory)
            : null;

        while (current is not null)
        {
            string slnCandidate = Path.Combine(current.FullName, "AdaptiveUiRecovery.sln");
            string gitCandidate = Path.Combine(current.FullName, ".git");
            if (File.Exists(slnCandidate) || Directory.Exists(gitCandidate) || File.Exists(gitCandidate))
            {
                return NormalizeSlashes(current.FullName).TrimEnd('/');
            }

            current = current.Parent;
        }

        return null;
    }

    private static string NormalizeSlashes(string path) => path.Replace('\\', '/');
}
