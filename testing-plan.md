# GGNet — Testing PR Plan (pre-interactivity)

> **STATUS: complete (2026-07-08).** All phases (0–5) done. Solution now runs **229 tests** via `dotnet test GGNet.slnx` (was **0** — the false-green is closed): `GGNet.Components.Tests` 11 (Phase 0 harness + Phase 2 circuit), `GGNet.Headless.Tests` 171 (incl. Phase 1 handlers, Phase 3 scales, Phase 4 all-cultures), `GGNet.Evals` 47. Green under all three CI gates (`-warnaserror`, test, `dotnet format`) and the `sv-SE` culture job. New deps: **bunit 2.7.2**, **Moq 4.20.72**; one production change: `InternalsVisibleTo("DynamicProxyGenAssembly2")` for Moq-on-internals. `ci.yml` unchanged (the slnx was the fix); trigger flip stays out (Release §3).

*Compiled 2026-07-08. A standalone plan for the testing PR that lands **before** interactivity. Grounded in the current `tests/` tree on `ai-skills-mcp-jul2026`; every "already covered" / "not covered" claim is verified against real files. This PR tests **current code only** — it builds the coverage and the component/circuit harness that (a) gate the eventual spec/realization split (`interactivity-blast-radius.md` §8c) and (b) the interactivity work (A + C) will build on. It writes no tests for features that don't exist yet.*

## 1. Why this PR, and why first

Two jobs, both prerequisites the roadmap already names (Testing B+ → A−: "interactive circuit paths — tooltips, mouse events, refresh — are untested"):

1. **Close the net-blind coverage gap.** The byte-pinned gallery is a single static Headless render; it cannot see the live circuit, the refresh loop, or concurrency — exactly where the split's regression risk concentrates (§8c). This PR covers that surface on today's code.
2. **Stand up the component/circuit harness.** GGNet has *no* live-component test today. Interactivity's discrete events, imperative API, and opt-in gate (Options A + C) all need one. Building it now, against stable code, de-risks both the split and interactivity.

## 2. Grounded starting point

**Projects (both `net11.0`):** `GGNet.Headless.Tests` (xUnit 2.9.3, AwesomeAssertions 9.4.0, Verify.Xunit 31.12.5 for goldens) and `GGNet.Evals`. **No Moq** in the repo — the stack is xUnit + AwesomeAssertions (+ Verify). Internals are exposed via `InternalsVisibleTo` to `GGNet.Headless` and `GGNet.Headless.Tests` only (`GGNet.csproj:26-27`) — so `GGNet.Headless.Tests` is the existing home for internal-facing tests (RenderPipeline, ShapeComposer, SortedBuffer, Breaks).

**CI reality (confirmed defect, must fix — see Phase 5):** `.github/workflows/ci.yml` runs `dotnet test GGNet.slnx`, but `GGNet.slnx` contains **only the four `src` projects — zero test projects**. So CI runs **zero tests today**: a latent false-green, masked only because the workflow is `workflow_dispatch` and has never fired. The existing suite is not gated by CI at all.

**Already covered (do not duplicate):**
- Multi-pass **output** equivalence at the Headless-string level — `RenderPipelineTests`: `RenderTwiceIdentical`, `RenderTwiceWithLegendIdentical`, `RenderTwiceThroughStatSourceIdentical`, `StatSourceRebinsOnRefresh`, `RenderTwiceFacetedIdentical`.
- `SortedBufferTests`, `TransformationsTests`, `ShapeComposerTests`, `Breaks`/`GridComposition`/`PanelLayout`/`PlotLayout`, polar (`CoordPolar`/`PolarProjection`/`PolarGridSvg`/`RadarSvg`), `PublicApiTests` (pinned 74-type surface), `OverloadConsistencyTests`, `ThemeContractTests`, gallery goldens, `LocaleTests` (three hardcoded cultures).

**Not covered today (this PR's target):**
- **Render-mode handlers** — `src/GGNet/Rendering/*` (`InteractiveRenderModeHandler` channel loop / coalescing / backpressure / disposal, `RenderModeHandler.Factory`, `ShouldRender` gating, `ChildRenderHandler`). Zero tests.
- **Live components & the circuit** — `Plot`/`Panel`/`Area`/`Tooltip`: event wiring (`@onclick`/`@onmouseover`/`@onmouseout`, panel `@onclick`), `Tooltip.Show/Hide`, child-refresh propagation, the refresh path end-to-end. Zero tests (Headless renders the tree to string but fires no events and never runs the Interactive loop).
- **Direct persist/clear semantics** — the §8c #1 seam, asserted directly rather than implied through the pipeline.
- **All-cultures locale** — the roadmap's cheap win.

## 3. Scope

**In:** render-handler unit tests; the component/circuit harness + its coverage; direct persist/clear unit tests; locale widening; wiring the new project into CI.

**Out (deferred, with reason):**
- **Playwright** → Option B only. No JS/interactivity exists yet; nothing to drive in a browser.
- **Interactivity-feature tests** (`ViewRange`/`SetView`/opt-in gate) → land with A + C, on this PR's harness.
- **Stryker mutation testing** → diagnostic, run 1–2×/year (roadmap); not a gate, not this PR.
- **CI trigger flip** (`workflow_dispatch` → push/PR) → release-gated (roadmap Release §3, after tag `2.0.0`). This PR makes CI *able* to run the new tests; it does not change when CI fires.

## 4. Tooling decision — bUnit (confirmed .NET 11-ready)

bUnit is confirmed working on net11, so it **is** the component harness — no spike-to-decide, no multi-targeting. Standard `RenderComponent`/`Find`/event-trigger API, least code to own; it stubs JSInterop, which is complete for the current no-JS surface *and* is the exact boundary Playwright takes over when Option B's JS lands (bUnit can assert the .NET side *calls* a module, never that the JS acted). GGNet's existing `HeadlessRenderer` (`src/GGNet.Headless/Host.cs`) remains a zero-dependency "driven circuit" alternative the roadmap allows, but it is not needed.

**Project layout (revised — internals vs components split).** Two homes, by visibility:
- **`GGNet.Headless.Tests`** (existing) hosts the **internal-facing** tests — Phase 1 render handlers and Phase 3 scales. It already has `InternalsVisibleTo` (`GGNet.csproj:27`) and is already the core-internals test home, so no new IVT entry is needed and nothing is duplicated.
- **`GGNet.Components.Tests`** (new, `net11.0`) → references `src/GGNet` + xUnit 2.9.3 + AwesomeAssertions 9.4.0 + bUnit. Scope: **bUnit component/circuit tests on the public component API** (`Plot`/`PlotContext`/`RenderMode` are public). It needs **no IVT** unless a specific component test must touch an internal (likely none); add `<InternalsVisibleTo Include="GGNet.Components.Tests" />` only if that arises.

**Doubling `IPlotRendering` (Phase 1): Moq.** `Mock<IPlotRendering>` recording `Render(target)`/`StateHasChangedAsync` is the tool. It is safe even in the concurrency stress test: the handler's channel is `SingleReader` (`InteractiveRenderModeHandler.cs`), so `plot.Render()` is invoked only by the single background-loop thread — never concurrently — so there is no parallel-recording race. Gate the deterministic wait on a `TaskCompletionSource` returned from the mocked `StateHasChangedAsync`. (This is GGNet's first Moq usage; it stays confined to these handler tests.)

## 5. Work breakdown (phased; each phase lands green independently)

**Phase 0 — bUnit harness setup — ✅ DONE (2026-07-08).**
Project `tests/GGNet.Components.Tests` created (`net11.0`, refs `src/GGNet`, **bunit 2.7.2**, xUnit + AwesomeAssertions; no IVT — public API only). `HarnessSmokeTests` lands both reference tests, green, and clean under all three gates (`-warnaserror`, `dotnet format whitespace`/`style`). The **reusable pattern for Phase 2**:

```csharp
using var ctx = new BunitContext();                               // bUnit 2.x: BunitContext, not TestContext
var context = PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Point(onclick: (item, e) => …).Style();
var cut = ctx.Render<Plot<XY, double, double>>(p => p             // Render<T>, not RenderComponent (obsolete-as-error)
    .Add(x => x.Context, context)
    .Add(x => x.RenderMode, RenderMode.InteractiveAuto));         // synchronous inline → no WaitForState
cut.Markup.Should().Contain("<svg");                              // markup assertions
cut.Find("circle").Click();                                      // fire a mark event through the circuit
```

Notes carried to Phase 2: no DI services are needed to render `Plot`/`Panel`/`Area`/`Tooltip`; the test namespace `GGNet.Components.Tests` resolves `GGNet`/`GGNet.Components` via enclosing scope (no global usings for them — `IDE0005`). *Not yet done (Phase 5):* the project is not in `GGNet.slnx`, so CI does not run it yet.

**Phase 1 — Render-mode handler unit tests — ✅ DONE (2026-07-08).**
`RenderModeHandlerTests` (9 tests) in `GGNet.Headless.Tests`, green; clean under `-warnaserror` and format. Doubled `IPlotRendering` with `Mock<IPlotRendering>` (**Moq 4.20.72** added) — this required `<InternalsVisibleTo Include="DynamicProxyGenAssembly2" />` on `src/GGNet/GGNet.csproj` (Castle DynamicProxy cannot proxy an *internal* interface without it; the price of Moq-on-internals — a hand fake would not have touched the production project). Determinism uses the handler's own backpressure as the signal: the mocked `StateHasChangedAsync` releases a test `SemaphoreSlim`, so the coalescing/serialization tests synchronize on "a batch was rendered" with **no `Task.Delay`**. Moq is scoped by a per-file `using Moq;` (its `Match` collides with the globally-used `System.Text.RegularExpressions.Match`). Coverage: Factory mode mapping + unknown-mode throw; Static/InteractiveAuto `ShouldRender`; Interactive one-shot `ShouldRender`; `ChildRenderHandler` single-bit; **coalescing + Render-subsumes-Loading** (burst queued behind backpressure → one `Render`); idempotent `DisposeAsync`; repeated-refresh serialization. *Discovered for Phase 5:* `GGNet.Headless.Tests` carries pre-existing `IDE0005` format warnings (redundant `global using GGNet;`/`GGNet.Headless;`, and in `OverloadConsistencyTests`/`GridCompositionTests`) — never caught because the project isn't in the slnx; fix before gating.

Original scope (all covered): observe the handler in isolation with `Mock<IPlotRendering>`.
- `RenderModeHandler.Factory` maps each `RenderMode`; `ShouldRender` gating (Interactive's `Interlocked.Exchange` one-shot; Static false).
- `InteractiveRenderModeHandler`: `RefreshAsync` **coalesces** multiple queued targets into one `Render`; a queued `Render` subsumes a queued `Loading`; the semaphore **serializes** (next render waits for `OnAfterRender`); `DisposeAsync` cancels the loop and is idempotent.
- `ChildRenderHandler` single-bit pending: set-then-consume never loses an update.
- **Concurrency stress** (§8c #4): interleave the **single writer** `RefreshAsync` (production writes are circuit-serialized; the channel is `SingleWriter`/`SingleReader`) with the background render loop and `DisposeAsync` → last target wins, no lost final render, clean shutdown, no deadlock. Repeat-runnable. *Do not* stress with N concurrent writers — that violates the channel's single-writer contract and exercises an unreachable state.
*Exit:* `Rendering/*` covered; stress test green under repetition.

**Phase 2 — Component / circuit tests — ✅ DONE (2026-07-08).**
`PlotComponentTests` (5) + `PlotInteractionTests` (5) in `GGNet.Components.Tests`, all green, clean under `-warnaserror`/format, and under `sv-SE`. Rendered with `RenderMode.InteractiveAuto` (synchronous inline → no `WaitForState`). Covers the roadmap's untested triad and more: **structure** (svg/`viewBox`/panel/circle counts), **legend** (color scale), **Static ≡ InteractiveAuto** markup (normalizing the Guid `Id` and bUnit's internal `blazor:onX="N"` event ids), **re-render idempotence** (component-level, complementing `RenderPipelineTests`); **mouse events** — point `onmouseover`/`onmouseout`, bar (`rect.animate-bar`) `onclick`, panel `onclick` + null-safe no-op; **tooltip** — hover shows the `foreignObject` bubble, mouse-out clears it (driven end-to-end through a mark hover, so no internal `Zone` dependency → project stays IVT-free). Robust selectors: data marks carry a class only under `animation: true`. Whole solution now **227 tests**. *Note:* child-refresh **gating** is not proven here (InteractiveAuto's child is a no-op that always renders) — it's covered in Phase 1.

Original approach (retained): render with `RenderMode.InteractiveAuto` — a synchronous inline handler running the real `Render()` pipeline — so re-render completes within the event/`@ref` dispatch turn and assertions are deterministic without `WaitForState` (§6).
- `Plot` renders `<svg viewBox="0 0 W H">`, the panel `<rect class="panel">`, legend when present; **`Static` vs `InteractiveAuto`** produce the same static markup (both synchronous — do *not* compare against the async `Interactive` handler here, which would become a dispatcher/timing test; real `Interactive` behavior is Phase 1).
- **Mouse events** (roadmap-named): `Area` circle/rect/line/polygon fire `@onclick`/`@onmouseover`/`@onmouseout` → bound geom handler invoked; `Panel` background `@onclick` → `Data.OnClick` invoked, and null-safe no-op when unset.
- **Tooltip** (roadmap-named): `Show(x,y,offset,content)` projects via `Coord.Project` and renders the `foreignObject` bubble; `Hide` clears it.
- **Refresh path** (roadmap-named): trigger a refresh → the component re-renders and output matches a second render (idempotence through the live circuit, complementing `RenderPipelineTests`' string level). *Scope note:* under `InteractiveAuto` the child handler is a no-op that always renders, so this proves *component render + event wiring + refresh-causes-rerender* — **not** the child-refresh **gating** (the single-bit set-then-consume), which is the real `Interactive` `ChildRenderHandler` and is covered in Phase 1.
*Exit:* "tooltips, mouse events, refresh" — the roadmap's untested triad — covered.

**Phase 3 — Direct persist/clear unit tests — ✅ DONE (2026-07-08).**
`ScalePersistenceTests` (4) in `GGNet.Headless.Tests` (`using GGNet.Scales;` per-file — internals via IVT), green under all gates. Asserted through `Extended`/`DiscretePosition<int>` via observable `Range`/`Map` (the `_min/_max` fields are protected): `ExtendedCommitHonorsLimitsOverTrainedBounds` (Limits (5,10) beat trained (0,100) → `Range` ≈ (4.75, 10.25) with 5% expansion); `ClearDropsTrainedBounds` (post-Clear, no reshape → `Range` collapses to ±0.05); **`ClearPreservesLimits`** (Limits survive `Clear()` and still drive `Range` — the §8c #1 invariant a runtime `ViewRange` depends on); `DiscretePositionClearEmptiesValues` (`Map` returns index before `Clear`, `NaN` after — guards the SortedBuffer scar).

**Phase 4 — Locale widening — ✅ DONE (2026-07-08).**
Replaced the three `[InlineData]` cultures in `LocaleTests` with a single `[Fact]` `GeometryIsCultureInvariantAcrossAllCultures` looping `CultureInfo.GetCultures(CultureTypes.AllCultures)` — every installed culture renders geometry byte-identical to invariant (no comma decimals, no U+2212), with the culture name in each `because` so a failure self-identifies. Fast (the whole Headless suite stays sub-second). Exotic cultures (non-Latin digits, U+2212 minus) are where invariance bugs hide.

**Phase 5 — CI / solution wiring — ✅ DONE (2026-07-08).**
Added all three test projects (`GGNet.Headless.Tests`, `GGNet.Evals`, `GGNet.Components.Tests`) to `GGNet.slnx` under a `/tests/` folder. `dotnet test GGNet.slnx` now runs **218 tests** (was **0** — the confirmed false-green is closed). Swept the pre-existing format debt (4 `IDE0005` redundant usings in `GGNet.Headless.Tests`). All three CI gates green on the full solution: `dotnet build GGNet.slnx -warnaserror` (0 warnings, test projects now included), `dotnet test GGNet.slnx` (218/218), `dotnet format whitespace`/`style --verify-no-changes`. New tests also pass under the `sv-SE` `test-culture` job. **No `ci.yml` change needed** — it already runs `dotnet test`/`build`/`format` on `GGNet.slnx`; the solution was the fix. Trigger flip stays out (Release §3).

Prior framing (for the record): the real fix is the solution, not the workflow — `GGNet.slnx` listed only `src` projects, so `dotnet test GGNet.slnx` ran nothing. **Add every test project — the existing `GGNet.Headless.Tests` and `GGNet.Evals`, plus the new `GGNet.Components.Tests` — to `GGNet.slnx`**, so restore/build/test all pick them up. Consequences to accept (Codex missed these): the test projects then fall under `dotnet build GGNet.slnx -warnaserror`, `dotnet format GGNet.slnx --verify-no-changes`, **and** the `test-culture` job (`dotnet test` under `sv-SE`) — so the new project must be warning-clean, format-clean, and culture-robust (the last is a free win: component tests get comma-decimal coverage). Alternative if keeping tests out of the slnx is preferred: change CI to `dotnet test` over `tests/**/*.csproj` globs. The trigger flip to push/PR stays out (Release §3).

## 6. Risks & unknowns

- **bUnit on net11** — resolved (confirmed ready); no multi-target needed, and the `HeadlessRenderer` driven-circuit stays an unused zero-dep fallback.
- **Background-thread Interactive handler vs a test dispatcher** — largely resolved by an existing seam, not a risk to engineer around. `RenderMode.InteractiveAuto` is already a **synchronous inline** handler (`InteractiveAutoRenderModeHandler`: `RefreshAsync` → `plot.Render(Render)` inline; `ShouldRender` → `true`), running the *same* `Render()` pipeline as `Interactive` minus the channel. So **component tests (Phase 2) render with `InteractiveAuto`** → every event/`@ref` refresh completes within the dispatch turn, deterministically, with **no `WaitForState`**. The async `Interactive` channel loop is never tested through a component — only **in isolation** (Phase 1) via `Mock<IPlotRendering>` + a `TaskCompletionSource` (assert coalescing/backpressure on signals, not sleeps).
- **Async flakiness** — a Phase-1-only concern now (coordinating with the real `Interactive` loop). Deterministic waits/signals only; no `Task.Delay` (mirrors the integration-test rule). *Optional, additive:* make the `Interactive` loop pumpable/injectable to single-step its coalescing under test — nice-to-have, not required.

## 7. Definition of done

- `GGNet.Components.Tests` green locally under all three gates.
- `Rendering/*` covered incl. a repeatable concurrency stress test.
- The named circuit paths (mark events, panel click, tooltip Show/Hide, refresh) covered.
- Persist/clear asserted directly; `LocaleTests` widened to all cultures.
- **`GGNet.slnx` includes every test project, so `dotnet test GGNet.slnx` actually runs them — the pre-existing zero-tests false-green is closed.**
- **Headless preservation:** adding the harness, any `InternalsVisibleTo`, or the new project does **not** move the Headless gallery goldens or the `PublicApiTests` snapshot.
- The harness choice and pattern documented, so interactivity's A + C tests (and the opt-in/Headless-purity gate assertion) slot straight in.

## 8. Explicitly deferred to later PRs

| Item | Lands with | Why not now |
|---|---|---|
| Playwright end-to-end | Option B | No JS/pointer surface exists yet |
| `ViewRange`/`SetView`/opt-in-gate tests | Interactivity A + C | Features don't exist; harness built here |
| Stryker mutation score | standalone, periodic | Diagnostic, not a gate |
| CI trigger flip (push/PR) | Release §3 (post-`2.0.0`) | Release-sequenced hygiene item |

---

*This plan is the down-payment §8c calls for: it closes the multi-pass/circuit/concurrency coverage the gallery can't see, on stable code, and leaves behind the harness the interactivity build order (`interactivity-blast-radius.md` §6) and the eventual split both depend on.*
