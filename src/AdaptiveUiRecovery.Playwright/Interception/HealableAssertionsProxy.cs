using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Playwright;

namespace AdaptiveUiRecovery.Playwright.Interception;

/// <summary>
/// Dynamic proxy implementing <see cref="ILocatorAssertions"/> so native <c>Expect(locator)</c>
/// calls resolve and heal <see cref="HealableLocatorProxy"/> instances before delegating to Playwright assertions.
/// </summary>
public class HealableAssertionsProxy : DispatchProxy
{
    private HealableLocatorProxy _locatorProxy = default!;
    private bool _isNot;

    public HealableAssertionsProxy()
    {
    }

    public static ILocatorAssertions Create(HealableLocatorProxy locatorProxy, bool isNot = false)
    {
        ArgumentNullException.ThrowIfNull(locatorProxy);

        ILocatorAssertions proxy = Create<ILocatorAssertions, HealableAssertionsProxy>();
        var state = (HealableAssertionsProxy)proxy;
        state.Initialize(locatorProxy, isNot);
        return proxy;
    }

    internal void Initialize(HealableLocatorProxy locatorProxy, bool isNot)
    {
        _locatorProxy = locatorProxy;
        _isNot = isNot;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);

        if (targetMethod.Name == $"get_{nameof(ILocatorAssertions.Not)}"
            && targetMethod.ReturnType == typeof(ILocatorAssertions))
        {
            return Create(_locatorProxy, !_isNot);
        }

        if (targetMethod.ReturnType == typeof(Task))
        {
            return InvokeAssertionAsync(targetMethod, args);
        }

        if (targetMethod.ReturnType.IsGenericType
            && targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            Type resultType = targetMethod.ReturnType.GetGenericArguments()[0];
            MethodInfo genericMethod = typeof(HealableAssertionsProxy)
                .GetMethod(nameof(InvokeGenericAssertionAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(resultType);
            return genericMethod.Invoke(this, [targetMethod, args]);
        }

        ILocatorAssertions syncAssertions = BuildNativeAssertions(_locatorProxy.CurrentNativeLocator);
        return InvokeTarget(syncAssertions, targetMethod, args);
    }

    private async Task InvokeAssertionAsync(MethodInfo targetMethod, object?[]? args)
    {
        ILocator nativeLocator = await ResolveLocatorForAssertionAsync(targetMethod.Name, args).ConfigureAwait(false);
        ILocatorAssertions nativeAssertions = BuildNativeAssertions(nativeLocator);
        var task = (Task)InvokeTarget(nativeAssertions, targetMethod, args)!;
        await task.ConfigureAwait(false);
    }

    private async Task<TResult> InvokeGenericAssertionAsync<TResult>(MethodInfo targetMethod, object?[]? args)
    {
        ILocator nativeLocator = await ResolveLocatorForAssertionAsync(targetMethod.Name, args).ConfigureAwait(false);
        ILocatorAssertions nativeAssertions = BuildNativeAssertions(nativeLocator);
        var task = (Task<TResult>)InvokeTarget(nativeAssertions, targetMethod, args)!;
        return await task.ConfigureAwait(false);
    }

    private async Task<ILocator> ResolveLocatorForAssertionAsync(string methodName, object?[]? args)
    {
        if (!RequiresPresentElement(methodName, args))
        {
            return _locatorProxy.CurrentNativeLocator;
        }

        bool requireVisible = methodName is nameof(ILocatorAssertions.ToBeVisibleAsync)
            or nameof(ILocatorAssertions.ToBeEnabledAsync)
            or nameof(ILocatorAssertions.ToBeEditableAsync)
            or nameof(ILocatorAssertions.ToBeCheckedAsync)
            or nameof(ILocatorAssertions.ToBeFocusedAsync)
            or nameof(ILocatorAssertions.ToBeInViewportAsync);

        return await _locatorProxy
            .ResolveNativeLocatorAsync(actionName: methodName, requireVisible: requireVisible)
            .ConfigureAwait(false);
    }

    private bool RequiresPresentElement(string methodName, object?[]? args)
    {
        if (_isNot)
        {
            return false;
        }

        if (methodName is nameof(ILocatorAssertions.ToBeHiddenAsync)
            or nameof(ILocatorAssertions.ToBeEmptyAsync))
        {
            return false;
        }

        if (methodName == nameof(ILocatorAssertions.ToHaveCountAsync)
            && args is { Length: > 0 }
            && args[0] is int expectedCount
            && expectedCount == 0)
        {
            return false;
        }

        if (methodName == nameof(ILocatorAssertions.ToBeVisibleAsync)
            && args is { Length: > 0 }
            && args[0] is LocatorAssertionsToBeVisibleOptions { Visible: false })
        {
            return false;
        }

        if (methodName == nameof(ILocatorAssertions.ToBeAttachedAsync)
            && args is { Length: > 0 }
            && args[0] is LocatorAssertionsToBeAttachedOptions { Attached: false })
        {
            return false;
        }

        return true;
    }

    private ILocatorAssertions BuildNativeAssertions(ILocator nativeLocator)
    {
        ILocatorAssertions nativeAssertions = Assertions.Expect(nativeLocator);
        return _isNot ? nativeAssertions.Not : nativeAssertions;
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
}
