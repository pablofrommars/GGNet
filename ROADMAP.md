# GGNet — Roadmap

*Distilled 2026-07-06 from the working assessment that drove the modernization effort (polar/radar, typed bindings, the architecture sessions, analyzer adoption, the golden gallery). The history lives in git; this file holds only what is open. Grades are honest self-assessment, calibrated for a single-author, dormant-burst library — the A+ tiers describe self-enforcing quality and are justified by external consumers or contributors, not by the author alone.*

## Where it stands

| Dimension | Grade | What separates it from the next grade |
|---|---|---|
| Architecture | **A** | A+: the specification/realization split (`PlotContext` still mixes builder state, trained state, and orchestration in one mutable object), and the interactivity seam exists as a design only. |
| Implementation hygiene | **A−** | A: flip the CI trigger from `workflow_dispatch` to push/PR — the gates (`-warnaserror`, format verify) already exist; enforcement is what's missing. A+: nothing manual left to get wrong (below). |
| Testing | **B+** | A−: interactive circuit paths (tooltips, mouse events, refresh) are untested, and CI doesn't run automatically. A+: a mutation score. |
| API design | **A** | A+: the Roslyn analyzer that turns DSL conventions into squiggles. |
| Maintainability | **B+** | A−: the 21 per-geom overload partials are hand-synced prose — machine-verified (`OverloadConsistencyTests`), not generated (generation retired by decision). The grade rises as the forwarding-bug class stays quiet under the gates. |

## Release — the `2.0.0` tag

The breaking window is open until the tag. Steps, in order:

1. Push the branch; run the CI workflow once via `workflow_dispatch` (it has never executed — first run on a fresh Linux runner is the real test).
2. Merge to `master`, tag `2.0.0`.
3. Flip the CI trigger to push/PR (completes hygiene A).

Breaking changes accumulated for the tag message: `Set`→`Commit`; builder type parameters (typed geom binding); `IWaiver`→`NoData`; Map generic constraints; `GGNet.Static`→`GGNet.Headless`; pure-SVG export (svg element only); `width`→`strokeWidth` on the ten stroke geoms; `alpha`→`opacity`; public surface cut 176→74 exported types (pinned manifest; the additions back are deliberate — stats, formatters); flag-free geom protocol (`Shape()`/`Set()`); `RenderTarget` collapsed to `Render | Loading`; `_color`→`colorBy` mapping family; `format:`/`timezone:` string parameters retired for `formatter:` (`IFormatter<T>` everywhere); `IAestheticMapping<T,TValue>.Map` returns `TValue?`; `stroke`→`strokeColor` on `Geom_Map`/`Geom_Violin`.

Out of scope by decision: NuGet packaging/publishing.

## Paths to A+, per dimension

### Architecture

- **Spec/realization split.** An immutable plot description built by the fluent API, realized per render. Enables safe re-render/concurrency and spec serialization; dissolves what remains of pipeline state management. The largest refactor on the board — touches everything.
- **Interactivity seam** (design exists; see backlog for tiers). `Unproject(px, py)` on `ICoordinateSystem`, `Invert` on scales, a view window as dynamic `Limits` exempt from `Reset()` (user state — the first deliberate exemption; document it when it lands).

### Implementation hygiene

- **A: flip the CI trigger.** One line.
- **A+ — nothing manual left to get wrong:** `TreatWarningsAsErrors` everywhere including tests; `AnalysisLevel=latest-all` fully triaged; a spell-check analyzer (typos were this codebase's signature failure mode); zero `!`/`#pragma`/suppression without a written reason. Defining property: a hygiene regression cannot merge.
- **Mutation testing** (Stryker.NET), run manually once or twice a year — the honest measure of which tests constrain the code and which merely touch it. Diagnostic, not a gate.

### Testing

- **A−: interactive circuit coverage** — the headless-untestable remainder (tooltips, mouse events, refresh paths). bUnit or a driven circuit; decide when the interactivity seam lands, since that work touches the same surface.
- **Cheap, any time:** widen the locale identity test from three hardcoded cultures to `CultureInfo.GetCultures(All)` — the NodaTime trick; exotic cultures (non-Latin digits, U+2212 minus) are where invariance bugs hide.
- **A+: mutation score** (same Stryker run as hygiene).

### API design

- **A+: the DSL Roslyn analyzer**, shipping with the package: warn on positional arguments beyond the selectors; warn on a genuinely-dead constant beside its mapping. Conventions stop being documentation and become squiggles. Real infrastructure (`Microsoft.CodeAnalysis.Testing`, maintenance forever) — build it alongside a decision to treat GGNet as a public product again.

### Maintainability

- No open action. Generation of the overload families was retired by decision in favor of verification; the residual risk class (positional-forwarding drift across the partials) is real — a forwarding bug shipped for years before the gates caught it — but the mitigation is the existing test battery, not new machinery.

## Backlog — demand-driven, each with its trigger

- **Interactivity, tiered by Blazor Server physics** (discrete events over the circuit are fine; `mousemove` never is — that split *is* the JS boundary). Tier 0, no JS: wheel-zoom, double-click reset, data-snapped crosshair via invisible hit-strips, coordinate readout (~1–2 sessions incl. the seam). Tier 1: +15 lines of measurement JS for responsive sizing. Tier 2: drag-pan/rubber-band/pixel-glued crosshair — the repo's first real JS asset; decide that precedent on its merits. Blast radius is concentrated (strategies, scales, `PlotContext`, `Panel` overlay, builders); zero impact on geoms, composer, Headless, gallery. *Trigger: a dashboard needs it (the fermentation wheel-zoom case is tier 0).*
- **Arc geometry** (pie/rose/coxcomb) — the designed slot exists (polar strategy + grid composition pattern). *Trigger: an actual chart need.*
- **Text measurement fidelity** — device-independent tables are the correct architecture (interop rejected: it reintroduces the two-pass protocol and breaks byte-pinned goldens); the defect is the unclosed chain: bundle Inter woff2 via `@font-face`, regenerate the width tables offline from the font's real advance widths, optional per-weight tables. *Trigger: visible layout misfits on real dashboards.*
- **PNG export** — rasterize the emitted SVG (resvg or similar); no second renderer. *Trigger: a consumer demands it.*
- **`--ggnet-series` data-mark theme default** — deferred from the theming refactor; requires verifying `var()` in presentation attributes renders across consumers, then a full gallery re-pin. *Trigger: that verification.*
- **Mapping-vs-constant unification** (`colorBy` + `color` in one slot) — blocked: union-typed parameters cannot express the DSL's in-signature constant defaults. *Trigger: C# grows a defaults design.*
- **Localized tick labels beyond per-scale opt-in** — a plot-level culture knob was deliberately not built. *Trigger: a dashboard wants localized axes everywhere.*

## Operating conventions

- Every commit lands green locally: `dotnet build GGNet.slnx -warnaserror`, full test suite, `dotnet format whitespace|style --verify-no-changes` (the same three gates CI runs).
- Render-touching changes byte-compare against the gallery snapshots (`tests/GGNet.Headless.Tests/Gallery/`); a re-pin is a deliberate, eyeballed decision, never a reflex.
- Breaking API changes go in their own commits, separately revertable, and each adds itself to the tag list above.
- Behavior claims are measured, not asserted — the `RenderTarget` optimization was implemented, measured, and reverted on the numbers; that calculus stands.
