# GGNet — Interactivity Implementation Options (Step 2)

*Compiled 2026-07-08. Step 2 of the interactivity session (inventory → **options** → blast radius). Grounded in a read of the actual GGNet source on `ai-skills-mcp-jul2026`; every claim about current behavior carries a `file:line`. Scope carried from Step 1 (`interactivity-inventory.md`): sort was user-value; the shipped event surface is refactorable; an imperative external-control API is in scope. Detailed per-option blast radius is Step 3 — this doc stops at the design.*

> Note: `src/GGNet.ChartSelection` is the deterministic **chart-type recommender** (the MCP engine, `Selector.cs`/`Profiler.cs`) — unrelated to interactive selection. Named here only to prevent confusion.

---

## 0. Two non-negotiables: interactivity is optional, Headless stays pure

Two hard constraints govern every option below; violate either and the design is wrong regardless of how good the interaction feels.

1. **Opt-in.** A plot that does not ask for interactivity must render exactly as it does today — same SVG, byte-for-byte. Every render-*structure* change an option introduces (the gesture-capture `<g>`, an overlay rect, the transformable group, a JS module load) must be **conditional on an explicit opt-in**, never emitted by default. The pure-C# inverse seam (`Unproject`/`Invert`) is exempt — it emits nothing.

2. **Headless purity.** `GGNet.Headless` renders pure, well-formed SVG that the gallery parses with `XDocument.Parse` and byte-compares against pinned goldens (`tests/GGNet.Headless.Tests/Gallery/`); `RenderMode.Static` is its path. Interactivity is a *Blazor-circuit* concern — it has no meaning without a live circuit — so the **Static/Headless path emits zero interactivity scaffolding**, unconditionally: no capture group, no event attributes, no JS hook. Headless output stays a picture.

These collapse to two gates:

- **Render-mode gate (automatic):** `RenderMode.Static` (Headless) never emits scaffolding. `Interactive`/`InteractiveAuto` may.
- **Opt-in gate (explicit):** even an interactive Blazor plot emits scaffolding only when the author turned it on.

Where the opt-in lives is a Step-3 decision; the natural candidates are a terminal DSL config (peer to `.Style(...)`) or a `Plot` component parameter. Whatever the surface, the acceptance test is fixed: **the existing gallery goldens must not move for any non-interactive plot.**

**Corollary — the imperative API (Option C) is Headless-safe by construction.** C only sets `Limits` and refreshes; it emits no scaffolding. So a *Headless* render with a programmatically-set window is simply a different static SVG — server-side "zoom to" / "show last 2 weeks" for export works for free and honors both constraints. Interactivity being optional does not cost Headless the view-window control; it only withholds the in-chart *gestures*.

---

## 1. The render model as it stands (grounded)

Five facts decide everything below.

**a. The pipeline is a flagless recompute.** `PlotContext.Render()` runs `Reset → EnsurePanels → Validate → Train → CommitAesthetics → BuildLegends → Shape → CommitPositions → MeasureAxes` and is idempotent — "rendering twice yields identical output" (`PlotContext.cs:357-382`). `Reset()` clears every scale's trained bounds each pass (`PlotContext.cs:384-418`).

**b. The view window already exists as `Limits`.** A continuous position scale carries `(TKey? min, TKey? max) Limits { get; set; }` (`Scales/Continuous.cs:8`). `Commit` resolves the axis range as `SetRange(Limits.min ?? _min ?? 0.0, Limits.max ?? _max ?? 0.0)` (`Scales/Extended.cs:30-32`) — **`Limits` overrides the data-trained bounds.** Crucially, `Position.Clear()` resets `_min/_max` but *not* `Limits` (`Scales/Position.cs:72-76`), so a runtime-set `Limits` **already survives `Reset()`**. `XLim(...)` is the only setter today, and it sets `Limits` through the scale *factory* at build time (`BuilderExtensions.cs:116-136`), not on the live instance.

**c. Projection is a clean, one-way seam.** Scales own value→fraction: `Position.Coord(value) = (value - min)/(max - min)` (`Scales/Position.cs:62-70`). The coordinate system owns fraction→pixel: Cartesian `Project(cx,cy) = (area.X + cx·area.W, area.Y + (1-cy)·area.H)` (`Coords/CartesianCoordinateSystem.cs:19-20`). `Panel` composes both and *is* the `ICoord`: `Project`, `ToX`, `ToY`, `XRange`, `YRange`, `XTransformation`, `YTransformation` (`Components/Panel.razor.cs:169-182`). **No inverse exists** — no `Unproject(px,py)`, no scale `Invert`. Both are pure math and pure additions.

**d. Re-render is a purpose-built async engine.** `RenderMode` is `Interactive | InteractiveAuto | Static` (`RenderMode.cs`). `InteractiveRenderModeHandler` runs a background `Channel<RenderTarget>` loop that **coalesces** queued refreshes, renders the latest, and applies semaphore backpressure so a render completes before the next starts (`Rendering/InteractiveRenderModeHandler.cs:25-102`). `RefreshAsync(target, token)` just writes to the channel. This is ideal for discrete, bursty re-renders and hostile to per-frame streaming.

**e. Every mark is a live circuit element; there is zero JS.** `Area` recomputes the whole scene via `ShapeComposer.Compose(geoms, coord, zone)` **on every render** (`Components/Area.razor:14`) and emits each primitive with `@onclick`/`@onmouseover`/`@onmouseout` wired individually (`Area.razor:49-51`, `69-71`, `89-91`, `128-130`). The panel background `<rect class="panel" @onclick=OnClick>` carries a click (`Panel.razor:28-34`, `Panel.razor.cs:204-213`). The tooltip is HTML in a `foreignObject`: `Tooltip.Show(x,y,…)` projects via `Coord.Project` and calls `StateHasChanged` (`TooltipBase.cs:22-36`). The only asset under `wwwroot/` is `dev/pixelWidthCalculator.html` — **GGNet ships no runtime JS today.** CSS already animates hover via SVG `transform` (`Themes/Default.css:179-214`) and has a `pointer-events-none` utility (`Default.css:58-60`).

**Consequence:** zooming is nearly already built. Set `Limits` on the live scale, call `RefreshAsync(Render)`, and the existing pipeline produces a correct new view — trained bounds recomputed, `Limits` honored, grid/ticks/labels/clip all correct. What is missing is (i) the *inverse* projection to turn a pixel gesture into data-unit `Limits`, (ii) a *runtime* setter distinct from the build-time `XLim`, and (iii) a way to *capture* the gesture.

---

## 2. The core decision, reframed: CSS transform vs re-render is *preview vs commit*, not either/or

The hint frames it as a versus. The code says it is a **layering**.

| Axis | Blazor re-render (set `Limits` → `RefreshAsync`) | CSS/SVG `transform` on a group |
|---|---|---|
| **Output correctness** | Exact: ticks, labels, gridlines, clip, non-scaled stroke/text all recomputed by the pipeline. | Wrong in isolation: a `scale()` distorts stroke width, text, circle radii; ticks/labels/grid go stale (they are outside the transformed geom, or frozen inside it). |
| **Latency** | Server recompute (`ShapeComposer` over all geoms) + full SVG diff + **one circuit round-trip** per step. | Instant, client-side, **zero round-trip**, zero recompute. |
| **Continuous gesture (drag/`mousemove`)** | No — flooding the coalescing channel (`InteractiveRenderModeHandler.cs`) drops intermediate frames; you get steps, not smoothness, plus RTT lag. | Yes — the *only* way to track `mousemove` at frame rate. |
| **JS required** | None. `@onwheel`/`@ondblclick`/`@onpointer*` are discrete Blazor events over the circuit. | Yes — `mousemove` can't ride the circuit; needs a JS module to mutate the transform. First JS asset in the repo. |
| **Structural change** | Add inverse projection + runtime `Limits` setter + gesture capture. No new render group. | All of the above **plus** wrap the geom layer in one transformable `<g>` that does not exist today (primitives are emitted flat — `Area.razor`). |

The transform is a **latency mask**: a client-side preview during a continuous gesture, always reconciled by a real re-render on gesture-end. It is not a second implementation of zoom; it is an optional accelerator over the one implementation. Therefore:

- **Every Tier-0 (discrete) interaction needs re-render only.** Wheel-zoom, double-click reset, range-selector buttons, hit-strip crosshair, coordinate readout, legend toggle, click/drill — all set `Limits` (or state) and refresh. No transform, no JS.
- **Every Tier-2 (continuous) interaction needs transform-preview + commit.** Drag-pan, rubber-band select, pixel-glued crosshair — JS drives the transform during the drag, then commits data-unit `Limits` and refreshes.

The real question is per-feature: **does this interaction have a continuous phase?** That is the discrete-vs-continuous input split the roadmap already named as the JS boundary — not a library-wide architecture choice.

---

## 3. Three option packages

Read these as cumulative layers, not alternatives. **A** stands alone and delivers most of Step 1's value; **B** adds the continuous gestures on top of A; **C** is orthogonal and shared by both.

### Option A — Re-render only (Tier 0, zero JS)

The whole discrete cluster, on the existing engine.

- **Mechanism:** gesture → compute new data-unit window via a new inverse projection → set the live scale's `Limits` → `RefreshAsync(RenderTarget.Render)` → existing pipeline produces correct output.
- **New seam (small, pure):** `Unproject(px,py)` on `ICoordinateSystem` (inverse of `Project`, `CartesianCoordinateSystem.cs:19-20`) and `Invert(fraction)` on `IPosition` (inverse of `Coord`, `Position.cs:62-70`), composed on `Panel` as the public inverse of `Project`. Both are two-line inverses of existing forward math.
- **Gesture capture — a real constraint found in the code:** the panel background `<rect>` is a *sibling* of the geom `<Area>`, not an ancestor (`Panel.razor:28-40`), so `@onwheel` on it will **not** fire when the pointer is over a data mark. Wheel/pointer capture must live on a group that *wraps* the panel content (bubbling carries the event up from any child) or on a top overlay rect. A wrapping `<g @onwheel=… @ondblclick=…>` is the clean choice: it catches gestures over anything inside via bubbling and does **not** suppress the per-mark `@onmouseover`/`@onclick` (those still fire and bubble). This is exactly the roadmap's "`Panel` overlay," and it is the same group Option B later transforms. **This group is emitted only under the opt-in gate and never under Static/Headless (§0)** — its presence is the one byte-visible change interactivity makes, so it is guarded, and non-interactive plots keep today's flat structure and pinned goldens.
- **Covers:** wheel-zoom, double-click reset, range-selector buttons, hit-strip crosshair + coordinate readout (invisible per-bucket `<rect>`s driving an extended `Tooltip.Show`), legend toggle (a click handler on the already-rendered legend swatches setting per-series visibility), click/drill (already shipped — `Area.razor:49`, `Panel.razor.cs:204`).
- **Cost of a step:** one full `ShapeComposer` recompute + SVG diff + RTT. Fine at discrete cadence; wheel spam is absorbed by the coalescing channel (may feel steppy, never janky).

### Option B — Re-render + CSS/SVG transform preview (Tier 2, first JS asset)

Adds the continuous gestures as a preview layer over A.

- **Mechanism:** `pointerdown` starts a gesture; a JS module mutates `transform` on the wrapping `<g>` at frame rate during `pointermove`; `pointerup` reads the net delta, converts to data-unit `Limits` via the Option-A inverse seam, sets them, and refreshes (falling back into A for the authoritative frame).
- **New over A:** the geom layer must render inside one transformable `<g>` (today primitives are flat siblings — `Area.razor`); a ~15–40-line JS interop module under `wwwroot/js/` (the repo's first — `Blazor.instructions §8` "module per concern" pattern applies: `AddScoped`, `IJSObjectReference`, catch `JSDisconnectedException`/`ObjectDisposedException`/`TaskCanceledException`); `non-scaling-stroke`/text handling so the preview doesn't visibly distort before commit.
- **Covers:** drag-pan, rubber-band zoom-select, brush/interval selection, pixel-glued crosshair, lasso.
- **Precedent decision:** this is where "GGNet takes on a JS asset" gets decided on its merits (roadmap). A is shippable without ever making that call.

### Option C — The imperative control API (orthogonal, tier-independent)

The Step-1 scope addition: host-owned controls (`reset`, `zoom to`, `last 2 weeks`) driving a public GGNet surface. This is **not** a rendering strategy — it is the shared **view-window state** that both A's gestures and the host command mutate. A method call is neither Tier 0 nor Tier 2; it sits beneath both.

- **Mechanism:** every entry point — a pointer gesture (A/B) or a host command (C) — resolves to the same operation: *set the view window, refresh*. Reconciliation is therefore trivial: **one state per axis per panel, last-writer-wins, coalesced by the render channel** (`InteractiveRenderModeHandler.cs`). No locking, no gesture-vs-command race beyond "the later one wins," which is the correct UX.
- **The one design subtlety (roadmap's "first deliberate exemption"):** today there is a single `Limits` holding the *build-time* `XLim`. Interactivity needs a **runtime view window distinct from build-time `Limits`**, so that "reset view" returns to the author's `XLim` (or to data bounds if none) rather than to `(null,null)`. Concretely: a second, runtime-only window that `Commit` prefers when present and `ResetView()` clears — leaving the build-time `Limits` untouched. This is the "dynamic `Limits` exempt from `Reset()`" the roadmap flagged; note `Clear()` already spares `Limits` (`Position.cs:72`), so the mechanism is a *distinction*, not a new exemption.

**The primitive core (known now).** The full host surface will be refined, but it reduces to an irreducible set the grounded view-window model already fixes: **two writes and three reads**, typed in data units (`TX`/`TY`), each parameterized by *(axis, plot-wide vs specific panel)* — that targeting is a parameter, not more primitives. Everything else is a composite over these; naming the composites is a later, additive decision.

| Primitive | Role |
|---|---|
| **P1 `SetView(axis, min, max)`** | Set an axis window to an explicit data-unit range. *This is the write*; `ZoomTo` is P1. |
| **P2 `ResetView(axis)`** | Clear the window back to author `XLim` / data bounds. Distinct from P1 — restores a fallback P1 can't name. |
| **P3 `GetView(axis)`** | Current window `(min,max)` — enables relative math (zoom-by-factor, pan-by-delta). |
| **P4 `GetDataExtent(axis)`** | Natural un-windowed trained bounds (`_min/_max`) — anchors data-relative ops. |
| **P5 `Unproject(px,py)`** | Pixel → data. The readout and the entry point for every pixel gesture. |

Contract: writes are async and schedule one coalesced refresh through the interactive channel (`InteractiveRenderModeHandler.cs`); pixels enter *only* through P5; on discrete/date axes P1/P4 speak data keys, snapped via `Map`/`IndexOf`. Composites reduce cleanly: `ShowLast(Period)` = P4→P1; double-click reset = P2; zoom/pan buttons = P3→P1; wheel-zoom = P5→P3→P1; fit = P4→P1.

*Not* in the core, by design (same set/clear/read shape, but their feature isn't grounded yet): **selection** (`Set/Clear/GetSelection` — your `ResetSelection` is `ClearSelection`; belongs to Option B's brush) and **series visibility** (legend toggle; a separate state domain from the view window).

**Illustrative composites** over that core (proposed — shape, not asserted API; none exist yet):

```csharp
// on the Plot<T,TX,TY> component (obtained via @ref) or a returned controller
ValueTask ZoomToXAsync(TX min, TX max, CancellationToken token = default);   // = SetView(X, …)
ValueTask ZoomToYAsync(TY min, TY max, CancellationToken token = default);   // = SetView(Y, …)
ValueTask ShowLastAsync(Period window, CancellationToken token = default);   // = GetDataExtent(X) → SetView(X, …); date/time x
ValueTask ResetViewAsync(CancellationToken token = default);                 // = ResetView(all)
ValueTask<(TX x, TY y)> UnprojectAsync(double px, double py);               // = Unproject (readout)
```

- **Typing:** commands speak **data units** (`TX`/`TY`), matching `XLim`/`Coord`; only the gesture layers (A/B) speak pixels, and they convert at the boundary via the inverse seam. `ShowLast(Period)` is meaningful only for date/time x-scales (`Scale_X_Instant`/`Discrete_Date`) — surface it on those closed generic instantiations, not universally.
- **Where it lives (recommendation):** on `Plot<T,TX,TY>` reached by `@ref`, delegating to `PlotContext` for state + `RefreshAsync` for the frame. Rationale: the component owns the render handler (`PlotBase.RefreshAsync`, `PlotBase.cs:34-42`) and the typed `TX`/`TY`; `PlotContext` owns the scales/`Limits`. A returned `PlotController` façade is the alternative if we want host code to hold the handle without a component ref — decide in Step 3.

---

## 4. What each option needs, at a glance

| Need | A (discrete) | B (continuous) | C (imperative API) |
|---|---|---|---|
| `Unproject`/`Invert` inverse seam | ✅ new (pure) | ✅ (via A) | ✅ for `Unproject`/readout |
| Runtime view-window distinct from build `Limits` | ✅ new | ✅ (via A) | ✅ new (the shared state) |
| Public control surface on `Plot`/`PlotContext` | partial (internal today) | partial | ✅ the whole point |
| Wrapping `<g>` gesture-capture group / `Panel` overlay | ✅ new (opt-in only) | ✅ reused as transform target (opt-in only) | — (emits nothing) |
| JS interop module (`wwwroot/js`, first in repo) | ❌ none | ✅ new | ❌ none |
| Emits any scaffolding under Static/Headless | ❌ never | ❌ never | ❌ never |
| Byte-pinned goldens move for non-interactive plots | ❌ never | ❌ never | ❌ never |
| Touches `ShapeComposer`/geoms/composer/gallery output | ❌ (Step 1 & roadmap: zero) | ❌ | ❌ |

The blast radius stays where the roadmap put it — strategies, scales, `PlotContext`, the `Panel` overlay, builders — and A is reachable with **no JS and no new render group** for any plot that doesn't opt in, only the inverse seam + runtime window + a capture group emitted behind the opt-in gate. That is the cheapest credible first increment (the fermentation wheel-zoom case), and it leaves Headless and every existing golden untouched.

---

## 5. Recommendation and what Step 3 refines

- **Build A + C first.** Together they deliver the entire discrete cluster *and* the host-command API on the existing render engine, with zero JS and a blast radius confined to the seam. C's runtime-window state is the foundation A's gestures write to, so they are naturally co-designed.
- **Gate B behind a real continuous-gesture need.** It is the only option that spends the first-JS-asset decision and the only one needing a transformable render group; its value (drag-pan, rubber-band) is real but Step-1-ranked below the discrete cluster.
- **Refactor freedom (Step-1 scope):** the existing per-mark event plumbing (`Area.razor`) and `Tooltip.Show` (`TooltipBase.cs`) are the surfaces the crosshair/readout extend; expect to generalize `Tooltip.Show` into a shared axis-readout rather than bolt a parallel component beside it.
- **Two constraints are acceptance criteria, not aspirations (§0):** interactivity ships opt-in, and Static/Headless emits nothing. The regression gate is concrete and already in the repo — the byte-pinned goldens (`tests/GGNet.Headless.Tests/Gallery/`) must not move for any non-interactive plot, and the pure-SVG Headless output stays a picture. C's Headless-safety means view-window control (export-time `ZoomTo`/`ShowLast`) is available even where gestures are not.

**Step 3 will refine, per option:** exact files touched and line-count estimate; the `Unproject`/`Invert` signatures on `ICoordinateSystem`/`IPosition`/`Panel`; the runtime-window representation (new property vs wrapper) and its interaction with `Reset()`/facets/polar; the final home of the control API (`Plot` `@ref` vs returned controller); **where the opt-in gate lives** (DSL config peer to `.Style(...)` vs `Plot` parameter) and how it threads to conditional emission; and confirmation that the Tier-0 gesture-capture group leaves the byte-pinned gallery goldens (`tests/GGNet.Headless.Tests/Gallery/`) unmoved for non-interactive plots.
