# Contributing & Commit Conventions

## Commit Message Format

```text
<type>(<scope>): <short summary>

[optional body: explain WHY, not just WHAT]

[optional footer: Breaking changes or issue/week references]
```

* `<scope>` is optional when a change spans multiple modules (e.g., `chore: scaffold .NET 8 solution`).
* Keep the subject line **under 72 characters**, in **imperative mood** ("add", "fix", "update" — not "added" or "updates"), lowercase after the colon, with **no trailing period**.
* Keep each commit focused on **one logical change** so it is easy to review, revert, and reference in the thesis.

---

## Allowed Types

| Type | When to use |
| :--- | :--- |
| `feat` | New capability or pipeline component (e.g., RCA gatekeeper, Split-Similo, Roslyn patcher) |
| `fix` | Bug fix in library, interception, or testbed behavior |
| `refactor` | Code restructuring with no behavior change |
| `perf` | Latency, token-cost, or memory optimization (e.g., JS extractor, caching) |
| `test` | Adding or updating E2E/unit tests and fault-injection fixtures |
| `bench` | Benchmark runner, offline `Similo2`/`WEBRL` dataset harness, or metrics/CSV export |
| `docs` | Architecture outline, timeline, thesis notes, README, or comments-only updates |
| `build` | `.csproj`, `.sln`, NuGet dependencies, `global.json`, or MSBuild/Roslyn generator setup |
| `ci` | CI workflows, Allure reporting pipeline config, or automated checks |
| `chore` | Repo scaffolding, `.gitignore`, formatting, or non-code maintenance |

---

## Project Scopes

Use scopes matching the ARIS modules and projects:

| Scope | Area |
| :--- | :--- |
| `core` | `AdaptiveUiRecovery.Core` (contracts, models, options, path normalization) |
| `playwright` | `AdaptiveUiRecovery.Playwright` (`.Heal()`, `DispatchProxy`, `HealablePageTest`) |
| `rca` | Stage 2 RCA Gatekeeper, CDP telemetry ring buffer, decision tree |
| `similo` | Stage 3 Tier 1 Split-Similo, Shannon entropy filter, selector synthesizer |
| `llm` | Stage 4 Tier 2 Aria Snapshot grounding, `ILlmHealingProvider`, JSON schema |
| `roslyn` | Stage 5 cumulative `CSharpSyntaxRewriter` & `[InterceptsLocation]` source generator |
| `allure` | Allure categories, attachments, screenshots, and diff reporting |
| `e2e` | `AdaptiveUiRecovery.Tests.E2E` Page Objects and NUnit scenarios |
| `bench` | `AdaptiveUiRecovery.Benchmarks` offline runner and ROI/chart scripts |
| `docker` | `docker-compose.yml` and local target app (`aris-juice-shop`) setup |

---

## Good vs. Bad Examples

* **Good:**
  * `feat(playwright): implement .Heal() DispatchProxy locator and assertion interception`
  * `feat(rca): add action-scoped CDP telemetry ring buffer`
  * `fix(similo): normalize Shannon entropy threshold for short CSS-in-JS tokens`
  * `test(e2e): add Wave 3 silent semantic shift traps`
  * `bench: export P50 and P95 pure heal latency to CSV`
* **Avoid:**
  * `update files` *(vague, no type)*
  * `feat(playwright): Added HealableLocatorProxy and fixed tests.` *(past tense, trailing period)*
  * `fix: stuff` *(unclear what changed)*

---

## Project Structure & Dependency Rules

```text
src/
  AdaptiveUiRecovery.Core/        # Pure domain logic (NO Microsoft.Playwright dependency)
  AdaptiveUiRecovery.Playwright/  # Playwright proxies, CDP telemetry, HealablePageTest, Allure
tests/
  AdaptiveUiRecovery.Tests.E2E/   # Page Objects (Pages/) + NUnit E2E test fixtures
benchmarks/
  AdaptiveUiRecovery.Benchmarks/  # Offline dataset runner + CSV metrics exporter
```

* **Keep `Core` Playwright-free:** `AdaptiveUiRecovery.Core` must not reference `Microsoft.Playwright`. Keep math (Split-Similo, Shannon entropy), RCA decision trees, LLM contracts, and Roslyn AST rewriting testable without a browser.
* **Keep `Playwright` thin:** `AdaptiveUiRecovery.Playwright` only adapts `IPage`/`ILocator`/`ILocatorAssertions` and CDP events to `Core` interfaces.

---

## Coding Conventions (C# / .NET 8)

* **Language & Nullability:** Target `net8.0` with `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, and file-scoped namespaces (`namespace ...;`).
* **Async Everywhere:** Never block on Playwright tasks with `.Result` or `.Wait()`. Use `ConfigureAwait(false)` inside `Core` and `Playwright` library code.
* **`DispatchProxy` Rules:** Any `DispatchProxy` implementation (`HealableLocatorProxy`, `HealableAssertionsProxy`) **must** be `public`, **must not** be `sealed`, and **must** have a `public` parameterless constructor.
* **Metadata Access:** Use `locator.GetHealMetadata()` instead of casting `ILocator` directly to `IHealableLocatorMetadata`.
* **Timing Separation (Benchmarks):** Always measure pure repair latency ($T_{\text{heal}}$) separately from Playwright's `1500` ms fast-fail stabilization window.

---

## Page Object & Locator Rules

* **Expression-Bodied `ILocator` Properties:** Define static healable locators as expression-bodied properties returning `ILocator` and ending in `.Heal()`:

  ```csharp
  public ILocator SignInButton => page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).Heal();
  ```

* **Static vs. Dynamic Selectors:**
  * Prefer static arguments (`GetByRole`, `GetByTestId`, `Locator("#id")`) in properties so Roslyn can patch them into source code.
  * Put parameterized selectors (`$"//tr[td='{name}']"`) in methods (`RowByName(string name)`)—they heal in memory at runtime, and Roslyn intentionally skips static AST patching for them.
* **One `.Heal()` Per Property:** Avoid multiple `.Heal()` calls inside the same method unless necessary; one property per UI element keeps `.locator-lock.json` keys (`CanonicalKey`) clean and stable across selector patches.

---

## Testing Rules

* **Inherit `HealablePageTest`:** All E2E fixtures must inherit from `HealablePageTest` (not raw `PageTest`) so `HealableExecutionScope` and shadowed `Expect(ILocator)` work automatically.
* **Use Instance `Expect(locator)`:** Write `await Expect(portal.SignInButton).ToBeVisibleAsync();` rather than static `Assertions.Expect(...)` (until the Month 5 `[InterceptsLocation]` source generator is added).
* **Deterministic Targets Only:** Run tests against `Page.SetContentAsync(...)` or the local `aris-juice-shop` Docker container (`http://localhost:3000`)—never against external live websites.
* **Don't Mask Healed Locators:** Once Shadow Retry + `[TearDown]` gating is enabled (Month 4+), let `LocatorHealedWarningException` fail the test in `[TearDown]` so broken locators are never silently ignored in CI.

---

## Branching Strategy

* **Keep `main` Green:** `main` should always build and pass `dotnet test`. Do new work on short-lived feature/milestone branches (e.g., one branch per week or pipeline stage).
* **Branch Naming:** `<type>/<short-kebab-description>` matching the commit types above:
  * `feat/week1-interception`
  * `feat/week2-dom-extractor`
  * `fix/strict-mode-rca`
  * `bench/webrl-dataset`
  * `docs/chapter2-similo`
