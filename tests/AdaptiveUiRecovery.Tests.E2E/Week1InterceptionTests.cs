using System.Diagnostics;
using AdaptiveUiRecovery.Core.Pipeline;
using AdaptiveUiRecovery.Playwright.Interception;
using AdaptiveUiRecovery.Playwright.Testing;
using AdaptiveUiRecovery.Tests.E2E.Pages;
using Microsoft.Playwright;
using NUnit.Framework;

namespace AdaptiveUiRecovery.Tests.E2E;

[TestFixture]
public sealed class Week1InterceptionTests : HealablePageTest
{
    private const string GreenPortalHtml = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <title>ARIS Week 1 Portal (v1)</title>
        </head>
        <body>
          <main>
            <h1 data-testid="welcome-banner">Welcome, Guest!</h1>
            <form id="login-form" onsubmit="event.preventDefault();">
              <input type="email" placeholder="Email address" id="email-input" />
              <input type="password" id="password-input" />
              <button type="button" onclick="document.querySelector('[data-testid=welcome-banner]').textContent = 'Welcome, QA!';">Sign In</button>
              <button type="button" id="legacy-submit-btn-v1">Submit Form</button>
            </form>
            <div id="error-toast" style="display: none;">Invalid credentials</div>
          </main>
        </body>
        </html>
        """;

    private const string MutatedPortalHtml = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <title>ARIS Week 1 Portal (v2)</title>
        </head>
        <body>
          <main>
            <h1 data-testid="welcome-banner">Welcome, Guest!</h1>
            <form id="login-form" onsubmit="event.preventDefault();">
              <input type="email" placeholder="Email address" id="email-input" />
              <input type="password" id="password-input" />
              <button type="button" id="v2-submit" onclick="this.setAttribute('data-clicked', 'true');">Complete Sign In</button>
            </form>
            <div id="error-toast" style="display: none;">Invalid credentials</div>
          </main>
        </body>
        </html>
        """;

    [Test]
    public void CallerMetadata_CapturesRelativeFilePathMemberLineAndExpression()
    {
        var portal = new SamplePortalPage(Page);

        IHealableLocatorMetadata? signInMeta = portal.SignInButton.GetHealMetadata();
        IHealableLocatorMetadata? passwordMeta = portal.PasswordInput.GetHealMetadata();

        Assert.That(signInMeta, Is.Not.Null);
        Assert.That(passwordMeta, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(signInMeta!.Caller.RelativeFilePath, Is.EqualTo("tests/AdaptiveUiRecovery.Tests.E2E/Pages/SamplePortalPage.cs"));
            Assert.That(signInMeta.Caller.ClassName, Is.EqualTo(nameof(SamplePortalPage)));
            Assert.That(signInMeta.Caller.MemberName, Is.EqualTo(nameof(SamplePortalPage.SignInButton)));
            Assert.That(signInMeta.Caller.LineNumber, Is.GreaterThan(0));
            Assert.That(signInMeta.Caller.LocatorExpression, Is.EqualTo("page.GetByRole(AriaRole.Button, new() { Name = \"Sign In\" })"));
            Assert.That(signInMeta.Caller.CanonicalKey, Is.EqualTo("tests/AdaptiveUiRecovery.Tests.E2E/Pages/SamplePortalPage.cs:SamplePortalPage.SignInButton"));

            Assert.That(passwordMeta!.Caller.RelativeFilePath, Is.EqualTo("tests/AdaptiveUiRecovery.Tests.E2E/Pages/SamplePortalPage.cs"));
            Assert.That(passwordMeta.Caller.ClassName, Is.EqualTo(nameof(SamplePortalPage)));
            Assert.That(passwordMeta.Caller.MemberName, Is.EqualTo(nameof(SamplePortalPage.PasswordInput)));
            Assert.That(passwordMeta.Caller.LocatorExpression, Is.EqualTo("page.Locator(\"#password-input\")"));
        });
    }

    [Test]
    public async Task GreenRun_ExecutesNativePlaywrightActionsAndExpectAssertionsSeamlessly()
    {
        await Page.SetContentAsync(GreenPortalHtml);
        var portal = new SamplePortalPage(Page);

        await portal.EmailInput.FillAsync("qa@aris.local");
        await portal.PasswordInput.FillAsync("SuperSecret123!");
        await Expect(portal.SignInButton).ToBeVisibleAsync();

        await portal.SignInButton.ClickAsync();

        string bannerText = await portal.WelcomeHeading.InnerTextAsync();
        Assert.That(bannerText, Is.EqualTo("Welcome, QA!"));

        await Expect(portal.WelcomeHeading).ToHaveTextAsync("Welcome, QA!");
        await Expect(portal.ErrorToast).ToBeHiddenAsync();
    }

    [Test]
    public async Task FastFailWindow_InterceptsBrokenLocatorIn1500MsInsteadOf30Seconds()
    {
        await Page.SetContentAsync(MutatedPortalHtml);
        var portal = new SamplePortalPage(Page);

        var sw = Stopwatch.StartNew();
        PlaywrightException? caught = null;
        try
        {
            await portal.BrokenSubmitButton.ClickAsync();
        }
        catch (PlaywrightException ex)
        {
            caught = ex;
        }
        sw.Stop();

        Assert.That(caught, Is.Not.Null);
        Assert.That(caught!.Message, Does.Contain("Fast-fail interception triggered"));
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(4000));
    }

    [Test]
    public async Task RecoveryHook_HealsBrokenLocatorInFlightDuringActionAndExpectAssertion()
    {
        await Page.SetContentAsync(MutatedPortalHtml);

        RecoveryPipeline = new DelegateRecoveryPipeline((caller, failedSelector, _) =>
        {
            if (failedSelector.Contains("#legacy-submit-btn-v1", StringComparison.Ordinal))
            {
                return new HealingResult(
                    HealedSelector: "internal:role=button[name=\"Complete Sign In\"i]",
                    Confidence: 0.96,
                    Tier: "Week1-Stub",
                    PureHealDuration: TimeSpan.FromMilliseconds(5),
                    Reasoning: "Matched redesigned Complete Sign In button.");
            }

            return null;
        });

        var portal = new SamplePortalPage(Page);
        ILocator brokenButton = portal.BrokenSubmitButton;

        await Expect(brokenButton).ToBeVisibleAsync();
        await brokenButton.ClickAsync();

        IHealableLocatorMetadata? metadata = brokenButton.GetHealMetadata();
        Assert.That(metadata, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(metadata!.WasHealed, Is.True);
            Assert.That(metadata.LastHealingResult, Is.Not.Null);
            Assert.That(metadata.LastHealingResult!.HealedSelector, Is.EqualTo("internal:role=button[name=\"Complete Sign In\"i]"));
        });

        string? clickedAttr = await Page.Locator("#v2-submit").GetAttributeAsync("data-clicked");
        Assert.That(clickedAttr, Is.EqualTo("true"));
    }
}
