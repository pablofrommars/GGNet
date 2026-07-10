# GGNet — Interactivity Implementation Blocks (Step 4 plan)

*Compiled 2026-07-10. Breaks the trilogy (`interactivity-inventory.md` → `interactivity-options.md` → `interactivity-blast-radius.md`) into landable blocks. Decisions folded in from the 2026-07-10 refinement session: the codex-variant plans are ignored; the demo app uses Tailwind v4; **Option B (continuous gestures / first JS asset) is out of scope** until a real need is demonstrated — when it arrives, the official .NET JS-interop skill will be provided; wheel capture uses Blazor's static `@onwheel:preventDefault` (stays Tier 0).*

Each block is one PR-sized change that lands green on the three gates, alone. Order is a dependency chain: 1 → 2 → 3 → 4 → 5 → 6 → 7; 8 is gated separately.

> **Status (2026-07-10):** Blocks 0–6 and 8 landed; Block 7 (legend toggle) **deferred by decision**. Block 8 shipped drag-pan on the skill-revised shape (collocated `Panel.razor.js`, typed `PanelInterop`, one callback per gesture; `IServiceProvider`-lazy `IJSRuntime` so the headless empty-container renderer stays untouched), plus **auto-fit y** (`InteractivityOptions.AutoFitY` + `PlotContext.FitYToXView()`): y follows the x window at every commit, derived from the default source/selectors — the coord-style zoom stays the default; the geom-level "window as training filter" variant is explicitly deferred to Block 7's retrain design note. Rubber-band/brush/lasso ride the same machinery later. Block 9 slices **9a+9b landed** (2026-07-10): under the `CursorTooltip` opt-in the bubble is a `popover="manual"` top-layer element (kept inside the foreignObject so `--ggnet-*` inheritance survives top-layer promotion) glued to the cursor and edge-flipped by the module at frame rate; the classic mark-anchored tooltip is byte-untouched without the opt-in. **9c (anchor model / multi-series readout) stays deferred with Block 7.** A+ (fit-from-shapes) is assessed and ready as a follow-on.
>
> **Also landed (2026-07-10): the responsive-svg upgrade and the Playwright smoke layer.** Wheel capture moved into the JS module — it converts client px to svg units against a fresh `getBoundingClientRect` per event (pan deltas likewise, sampled at gesture start), so the fixed-size-svg constraint and the cross-browser `OffsetX` bet are both retired; the svg is responsive under the opt-in. `tests/GGNet.E2ETests` (Playwright + `Xunit.SkippableFact`) executes the module in real Chromium against the spawned demo app: wheel-on-responsive-svg, pan preview/commit, glued-popover tooltip — **all passing**. Tests self-skip without `GGNET_E2E=1`, so the plain test gate stays browser-free; CI gained a `test-e2e` job. Lesson pinned in the tooltip test: `Locator("circle").First` matches a *legend swatch* — marks live in `g[transform]` wrappers. Open items: none — the working ledger is clear. **Split phase 0 landed** (2026-07-10): `ConcurrencyStressTests` closes §8c #4 — the last net-blind seam — pinning the multi-writer refresh contract (coalescing under an 8-writer storm, loop survival), view-window convergence to the last writer after writes race full render passes (torn frames transient by design, next frame corrects), and clean dispose mid-storm. Finding fixed en route: the channel claimed `SingleWriter = true` while `RefreshAsync` is public multi-thread surface — declaration corrected. The spec/realization split's preconditions are now 3/3; phases 1–4 wait on the roadmap trigger. **Resolved since:** the render-loop logging seam — `PlotBase` resolves `ILoggerFactory` lazily via `IServiceProvider` (headless-safe) and threads an optional `ILogger` ("GGNet.Plot") into the interactive handler, whose loop now logs failed frames (`LoggerMessage`-generated, shutdown exceptions still filtered silently) instead of swallowing them; a test proves both the log and loop survival. **A+ fit-from-shapes** — `FitYToXView` measures the drawn geom layers via the new `Shapes/ShapeExtents` walker (exhaustive over the `Shape` union: bar rectangles keep their baseline, areas contribute both bands, segments interpolate at window boundaries, `HLine` participates, `VLine`/`ABLine` don't), falling back to the source scan pre-render; statistics stay frozen by construction. the tooltip mark-color wiring — `Show`'s `?? "#ffffff"` coalesce erased "no color" and the typo'd `--tootip-color` was consumed by nothing; now null stays null, the host `foreignObject` emits `--tooltip-color`/`--tooltip-opacity` (both branches inherit — top layer included), and the theme derives `--tooltip-theme-color: var(--tooltip-color, var(--ggnet-tooltip-bg, …))`, so bubbles tint with the mark and fall back to the theme background. Readability of dark mark colors is authorial (pass lighter colors or alpha). Playwright smoke layer (landed, see below), the responsive-svg upgrade (landed, see below), and the circle `clip-path` artifact — fixed with one scene-level clip group in `Area` (per-circle clips would shift with the position transform); all 42 gallery goldens re-pinned after mechanical verification that every diff is exactly the wrapper + one indent level. Marks now clip at the panel on zoomed/panned views; the pan *preview* still translates the clip window with the marks (transient, corrected at commit).

## Cross-block acceptance (every block, no exceptions)

- `dotnet build GGNet.slnx -warnaserror` · `dotnet test GGNet.slnx` · `dotnet format whitespace|style --verify-no-changes`.
- **Existing gallery goldens byte-identical** (`tests/GGNet.Headless.Tests/Gallery/`). New goldens are additive only; a moved existing golden is a bug, not a re-pin.
- **Headless purity is structural**: interactivity opt-in is a `Plot` component parameter Headless never passes — no block may thread interactivity state through `PlotContext` into the Headless path.
- Blocks that grow the public surface (4, 5, 6, 7) move `Api/PublicApiTests.*.verified.txt` — that re-pin is deliberate and eyeballed per block.
- `ShapeComposer`, `ScreenPrimitive`, the 21 geoms, and `GGNet.Headless` internals stay untouched through Block 6 (Block 7 may touch geom *visibility* only, per its design note).

---

## Block 0 — Demo scaffold ✅ (landed 2026-07-10)

`src/GGNet.Demo`: Blazor Web App (Interactive Server), net11.0/preview, Tailwind v4, in `GGNet.slnx` under the gates. Home page proves the wiring with the shipped surface (`Geom_Point` tooltip + discrete color legend). Each block below lands with a dedicated demo page; after Block 7, a combined page exercises all features together.

## Block 1 — Inverse projection seam (pure C#, emits nothing) · ~35 LOC

The `Unproject`/`Invert` inverses of existing forward math. No state, no emission, zero golden risk.

- `Coords/ICoordinateSystem.cs` + `CartesianCoordinateSystem.cs`: `(double cx, double cy) Unproject(double px, double py)` — inverse of `Project` (`CartesianCoordinateSystem.cs:19-20`).
- `Coords/PolarCoordinateSystem.cs`: throws (`GGNetUserException` — "gesture on a polar plot" is caller-facing misuse per the exceptions guide §8; confirm at implementation).
- `Scales/Position.cs`: `Invert(double fraction) → double` on `IPosition` — inverse of `Coord` (`Position.cs:62-70`).
- `Components/ICoord.cs` + `Panel.razor.cs`: composed `Unproject(px, py)` in double space, inverting through the same `Flip()` swap it projects through.
- **Not here:** the typed `Unmap(double) → TKey` readout inverse — deferred to Block 6, where its consumer lands.

Tests (xUnit, in `GGNet.Headless.Tests`): `Unproject∘Project ≈ identity` (Cartesian, incl. flipped); `Invert∘Coord ≈ identity` incl. a log10-transformed scale; polar throws.

## Block 2 — Legibility pass (§8a — behavior-free) · file moves + comments

Make the mutability buckets legible *before* the new state lands, using the three-bucket vocabulary: **spec / realization / interaction**.

- Sort `PlotContext` members into labeled partial files (`PlotContext.Spec.cs` / `.Realization.cs` / `.State.cs`, following the existing `PlotContext.Build.cs` pattern — not `#region`), one invariant banner per file.
- `Position<TKey>`: bucket comments (`Limits` = spec, `_min/_max/Range/Breaks/Labels` = realization cleared by `Clear()`, Block 3's `ViewRange` = interaction, surviving `Clear()`).
- Tighten accessors only where free (`init` / `private set`); **no** type extraction, no fighting the fluent API's `internal set` spec members.

Exit: zero output change — gallery byte-identical, `PublicApiTests` unmoved.

## Block 3 — Runtime view window (`ViewRange`) · ~10 LOC + tests

The shared interaction state everything else writes.

- `Scales/Position.cs`: `(double min, double max)? ViewRange { get; set; }` — interaction bucket, **not** touched by `Clear()`.
- `Extended.cs`, `DiscretePosition.cs`, `DateTimePosition.cs`, `InstantPosition.cs`: `Commit` prefers `ViewRange` when present, else today's `SetRange(Limits/…)` path.
- **Decision (locked):** `ViewRange` bypasses `SetRange`'s `expand` — zoom shows the exact window, unpadded; reset returns to the author's expanded view. `ViewRange` lives in *transformed* double space, so typed entry goes through `Map`.

Tests (Layer 1, §9): `ViewRange` survives a full `Render()` pass and windows the output; cleared → output byte-identical to never-set (headless string compare); **multi-pass render equivalence** (render twice = identical — claimed at `PlotContext.cs:357`, never tested); discrete/date snapping at the `Map`/`IndexOf` level.

## Block 4 — Option C: imperative control API · ~75 LOC

Host commands over the view window. No scaffolding, no opt-in needed, Headless-safe by construction.

- `PlotContext<T,TX,TY>`: typed state ops — `SetXView(TX min, TX max)` / `SetYView` (via `Map`), `ResetView()`, `ShowLast(Period)` (date/instant x instantiations only).
- `Data/Position.cs`: set/clear across all facet scales (one shared or N free — plot-wide writes all; panel-scoped overload writes one).
- `Components/Plot.razor.cs`: public `@ref` commands — `ZoomToXAsync` / `ZoomToYAsync` / `ResetViewAsync` / `ShowLastAsync`, each = mutate context + `RefreshAsync(RenderTarget.Render)`. Reconciliation with future gestures: last-writer-wins through the coalescing channel.
- Full `///` docs on all new public surface; deliberate `PublicApiTests` re-pin.

Tests: bUnit (extending the existing `GGNet.Components.Tests`) — `@ref` command → re-rendered markup reflects the window; xUnit — `ShowLast` window arithmetic on date/instant scales. Headless: a programmatically-windowed context exports a correctly-windowed static SVG (**new** pinned golden — the export-time `ZoomTo`/`ShowLast` deliverable).

Demo: **/external-controls** page — "zoom to", "last N hours", "reset" buttons driving `@ref` commands; QAs C end-to-end.

## Block 5 — Opt-in gate + wheel-zoom & double-click reset (A, slice 1) · ~50 LOC

First byte-visible change, guarded.

- `Plot<T,TX,TY>`: `[Parameter] InteractivityOptions? Interactivity { get; init; }` — unset means today's output, byte-for-byte. Keep the options record minimal (wheel-zoom axis selection; grow per block). `SparkLine` does not get the parameter.
- `Panel.razor`: conditional `<g @onwheel=… @onwheel:preventDefault @ondblclick=…>` wrapping panel content (bubbling catches gestures over marks; the panel rect is a sibling — `Panel.razor:28-40`).
- `Panel.razor.cs`: `OnWheel` — cursor px → `Unproject` → shrink/grow `ViewRange` about the cursor → refresh; `OnReset` — clear `ViewRange`, refresh.
- Consider a debug-level logging seam in `InteractiveRenderModeHandler.RunBackground`'s silent `catch` — gesture-handler bugs currently vanish (refinement note #5).

Tests: bUnit — **the §0 acceptance**: without `Interactivity` the markup is identical to today's; with it the capture group appears; wheel event → windowed re-render; dblclick → restored. Gallery untouched (opt-in off everywhere).

Demo: **/wheel-zoom** page (the fermentation case: time series, wheel in, double-click out).

## Block 6 — Crosshair + coordinate readout (A, slice 2) · ~70 LOC

The highest-value gap from Step 1 (#2), on the same machinery.

- Typed `Unmap(double) → TKey?` on scales (deferred from Block 1): `Extended` via `transformation.Inverse`; discrete/date snap to `values[round(index)]`.
- Generalize `TooltipBase.Show` into a shared axis readout (crosshair rules + x/y labels) rather than a parallel component; hit-strip `<rect>`s for the data-snapped variant. All behind the opt-in.
- Labels format through the scales' `IFormatter<T>` — extend `LocaleTests` (geometry invariant under all cultures; label text localizes only via explicit `formatter:`).

Tests: bUnit hover → readout content/position; locale assertions; goldens untouched.

Demo: **/crosshair** page (multi-series readout).

## Block 7 — Legend toggle (A, slice 3) · ~40 LOC + design note

**Pre-implementation design note required** — the fuzziest edges of the plan:

1. *Where visibility filters.* Per-series hiding is per-item (by mapped aesthetic key), which naïvely lands inside geoms. Resolve honestly: either a visibility predicate the geom's `Shape` pass consults, or pre-filtering before `ShapeComposer.Compose` — pick the one that keeps "composer untouched" true and geom churn minimal.
2. *Retrain or freeze.* Hiding a series changes trained bounds. Decide: rescale to visible data (ggplot-consistent) vs. hold the frame; interaction with an active `ViewRange`.
3. Series-visibility state is a second interaction-bucket domain (named in Step 2 §3 as out of C's core) — same lifecycle as `ViewRange`, distinct state.

Plus: click handlers on the already-rendered legend swatches (`Plot.razor`), faded style for hidden series (new paintable class ⇒ `Themes/Default.css` updated in the same change — `ThemeContractTests`).

Tests: bUnit toggle → marks disappear/reappear, swatch style flips; goldens untouched.

Demo: **/legend-toggle** page, then the **/combined** page (wheel + crosshair + legend + external controls on one chart, plus a multi-panel facet case) — closing task 2's "page combining different features".

## Block 8 — Option B: continuous gestures (deferred, gated)

Not scheduled. Trigger: a demonstrated continuous-gesture need (drag-pan / rubber-band) from real demo/dashboard use. The decision to take a JS dependency is made *then*, on its merits.

### Blast radius, revised against the official `use-js-interop` skill (2026-07-10)

The skill (reviewed from `tmp/SKILL.md`) supersedes the Step-3 §4 shape on four points, three of them cheaper:

1. **Collocated `.razor.js`, not `wwwroot/js/`.** The JS lives at `Components/Panel.razor.js`; the Razor SDK ships it as a static web asset automatically (import path `./_content/GGNet/Components/Panel.razor.js`) — no csproj change, no host wiring, Headless never loads it (no circuit, opt-in absent).
2. **No DI-scoped module class.** Step 3 pointed at the parent repo's "AddScoped module per concern" pattern; the skill (and GGNet's own Blazor guide) rejects it. Instead: one `sealed` typed wrapper (`PanelInterop`) instantiated by the component, wrapping the framework-provided `IJSRuntime` (`[Inject]` — no host registration), `const` module path/method names. A library win: consumers need zero setup.
3. **Gesture stays entirely in JS.** JS attaches its own `pointerdown/move/up` (class wrapping `#dotNetRef`, `AbortController` for teardown) on an `ElementReference` to the transform target — no Blazor pointer events, no circuit traffic during the drag. One batched `invokeMethodAsync` on gesture-end carries the net delta (both-direction batching rule). The `[JSInvokable]` commit method must be **public**, converts px → data via the Block-1 seam, writes the Block-3/4 container view, refreshes — the existing preview-then-commit design, unchanged.
4. **Lifecycle discipline.** Init only in `OnAfterRenderAsync(firstRender)` (prerender-safe — the demo prerenders); param changes flagged and applied there. `Panel` gains `IAsyncDisposable`: JS `dispose()` first, then module + `DotNetObjectReference`, catching `JSDisconnectedException` — the repo's existing idempotent-dispose idiom.

Revised file map: `Components/Panel.razor.js` (**new**, ~70 lines), `Components/PanelInterop.cs` (**new**, ~60), `Panel.razor` (+~5: inner transform-target `<g @ref>` around `<Area>` — the Block-5 capture group is the *event* surface, the transform target is the marks-only subtree), `Panel.razor.cs` (+~50: init/flag/dispose/`[JSInvokable]` commit), `InteractivityOptions` (+ pan/brush flags). Unchanged from Step 3: `ShapeComposer`/geoms/Headless untouched; goldens move only if the inner `<g>` is emitted outside the opt-in (it must not be).

Costs unchanged by the skill: Playwright remains the only layer that can execute the JS (bUnit stubs it — it can assert the wrapper *was called*, not that JS acted), and that test-infra adoption is still the single biggest line item of the block. Open design points carried: whether the pan preview transforms marks only or marks+grid; transform reset vs. authoritative-frame flicker on commit; `[DynamicDependency]` on the `[JSInvokable]` for trim safety (skill is silent; cheap to add).

**Bonus unlocked:** once the module exists, a one-line `getBoundingClientRect` measurement can replace Block 5's fixed-size-svg constraint — responsive interactive plots and browser-proof wheel coordinates (the Tier-1 item) ride along nearly free.

---

## Block 9 — Tooltip refactor: popover host, cursor-glue, anchor model (proposed, not scheduled)

Grounded in `Tooltip.razor` as shipped: `Show` projects the mark's data coords, renders HTML in a `foreignObject` (`width="1"` + overflow), and an 8-way quadrant switch already flips the bubble's side by the mark's *position* — what's missing is *size-aware* collision (a wide bubble at px ≈ 0.7 still clips), cursor-following, and escape from `foreignObject`'s Safari bug class. **Honest framing: this is "better tooltips," not "faster tooltips"** — today's cost is one round-trip per mark enter (not per move), and that first-show latency is irreducible while content stays a server-rendered `Func<T, RenderFragment>` evaluated on hover. JS-owned show/hide with pre-rendered content is a different (per-mark-DOM-weight) trade, out of scope.

Three slices, each landable alone, all riding the `Interactivity` opt-in gate (tooltips never render in Headless output — a hover context never exists there — so goldens are structurally safe):

1. **9a — Popover host.** Move the bubble out of `foreignObject` onto a `popover="manual"` top-layer div (baseline API; hover-appropriate semantics, no light-dismiss). Retires the `foreignObject` hack and svg-edge clipping outright; show/hide stays circuit-driven. Costs: the element leaves the `.ggnet` scope, so it must carry the theme attribute itself (`ThemeContractTests`), and it leaves svg coordinate space (moot under the opt-in's fixed-size svg). CSS Anchor Positioning is not yet dependable cross-browser — positioning stays JS/server — but can arrive later as progressive enhancement.
2. **9b — Cursor-glue + measured edge-flip** in the collocated Block-8 module: JS glues the top-layer element to `clientX/clientY` during `mousemove` (page coordinates — no svg conversion at all, which is why 9a goes first) and flips/clamps against the measured box. Zero circuit traffic during the move; content/show/hide unchanged. Same typed-wrapper/`DotNetObjectReference`/batching patterns as pan.
3. **9c — Anchor model.** Generalize placement into mark-anchored (today), axis-anchored (the multi-series shared readout at one x — the `Tooltip.Show` generalization Block 6 deliberately deferred), and cursor-glued. This is the slice that needs data-position access, so co-design it with Block 7's retrain/data design note.

Drive-by findings to fix in whichever slice touches them first: the `tooltip-left-end` arm's `_px > 0.8` threshold (siblings use `0.75` — looks latent), and the typo'd `--tootip-color` CSS variable (a shipped contract with the theme CSS — renaming re-pins `SelfContained`).

## Order rationale & risk ledger

- 1 → 3 are pure/internal with an airtight oracle (goldens + round-trip tests); 2 slots between them so `ViewRange` lands in a labeled home (§8a "step 1.5").
- 4 before 5: C delivers dashboard value with zero scaffolding and gives the demo its first interactive QA page before the first byte-visible change (5) exists.
- 5 → 7 order by value-per-risk: wheel-zoom exercises the full seam+window+refresh loop; crosshair reuses it; legend toggle carries the only unresolved design questions, so it goes last.
- Biggest known risks: the §8c net-blind seams (multi-pass lifecycle, shared-reference invariants) — mitigated by Block 3's equivalence tests landing *before* any consumer; and Block 7's filter placement — mitigated by its design note.
