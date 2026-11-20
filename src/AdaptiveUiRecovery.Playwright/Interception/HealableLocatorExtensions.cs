using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using AdaptiveUiRecovery.Core.Interception;
using AdaptiveUiRecovery.Core.Pipeline;
using Microsoft.Playwright;

namespace AdaptiveUiRecovery.Playwright.Interception;

/// <summary>
/// Postfix extension methods enabling native Playwright <see cref="ILocator"/> expressions to opt into adaptive healing.
/// </summary>
public static class HealableLocatorExtensions
{
    private static readonly ConcurrentDictionary<(string FilePath, int LineNumber, string MemberName, string LocatorExpr), CallerContext> CallerContextCache = new();

    /// <summary>
    /// Wraps any Playwright <see cref="ILocator"/> (e.g. <c>Page.GetByRole(...).Heal()</c>) in a healable proxy
    /// while capturing repository-relative caller location and full C# expression metadata.
    /// </summary>
    public static ILocator Heal(
        this ILocator locator,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(locator))] string locatorExpr = "")
    {
        ArgumentNullException.ThrowIfNull(locator);

        if (locator is HealableLocatorProxy)
        {
            return locator;
        }

        HealableExecutionScope? scope = HealableExecutionScope.Current;
        InterceptionOptions options = scope?.Options ?? InterceptionOptions.FromEnvironment();
        ILocatorRecoveryPipeline? pipeline = scope?.Pipeline;

        CallerContext caller = CallerContextCache.GetOrAdd(
            (filePath, lineNumber, memberName, locatorExpr),
            static key =>
            {
                string relativePath = PathNormalizer.NormalizeToRepoRelative(key.FilePath);
                string className = ResolveDeclaringClassName(key.MemberName, key.FilePath);
                return new CallerContext(
                    RelativeFilePath: relativePath,
                    ClassName: className,
                    MemberName: key.MemberName,
                    LineNumber: key.LineNumber,
                    LocatorExpression: key.LocatorExpr);
            });

        return HealableLocatorProxy.Create(locator, caller, options, pipeline, scope);
    }

    /// <summary>
    /// Returns the healing metadata for a <c>.Heal()</c>-wrapped <see cref="ILocator"/>, or <c>null</c> if unwrapped.
    /// </summary>
    public static IHealableLocatorMetadata? GetHealMetadata(this ILocator locator) =>
        locator is HealableLocatorProxy proxy ? proxy : null;

    private static string ResolveDeclaringClassName(string memberName, string filePath)
    {
        var trace = new StackTrace(skipFrames: 1, fNeedFileInfo: false);
        foreach (StackFrame frame in trace.GetFrames())
        {
            MethodBase? method = frame.GetMethod();
            Type? declaringType = method?.DeclaringType;
            if (declaringType is null || declaringType == typeof(HealableLocatorExtensions))
            {
                continue;
            }

            if (declaringType.GetCustomAttribute<CompilerGeneratedAttribute>() is not null
                && declaringType.DeclaringType is not null)
            {
                declaringType = declaringType.DeclaringType;
            }

            string methodName = method!.Name;
            if (methodName == memberName
                || methodName == $"get_{memberName}"
                || methodName == $"set_{memberName}")
            {
                return declaringType.Name;
            }
        }

        string fileStem = Path.GetFileNameWithoutExtension(filePath);
        return string.IsNullOrWhiteSpace(fileStem) ? "UnknownClass" : fileStem;
    }
}
