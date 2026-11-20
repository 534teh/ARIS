# Project Architecture & Methodology Outline
**Thesis Title:** Method and Software Tools for Automated Root Cause Analysis of Failures and Adaptive Recovery of Interface Locators in End-to-End Software Testing Systems  
*(Метод і програмні засоби автоматизованого аналізу причин збоїв та адаптивного відновлення інтерфейсних локаторів у системах наскрізного тестування ПЗ)*

---

## 1. Core Thesis Positioning & State-of-the-Art Foundation

### 1.1. Research Scope
* **Target Ecosystem:** .NET 8 / C#, Microsoft Playwright for .NET, NUnit/xUnit, Roslyn Compiler API (`Microsoft.CodeAnalysis`), Allure Report (`Allure.Net.Commons`).
* **Object of Study:** The process of maintaining stability and reducing maintenance overhead of automated End-to-End (E2E) UI test suites under continuous web application evolution.
* **Subject of Study:** Deterministic Root Cause Analysis (RCA) decision trees, structural-semantic DOM and Accessibility Tree similarity models, and Abstract Syntax Tree (AST) source code patching methods.
* **Core Design Philosophy:** Eliminate the "blind healing" flaw of existing open-source tools (such as Healenium) and the lack of runtime context in log triage platforms (such as ReportPortal) by enforcing a strict **RCA Gatekeeper $\rightarrow$ 2-Tier Hybrid Locator Recovery $\rightarrow$ Shadow Verification $\rightarrow$ Roslyn Patch Recommendation** pipeline.

### 1.2. Key Architectural Lessons Adapted from Prior Art
1. **Identity vs. Appearance Separation (Adapted from WEBRL, ICSME 2024):**
   Classic Similo (Nass et al., 2022) combines all element attributes into a single weighted sum, allowing unchanged spatial/structural coordinates to mask a complete change in element identity ("Silent Semantic Shift"). Our architecture splits similarity into an **Identity Score** ($\operatorname{Sim}_{\text{id}}$) and a **Structural/Visual Score** ($\operatorname{Sim}_{\text{struct}}$).
2. **Serverless In-Process Footprint (Addressing Healenium's Infrastructure Bloat):**
   Healenium requires a PostgreSQL database, a Java Spring Boot backend, a network proxy, and a Python `selector-imitator` container. Our solution operates $100\%$ in-process as a NuGet library, storing reference element baselines in a version-controlled `.locator-lock.json` file directly inside the repository.
3. **Style-Aware Selector Synthesis with Live Verification (Upgrading `selector-imitator`):**
   Healenium's selector imitator attempts to preserve user selector preferences without guaranteeing uniqueness in the live DOM. Our synthesizer generates idiomatic Playwright locators in priority order and enforces a mandatory live uniqueness check ($|\{e \in \text{DOM} \mid e \models L\}| = 1$) before recommending a patch.

### 1.3. Literature Review & Research Streams (Chapter 1 Foundation)
The thesis sits at the intersection of two converging research streams: **Automated Web Test Locator Repair** and **LLM Web Agent DOM/Accessibility Grounding**.

#### Stream A: Multi-Property Heuristic & Visual Locator Repair
* **Similo (Nass et al., 2022):** Introduced weighted similarity scoring across 14 DOM and spatial properties to relocate broken elements across evolving web releases. Our Tier 1 engine directly upgrades Similo by splitting its flat score into orthogonal vectors ($\operatorname{Sim}_{\text{id}}$ and $\operatorname{Sim}_{\text{struct}}$) and adding normalized Shannon entropy filtering.
* **VON Similo & LLM VON Similo (Nass et al., 2024):** Extended Similo with Visually Overlapping Nodes (VON) and subsequently passed the Top-10 ranked VON Similo candidates encoded as JSON to GPT-4 to select the most semantically similar widget ID.
  * *How our pipeline advances beyond LLM VON Similo:* Nass et al. (2024) only send isolated JSON property dictionaries of the Top-10 candidates to the LLM without surrounding page hierarchy, lack a Root Cause Analysis (RCA) gatekeeper, do not guard against "Silent Semantic Shifts", and do not synthesize Playwright role locators or AST source code patches.
* **Similo++, VON Similo++, and HybridSimilo (Kluge & Stocco, 2025, `arXiv:2505.16424`):** Replicated and optimized Similo, VON Similo, and LLM VON Similo across an expanded benchmark of 10,376 element pairs ($23\times$ larger than the original Similo study). Demonstrated that (a) Visually Overlapping Nodes (`VON Similo`) produces a higher false positive rate than node-exact `Similo` when selecting decorative wrappers, (b) optimizing attribute weights (`Similo++`) to prioritize semantic/structural identity over volatile coordinates improves accuracy by $5.62\%$, (c) invoking GPT-4 on every broken locator (`LLM VON Similo`) adds $2\text{–}3$ orders of magnitude latency overhead for marginal gains on simple structural drift, and (d) heuristic matchers always return a candidate under a closed-world assumption even when no valid match exists—directly motivating our **Action-Compatibility Pre-Filtering**, **RCA Gatekeeper**, **2-Tier Escalation Gate**, and **Shadow Verification** stages.
* **VISTA (Stocco et al., 2018):** Uses computer vision template matching on full-page screenshots to repair broken web tests across releases.
* **WEBRL (Wen et al., ICSME 2024):** Two-stage repair separating identity attributes (Stage 1) from structural and CNN visual screenshot similarity (Stage 2).

#### Stream B: DOM + Accessibility Tree + LLM Systems
* **D2Snap (Schiepanski & Piël, 2025):** Investigated DOM downsampling vs. Accessibility Trees vs. vision screenshots for LLM web agents. Empirical evaluation showed that **removing screenshot images from grounded DOM/text snapshots caused no statistically significant drop in LLM performance ($p = 0.37$)**, proving that structural/textual DOM representations carry the primary utility for LLM UI reasoning while avoiding vision token overhead.
* **Playwright Accessibility Tree Web Agents (Patel et al., 2024; WebArena / BrowserGym):** Parses the page into a compact Accessibility Tree (AXTree) combined with unique grounded element IDs (`bid` / `data-heal-id`) so the LLM reasons over semantic roles (`button`, `textbox`, `link`) and accessible names rather than noisy raw HTML.
* **Priority-Ranked Accessibility Tree Self-Healing (Joseph, 2026):** Extracts Playwright accessibility trees using a strict priority cascade (`get_by_role` $\rightarrow$ `data-testid` $\rightarrow$ ARIA labels $\rightarrow$ CSS fragments) to reduce LLM API token costs—aligning with our Stage 3 Playwright-Priority Cascade Synthesizer.
* **ChatGPT-Enhanced Attribute Prioritization (Xu et al., ICST 2025):** Passes Top-10 candidates from classical repair tools (WATER, VISTA, Edit Distance) to an LLM in a two-step prompt to select the best candidate ID and validate explanation consistency.

---

### 1.4. Pipeline Justification: Why DOM + Accessibility Tree + LLM Over Computer Vision (CV)?

A critical defense question is why our pipeline uses **DOM + Accessibility Tree (`AriaSnapshotAsync`) + LLM** rather than **Computer Vision / Image Recognition (CNN / OCR / VLM)**. We distinguish between **Pure Pixel-Coordinate Vision** and **DOM-Anchored Visual Matching (WEBRL)**:

#### A. Comparison Against Pure Vision / Template Matching (VISTA, Sikuli, VLM Coordinate Grounding)
| Comparison Axis | Proposed Pipeline (DOM + Aria Snapshot + LLM) | Pure Vision / CV (CNN / OCR / VLM Coordinate Grounding) |
| :--- | :--- | :--- |
| **1. Output Contract & Code Patching** | Directly yields a **DOM element reference** and synthesizes a deterministic Playwright selector (`GetByRole`, `data-testid`, CSS) that Roslyn patches into the `.cs` Page Object. | Outputs **pixel coordinates $(x, y)$**. Clicking $(x, y)$ does not fix the broken C# selector, and reverse-mapping $(x, y)$ via `document.elementFromPoint(x, y)` frequently lands on decorative child `<svg>`, `<path>`, or `<span>` wrappers or transparent overlays rather than the interactive control holding the event listener or `data-testid`. |
| **2. Resistance to Benign Visual Restyling** | **Invariant** to dark/light mode switches, CSS color/theme changes, font rendering differences across OS containers (Linux Docker vs. Windows), and viewport resizing. | Highly sensitive to color palette swaps (e.g., primary button changing from blue to green), dark mode, resolution scaling, and sub-pixel font anti-aliasing in headless Linux CI containers. |
| **3. Protection Against "Silent Semantic Shift"** | High precision: DOM + Aria Snapshot extracts exact accessible names and roles in $< 5\text{ ms}$ with $100\%$ character fidelity, allowing Tier 1 ($\operatorname{Sim}_{\text{id}}$) and Tier 2 (LLM) to block `"Save"` $\rightarrow$ `"Delete"` swaps. | Visual appearance of a button often remains identical when only its label changes (`"Save"` vs. `"Delete"`). Catching this with CV requires running an OCR pipeline (Tesseract/EasyOCR) on cropped screenshots—introducing OCR character errors and $200\text{–}800\text{ ms}$ of CPU latency to read text already present as a UTF-8 string in the DOM. |
| **4. Spatial Geometry Captured for Free** | `el.getBoundingClientRect()` inside `dom-extractor.js` captures the normalized 2D coordinates $(x, y, w, h)$ of every element in $< 2\text{ ms}$ via the browser's layout engine. | Using image recognition just to determine spatial position duplicates layout calculations the browser's rendering engine (Blink) has already computed. |
| **5. CI/CD Resource & Latency Footprint** | Runs $100\%$ in-process on standard CPU-only CI runners ($P_{50} < 50\text{ ms}$ for Tier 1; $< 800$ text tokens for Tier 2 escalation). | Local CNN/OCR models require heavy native dependencies (`OpenCV`, `ONNX`/PyTorch weights) in the test runner container, while Vision-Language Models (VLMs) require transmitting full-page PNG buffers ($1,000\text{–}2,500$ vision tokens), increasing latency and token cost by $3\text{–}5\times$. |

#### B. Comparison Against Two-Stage DOM-Anchored Visual Matching (WEBRL, ICSME 2024)
Unlike pure template matching, **WEBRL** avoids the `elementFromPoint(x, y)` trap by starting with candidate DOM nodes $e' \in \text{DOM}$ in Stage 1 (identity attributes), cropping each candidate's bounding box from the page screenshot, running a CNN in Stage 2 to rank visual similarity, and synthesizing a selector from the winning DOM node $e'$. While WEBRL also outputs a valid DOM selector, our **Stage 1 Split-Similo $\rightarrow$ Stage 2 Aria Snapshot + LLM** escalation mechanism outperforms WEBRL's Stage 2 CNN on four architectural axes:

| Comparison Axis | WEBRL (Stage 1 Identity DOM $\rightarrow$ Stage 2 CNN Image Similarity) | Proposed Pipeline (Stage 1 Split-Similo $\rightarrow$ Stage 2 Aria Snapshot + LLM) |
| :--- | :--- | :--- |
| **1. Baseline Storage Overhead** | Must capture, store, and version-control thousands of cropped PNG reference images across every test step and screen resolution. | Stores a lightweight, text-only `.locator-lock.json` file directly in Git. |
| **2. Cross-Language / Semantic Redesigns** | If `<button>Checkout</button>` changes to `<button>Complete Order</button>` with a new CSS theme, Stage 1 fails (text changed) and Stage 2 CNN fails (visual pixels of the word and button style changed). | Stage 1 flags low $\operatorname{Sim}_{\text{id}}$ and escalates to Stage 2; the LLM reads the Accessibility Tree YAML and matches the semantic intent (`"Checkout"` $\leftrightarrow$ `"Complete Order"`). |
| **3. Silent Semantic Shift ("Save" $\rightarrow$ "Delete")** | If an icon+text button changes from `"Save"` to `"Delete"` while keeping the exact same dimensions, font, and CSS styling, visual CNN feature embeddings remain dangerously similar. | Our Escalation Veto ($\operatorname{Sim}_{\text{struct}} \ge 0.80 \land \operatorname{Sim}_{\text{id}} < 0.50$) forces Tier 2 evaluation, where the LLM explicitly flags `"is_destructive_semantic_shift": true`. |
| **4. CI/CD Headless Rendering Drift** | Font anti-aliasing, dark/light themes, or responsive viewport shifts between a developer's Windows/macOS machine and a Linux Docker CI runner degrade CNN cosine similarity. | Accessibility Trees (`AriaSnapshotAsync`) and DOM attributes are $100\%$ invariant to OS font rendering and CSS theme colors. |

---

### 1.5. Takeaways & Limitations of Kluge & Stocco (2025) (`Similo++` / `HybridSimilo` / `LLM VON Similo`) Applied to ARIS

#### A. Actionable Takeaways Adopted in Our Architecture
1. **Why Visually Overlapping Nodes (`VON`) Alone Causes False Positives $\rightarrow$ Action-Compatibility Pre-Filtering:**
   Kluge & Stocco showed that clustering visually overlapping parent/child nodes ($IoU \ge 0.85$) causes false positives when a wrapper `<div>` or child `<span>` is selected instead of the interactive `<input>` or `<button>`. Rather than blindly merging overlapping nodes, our `dom-extractor.js` pre-filters candidates by **action compatibility** (`FillAsync` $\rightarrow$ editable controls; `ClickAsync` $\rightarrow$ interactive elements) while encoding `ParentSignature` and `SiblingTags` inside $\operatorname{Sim}_{\text{struct}}$ (paralleling how `HybridSimilo` uses contextual overlap to narrow candidates before exact node selection).
2. **Optimized Attribute Weighting (`Similo++`) $\rightarrow$ Split-Similo Prioritization:**
   Genetic weight optimization in `Similo++` (+5.62% accuracy) proved that raw 2D coordinates degrade rapidly on responsive layouts compared to semantic identity attributes. Our Split-Similo model reflects this by weighting $\operatorname{Sim}_{\text{id}}$ at $\alpha = 0.65$ vs. $\operatorname{Sim}_{\text{struct}}$ at $0.35$, keeping normalized bounding-box proximity at $w = 0.20$ inside $\operatorname{Sim}_{\text{struct}}$.
3. **Latency Overhead of `LLM VON Similo` $\rightarrow$ Selective 2-Tier Escalation Gate:**
   Kluge & Stocco demonstrated that querying GPT-4 on every locator failure (`LLM VON Similo`) increases latency by $100\text{–}1000\times$ while only benefiting hard semantic redesigns. This directly justifies our Tier 1 $\rightarrow$ Tier 2 Escalation Gate ([Section 3.3](#33-tier-1-rightarrow-tier-2-escalation-decision-rule)), which resolves $75\text{–}85\%$ of structural changes locally in $< 20\text{ ms}$.
4. **Expanded Benchmark Resource ($10,376$ Element Pairs):**
   Provides a $23\times$ larger real-world longitudinal dataset from which we sample static HTML evolution pairs for our offline benchmark runner (`AdaptiveUiRecovery.Benchmarks`).

#### B. Limitations of `arXiv:2505.16424` Addressed by Our System
| Limitation in Kluge & Stocco (`arXiv:2505.16424`) | Why It Matters in Real E2E Testing | How Our System (ARIS) Solves It |
| :--- | :--- | :--- |
| **1. "Closed-World / Always-Match" Assumption (No RCA Gatekeeper)** | Every benchmark pair $(V_{\text{old}}, V_{\text{new}})$ in the paper guarantees that the target element exists on $V_{\text{new}}$. In real CI/CD runs, when a page crashes with HTTP `500`, redirects to a `401` login screen, or fails to render a modal, `Similo++` / `HybridSimilo` **still returns the highest-scoring element on the wrong page** (forced false positive). | **Stage 2 Action-Scoped RCA Gatekeeper:** Inspects CDP network/console telemetry within $[t_{\text{action\_start}} - 500\text{ ms}, t_{\text{fail}}]$ and DOM state (`ElementHidden`, `StrictModeAmbiguity`) to block healing when the root cause is not locator drift. |
| **2. Vulnerability to "Silent Semantic Shift" (Flat Weighted Sum)** | Even in `Similo++` and `HybridSimilo`, all 14 attributes are combined into a **single flat weighted sum**. If a button's `tag`, `id`, `class`, `xpath`, and $(x, y, w, h)$ remain $100\%$ identical while its label changes from `"Save Draft"` to `"Delete Account"`, structural/spatial weights overwhelm the single `visible_text` weight. | **Orthogonal Split-Similo ($\operatorname{Sim}_{\text{id}}$ vs. $\operatorname{Sim}_{\text{struct}}$) + Escalation Veto:** Whenever $\operatorname{Sim}_{\text{struct}} \ge 0.80$ but $\operatorname{Sim}_{\text{id}} < 0.50$, Tier 1 is forbidden from auto-healing and escalates to Tier 2 LLM, which flags `"is_destructive_semantic_shift": true`. |
| **3. Context-Blind LLM Prompting & Top-10 Cutoff in `LLM VON Similo`** | The evaluated `LLM VON Similo` variant only passes isolated JSON attribute dictionaries of the Top-10 VON candidates to GPT-4—without surrounding page hierarchy (e.g., header vs. confirmation dialog) and with no ability to recover if the true element ranked `#11+`. | **"Set-of-Marks" Aria Snapshot YAML + Dual-Grounding Schema:** Injects `data-heal-id="cand_1..10"` into the full-page `AriaSnapshotAsync()` YAML and allows the LLM to output a direct `aria_selector` if the redesigned element fell outside Similo's Top-10. |
| **4. No Dynamic CSS-in-JS Hash Filtering** | Evaluates `class` similarity using raw string distance; auto-generated build hashes (`sc-9f8a1b`, `_btn_1x9a2_4`) in React/Angular apps pollute class similarity scores across versions. | **Normalized Shannon Entropy Filter ($H_{\text{norm}}(X) \ge 0.88$):** Strips volatile high-entropy tokens before computing CSS class Jaccard similarity or synthesizing selectors. |
| **5. Stops at Offline Node Matching (No Synthesis, Shadow Retry, or AST Patching)** | Treats relocalization purely as finding a matching DOM node $e' \in V_{\text{new}}$. Does not synthesize a new Playwright locator, verify uniqueness ($\text{CountAsync()} == 1$), execute a Shadow Retry of the test, or patch the broken test source code. | **End-to-End Closed Loop (Stages 3 & 5):** Synthesizes an idiomatic Playwright selector (`GetByRole`, `data-testid`, CSS), verifies live uniqueness, runs the rest of the test via Shadow Retry, and generates a cumulative **Roslyn AST `.patch` file** attached to Allure Report. |

---

## 2. System Components & High-Level Pipeline

The software module consists of **six decoupled components** organized as a sequential pipeline:

1. **`Core.Interception` (`HealableLocatorProxy` & `.Heal()` Extension via `DispatchProxy`, Baseline Harvester):**
   Exposes a postfix `.Heal()` extension method on Playwright's `ILocator` (`public static ILocator Heal(this ILocator locator, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerArgumentExpression("locator")] string locatorExpr = "")`) that returns a standard `ILocator` backed by `DispatchProxy.Create<ILocator, HealableLocatorProxy>()`, avoiding manual implementation of 40+ `ILocator` methods while supporting all `IPage.GetBy*` (`GetByRole`, `GetByTestId`, `GetByText`, etc.) and `IPage.Locator` factories. Shadows `Expect(ILocator)` on `HealablePageTest : PageTest` via `DispatchProxy.Create<ILocatorAssertions, HealableAssertionsProxy>()` so native `await Expect(locator).ToBeVisibleAsync()` awaits `ResolveNativeLocatorAsync()` and forwards the unwrapped native `ILocator` to `Assertions.Expect(resolvedNativeLocator)`, preventing Playwright's internal concrete `Locator` cast `InvalidCastException`. Captures caller metadata (`[CallerFilePath]`, `[CallerMemberName]`, `[CallerLineNumber]`, `[CallerArgumentExpression("locator")]`), enforces a fast-fail polling window ($1.5\text{ s}$ stabilization check instead of waiting for Playwright's full $30\text{ s}$ timeout), and extracts baseline signatures on green runs directly via `await rawLocator.EvaluateAsync<ElementProfile>(domExtractorJs)` (and optionally upgrades in Stage 5 to zero-touch compile-time interception via a C# 12 `[InterceptsLocation]` Roslyn Incremental Source Generator).
2. **`Rca.Engine` (Action-Scoped CDP Telemetry & DOM State Gatekeeper):**
   Continuously buffers network failures (`IPage.Response`, `IPage.RequestFailed`), console errors (`IPage.PageError`), and routing changes. Watermarks/clears the telemetry buffer after every successful `HealableLocatorProxy` action so stale errors from earlier test steps (e.g., an intentional `401` in Step 1) never contaminate later steps. Upon failure at $t_{\text{fail}}$, evaluates events strictly within $[t_{\text{action\_start}} - 500\text{ ms}, t_{\text{fail}}]$ using a deterministic decision tree to isolate `Product Defect`, `Backend Breakdown`, `Environment/Auth Issue`, `UI State Regression`, and `Strict Mode Disambiguation` before permitting locator recovery.
3. **`Recovery.Heuristics` (Tier 1 Split-Similo & Playwright Cascade Synthesizer):**
   Executes a single-trip JavaScript injection (`IPage.EvaluateAsync`) pre-filtered by **action compatibility** (e.g., editable controls for `FillAsync`, interactive elements for `ClickAsync`) to extract feature vectors for candidate DOM elements in $< 15\text{ ms}$. Filters dynamic CSS-in-JS hashes via **Normalized Shannon Entropy** ($H_{\text{norm}}(X) \ge 0.88$ with character-class transition checks) and computes $\operatorname{Sim}_{\text{id}}$ and $\operatorname{Sim}_{\text{struct}}$.
4. **`Recovery.SemanticLlm` (Tier 2 Aria Snapshot + Top-$10$ Candidate Escalation):**
   Triggered only when Tier 1 confidence is ambiguous or exhibits divergence between identity and structure. Injects `data-heal-id="cand_1..10"` ("Set-of-Marks" grounding) into Tier 1's Top-10 candidates, extracts a compact YAML Accessibility Tree (`ILocator.AriaSnapshotAsync()`) alongside the compact HTML of `cand_1..10` and the C# property name/baseline profile, and queries an LLM via Constrained Decoding (`response_format` JSON Schema). Supports both selecting `cand_1..10` or synthesizing a direct `aria_selector` from the full-page YAML if a radically redesigned element fell outside Similo's Top-10.
5. **`Verification.ShadowRunner` (Uniqueness & Deferred TearDown Gate):**
   Validates that the synthesized selector resolves to exactly one element in the live DOM ($\text{CountAsync()} == 1$), executes the healed action in-flight, and appends a `HealingRecord` to a test-scoped `HealingContext` (`AsyncLocal<T>`). Allows the remainder of the E2E test to run to completion ("Shadow Retry"—including healing multiple broken locators in a single test), and defers throwing `LocatorHealedWarningException` to the base class `[TearDown]` hook so broken locators are never silently masked in CI.
6. **`Integration.PatchAndReport` (Cumulative Roslyn AST Rewriter, Compile-Time Interceptors & Allure Lifecycle Adapter):**
   Maintains an in-memory `ConcurrentDictionary<string, SyntaxTree>` keyed by normalized repository-relative path so multiple healed locators within the same Page Object `.cs` file are applied cumulatively to a single syntax tree. Replaces static locator receiver expressions `<ReceiverExpr>` inside `<ReceiverExpr>.Heal()` (covering `Page.GetByRole(...)`, `Page.GetByTestId(...)`, and `Page.Locator("...")`) via `CSharpSyntaxRewriter` while preserving formatting trivia (explicitly excluding dynamic `InterpolatedStringExpressionSyntax` and parameterized expressions from AST patching), emits a unified `.patch` file per Page Object, and attaches the RCA diagnostic summary, visual bounding-box diff screenshot, and patch to the Allure report. Additionally provides an optional Month 5 Roslyn `IIncrementalGenerator` emitting C# 12 `[InterceptsLocation]` interceptors for zero-touch `IPage.Locator`/`GetBy*` and static `Assertions.Expect(ILocator)` interception without `.Heal()`.

---

## 3. Mathematical Models, Complexity & Formal Methods

### 3.1. Split-Similo Similarity Model (Tier 1)
Instead of a single flat sum, candidate elements $e_c \in E_{\text{DOM}}$ (pre-filtered by action compatibility) are evaluated against the baseline profile $e_t$ across two orthogonal vectors:

$$\operatorname{Sim}_{\text{id}}(e_t, e_c) = \frac{\sum_{i \in I} w_i \cdot s_i(a_{t,i}, a_{c,i})}{\sum_{i \in I} w_i}$$

$$\operatorname{Sim}_{\text{struct}}(e_t, e_c) = \frac{\sum_{j \in S} w_j \cdot s_j(a_{t,j}, a_{c,j})}{\sum_{j \in S} w_j}$$

* **Identity Feature Set ($I$):** `data-testid` exact match ($w = 0.30$), `aria-label` / accessible name normalized Levenshtein similarity ($w = 0.25$), `innerText` Levenshtein/Jaccard token similarity ($w = 0.25$), `id` / `name` / `placeholder` similarity ($w = 0.20$).
* **Structural & Spatial Feature Set ($S$):** HTML `tag` and `role` match ($w = 0.25$), Entropy-filtered CSS class Jaccard overlap ($w = 0.25$), immediate parent & sibling tag/id overlap ($w = 0.30$), normalized Euclidean bounding-box proximity ($w = 0.20$).
* **Combined Score:**
  $$\operatorname{Sim}_{\text{total}}(e_t, e_c) = \alpha \cdot \operatorname{Sim}_{\text{id}}(e_t, e_c) + (1 - \alpha) \cdot \operatorname{Sim}_{\text{struct}}(e_t, e_c), \quad \alpha = 0.65$$

### 3.2. Normalized Shannon Entropy Filter for Dynamic CSS-in-JS Tokens
To prevent dynamic classes (`sc-9f8a1b`, `css-1dbjc4n`, `_btn_1x9a2_4`, `:r2:`) from polluting $\operatorname{Sim}_{\text{struct}}$ or generated selectors, every attribute token $X$ of length $n \ge 5$ is evaluated. Because the maximum theoretical Shannon entropy of a string of length $n$ is $\log_2(n)$ (meaning tokens of length $5 \le n \le 9$ have $H_{\max} \le \log_2(9) \approx 3.17\text{ bits/char}$ and can never reach an unnormalized threshold of $3.2\text{ bits/char}$), we use **Normalized Shannon Entropy** $H_{\text{norm}}(X)$ for tokens of length $5 \le n \le 12$:

$$H_{\text{norm}}(X) = -\frac{1}{\log_2(n)} \sum_{k} P(x_k) \log_2 P(x_k)$$

A token is excluded as volatile if:
1. $H_{\text{norm}}(X) \ge 0.88$ (or raw $H(X) > 2.5\text{ bits/char}$ for $n > 12$) **combined with a character-class transition check** ($\ge 2$ transitions between letters and digits, e.g., `a1b2`, or mixed case/hex digits), **or**
2. It matches known framework prefix/suffix patterns (`^(sc-|css-|tw-|svelte-|:r)|^_[a-zA-Z0-9]+_[a-zA-Z0-9]{5,}`).

### 3.3. Tier 1 $\rightarrow$ Tier 2 Escalation Decision Rule
Escalation to the Tier 2 LLM module occurs if and only if at least one of three conditions holds for the top candidate $e_c^*$:
1. **Low Overall Confidence:** $\operatorname{Sim}_{\text{total}}(e_t, e_c^*) < \theta_{\text{high}}$ (where $\theta_{\text{high}} = 0.85$, while $\operatorname{Sim}_{\text{total}} \ge \theta_{\text{min}} = 0.45$).
2. **Candidate Ambiguity:** The margin between the top two candidates is too narrow: $\operatorname{Sim}_{\text{total}}(e_t, e_{c,1}^*) - \operatorname{Sim}_{\text{total}}(e_t, e_{c,2}^*) < \Delta_{\text{min}}$ ($\Delta_{\text{min}} = 0.06$).
3. **Silent Semantic Shift Veto:** Structural similarity is high ($\operatorname{Sim}_{\text{struct}} \ge 0.80$), but Identity/Text similarity dropped sharply ($\operatorname{Sim}_{\text{id}} < 0.50$). This catches cases where a "Save Draft" button was replaced in-place by a "Delete Account" button.

### 3.4. Formal Algorithmic Complexity Analysis (Chapter 2)
* **Tier 1 Feature Extraction & Split-Similo Scoring:**
  * **Time Complexity:** $\mathcal{O}(|E_{\text{DOM}}| \cdot |A|)$, where $|E_{\text{DOM}}|$ is the number of action-compatible interactive candidate nodes on the page ($\sim 30\text{–}200$) and $|A| = 8$ is the constant number of evaluated attribute dimensions (with string comparisons bounded by short attribute lengths $L \le 64$).
  * **Space Complexity:** $\mathcal{O}(|E_{\text{DOM}}| \cdot |A|)$ to store the serialized `ElementProfile` candidate array in memory.
  * **Comparison vs. Classical ROBULA+:** Classical ROBULA+ (Leotta et al., 2016) exhibits exponential worst-case time complexity $\mathcal{O}(b^d \cdot |E_{\text{DOM}}|)$ (where $b$ is the branching factor of specialization rules and $d$ is the DOM ancestor depth) because it iteratively executes live XPath queries against the browser DOM at every specialization step. Our Playwright-Priority Cascade Synthesizer evaluates a bounded priority list of 4 rule families against pre-extracted profiles in $\mathcal{O}(1)$ candidate synthesis time plus at most $k \le 4$ live `CountAsync()` uniqueness checks.

### 3.5. Evaluation Metrics & Economic Efficiency (ROI) Model
* **Precision:** $\text{Precision} = \frac{TP}{TP + FP}$ (Target: $\ge 90\%$)
* **Recall:** $\text{Recall} = \frac{TP}{TP + FN}$ (Target: $\ge 85\%$)
* **False Healing Rate (FHR):** $\text{FHR} = \frac{FP}{TP + FP} \times 100\%$ (Target: $< 5\%$)
* **Fragility Score (FS):** $FS(L) = \frac{1}{K} \sum_{k=1}^{K} \operatorname{broken}(L, V_k)$ across $K$ subsequent versions (Target: $< 0.15$)
* **Pure Algorithm Repair Latency ($P_{50}$ and $P_{95}$):** Measured as $T_{\text{heal}} = t_{\text{patch}} - t_{\text{intercept}}$ (decoupled from Playwright's $1.5\text{ s}$ fast-fail polling window). Target median repair time $P_{50} < 100\text{ ms}$; 95th percentile (with LLM escalation) $P_{95} < 3000\text{ ms}$.
* **Economic Efficiency & Maintenance Time Saved ($\Delta T_{\text{saved}}$ for Chapter 4):**
  $$\Delta T_{\text{saved}} = N_{\text{failures}} \cdot \left(\text{MTTR}_{\text{manual}} - \left(\text{Precision} \cdot \text{MTTR}_{\text{patch}} + \text{FHR} \cdot \text{MTTR}_{\text{debug}}\right)\right)$$
  Furthermore, filtering $75\text{–}85\%$ of structural locator failures through Tier 1 Split-Similo reduces cloud LLM API token consumption and cost by $\sim 80\%$ compared to pure LLM healing (Config B vs. Config C).

---

## 4. Stage-by-Stage Approaches, Alternatives, and Trade-Off Analysis

### Stage 0: Runtime Interception & Baseline Storage

| Design Aspect | Original / Primary Approach | Alternative Approach | Problems, Risks & Trade-Offs |
| :--- | :--- | :--- | :--- |
| **Wrapper Contract & Playwright Assertions** | **Postfix `.Heal()` Extension Returning `ILocator` (`DispatchProxy<ILocator, HealableLocatorProxy>`) + `HealablePageTest.Expect(ILocator)` (`DispatchProxy<ILocatorAssertions, HealableAssertionsProxy>`):** Return a standard `ILocator` dynamic proxy intercepting async actions, paired with an `ILocatorAssertions` proxy that awaits `ResolveNativeLocatorAsync()` before delegating to `Assertions.Expect(resolvedNativeLocator)` (plus a Month 5 C# 12 `[InterceptsLocation]` extension for zero-touch interception). | **Custom `HealableLocator` Return Type (`ExpectHealable()`) OR Unwrapped Direct `ILocator` Implementation.** | **Primary Advantage:** Preserves $100\%$ native `ILocator` types and `await Expect(locator).ToBeVisibleAsync()` syntax in ~80 LOC while supporting all 40+ `ILocator` methods and `GetBy*` factories.<br>**Alternative Problem:** A custom `HealableLocator` type breaks standard `ILocator` return types, requires non-standard `ExpectHealable()`, and only wraps a few methods; passing an unwrapped custom `ILocator` directly to `Assertions.Expect(ILocator)` throws `InvalidCastException` due to Playwright's internal cast to concrete `Locator`. |
| **Caller Mapping & Disambiguation** | **Postfix `.Heal()` with `[CallerArgumentExpression("locator")]`, `[CallerMemberName]`, `[CallerFilePath]`, `[CallerLineNumber]` + `rawLocator.EvaluateAsync()` (Upgradable to C# 12 `[InterceptsLocation]` in Month 5):** Key `.locator-lock.json` and AST patching by `{RelativeFilePath}:{PageClassName}.{MemberName}` disambiguated by the captured C# expression string (`locatorExpr`) and line number; harvest baselines via `rawLocator.EvaluateAsync<ElementProfile>()`. Later extend with a Roslyn Source Generator (`[InterceptsLocation]`) for zero-touch compile-time AST span mapping. | **String-Only `Heal(string selector)` Keyed Solely by `[CallerMemberName]` OR Runtime `StackTrace` PDB Walking.** | **Primary Advantage:** Captures full `Page.GetByRole(...)` / `Page.Locator(...)` C# expressions, disambiguates multiple `.Heal()` calls in one method, and resolves any Playwright locator engine during baseline harvesting.<br>**Alternative Problem:** `Heal(string selector)` forbids `GetByRole`/`GetByTestId` and fails `document.querySelector` on Playwright `internal:` selectors; runtime `StackTrace` walking breaks when JIT inlines property getters in `Release` builds. |
| **Action Timeout & Interception Latency** | **Fast-Fail Polling Window ($1.5\text{ s}$):** Inside `HealableLocatorProxy`, poll `WaitForAsync(new() { Timeout = 1500 })`. If `CountAsync() == 0` after $1.5\text{ s}$, immediately trigger RCA + Healing and measure $T_{\text{heal}} = t_{\text{patch}} - t_{\text{intercept}}$. | **Waiting for Default $30\text{ s}$ Playwright Action Timeout.** | **Primary Advantage:** Keeps end-to-end test execution fast and isolates pure algorithm latency ($< 100\text{ ms}$).<br>**Alternative Problem:** Waiting $30,000\text{ ms}$ before entering the healing pipeline makes every healed step take $> 30\text{ s}$ wall-clock time. |
| **Baseline Harvesting Timing** | **Explicit `HEAL_MODE=Record` Mode (or Lazy Pre-Action Snapshot):** Extract DOM signatures via JS injection immediately *before* action execution only when recording baselines or when cache is missing. | **Continuous Harvesting on Every Green Test Run:** Re-extract and overwrite DOM signatures on every action of every passing test run. | **Primary Trade-off:** Requires running the test suite with a flag (or on the `main` branch CI job) when new tests are added.<br>**Alternative Problem:** Extracting bounding boxes and DOM subtrees on every single click adds $15\text{–}30\%$ runtime overhead to normal regression suites and causes constant noisy Git diffs in the baseline JSON file. |
| **Baseline Persistence** | **In-Repo JSON Lockfile (`.locator-lock.json`):** Keyed by normalized Page Object and property identifier, committed alongside test code. | **External Database (PostgreSQL / SQLite Server like Healenium):** Store element snapshots in a centralized DB container. | **Primary Trade-off:** Large test suites (500+ locators) create a multi-megabyte JSON file unless split per Page Object.<br>**Alternative Problem:** Requires Docker/DB infrastructure in CI/CD and breaks branch isolation when two branches modify UI elements simultaneously. |

---

### Stage 1: Experimental Testbed & Dataset Construction

| Design Aspect | Original / Primary Approach | Alternative Approach | Problems, Risks & Trade-Offs |
| :--- | :--- | :--- | :--- |
| **Live E2E Testbed Strategy** | **1–2 Self-Hosted Docker Apps (OWASP Juice Shop / Conduit) + Lazy Pre-Action / `MutationObserver` DOM Mutation Injector & Network Fault Injector (`RouteAsync`) + Subset of Public `Similo2`/`WEBRL` Dataset.** | **One-Shot `AddInitScriptAsync` Mutation Script OR Writing 60 Independent Tests Across 4 Separate Apps from Scratch.** | **Primary Advantage:** Injecting mutations lazily inside `HealableLocatorProxy` (just before querying, after Angular/React renders) or via a persistent `MutationObserver` guarantees mutations survive SPA hydration.<br>**Alternative Problem:** A one-shot script in `AddInitScriptAsync()` runs before SPA hydration on an empty `<app-root></app-root>`, or Angular/React immediately overwrites mutated attributes upon mounting. |

---

### Stage 2: Root Cause Analysis (RCA) Gatekeeper

| Design Aspect | Original / Primary Approach | Alternative Approach | Problems, Risks & Trade-Offs |
| :--- | :--- | :--- | :--- |
| **Action-Scoped Telemetry Window & Watermarking** | **Per-Action Watermarked CDP Ring Buffer:** Clear/watermark the buffer at the end of every *successful* `HealableLocatorProxy` action; on failure at $t_{\text{fail}}$, evaluate only domain-whitelisted API/console events within $[t_{\text{action\_start}} - 500\text{ ms}, t_{\text{fail}}]$. | **Unwatermarked Global Test-Run Ring Buffer (Last 50 Events Across Entire Test).** | **Primary Advantage:** Prevents stale errors from earlier steps from contaminating later steps.<br>**Alternative Problem:** If Step 1 intentionally triggers a `401 Unauthorized` (e.g., testing invalid login recovery) or a non-fatal background error, an unwatermarked buffer will falsely classify a Step 4 broken locator as `Environment_AuthOrNetwork` and block healing. |
| **Playwright Strict Mode Handling** | **Dedicated `Disambiguation Mode` Branch:** Detect when `CountAsync() > 1` (Playwright strict mode violation) and use Similo spatial/parent scoring to refine the locator rather than treating it as a missing element. | **Treat All `PlaywrightException` Errors Identically:** Run full page scan even when the old selector matched $2$ elements. | **Primary Trade-off:** Adds an extra branch in the RCA decision tree.<br>**Alternative Problem:** Ignoring strict-mode violations misses one of the most common failure modes in Playwright (when a new button with the same text/class is added to the page). |

---

### Stage 3: Tier 1 Heuristic Matching & Selector Synthesis

| Design Aspect | Original / Primary Approach | Alternative Approach | Problems, Risks & Trade-Offs |
| :--- | :--- | :--- | :--- |
| **DOM Feature Extraction & Pre-Filtering** | **Single-Trip Action-Filtered Browser JS Injection (`IPage.EvaluateAsync`):** Filter candidates by action compatibility (`FillAsync` $\rightarrow$ `<input>`, `<textarea>`, `[contenteditable]`, `[role=textbox]`; `ClickAsync` $\rightarrow$ interactive elements), compute `getBoundingClientRect()`, and return a serialized JSON array in $\sim 10\text{–}20\text{ ms}$. | **Playwright C# API Iteration (`page.Locator("*").AllAsync()`) Without Action Filtering.** | **Primary Advantage:** Reduces candidate noise from ratusan of decorative `<div>`/`<span>` nodes to typically $< 35$ interactive elements and completes in one CDP roundtrip.<br>**Alternative Problem:** Calling `BoundingBoxAsync()` across 500 unfiltered elements from C# causes 500+ sequential WebSocket roundtrips over CDP ($5000\text{–}9000\text{ ms}$). |
| **Selector Synthesis Algorithm** | **Playwright-Priority Cascade Generator:** Synthesize selectors in strict stability order: `data-testid` $\rightarrow$ `role=...[name=...]` $\rightarrow$ unique stable attribute (`name`, `placeholder`, `href`) $\rightarrow$ parent anchor + entropy-filtered CSS/XPath. | **Full Classical ROBULA+ Upward/Downward XPath Refinement:** Implement the complete recursive XPath specialization algorithm from Leotta et al. | **Primary Trade-off:** Generates Playwright engine selector strings (e.g., `internal:role=button[name="Submit"i]` or CSS) rather than pure W3C XPaths.<br>**Alternative Problem:** Full ROBULA+ has exponential worst-case complexity $\mathcal{O}(b^d \cdot |E_{\text{DOM}}|)$ and produces brittle hierarchical XPaths that go against modern Playwright best practices. |

---

### Stage 4: Tier 2 Semantic LLM Escalation

| Design Aspect | Original / Primary Approach | Alternative Approach | Problems, Risks & Trade-Offs |
| :--- | :--- | :--- | :--- |
| **DOM Context Compression ("Set-of-Marks" for DOM + AXTree)** | **Three-Part Grounded Payload:** Inject `data-heal-id="cand_1..10"` before calling `page.Locator("body").AriaSnapshotAsync()`, sending: (1) Target Baseline Profile + C# property name (e.g. `CheckoutButton`), (2) Full-page Aria Snapshot YAML for surrounding hierarchy, and (3) Compact HTML + Similo scores of `cand_1..10` ($< 800\text{ tokens}$ total). | **Option A: Isolated Top-10 JSON Dicts Only (LLM VON Similo style).**<br>**Option B: Pruned Full HTML DOM Tree or VLM Screenshot.** | **Primary Advantage:** Combines global hierarchical context (e.g., knowing a button is inside `dialog "Confirm Deletion"`) with exact HTML attributes of top candidates.<br>**Alternative A Problem:** Lacks surrounding page context and misses elements ranked `#11..#35`.<br>**Alternative B Problem:** Full HTML trees consume $15,000\text{–}60,000\text{ tokens}$ and VLM screenshots add $1,000\text{–}2,500$ vision tokens with no statistically significant accuracy gain ($p = 0.37$, D2Snap 2025). |
| **LLM Output Contract & Top-10 Blind Spot Recovery** | **Constrained Decoding JSON Schema (`response_format`) with Dual Grounding:** LLM returns `selected_candidate_id` (`cand_1..10 | none`), `aria_selector` (direct ARIA selector from the full-page YAML if the redesigned target ranked outside Similo's Top-10), `recommended_selector`, `is_destructive_semantic_shift` (bool), `confidence`, and `reasoning`. Verified via mandatory `CountAsync() == 1` gate. | **Restricting LLM Strictly to `cand_1..10` OR Free-Form Unconstrained Text Generation.** | **Primary Advantage:** Eliminates the "Top-10 Blind Spot" when a radical redesign (tag + text + position change) pushes the true element to rank `#18` in Similo while remaining clearly visible in the full-page Aria YAML.<br>**Alternative Problem:** Strictly forcing `cand_1..10` fails whenever Similo ranks the true element `#11+`; free-form output without `response_format` schema breaks parsers or hallucinates selectors. |
| **LLM Hosting & Multi-Model Evaluation** | **Unified `ILlmProvider` Adapter Comparing Cloud Frontier (`Gemini 2.5 Flash`, `GPT-4o-mini`) vs. Code-Specialized & General Open-Weights (`Qwen-2.5-Coder-32B-Instruct`, `Llama-3.3-70B-Instruct`).** | **Running Local General Chat LLM on Laptop CPU.** | **Primary Advantage:** Code-specialized models (`Qwen-2.5-Coder`) are heavily pre-trained on HTML, YAML, and Playwright/Testing-Library syntax, reducing selector syntax hallucinations.<br>**Alternative Problem:** Running a 7B–14B model on CPU takes $20\text{–}45\text{ s}$ per request, skewing MTTR benchmarks. |

---

### Stage 5: Runtime Verification, Roslyn AST Patching & Allure Reporting

| Design Aspect | Original / Primary Approach | Alternative Approach | Problems, Risks & Trade-Offs |
| :--- | :--- | :--- | :--- |
| **Runtime Healing & Exception Timing** | **In-Flight Healing + `AsyncLocal<HealingContext>` + Deferred `[TearDown]` Exception:** Heal and execute $L_{\text{new}}$ normally during the test so all subsequent steps (and any additional broken locators) execute ("Shadow Retry"). If final assertions pass, mark `ShadowRetryPassed = true`, generate patches, and throw `LocatorHealedWarningException` in `[TearDown]`. | **Option A: Throw `LocatorHealedWarningException` Immediately Inside `HealableLocatorProxy`.**<br>**Option B: Silent Auto-Pass (Healenium style).** | **Primary Advantage:** Executes the full E2E test to empirically prove $TP$ and heals multiple broken locators (e.g., Step 2 and Step 4) in a single run, while still failing the CI build in `[TearDown]`.<br>**Alternative A Problem:** Throwing inside `HealableLocatorProxy` aborts the test immediately—Shadow Retry never runs and Step 4 is never reached.<br>**Alternative B Problem:** Masks real regressions in CI. |
| **Roslyn AST Source Code Rewriting** | **In-Memory `ConcurrentDictionary<string, SyntaxTree>` Keyed by Relative Path + Receiver Replacement (Plus Optional C# 12 `[InterceptsLocation]` Source Generator):** Apply multiple heals in the same Page Object cumulatively to the same in-memory `SyntaxTree`, replacing `<ReceiverExpr>` in `<ReceiverExpr>.Heal()` with idiomatic C# Playwright locator calls (`Page.GetByRole(...)`, `Page.GetByTestId(...)`, `Page.Locator(...)`) and emitting one unified `.patch` file per Page Object. Explicitly exclude dynamic `InterpolatedStringExpressionSyntax` and parameterized expressions from AST rewriting; optionally add a Month 5 `IIncrementalGenerator` (`[InterceptsLocation]`) for zero-touch compile-time interception. | **Parsing Disk File Independently Per Healed Locator or Rewriting Dynamic Interpolated Strings.** | **Primary Advantage:** Prevents conflicting `.patch` files when two properties in `LoginPage.cs` heal in the same run, preserves idiomatic `GetByRole`/`GetByTestId` code style, and avoids embedding build-machine absolute paths.<br>**Alternative Problem:** Independent disk parses overwrite each other when applied, and runtime values in interpolated strings (`$"//tr[td='{name}']"`) cannot be statically patched into source code. |

---

## 5. Known Architectural Limitations & Threats to Validity (Chapter 4 Defense Preparation)

To provide a rigorous, scientifically transparent evaluation for the examination committee (ДЕК), Chapter 4 explicitly documents the boundary conditions where locator-level self-healing cannot recover a test:

1. **1-to-$N$ Interaction Flow Decomposition:**
   When a UI refactoring decomposes a single atomic action (e.g., clicking a `"Quick Buy"` button) into a multi-step procedural workflow (e.g., clicking `"Buy"`, selecting a shipping address in a modal, and clicking `"Confirm"`), single-locator recovery cannot synthesize new intermediate test actions or assertions.
2. **Non-DOM Rendering Surfaces (`<canvas>` / WebGL / Closed Shadow DOM):**
   Applications rendered entirely inside an HTML5 `<canvas>` or WebGL context (e.g., Figma, map canvases, Unity WebGL) or encapsulated within closed Shadow DOM roots (`mode: 'closed'`) expose neither standard DOM subtrees nor Playwright Accessibility Trees (`AriaSnapshotAsync`).
3. **Cross-Origin `<iframe>` Boundaries:**
   Elements embedded inside cross-origin `<iframe>` documents require explicit Playwright frame-context resolution (`page.FrameLocator(...)`) prior to injecting `dom-extractor.js` due to browser Same-Origin Policy restrictions.
4. **Parameterized / Dynamic Runtime Selectors:**
   Locators constructed dynamically at runtime via string interpolation (`public ILocator RowByName(string name) => Page.Locator($"//tr[td='{name}']").Heal();`) can be diagnosed by the RCA engine and healed in memory during the test run, but are intentionally excluded from automated Roslyn AST source patching because runtime parameter values cannot be reversed into static string literals.

---

## 6. Key References

1. **Kluge, A., & Stocco, A. (2025).** Web Element Relocalization in Evolving Web Applications: A Comparative Analysis and Extension Study. *arXiv*. https://doi.org/10.48550/arxiv.2505.16424
2. **Leotta, M., Clerissi, D., Ricca, F., & Tonella, P. (2016).** ROBULA+: An Algorithm for Generating Robust XPath Locators for Web Testing. *Journal of Software: Evolution and Process*, 28(3), 177–204.
3. **Nass, M., Alégroth, E., & Feldt, R. (2024).** Improving Web Element Localization by Using a Large Language Model. *Software Testing, Verification and Reliability*, 34. https://doi.org/10.1002/stvr.1893
4. **Nass, M., Alégroth, E., Feldt, R., Leotta, M., & Ricca, F. (2022).** Similarity-based web element localization for robust test automation. *arXiv*. https://doi.org/10.48550/arxiv.2208.00677
5. **Patel, A., Hofmarcher, M., Leoveanu-Condrei, C., Dinu, M.-C., Callison-Burch, C., & Hochreiter, S. (2024).** Large Language Models Can Self-Improve At Web Agent Tasks. *arXiv*. https://doi.org/10.48550/arxiv.2405.20309
6. **Schiepanski, T. M., & Piël, N. (2025).** Beyond Pixels: Exploring DOM Downsampling for LLM-Based Web Agents. *arXiv*. https://doi.org/10.48550/arxiv.2508.04412
7. **Stocco, A., Yandrapally, R., & Mesbah, A. (2018).** Visual web test repair. *Proceedings of the 2018 26th ACM Joint Meeting on European Software Engineering Conference and Symposium on the Foundations of Software Engineering (ESEC/FSE)*. https://doi.org/10.1145/3236024.3236063
8. **Wen, C., et al. (2024).** Enhancing Web Test Script Repair Using Integrated UI Structural and Visual Information (WEBRL). *Proceedings of the IEEE International Conference on Software Maintenance and Evolution (ICSME 2024)*.
9. **Xu, Z., et al. (2025).** ChatGPT-Enhanced Attribute Prioritization for Web Test Repair. *Proceedings of the IEEE Conference on Software Testing, Verification and Validation (ICST 2025)*.
10. **Joseph, R. N. (2026).** Priority-Ranked Accessibility Tree Self-Healing for Automated Web Testing. *Preprint*.
