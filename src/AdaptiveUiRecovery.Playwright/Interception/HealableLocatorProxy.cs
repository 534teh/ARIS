using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using AdaptiveUiRecovery.Core.Interception;
using AdaptiveUiRecovery.Core.Pipeline;
using Microsoft.Playwright;

namespace AdaptiveUiRecovery.Playwright.Interception;

/// <summary>
/// Dynamic proxy implementing <see cref="ILocator"/> to intercept Playwright actions with fast-fail stabilization and recovery.
/// </summary>
public class HealableLocatorProxy : DispatchProxy, IHealableLocatorMetadata
{
    private static readonly HashSet<string> VisibilityRequiredActionMethods = new(StringComparer.Ordinal)
    {
        nameof(ILocator.ClickAsync),
        nameof(ILocator.DblClickAsync),
        nameof(ILocator.TapAsync),
        nameof(ILocator.FillAsync),
        nameof(ILocator.ClearAsync),
        nameof(ILocator.PressAsync),
        nameof(ILocator.PressSequentiallyAsync),
        nameof(ILocator.TypeAsync),
        nameof(ILocator.CheckAsync),
        nameof(ILocator.UncheckAsync),
        nameof(ILocator.SetCheckedAsync),
        nameof(ILocator.SelectOptionAsync),
        nameof(ILocator.HoverAsync),
        nameof(ILocator.FocusAsync),
        nameof(ILocator.SetInputFilesAsync),
        nameof(ILocator.SelectTextAsync),
        nameof(ILocator.ScrollIntoViewIfNeededAsync),
        nameof(ILocator.InnerTextAsync),
        nameof(ILocator.InputValueAsync),
        nameof(ILocator.BoundingBoxAsync),
        nameof(ILocator.ScreenshotAsync),
        nameof(ILocator.DragToAsync)
    };

    private static readonly HashSet<string> AttachmentOnlyActionMethods = new(StringComparer.Ordinal)
    {
        nameof(ILocator.BlurAsync),
        nameof(ILocator.DispatchEventAsync),
        nameof(ILocator.InnerHTMLAsync),
        nameof(ILocator.TextContentAsync),
        nameof(ILocator.GetAttributeAsync),
        nameof(ILocator.HighlightAsync)
    };

    private static readonly MethodInfo GenericInterceptMethod = typeof(HealableLocatorProxy)
        .GetMethod(nameof(InterceptGenericActionAsync), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private ILocator _rawLocator = default!;
    private ILocator? _resolvedLocator;
    private CallerContext _caller = default!;
    private InterceptionOptions _options = new();
    private ILocatorRecoveryPipeline? _pipeline;
    private HealableExecutionScope? _scope;

    public HealableLocatorProxy()
    {
    }

    public CallerContext Caller => _caller;

    public ILocator RawLocator => _rawLocator;

    public ILocator CurrentNativeLocator => _resolvedLocator ?? _rawLocator;

    public bool WasHealed { get; private set; }

    public HealingResult? LastHealingResult { get; private set; }

    public double? LastInterceptionElapsedMs { get; private set; }

    /// <summary>
    /// Creates an <see cref="ILocator"/> proxy wrapping <paramref name="rawLocator"/>.
    /// </summary>
    public static ILocator Create(
        ILocator rawLocator,
        CallerContext caller,
        InterceptionOptions? options = null,
        ILocatorRecoveryPipeline? pipeline = null,
        HealableExecutionScope? scope = null)
    {
        ArgumentNullException.ThrowIfNull(rawLocator);
        ArgumentNullException.ThrowIfNull(caller);

        ILocator proxy = Create<ILocator, HealableLocatorProxy>();
        var state = (HealableLocatorProxy)proxy;
        state.Initialize(
            rawLocator,
            caller,
            options ?? InterceptionOptions.FromEnvironment(),
            pipeline,
            scope ?? HealableExecutionScope.Current);
        return proxy;
    }

    internal void Initialize(
        ILocator rawLocator,
        CallerContext caller,
        InterceptionOptions options,
        ILocatorRecoveryPipeline? pipeline,
        HealableExecutionScope? scope)
    {
        _rawLocator = rawLocator;
        _caller = caller;
        _options = options;
        _pipeline = pipeline;
        _scope = scope;

        if (_scope is not null && _scope.TryGetCachedHeal(_caller, out HealingResult? cachedResult))
        {
            _resolvedLocator = _rawLocator.Page.Locator(cachedResult.HealedSelector);
            WasHealed = true;
            LastHealingResult = cachedResult;
        }
    }

    public async Task<ILocator> ResolveNativeLocatorAsync(
        string actionName = "ResolveNativeLocatorAsync",
        bool requireVisible = true)
    {
        if (_resolvedLocator is not null)
        {
            return _resolvedLocator;
        }

        if (_scope is not null && _scope.TryGetCachedHeal(_caller, out HealingResult? cachedResult))
        {
            _resolvedLocator = _rawLocator.Page.Locator(cachedResult.HealedSelector);
            WasHealed = true;
            LastHealingResult = cachedResult;
            return _resolvedLocator;
        }

        if (_options.Mode == HealMode.Off)
        {
            return _rawLocator;
        }

        long startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            await _rawLocator.WaitForAsync(new LocatorWaitForOptions
            {
                State = requireVisible ? WaitForSelectorState.Visible : WaitForSelectorState.Attached,
                Timeout = _options.FastFailTimeoutMs
            }).ConfigureAwait(false);

            return _rawLocator;
        }
        catch (TimeoutException ex)
        {
            return await HandleFastFailAsync(ex, startTimestamp, actionName, requireVisible).ConfigureAwait(false);
        }
        catch (PlaywrightException ex)
        {
            return await HandleFastFailAsync(ex, startTimestamp, actionName, requireVisible).ConfigureAwait(false);
        }
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);

        if (targetMethod.DeclaringType == typeof(IHealableLocatorMetadata))
        {
            return InvokeTarget(this, targetMethod, args);
        }

        bool isVisibilityAction = VisibilityRequiredActionMethods.Contains(targetMethod.Name);
        if (isVisibilityAction || AttachmentOnlyActionMethods.Contains(targetMethod.Name))
        {
            if (targetMethod.ReturnType == typeof(Task))
            {
                return InterceptVoidActionAsync(targetMethod, args, targetMethod.Name, isVisibilityAction);
            }

            if (targetMethod.ReturnType.IsGenericType
                && targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                Type resultType = targetMethod.ReturnType.GetGenericArguments()[0];
                MethodInfo typedHelper = GenericInterceptMethod.MakeGenericMethod(resultType);
                return typedHelper.Invoke(this, [targetMethod, args, targetMethod.Name, isVisibilityAction]);
            }
        }

        object? result = InvokeTarget(CurrentNativeLocator, targetMethod, args);

        if (targetMethod.ReturnType == typeof(ILocator) && result is ILocator childLocator)
        {
            return Create(childLocator, _caller, _options, _pipeline, _scope);
        }

        return result;
    }

    private async Task<ILocator> HandleFastFailAsync(
        Exception failure,
        long startTimestamp,
        string actionName,
        bool requireVisible)
    {
        LastInterceptionElapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

        int domMatchCount = 0;
        try
        {
            domMatchCount = await _rawLocator.CountAsync().ConfigureAwait(false);
            if (!requireVisible && domMatchCount == 1)
            {
                return _rawLocator;
            }
        }
        catch
        {
            // Continue into recovery pipeline if CountAsync cannot resolve (e.g., invalid selector).
        }

        string failedSelector = ExtractSelector(_rawLocator);

        if (_pipeline is not null)
        {
            var request = new RecoveryRequest(_caller, failedSelector, actionName, domMatchCount, failure);
            HealingResult? healingResult = await _pipeline
                .TryRecoverAsync(request)
                .ConfigureAwait(false);

            if (healingResult is not null && !string.IsNullOrWhiteSpace(healingResult.HealedSelector))
            {
                ILocator candidateLocator = _rawLocator.Page.Locator(healingResult.HealedSelector);
                if (await candidateLocator.CountAsync().ConfigureAwait(false) == 1)
                {
                    _resolvedLocator = candidateLocator;
                    WasHealed = true;
                    LastHealingResult = healingResult;
                    _scope?.RecordHeal(_caller, healingResult);
                    return _resolvedLocator;
                }
            }
        }

        throw new PlaywrightException(
            $"Fast-fail interception triggered after {_options.FastFailTimeoutMs} ms for locator " +
            $"'{_caller.CanonicalKey}' ({_caller.LocatorExpression}) during '{actionName}'; " +
            $"selector '{failedSelector}' (Count={domMatchCount}) could not be resolved.",
            failure);
    }

    private async Task InterceptVoidActionAsync(
        MethodInfo targetMethod,
        object?[]? args,
        string actionName,
        bool requireVisible)
    {
        ILocator nativeLocator = await ResolveNativeLocatorAsync(actionName, requireVisible).ConfigureAwait(false);
        var task = (Task)InvokeTarget(nativeLocator, targetMethod, args)!;
        await task.ConfigureAwait(false);
    }

    private async Task<TResult> InterceptGenericActionAsync<TResult>(
        MethodInfo targetMethod,
        object?[]? args,
        string actionName,
        bool requireVisible)
    {
        ILocator nativeLocator = await ResolveNativeLocatorAsync(actionName, requireVisible).ConfigureAwait(false);
        var task = (Task<TResult>)InvokeTarget(nativeLocator, targetMethod, args)!;
        return await task.ConfigureAwait(false);
    }

    private static object? InvokeTarget(object target, MethodInfo method, object?[]? args)
    {
        try
        {
            return method.Invoke(target, args);
        }
        catch (TargetInvocationException tie) when (tie.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            return null;
        }
    }

    private static string ExtractSelector(ILocator locator)
    {
        FieldInfo? selectorField = locator.GetType()
            .GetField("_selector", BindingFlags.Instance | BindingFlags.NonPublic);
        if (selectorField?.GetValue(locator) is string internalSelector && !string.IsNullOrWhiteSpace(internalSelector))
        {
            return internalSelector;
        }

        string text = locator.ToString() ?? string.Empty;
        const string prefix = "Locator@";
        return text.StartsWith(prefix, StringComparison.Ordinal)
            ? text[prefix.Length..]
            : text;
    }
}
