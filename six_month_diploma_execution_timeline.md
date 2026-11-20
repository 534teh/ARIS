# Six-Month Bachelor's Diploma Execution Timeline
**Total Duration:** 24 Weeks (6 Months)  
**Target Output:** Working .NET 8 / Playwright Library + Experimental Benchmark Suite + 4-Chapter Bachelor's Thesis Document (ДСТУ 3008:2015)

---

## Month 1 (Weeks 1–4): Foundation, Architecture Lock & Testbed Setup
**Goal:** Establish the .NET solution structure, deploy the local target web application, build the `.Heal()` `DispatchProxy` interception layer (`HealableLocatorProxy` & `HealableAssertionsProxy`), and implement the SPA-safe mutation injector.

### Week 1: Solution Scaffolding & Target App Deployment
* **What to Do:**
  1. Create a `.NET 8` solution `AdaptiveUiRecovery.sln` with four projects:
     * `AdaptiveUiRecovery.Core` (Class Library: models, interfaces, RCA, Similo, LLM, Roslyn).
     * `AdaptiveUiRecovery.Playwright` (Playwright integration, `HealablePageTest`, `HealableLocatorProxy`, `HealableAssertionsProxy`, `.Heal()` extension, Allure adapter).
     * `AdaptiveUiRecovery.Tests.E2E` (NUnit test suite with Page Objects).
     * `AdaptiveUiRecovery.Benchmarks` (Runner for offline `Similo2`/`WEBRL` HTML pairs and CSV metrics exporter).
  2. Spin up a local containerized target application (e.g., **OWASP Juice Shop** or **RealWorld Conduit** via `docker compose up -d`) so all tests run offline with zero external network flakiness.
* **How to Do It:**
  * **Seamless `ILocator` & `Expect(ILocator)` Interception via `DispatchProxy`:** Do *not* manually implement all 40+ members of `Microsoft.Playwright.ILocator` or expose a non-standard `HealableLocator` return type (`ExpectHealable`). Instead, implement a postfix extension `public static ILocator Heal(this ILocator locator, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerArgumentExpression("locator")] string locatorExpr = "")` returning `DispatchProxy.Create<ILocator, HealableLocatorProxy>()`.
  * To keep assertions 100% native (`await Expect(page.SubmitBtn).ToBeVisibleAsync()`) without triggering Playwright's internal concrete `Locator` `InvalidCastException`, shadow `public new ILocatorAssertions Expect(ILocator locator)` in `HealablePageTest : PageTest` using `DispatchProxy.Create<ILocatorAssertions, HealableAssertionsProxy>()`, which awaits `ResolveNativeLocatorAsync()` (running RCA + healing if needed) and delegates to `Assertions.Expect(resolvedNativeLocator)`.
  * **Support Native `GetBy*` Factories, Path Normalization & Fast-Fail Polling:** Allow standard `ILocator` Page Object properties using any Playwright factory (`public ILocator SubmitBtn => Page.GetByRole(AriaRole.Button, new() { Name = "Submit" }).Heal();` or `Page.Locator("#submit").Heal();`), normalize `[CallerFilePath]` to repository-relative paths, and implement a fast-fail stabilization window (`WaitForAsync(new() { Timeout = 1500 })`) inside `HealableLocatorProxy` so broken selectors enter the RCA + healing pipeline after $1.5\text{ s}$ rather than waiting for Playwright's default $30\text{ s}$ timeout.

### Week 2: Baseline Harvester & Single-Trip JS Extractor
* **What to Do:**
  1. Implement the `ElementProfile` C# record (Tag, Id, Name, TestId, AriaRole, AriaLabel, InnerText, Classes, XPath, NormalizedBoundingBox, ParentSignature, SiblingTags).
  2. Write the embedded JavaScript function (`dom-extractor.js`) executed directly on the target `ILocator` via `await rawLocator.EvaluateAsync<ElementProfile>(...)` (so `GetByRole`, `GetByTestId`, CSS, and XPath selectors are all resolved natively by Playwright before extracting DOM properties).
  3. Implement `.locator-lock.json` serialization when environment variable `HEAL_MODE=Record` is set, keyed by `{RelativeFilePath}:{PageClassName}.{PropertyName}` (with `[CallerLineNumber]` and `[CallerArgumentExpression]` disambiguation).
* **How to Do It:**
  * Inside `dom-extractor.js`, compute `el.getBoundingClientRect()` normalized by `window.innerWidth` and `window.innerHeight` so coordinates remain stable across different screen resolutions.

### Week 3: Writing the 15 Baseline E2E Test Scenarios
* **What to Do:**
  1. Implement 3–4 Page Objects covering ~25–30 distinct UI elements (buttons, inputs, navigation links, dropdowns, dynamic list cards).
  2. Write 15 NUnit E2E test scenarios covering core user flows (Authentication, Search/Filter, Cart/Item Management, Form Validation, Profile/Settings).
  3. Execute the suite in `HEAL_MODE=Record` to generate the ground-truth `.locator-lock.json` file.

### Week 4: Building the Controlled Fault & SPA-Safe Mutation Injector
* **What to Do:**
  1. Create a declarative mutation system (`MutationProfile.json`) that injects controlled faults before page interaction:
     * **Wave 1 (Minor Structural/Attribute Drift - 15 cases):** Rename `id`/`class` attributes, inject CSS-in-JS random hashes (`sc-9f8a1b`, `_btn_1x9a2_4`), wrap elements in extra `<div>` containers.
     * **Wave 2 (Semantic & Redesign Drift - 15 cases):** Change button text (`"Login"` $\rightarrow$ `"Sign In"`, `"Checkout"` $\rightarrow$ `"Complete Order"`), convert `<button>` to `<a role="button">`, move element across containers.
     * **Wave 3 (Silent Semantic Shift Traps - 10 cases):** Replace a safe button's text/icon in-place with a destructive action (`"Save Changes"` $\rightarrow$ `"Delete Account"`) while keeping ID, classes, and coordinates identical.
     * **Wave 4 (Non-Locator Failures for RCA - 20 cases):** Inject HTTP `500 Internal Server Error`, `401 Unauthorized`, network timeouts ($> 6000\text{ ms}$), and `display: none` / `aria-hidden="true"` regressions.
* **How to Do It:**
  * **Avoid One-Shot `AddInitScriptAsync` Overwrites:** Because `page.AddInitScriptAsync()` runs before Angular/React SPA frameworks mount or hydrate components on `<app-root></app-root>`, hook the DOM mutation injector **lazily/pre-action inside `HealableLocatorProxy`** (right before the locator is queried, after the SPA has rendered) or register a persistent `MutationObserver` in `AddInitScriptAsync` for Waves 1–3. Use `await page.RouteAsync(...)` to intercept API requests and return synthetic `500`/`401` responses for Wave 4.
* **Thesis Progress:** Finalize **Chapter 1** text:
  * Literature review across Stream A (Similo [Nass et al., 2022], VON Similo & LLM VON Similo [Nass et al., 2024], Similo++/HybridSimilo [Kluge & Stocco, 2025], VISTA [Stocco et al., 2018], WEBRL [Wen et al., 2024]) and Stream B (D2Snap [Schiepanski & Piël, 2025], Playwright AXTree Web Agents [Patel et al., 2024], Priority-Ranked AXTree Self-Healing [Joseph, 2026], Xu et al. [2025]).
  * Pipeline justification tables contrasting DOM + Accessibility Tree + LLM against both Pure Pixel-Coordinate Vision (VISTA/OCR/VLM) and Two-Stage DOM-Anchored Visual Matching (WEBRL).

---

## Month 2 (Weeks 5–8): Root Cause Analysis (RCA) Gatekeeper Engine
**Goal:** Build the action-scoped telemetry collectors and deterministic RCA classifier so the framework accurately blocks healing on Wave 4 and assertion errors without cross-step contamination.

### Week 5: Action-Scoped CDP Network & Console Ring Buffer
* **What to Do:**
  1. Implement `TelemetryCollector` attached to `IPage` creation.
  2. Subscribe to `page.Response`, `page.RequestFailed`, `page.PageError`, and `page.Console`.
* **How to Do It:**
  * Maintain a thread-safe ring buffer of network and console events with a domain whitelist filter (matching the local Docker app's base URL and ignoring `.png`, `.ico`, `.css`, `.woff2`).
  * **Prevent Cross-Step Contamination:** Watermark or clear the telemetry buffer at the end of every *successful* `HealableLocatorProxy` action. When an action fails at $t_{\text{fail}}$, only evaluate telemetry events whose timestamps fall within the active action window $[t_{\text{action\_start}} - 500\text{ ms}, t_{\text{fail}}]$ so an intentional `401` in Step 1 (e.g., testing invalid login) never falsely blocks locator healing in Step 4.

### Week 6: Deterministic RCA Decision Tree Implementation
* **What to Do:**
  1. Implement `RcaClassifier.AnalyzeAsync(Exception ex, IPage page, string failedSelector, TelemetrySnapshot actionWindowTelemetry)`.
  2. Encode the 6 diagnostic outcomes:
     * `ProductDefect_Assertion` (when `ex` is `AssertionException`)
     * `ProductDefect_Backend5xx` (when active action window contains API status $500\text{–}599$)
     * `Environment_AuthOrNetwork` (when active action window contains API status $401/403$ or pending requests $> 5000\text{ ms}$)
     * `UiStateRegression_ElementHidden` (when `await page.Locator(failedSelector).CountAsync() == 1`, meaning the node exists in DOM but failed visibility/actionability)
     * `StrictModeAmbiguity` (when `CountAsync() > 1`)
     * `UiLocatorDrift_Healable` (when `CountAsync() == 0` and all environment/backend checks in $[t_{\text{action\_start}} - 500\text{ ms}, t_{\text{fail}}]$ are clean)

### Week 7–8: Validating RCA on Wave 4 & Drafting Chapter 2.1
* **What to Do:**
  1. Run unit and integration tests against Wave 4 fault injections and multi-step tests where Step 1 triggers an expected `401` and Step 4 triggers a mutated locator.
  2. Verify that $100\%$ of active backend $500$ errors and hidden-element regressions are caught and blocked from entering the healing pipeline, while stale earlier-step errors are ignored.
* **Thesis Progress:** Write **Section 2.1** (Formalization of the RCA decision graph, action-window CDP telemetry correlation, and diagnostic taxonomy).

---

## Month 3 (Weeks 9–12): Tier 1 Heuristic Engine (Split-Similo + Cascade Synthesizer)
**Goal:** Implement the fast in-memory structural/identity matcher, normalized Shannon entropy filter, action-compatible candidate harvester, and Playwright selector generator.

### Week 9: Normalized Shannon Entropy Filter & Action-Filtered Candidate Harvesting
* **What to Do:**
  1. Implement `EntropyFilter.IsVolatileToken(string token)` using **Normalized Shannon Entropy** $H_{\text{norm}}(X)$:
     $$H_{\text{norm}}(X) = -\frac{1}{\log_2(n)} \sum_{k} P(x_k) \log_2 P(x_k)$$
     Flag tokens of length $5 \le n \le 12$ as volatile when $H_{\text{norm}}(X) \ge 0.88$ (or raw $H(X) > 2.5\text{ bits/char}$ for $n > 12$) **combined with a character-class transition check** ($\ge 2$ transitions between letters and digits, e.g., `a1b2`, `_btn_1x9a2_4`), alongside regex matching for known CSS-in-JS framework patterns (`^(sc-|css-|tw-|svelte-|:r)|^_[a-zA-Z0-9]+_[a-zA-Z0-9]{5,}`).
  2. Write the bulk candidate harvester in `dom-extractor.js` that pre-filters DOM nodes by **action compatibility** (`FillAsync` $\rightarrow$ `<input>`, `<textarea>`, `[contenteditable]`, `[role=textbox]`; `ClickAsync` $\rightarrow$ interactive elements) and returns feature vectors in a single `EvaluateAsync` call.

### Week 10: Split-Similo Mathematical Engine
* **What to Do:**
  1. Implement string similarity metrics (`NormalizedLevenshtein`, `TokenJaccard`).
  2. Implement `SimiloScorer.ComputeScore(ElementProfile baseline, ElementProfile candidate)` returning `SimIdentity`, `SimStructure`, and `SimTotal`.
  3. Implement the **Escalation Gate logic**:
     * Fast-path accept if $\operatorname{Sim}_{\text{total}} \ge 0.85$, margin $\Delta \ge 0.06$, and $\operatorname{Sim}_{\text{id}} \ge 0.50$.
     * Flag for Tier 2 LLM escalation otherwise.

### Week 11: Playwright-Priority Cascade Selector Synthesizer
* **What to Do:**
  1. Implement `PlaywrightSelectorSynthesizer` (upgrading ROBULA+ refinement rules for Playwright):
     * Priority 1: `[data-testid="..."]`
     * Priority 2: `internal:role={role}[name="{name}"i]`
     * Priority 3: Unique stable attributes (`input[name="..."]`, `[placeholder="..."]`, `a[href="..."]`)
     * Priority 4: Closest stable ancestor ID/testId + entropy-filtered tag/class path.
  2. Add live verification: test each candidate selector via `await page.Locator(synth).CountAsync() == 1`.

### Week 12: Offline Benchmark Harness (`Similo2` / `HybridSimilo` / `WEBRL` Dataset Subset)
* **What to Do:**
  1. Download 20 real-world HTML version pairs from the open-source `Similo2` / Kluge & Stocco (`arXiv:2505.16424`, 10,376-pair benchmark) and `WEBRL` repositories.
  2. Load them via Playwright `page.SetContentAsync(html)` in `AdaptiveUiRecovery.Benchmarks` and test Tier 1 accuracy and pure algorithm latency ($P_{50}$).
* **Thesis Progress:** Write **Section 2.2** (Split-Similo equations, normalized Shannon entropy derivation proving why unnormalized $H(X) > 3.2$ fails for $5 \le n \le 9$ where $H_{\max} = \log_2(n) \le 3.17$, Playwright synthesis priority cascade, and formal time/space complexity proof $\mathcal{O}(|E_{\text{DOM}}| \cdot |A|)$ vs. ROBULA+ exponential worst-case $\mathcal{O}(b^d \cdot |E_{\text{DOM}}|)$).

---

## Month 4 (Weeks 13–16): Tier 2 Semantic LLM Engine & Shadow Verification
**Goal:** Build the "Set-of-Marks" Aria Snapshot + Top-10 HTML compressor, integrate cloud and open-weights LLMs with constrained JSON decoding, resolve the Top-10 blind spot, and implement deferred Shadow Verification.

### Week 13: "Set-of-Marks" Aria Snapshot & Top-10 Candidate Context Builder
* **What to Do:**
  1. When Tier 1 triggers escalation, take the Top-10 action-compatible candidates from Similo and inject temporary attributes `data-heal-id="cand_1"` ... `"cand_10"` in the live DOM (inspired by BrowserGym `bid` grounding).
  2. Call `await page.Locator("body").AriaSnapshotAsync()` to capture the full-page semantic YAML accessibility tree (providing surrounding hierarchy such as `dialog "Confirm Deletion"` or `navigation "Main Menu"`).
  3. Assemble the three-part LLM context payload:
     * **Part 1:** Target Baseline Profile + C# Page Object property name (e.g., `CheckoutButton`) to convey test intent.
     * **Part 2:** Full-page Aria Snapshot YAML.
     * **Part 3:** Compact outer HTML (stripped of inline SVGs and styles) and Split-Similo scores of `cand_1..10`.

### Week 14: Multi-LLM Adapter, Constrained Decoding & Top-10 Blind Spot Recovery
* **What to Do:**
  1. Define `ILlmHealingProvider` and implement an OpenAI-compatible HTTP client with **Constrained Decoding / Structured Outputs (`response_format` JSON Schema)** supporting:
     * **Cloud Frontier:** `Gemini 2.5 Flash` and `GPT-4o-mini`.
     * **Code-Specialized & General Open-Weights (via Groq / OpenRouter / Colab Ollama):** `Qwen-2.5-Coder-32B-Instruct` (or `7B`/`14B` variants, pre-trained heavily on HTML/YAML/Playwright syntax) and `Llama-3.3-70B-Instruct`.
  2. Design the system prompt and strict JSON output schema to solve the **Top-10 Candidate Blind Spot** (allowing the LLM to either pick `cand_1..10` *or* provide an `aria_selector` directly from the YAML Accessibility Tree if a radical redesign pushed the target outside Similo's Top-10):
     ```json
     {
       "selected_candidate_id": "cand_2 | outside_top_10 | none",
       "aria_selector": "internal:role=button[name=\"Account\"i]",
       "recommended_selector": "internal:role=button[name=\"Proceed to Checkout\"i]",
       "is_destructive_semantic_shift": false,
       "confidence": 0.92,
       "reasoning": "Button text changed from 'Buy' to 'Proceed to Checkout', preserving purchase intent."
     }
     ```
  3. Implement multi-step live verification: verify `recommended_selector` or `aria_selector` via `CountAsync() == 1`; if a non-unique selector was suggested for a chosen `cand_1..10`, fall back to the deterministic Stage 3 synthesizer on `selected_candidate_id`.

### Week 15: In-Flight Shadow Retry & Deferred `[TearDown]` Exception
* **What to Do:**
  1. Inside `HealableLocatorProxy`, after Tier 1 or Tier 2 produces a verified selector $L_{\text{new}}$, execute the intended Playwright action (`ClickAsync`, `FillAsync`) on $L_{\text{new}}$ normally without throwing an immediate exception.
  2. Append a `HealingRecord` (including pure algorithm latency $T_{\text{heal}} = t_{\text{patch}} - t_{\text{intercept}}$) to a test-scoped `HealingContext` (`AsyncLocal<T>`), allowing the remainder of the NUnit test to run to completion ("Shadow Retry"—including healing subsequent broken locators in steps 3 or 4).
  3. In the test base class `[TearDown]` hook, check `HealingContext`: if the test passed all assertions, mark `ShadowRetryPassed = true` on all healed records, trigger cumulative Roslyn patch generation, and throw `LocatorHealedWarningException` so CI never silently passes with unpatched Page Object code.

### Week 16: Validating Wave 2 (Semantic Drift) & Wave 3 (Silent Semantic Shift Traps)
* **What to Do:**
  1. Run Waves 2 and 3 against the Hybrid pipeline.
  2. Confirm that Tier 1 escalates Wave 3 ("Save" $\rightarrow$ "Delete") due to $\operatorname{Sim}_{\text{id}} < 0.50$, and Tier 2 LLM correctly returns `"is_destructive_semantic_shift": true`, blocking false healing ($\text{FHR} < 5\%$).
* **Thesis Progress:** Write **Section 2.3** (Hybrid escalation architecture, "Set-of-Marks" Aria Snapshot grounding, Top-10 blind spot recovery, and constrained JSON schema).

---

## Month 5 (Weeks 17–20): Roslyn AST Rewriter & Allure Reporting Integration
**Goal:** Automate cumulative C# Page Object patch generation and create rich diagnostic attachments in Allure Report.

### Week 17–18: Cumulative In-Memory Roslyn `CSharpSyntaxRewriter`, Zero-Touch Interceptors & Unified Git Diff Generator
* **What to Do:**
  1. Add NuGet package `Microsoft.CodeAnalysis.CSharp`.
  2. Implement `PageObjectAstPatcher : CSharpSyntaxRewriter` backed by an in-memory `ConcurrentDictionary<string, SyntaxTree>` keyed by normalized repository-relative file path:
     * When multiple locators inside the same Page Object (e.g., `UsernameInput` and `LoginButton` in `LoginPage.cs`) heal during a single test run, apply all AST rewrites cumulatively to the same in-memory `SyntaxTree` instance so patches never conflict or overwrite each other.
     * Locate the `.Heal()` invocation (or intercepted factory call) matching `[CallerMemberName]` (disambiguated by `[CallerArgumentExpression("locator")]` and `[CallerLineNumber]`).
     * Replace the receiver expression `<ReceiverExpr>` inside `<ReceiverExpr>.Heal()` with synthesized idiomatic C# Playwright locator expressions (`Page.GetByRole(...)`, `Page.GetByTestId(...)`, `Page.Locator("...")`) while preserving leading/trailing whitespace (`WithTriviaFrom`).
     * Explicitly skip dynamic `InterpolatedStringExpressionSyntax` and parameterized expressions (such as `Page.Locator($"//tr[td='{name}']").Heal()`) from static AST rewriting, logging them as runtime-only heals.
  3. **Zero-Touch Compile-Time Interception (Option B Upgrade):** Implement a Roslyn Incremental Source Generator (`IIncrementalGenerator`) emitting C# 12 `[InterceptsLocation]` interceptors for `IPage.Locator`, `IPage.GetBy*`, and `Assertions.Expect(ILocator)` (baking exact compile-time AST `TextSpan` and member metadata into each call site) so existing Playwright test suites can run with zero `.Heal()` boilerplate.
  4. Emit a single unified `.patch` file per modified Page Object at the end of the run (and optionally provide a CLI helper `dotnet heal apply` for local development).

### Week 19: Allure Report Lifecycle Enrichment (`Allure.Net.Commons`)
* **What to Do:**
  1. Configure `categories.json` for Allure to display custom diagnostic buckets:
     * `Product Defect (Assertion / Backend 5xx)`
     * `Environment / Auth Issue`
     * `UI State Regression (Element Hidden)`
     * `UI Locator Healed (Patch Ready)`
  2. Use `AllureLifecycle.Instance.UpdateTestCase` and `AddAttachment` to attach:
     * Structured Markdown/JSON diagnostic card (RCA verdict, $\operatorname{Sim}_{\text{id}}$, $\operatorname{Sim}_{\text{struct}}$, LLM reasoning, pure repair latency $T_{\text{heal}}$ in $\text{ms}$).
     * Screenshot with a red bounding box on the old coordinates and a green bounding box on the healed element.
     * The unified `PageObject.patch` Git diff file.

### Week 20: End-to-End Pipeline Polish & Chapter 3 Drafting
* **What to Do:**
  1. Run a full end-to-end smoke test (including multi-locator heals in one Page Object) and generate the HTML Allure Report (`allure serve`).
  2. Capture high-resolution screenshots of the Allure dashboard, RCA traces, and Git Diff attachments for Chapter 3 and defense slides.
* **Thesis Progress:** Write **Chapter 3** (Software architecture class diagrams, sequence diagrams, `.Heal()` `DispatchProxy` interception and C# 12 `[InterceptsLocation]` source generator design, cumulative Roslyn AST code listings, and Allure integration).

---

## Month 6 (Weeks 21–24): Empirical Benchmarks, Statistical Analysis & Defense Prep
**Goal:** Execute the full experimental matrix, generate comparative charts, calculate economic ROI, document boundary conditions, finalize the thesis document, and prepare the defense presentation.

### Week 21: Running the Experimental Matrix
* **What to Do:**
  Execute all 60 live mutated test runs + 20 offline `WEBRL`/`Similo2` pairs across **4 experimental configurations**:
  1. **Baseline 1 (Healenium-style LCS/Flat Heuristic without RCA):** Demonstrates high False Healing Rate ($\text{FHR}$) on Wave 3 and Wave 4.
  2. **Config A (Tier 1 Split-Similo Only + RCA):** Fast ($< 40\text{ ms}$), zero cost, high precision on structural drift, lower recall on deep semantic rewrites.
  3. **Config B (Tier 2 LLM Only + RCA across 4 Models):** High semantic recall, but high latency ($1500\text{–}3500\text{ ms}$) and token cost.
  4. **Config C (Proposed Hybrid Pipeline: RCA + Tier 1 + Tier 2 Escalation):** Optimal Pareto frontier ($\text{Precision} \ge 92\%$, $\text{FHR} < 5\%$, pure algorithm $P_{50} < 50\text{ ms}$, $\sim 80\%$ LLM token cost reduction).

### Week 22: Data Processing, Economic ROI & Chart Generation
* **What to Do:**
  1. Export benchmark logs to CSV (`results_summary.csv`), recording pure repair latency ($T_{\text{heal}} = t_{\text{patch}} - t_{\text{intercept}}$) separately from Playwright's $1.5\text{ s}$ fast-fail polling window.
  2. Use a Python script (`pandas` + `matplotlib` / `seaborn`) to plot publication-grade charts for Chapter 4:
     * Bar chart: $\text{Precision}$, $\text{Recall}$, and $\text{FHR}$ across the 4 configurations.
     * Box/Violin plot: Pure algorithm latency distribution ($P_{50}$ vs. $P_{95}$ in $\text{ms}$).
     * Comparison table: Cloud Frontier (`Gemini 2.5 Flash`, `GPT-4o-mini`) vs. Code-Specialized & General Open-Weights (`Qwen-2.5-Coder-32B`, `Llama-3.3-70B`) accuracy and API cost per 100 failures.
     * Line chart: Fragility Score ($FS$) across $K=4$ sequential mutation waves.
  3. Compute the quantitative **Economic Efficiency & ROI model**:
     $$\Delta T_{\text{saved}} = N_{\text{failures}} \cdot \left(\text{MTTR}_{\text{manual}} - (\text{Precision} \cdot \text{MTTR}_{\text{patch}} + \text{FHR} \cdot \text{MTTR}_{\text{debug}})\right)$$

### Week 23: Final Thesis Assembly & Formatting (ДСТУ 3008:2015)
* **What to Do:**
  1. Write **Chapter 4** including:
     * Experimental methodology, statistical tables, and charts.
     * Economic efficiency calculation ($\Delta T_{\text{saved}}$ and $\sim 80\%$ LLM cost reduction via Tier 1 filtering).
     * **Boundary Conditions & Threats to Validity subsection** (1-to-$N$ flow decomposition, `<canvas>`/WebGL/Closed Shadow DOM limitations, cross-origin `<iframe>` boundaries, and parameterized/interpolated runtime selectors).
  2. Compile Introduction, Chapters 1–4, Conclusions (*Висновки*), References (*Список використаних джерел* — including Leotta/ROBULA+ 2016, Stocco/VISTA 2018, Nass/Similo 2022, Nass/LLM VON Similo 2024, Wen/WEBRL 2024, Patel 2024, Kluge & Stocco/Similo++ 2025, Schiepanski & Piël/D2Snap 2025, Xu 2025, Joseph 2026), and Appendices (code listings).
  3. Submit draft to your academic advisor for review.

### Week 24: Defense Presentation & Live/Recorded Demo Preparation
* **What to Do:**
  1. Prepare a 12–14 slide presentation emphasizing the problem ($70\%$ test fragility + Healenium's blind healing flaw), the mathematical model (Action-Scoped RCA + Split-Similo + Normalized Entropy + LLM Escalation), the justification of DOM + Accessibility Tree + LLM over CV/WEBRL, the architecture (Playwright + Cumulative Roslyn AST + Allure), and the empirical charts.
  2. Record a 90-second backup video of the terminal running the mutated tests and opening the enriched Allure Report with the generated `.patch` file so your defense demo is $100\%$ immune to live Wi-Fi or projector issues.
