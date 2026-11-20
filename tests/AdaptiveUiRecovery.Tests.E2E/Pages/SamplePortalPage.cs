using AdaptiveUiRecovery.Playwright.Interception;
using Microsoft.Playwright;

namespace AdaptiveUiRecovery.Tests.E2E.Pages;

/// <summary>
/// Sample Page Object demonstrating native <see cref="ILocator"/> properties backed by <c>.Heal()</c>.
/// </summary>
public sealed class SamplePortalPage(IPage page)
{
    public ILocator EmailInput => page.GetByPlaceholder("Email address").Heal();

    public ILocator PasswordInput => page.Locator("#password-input").Heal();

    public ILocator SignInButton => page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).Heal();

    public ILocator WelcomeHeading => page.GetByTestId("welcome-banner").Heal();

    public ILocator BrokenSubmitButton => page.Locator("#legacy-submit-btn-v1").Heal();

    public ILocator ErrorToast => page.Locator("#error-toast").Heal();
}
