using AdaptiveUiRecovery.Core.Interception;
using AdaptiveUiRecovery.Core.Pipeline;
using AdaptiveUiRecovery.Playwright.Interception;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace AdaptiveUiRecovery.Playwright.Testing;

/// <summary>
/// Base NUnit Playwright test fixture providing ambient healing scope and native <see cref="Expect(ILocator)"/> support.
/// </summary>
public abstract class HealablePageTest : PageTest
{
    private HealableExecutionScope? _scope;

    protected InterceptionOptions HealOptions
    {
        get => _scope?.Options ?? InterceptionOptions.FromEnvironment();
        set
        {
            _scope ??= new HealableExecutionScope(value);
            _scope.Options = value;
        }
    }

    protected ILocatorRecoveryPipeline? RecoveryPipeline
    {
        get => _scope?.Pipeline;
        set
        {
            _scope ??= new HealableExecutionScope(HealOptions, value);
            _scope.Pipeline = value;
        }
    }

    [SetUp]
    public void InitializeHealableScope()
    {
        _scope = new HealableExecutionScope(InterceptionOptions.FromEnvironment());
    }

    [TearDown]
    public void CleanupHealableScope()
    {
        _scope?.Dispose();
        _scope = null;
    }

    /// <summary>
    /// Shadows <see cref="PlaywrightTest.Expect(ILocator)"/> so healable <see cref="ILocator"/> proxies
    /// resolve and heal before calling Playwright's internal concrete <c>Locator</c> assertion implementation.
    /// </summary>
    public new ILocatorAssertions Expect(ILocator locator)
    {
        if (locator is HealableLocatorProxy healableProxy)
        {
            return HealableAssertionsProxy.Create(healableProxy);
        }

        return base.Expect(locator);
    }
}
