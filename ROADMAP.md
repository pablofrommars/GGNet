# GGNet — Roadmap

*Distilled 2026-07-06 from the working assessment that drove the modernization effort (polar/radar, typed bindings, the architecture sessions, analyzer adoption, the golden gallery). The history lives in git; this file holds only what is open. Grades are honest self-assessment, calibrated for a single-author, dormant-burst library — the A+ tiers describe self-enforcing quality and are justified by external consumers or contributors, not by the author alone.*

## Where it stands

| Dimension | Grade | What separates it from the next grade |
|---|---|---|
| Architecture | **A** | A+: the specification/realization split. The interactivity seam has landed (2026-07-10, `interactivity-draft`), and it made the split cheaper: `PlotContext` is pre-sorted into labeled Spec/Realization/State partials, and the work revealed the split's true shape — three buckets (spec / realization / durable interaction state), not two. |
| Implementation hygiene | **A−** | A: flip the CI trigger from `workflow_dispatch` to push/PR — the gates (`-warnaserror`, format verify) already exist; enforcement is what's missing. A+: nothing manual left to get wrong (below). |
| Testing | **B+** | A−: CI doesn't run automatically. The circuit gap closed with interactivity: bUnit covers tooltips/mouse events/refresh, and a Playwright smoke layer executes the JS module for real (opt-in, `GGNET_E2E=1`). A+: a mutation score. |
| API design | **A** | A+: the Roslyn analyzer that turns DSL conventions into squiggles. |
| Maintainability | **B+** | A−: the 21 per-geom overload partials are hand-synced prose — machine-verified (`OverloadConsistencyTests`), not generated (generation retired by decision). The grade rises as the forwarding-bug class stays quiet under the gates. |

## Release — the `2.0.0` tag

The breaking window is open until the tag. Steps, in order:

1. Push the branch; run the CI workflow once via `workflow_dispatch` (it has never executed — first run on a fresh Linux runner is the real test; it now carries four jobs incl. the Playwright smoke).
2. Merge to `master`, tag `2.0.0`.
3. Flip the CI trigger to push/PR (completes hygiene A).

Breaking changes accumulated for the tag message: `Set`→`Commit`; builder type parameters (typed geom binding); `IWaiver`→`NoData`; Map generic constraints; `GGNet.Static`→`GGNet.Headless`; pure-SVG export (svg element only); `width`→`strokeWidth` on the ten stroke geoms; `alpha`→`opacity`; public surface cut 176→74 exported types (pinned manifest; the additions back are deliberate — stats, formatters, and the interactivity surface: `InteractivityOptions`, `ZoomAxis`, `ViewExtensions`); flag-free geom protocol (`Shape()`/`Set()`); `RenderTarget` collapsed to `Render | Loading`; `_color`→`colorBy` mapping family; `format:`/`timezone:` string parameters retired for `formatter:` (`IFormatter<T>` everywhere); `IAestheticMapping<T,TValue>.Map` returns `TValue?`; `stroke`→`strokeColor` on `Geom_Map`/`Geom_Violin`.

Out of scope by decision: NuGet packaging/publishing.

## Paths to A+, per dimension

### Architecture

- **Spec/realization split.** An immutable plot description built by the fluent API, realized per render. Enables safe re-render/concurrency and spec serialization; dissolves what remains of pipeline state management. The largest refactor on the board — touches everything. Interactivity de-risked it (`interactivity-plan/implementation-blocks.md`): members are pre-sorted into labeled partials, the third bucket (durable interaction state — `ViewRange`, the container view) exists and is documented in place, and **all three coverage preconditions have landed** (multi-pass render equivalence, circuit tests, and `ConcurrencyStressTests` — refresh storms, view-window writes racing renders, dispose racing writers; it also corrected the channel's `SingleWriter` declaration to match the public multi-writer contract). The split now waits only on its trigger.
- ~~**Interactivity seam**~~ **Landed** (2026-07-10): `Unproject` on `ICoordinateSystem`, `Invert`/`Unmap` on scales, `ViewRange` as the runtime window exempt from `Reset()` — the first deliberate exemption, documented where it lives (`Scales/Position.cs`, `PlotContext.State.cs`). What remains of interactivity is in the backlog below.

### Implementation hygiene

- **A: flip the CI trigger.** One line.
- **A+ — nothing manual left to get wrong:** `TreatWarningsAsErrors` everywhere including tests; `AnalysisLevel=latest-all` fully triaged; a spell-check analyzer (typos were this codebase's signature failure mode); zero `!`/`#pragma`/suppression without a written reason. Defining property: a hygiene regression cannot merge.
- **Mutation testing** (Stryker.NET), run manually once or twice a year — the honest measure of which tests constrain the code and which merely touch it. Diagnostic, not a gate.

### Testing

- ~~**Interactive circuit coverage**~~ **Landed with interactivity**: bUnit covers the circuit surface (tooltips, mouse events, gestures, the opt-in gate), and `tests/GGNet.E2ETests` (Playwright, self-skipping without `GGNET_E2E=1`) executes the JS module against the spawned demo app — the only layer that can. Chromium only; the coordinate math is spec-based, but a manual Firefox/Safari pass on the demo is still owed before merge.
- ~~**Widen the locale identity test**~~ Done — `GeometryIsCultureInvariantAcrossAllCultures` iterates `CultureInfo.GetCultures(AllCultures)`.
- **A+: mutation score** (same Stryker run as hygiene).

### API design

- **A+: the DSL Roslyn analyzer**, shipping with the package: warn on positional arguments beyond the selectors; warn on a genuinely-dead constant beside its mapping. Conventions stop being documentation and become squiggles. Real infrastructure (`Microsoft.CodeAnalysis.Testing`, maintenance forever) — build it alongside a decision to treat GGNet as a public product again.

### Maintainability

- No open action. Generation of the overload families was retired by decision in favor of verification; the residual risk class (positional-forwarding drift across the partials) is real — a forwarding bug shipped for years before the gates caught it — but the mitigation is the existing test battery, not new machinery.

## Backlog — demand-driven, each with its trigger

- **Interactivity — the remainder** (the tiers landed 2026-07-10: wheel-zoom, reset, crosshair+readout, drag-pan with client-side preview, cursor-glued popover tooltips, auto-fit y, the imperative view API, the opt-in gate; ledger in `interactivity-plan/implementation-blocks.md`). Still open, each with its design note: **legend toggle** (Block 7 — visibility filter placement, retrain-vs-freeze), the **anchor-model tooltip / multi-series shared readout** (Block 9c — wants data-position access; co-design with Block 7), and **rubber-band/brush/lasso selection** riding the existing pan machinery (selection is a new state domain beside the view window). *Trigger: a dashboard needs them.*
- **Arc geometry** (pie/rose/coxcomb) — the designed slot exists (polar strategy + grid composition pattern). *Trigger: an actual chart need.*
- **Text measurement fidelity** — device-independent tables are the correct architecture (interop rejected: it reintroduces the two-pass protocol and breaks byte-pinned goldens); the defect is the unclosed chain: bundle Inter woff2 via `@font-face`, regenerate the width tables offline from the font's real advance widths, optional per-weight tables. *Trigger: visible layout misfits on real dashboards.*
- **PNG export** — rasterize the emitted SVG (resvg or similar); no second renderer. *Trigger: a consumer demands it.*
- **`--ggnet-series` data-mark theme default** — deferred from the theming refactor; requires verifying `var()` in presentation attributes renders across consumers, then a full gallery re-pin. *Trigger: that verification.*
- **Mapping-vs-constant unification** (`colorBy` + `color` in one slot) — blocked: union-typed parameters cannot express the DSL's in-signature constant defaults. *Trigger: C# grows a defaults design.*
- **Localized tick labels beyond per-scale opt-in** — a plot-level culture knob was deliberately not built. *Trigger: a dashboard wants localized axes everywhere.*

## Operating conventions

- Every commit lands green locally: `dotnet build GGNet.slnx -warnaserror`, full test suite, `dotnet format whitespace|style --verify-no-changes` (the same gates CI runs).
- Render-touching changes byte-compare against the gallery snapshots (`tests/GGNet.Headless.Tests/Gallery/`); a re-pin is a deliberate, eyeballed decision, never a reflex.
- Changes touching the JS module (`Components/Panel.razor.js`) or its wrapper also run the executed-JS smoke: `GGNET_E2E=1 dotnet test tests/GGNet.E2ETests` (needs a Playwright Chromium; tests self-skip without the flag, so the plain gates stay browser-free).
- Breaking API changes go in their own commits, separately revertable, and each adds itself to the tag list above.
- Behavior claims are measured, not asserted — the `RenderTarget` optimization was implemented, measured, and reverted on the numbers; that calculus stands.
