# GGNet — Interactivity Blast Radius (Step 3)

*Compiled 2026-07-08. Step 3 of the interactivity session (inventory → options → **blast radius**). Grounded in source on `ai-skills-mcp-jul2026`; existing behavior carries `file:line`, and every new API is marked **proposed** — none of it exists yet. Builds on `interactivity-options.md` (the A/B/C option packages and the two §0 non-negotiables: opt-in, Headless-pure). LOC figures are order-of-magnitude sizing for a first cut, not commitments.*

## What Step 3's grounding changed vs Step 2

Reading the scale internals corrected three things in the options doc — all in the *cheaper* direction:

1. **`ZoomTo(TX)` / `ShowLast` need no new inverse math.** Every position scale already has a public `Map(TKey)→double` (`Scales/Scale.cs:23`; `Extended.cs:74`, `DiscretePosition.cs:105`, `DateTimePosition.cs:172`). A typed window converts to the scale's double space via `Map`. Only *pixel-driven* gestures need an inverse.
2. **The inverse seam is layered, not monolithic.** `Invert(fraction)→double` is universal and trivial (pure inverse of `Coord`, `Scales/Position.cs:62-70`). A *typed* inverse `Unmap(double)→TKey` is separate, per-scale, and needed only for the coordinate readout — continuous is the already-present `transformation.Inverse` (`Transformations/ITransformation.cs`), discrete/date is snap-to-index. It can lag the first release.
3. **Discrete & date axes zoom by snapping, not by pixel.** `DiscretePosition`/`DateTimePosition` `Map` to a **buffer index**, and `Limits` resolve via `values.IndexOf` (`DiscretePosition.cs:66-84`, `DateTimePosition.cs:93-111`). Pixel-precise zoom is a continuous-axis feature; on categorical/time axes a drag snaps to category/sample boundaries. `ShowLast(Period)` sidesteps this — it is a data-key window, not a pixel one.

And one constraint hardened: **the opt-in belongs on the `Plot` component, not the DSL spec** — see §5.

---

## 1. The shared foundation (needed by A; reused by B; C uses the typed subset)

### 1a. Inverse projection seam — *pure C#, emits nothing, Headless-irrelevant*

Two-line inverses of math that already exists. Every forward function has a cited inverse target.

| File | Change (**proposed**) | Inverts | ~LOC |
|---|---|---|---|
| `Coords/ICoordinateSystem.cs` | `(double cx, double cy) Unproject(double px, double py);` | `Project` (`ICoordinateSystem.cs:23`) | +2 |
| `Coords/CartesianCoordinateSystem.cs` | `cx=(px-area.X)/area.Width; cy=1-(py-area.Y)/area.Height;` | `Project` (`CartesianCoordinateSystem.cs:19-20`) | +3 |
| `Coords/PolarCoordinateSystem.cs` | **defer** — `throw new NotSupportedException()` first cut | `Polar.Project` (`PolarCoordinateSystem.cs:38-39`) | +3 |
| `Scales/Position.cs` | `IPosition.Invert`; `double Invert(double f) => Range.min + f*(Range.max-Range.min);` | `Coord` (`Position.cs:62-70`) | +5 |
| `Components/ICoord.cs` | `(double x, double y) Unproject(double px, double py);` (double-space) | `Project` default (`ICoord.cs:19`) | +2 |
| `Components/Panel.razor.cs` | compose `coord.Unproject` + `xscale.Invert`/`yscale.Invert` | `Project` (`Panel.razor.cs:181-182`) | +6 |

Typed readout (deferrable, separate): `Unmap(double)→TKey?` on the scale — `Extended` returns `transformation.Inverse(value)`; discrete/date return `values[round(index)]`. ~+4 base + per-scale. Not required for pan/zoom or `ZoomTo`.

*Flip and polar:* `Flip()` swaps axis roles (`PlotContext.cs:50`) — the Panel composition must invert through the same swap it projects through. Polar (`CarvesAxisBands=false`) is a different interaction model; first cut is Cartesian-only, polar `Unproject` throws until a radar/pie need arrives.

### 1b. Runtime view-window — *the shared state; core of C, written by A/B*

The recommended representation is a **double-space `ViewRange` override**, not a second typed `Limits`. Rationale: pixel gestures and `Map` both produce doubles, so one representation serves every entry point; it leaves the author's build-time `Limits` (`Scales/Continuous.cs:8`) completely untouched, so "reset view" is just "clear `ViewRange`"; and it survives `Reset()` for free because `Position.Clear()` only nulls `_min/_max` (`Scales/Position.cs:72-76`), exactly as `Limits` does today.

| File | Change (**proposed**) | ~LOC |
|---|---|---|
| `Scales/Position.cs` | `(double min, double max)? ViewRange { get; set; }` on `Position<TKey>` | +2 |
| `Scales/Extended.cs` | `Commit`: `if (ViewRange is {} v) Range = v; else SetRange(Limits.min ?? _min ?? 0, Limits.max ?? _max ?? 0);` (`Extended.cs:32`) | +2 |
| `Scales/DiscretePosition.cs`, `DateTimePosition.cs`, `InstantPosition.cs` | same `ViewRange`-preferred branch before `SetRange` (`DiscretePosition.cs:86`, `DateTimePosition.cs:113`) | +2 each |
| `Scales/Position.cs` `Clear()` | **no change** — `ViewRange` must persist across `Reset()`; confirm it is not nulled | +0 |

This *is* the roadmap's "dynamic `Limits` exempt from `Reset()`," made concrete: a distinct runtime window, so `ResetView` returns to the author's `XLim`, never to `(null,null)`.

---

## 2. Option C — imperative control API (build first with 1a+1b)

C is the smallest package that delivers value and needs **no scaffolding, no JS, and is Headless-safe by construction** (it only sets `ViewRange` + refreshes; §0 corollary).

| File | Change (**proposed**) | ~LOC |
|---|---|---|
| `PlotContext.cs` | public typed state setters: `SetXView(TX min, TX max)` → `Positions.X.Scales[*].ViewRange = (Map(min),Map(max))`; `SetYView`; `ResetView()` → clear all `ViewRange`; `ShowLast(Period)` (date/instant x only) | +30 |
| `Data/Position.cs` | helper to set/clear `ViewRange` across all facet `Scales` (`Data/Position.cs:7`) — one shared scale or N free ones | +8 |
| `Components/Plot.razor.cs` | public async handles on the `@ref` component: `ZoomToXAsync`/`ZoomToYAsync`/`ShowLastAsync`/`ResetViewAsync` → mutate `Context` + `RefreshAsync(RenderTarget.Render)` (`PlotBase.cs:34-42`) | +25 |
| `PanelFactory.cs` | optional per-panel Y variants (mirrors panel `YLim`, `BuilderExtensions.cs:384`) | +10 |

**Home (decided):** typed *state* on `PlotContext<T,TX,TY>` (it owns `Positions`/scales, all `internal` today — `PlotContext.cs:44`), typed *commands* on `Plot<T,TX,TY>` reached by `@ref` (it owns the render handler and `TX`/`TY`). `IPlotContext` stays as-is — it is untyped (`IPlotContext.cs`) and cannot carry `TX`/`TY`. Host and gesture layers converge on the same `ViewRange` state; reconciliation is last-writer-wins, coalesced by the interactive render channel (`InteractiveRenderModeHandler.cs`).

**Facet/free-scale handling:** `Panel.X`/`Y` selects one shared scale (`Scales.Count==1`) or the per-cell scale (`Data/Panel.cs:41-47`). Plot-level `ZoomToX` writes every X scale; a panel-scoped overload writes one. Free-scaled facets (independent scales) each hold their own `ViewRange`.

---

## 3. Option A — discrete interactions (Tier 0, zero JS, opt-in)

Adds the gesture-capture group and discrete handlers on top of 1+2. **Every render-structure item here is gated on the opt-in (§5) and absent from Headless.**

| Feature | File | Change (**proposed**) | ~LOC |
|---|---|---|---|
| Capture group | `Components/Panel.razor` | conditional `<g @onwheel=OnWheel @ondblclick=OnReset>` wrapping panel content (bubbling catches gestures over marks; siblings don't — `Panel.razor:28-40`) | +8 |
| Wheel-zoom / reset | `Components/Panel.razor.cs` | `OnWheel(WheelEventArgs)`: cursor px → `Unproject` → shrink/grow `ViewRange` about cursor → refresh. `OnReset`: `ResetView` | +30 |
| Crosshair + readout | `Components/TooltipBase.cs`, `Tooltip.razor` | generalize `Show` (`TooltipBase.cs:22-36`) into an axis-readout: crosshair rules + x/y labels via `Unmap` | +40 |
| Hit-strips | `Components/Panel.razor` / `Area.razor` | invisible per-bucket `<rect @onmouseover>` driving the readout (discrete, snapped — Step 1's Tier-0 crosshair) | +25 |
| Legend toggle | `Components/Plot.razor` (+ geom visibility) | click on already-rendered swatches (`Plot.razor:119-156`) → per-series hide flag consumed at compose | +30 |
| Range buttons | host UI → **Option C** | preset windows are just `ShowLastAsync(...)`/`ZoomToXAsync(...)` — no new GGNet surface | +0 |

Click/drill is **already shipped** (`Area.razor:49`, `Panel.razor.cs:204-213`) — no work.

---

## 4. Option B — continuous gestures (Tier 2, first JS asset, opt-in)

The only package that spends the first-JS-asset decision and needs a transformable render group.

| File | Change (**proposed**) | ~LOC |
|---|---|---|
| `Components/Area.razor` / `Panel.razor` | wrap the geom layer in one `<g id transform>` — today primitives are flat (`Area.razor:12-34`) | +6 |
| `wwwroot/js/interactivity.js` (**new — repo's first runtime JS**) | `pointerdown/move/up`: mutate the group `transform` at frame rate; report net delta on release | +40 |
| `Components/UI/Interop/Interactivity/…` (**new module class**) | `blazor.instructions §8`: `AddScoped`, `IJSObjectReference`, catch `JSDisconnectedException`/`ObjectDisposedException`/`TaskCanceledException` | +40 |
| `Components/Panel.razor.cs` | `pointerdown/up` → JS wiring → on release convert delta via 1a → set `ViewRange` → refresh (falls into A for the authoritative frame) | +40 |
| `Components/Plot.razor` / DI | register the scoped module; `non-scaling-stroke`/text handling so the preview doesn't distort pre-commit | +15 |

Covers drag-pan, rubber-band zoom-select, brush/interval selection, pixel-glued crosshair, lasso.

---

## 5. The opt-in gate — a `Plot` component parameter (decided)

Two candidate homes; the grounding makes the choice clear.

- **Recommended — `Plot` component parameter** (e.g. `[Parameter] public InteractivityOptions? Interactivity { get; init; }` on `Plot<T,TX,TY>`). Headless renders via `Host.RenderAsync(context.PlotType, …)` passing only `Context`/`Width`/`Height`/`Theme` and **never an interactivity parameter** (`IPlotContextExtensions.cs:5-14`, `Host.cs:15-34`). So the parameter defaults unset in Headless, the capture-group/JS conditionals never fire, and the tree `WriteHTML` walks is **byte-identical to today — purity is structural, not conditional**. It also co-locates with the imperative `@ref` handle (§2), since interactivity is a component-hosting concern.
- **Alternative — `.Interactive(...)` DSL terminal** (peer to `.Style`, `BuilderExtensions.cs:887`) setting a `context` flag. More discoverable in the fluent chain, but the flag travels with the context *into* Headless, so the Headless entry points must **explicitly suppress** it or the goldens move. Weaker guarantee; reject unless DSL discoverability outweighs it.

Do **not** gate on `RenderMode`: Headless doesn't pass it (it defaults by reflection), so it is not a reliable purity signal.

---

## 6. Blast-radius rollup & build order

| Package | New files | Files touched | ~LOC | JS? | Headless emits? | Goldens move? |
|---|---|---|---|---|---|---|
| **1. Seam + view-window** | 0 | ~9 (coords, scales, `ICoord`, `Panel`) | ~35 | no | no | no |
| **C. Control API** | 0 | ~4 (`PlotContext`, `Data.Position`, `Plot`, `PanelFactory`) | ~75 | no | no | no |
| **A. Discrete** | 0 | ~5 (`Panel`, `Tooltip`, `Plot`, geom visibility) | ~135 | no | **only the opt-in group** | **no (opt-in off)** |
| **B. Continuous** | 2 (JS + interop module) | ~4 (`Area`/`Panel`/`Plot`) | ~140 | **yes** | only the opt-in group | no (opt-in off) |
| **Opt-in gate** | 0 | ~2 (`Plot`, `Panel`) | ~10 | no | no | no |

**Untouched, confirming Step 1 & the roadmap:** `Scene/ShapeComposer.cs` and `ScreenPrimitive.cs` (primitives are coordinate-space data; the wrapping group is a Razor-template concern in `Area`/`Panel`), all 21 geoms, the composer, `GGNet.Headless` internals, and — with opt-in off — the gallery output.

**Recommended order:**
1. **Seam (1a) + view-window (1b)** — pure C#, no emission, fully unit-testable (`Project`∘`Unproject`=identity; `Map`∘`Invert` round-trips; `ViewRange` survives `Reset()`). Lands green with zero golden risk.
2. **Option C** — host API + the export-time `ShowLast`/`ZoomTo` that works in Headless. Delivers Step-1 Band-B/dashboard value with no scaffolding.
3. **Opt-in gate + Option A** — the discrete cluster (wheel-zoom, reset, crosshair, legend toggle). First byte-visible change; guarded; new circuit-path tests (the roadmap's "interactive circuit coverage" A− item lands here).
4. **Option B** — only when a real continuous-gesture need arrives; this is where "GGNet takes a JS asset" is decided on its merits.

## 7. Verification checklist (acceptance)

- [ ] Seam round-trips: `Unproject(Project(x,y)) ≈ (x,y)` on Cartesian continuous; `Invert(Coord(v)) ≈ v`.
- [ ] `ViewRange` set → survives a full `PlotContext.Render()` pass (`Reset→…→CommitPositions`, `PlotContext.cs:361-382`); cleared → falls back to `Limits`/data bounds identically.
- [ ] **Every existing gallery golden byte-identical with opt-in off** (`tests/GGNet.Headless.Tests/Gallery/`) — the §0 hard gate.
- [ ] Headless `AsStringAsync` on an interactivity-configured context still emits pure SVG (parameter never set → group absent).
- [ ] `ShowLast(Period)` in Headless produces a correctly-windowed static SVG (export path).
- [ ] Discrete/date zoom snaps to category/sample boundaries (documented limitation, not a bug).
- [ ] Interactive circuit tests for wheel-zoom/reset/crosshair (bUnit or driven circuit) — the deferred testing item.

---

## 8. Out of scope — the spec/realization split (and why interactivity de-risks it)

A preview review flagged the roadmap's largest Architecture item: split the immutable plot *spec* (what the fluent API builds) from per-render *realization*, because `PlotContext` today mixes builder + trained + layout + orchestration state in one mutable object. Since re-rendering is central to interactivity, the question is whether to pull it in. **Decision: no — keep it separate; interactivity ships on the current model, designed split-compatible.**

- **Interactivity doesn't need it.** Re-rendering already works: `InteractiveRenderModeHandler` re-runs `Render()`, which is idempotent by construction (`PlotContext.cs:357`). Every P1–P5 primitive works by adding one runtime field (`ViewRange`) that survives `Reset()` and re-running the pipeline — no immutability required.
- **No requirement forces it.** Concurrency is already contained (renders serialized by the handler's semaphore/channel; the gesture writes `ViewRange` on the circuit thread, the channel's ordering makes the render's read safe). "Share a zoomed view" needs **view-state** serialization (the 5 `SetView` numbers), not spec serialization. Multi-view of one spec is not required. The gesture mutates only `ViewRange` — never builder/trained state — so interactivity stays in a safe subset of the mutable model.
- **Coupling is a bad trade.** The split "touches everything" and risks every byte-pinned golden; interactivity is concentrated with zero geom/composer/Headless/gallery impact. Chaining the feature to the biggest refactor delays it and endangers the goldens for benefits it doesn't consume.
- **Interactivity is a precursor that de-risks the split.** It forces the explicit line between *author spec* (`Limits`/`XLim`) and *runtime realization* (`ViewRange`, trained bounds) — exactly the boundary the split formalizes (Step 2 §0's runtime-window distinction) — and stress-tests re-render safety where a real concurrency bug (the split's true trigger) would surface. It moves the split closer and safer.
- **The insulating rule:** public API on the `Plot` component (`@ref`), state on internals (§2). When the split later relocates `ViewRange` into a realization object, internals move but `SetView`/`ResetView`/… stay stable. Mark the home: `// TODO(spec-split): realization state`.

**Trigger for the split (unchanged):** its own drivers — a serialization/multi-view demand, or a concurrency bug the render-serialization doesn't cover — not interactivity.

### 8a. Adopted middle ground — a legibility pass, not a split

Between "ignore the mutability mixing" and "do the full split" there is a cheap, golden-safe step worth folding into interactivity as its **first structural move**: make `PlotContext`'s (and `Position<TKey>`'s) mutability categories *legible*, without extracting types or enforcing immutability. The members already sort cleanly:

| Bucket | Members (`PlotContext`) | Lifecycle |
|---|---|---|
| Identity | `Id`, `PlotType`, `Source` | immutable (already `init`/get-only) |
| **Spec** | `Title`/`SubTitle`/`XLab`/`Caption`, `Selectors`, `X/YScaleDefault`, `Faceting`, `Flip`, `CoordSystem`, `PolarOptions`, `Style`, `DefaultFactory`, `PanelFactories`, `Positions`/`Aesthetics` **`Factory`** | write during build, frozen during render |
| **Realization** | `Positions`/`Aesthetics` **`Scales`**, `Coord`, `Panels`, `Legends`, `N`/`Strip`/`Axis`/`AxisVisibility`/`AxisTitles`/`AxisTitlesVisibility` | rebuilt/cleared every `Render()` pass |
| **Runtime state** (new) | `ViewRange`, series-visibility flags | persists across passes; survives `Reset()` |
| Orchestration | `Initialized`, `Init`/`Render`/`Reset` | control |

`Position<TKey>` mirrors it: `Limits` (spec), `_min/_max` (realization, cleared by `Clear()`), `ViewRange` (runtime, survives `Clear()`).

**The pass = three behavior-free moves:** (1) sort members into labeled partial files by bucket (`PlotContext.Spec.cs`/`.Realization.cs`/`.State.cs`, following the existing `PlotContext.Build.cs`; **not** `#region` — CLAUDE.md reserves it); (2) a one-line invariant banner per file; (3) tighten accessors only where free (`init` for construct-once, `private set` for set-once-then-read). This is where `ViewRange`/visibility get a labeled home — the Step-2 §0 runtime-vs-build-`Limits` distinction becomes structure, not a TODO.

**Ceiling (honest):** the boundary stays *conventional, not compiler-enforced*. The fluent API mutates the context after construction via extension methods (`.Style()` sets `context.Style`; `XLim` rewrites `Positions.X.Factory`), so the Spec bucket must remain `internal set` — true immutability still needs the split. **Do not** extract a nested `State`/`Realization` object (rewrites every access site — the split's churn without its enforcement) or fight the accessors.

**Cost:** mechanical file-moves + comments + a few accessor tweaks; blast radius `PlotContext` partials + `Position<TKey>` comments; zero output change, zero golden risk. Payoff: a labeled home for interactivity's runtime state now, and a future split that is mechanical (members pre-sorted, boundary documented) rather than archaeological. **Slot it before `ViewRange` lands** in the build order (§6), as step 1.5.

### 8b. The pattern is graph-wide, and it is three categories, not two

The description+state mixing is not a `PlotContext` quirk — it is the pipeline's architecture. Every participant with a `Clear()`/`Reset()` hook is a spec-object that accumulates per-pass state, and `PlotContext.Reset()` (`PlotContext.cs:384-418`) walks the whole graph calling `Clear()` on each:

| Participant | Spec (authored) | State (per-pass, cleared) | Hook |
|---|---|---|---|
| `Position<TKey>` | `transformation`, `expand`, `Limits`, `formatter` | `_min/_max`, `Range`, `Breaks`, `Labels`, `values` | `Position.cs:72` |
| Aesthetic scales | selector, palette, guide, name | trained domain, mapping, legend items | `Scale.cs:25` |
| Geoms | selectors, constants, bindings | resolved screen-ready positions/aesthetics | `Geom.cs:21` |
| Coord systems | `style`, `options` | `area` / `CenterX/Y/Radius` (from `Measure`) | `CartesianCoordinateSystem.cs:17` |
| Stat sources | stat config, source | recomputed bins/density | `IStatSource.cs:4` |
| `Data.Panel` | `Coord`, `width`, `Geoms`, `OnClick` | `Component`, `Registered`, layout aggregates | — |

So "the split touches everything" is literal: everything with a `Clear()` has this shape. **This resizes the full split** — it is "split every participant," larger than "fix one god object," which *reinforces* keeping it out of interactivity scope (§8).

**Two categories → three.** Pushing the pattern onto scales exposes a hole in the review's framing: the runtime view window (`ViewRange`) is *neither* spec (unauthored) *nor* realization (realization is derived from spec+data, deterministic, disposable; `ViewRange` survives `Reset()` and is not derivable). It is a third category — **durable interaction state** (view window, selection, series-visibility, focus). The key point: **GGNet's split looked two-way only because the library had no durable interaction state; interactivity is what creates the third category.** So interactivity is not merely a consumer of the eventual split — it reveals its correct shape: **spec / realization / interaction**, graph-wide.

**Already half-present.** Scales carry a partial seam today: `Data.Position<T>.Factory` (`Data/Position.cs:11`) is the immutable *recipe*; each `Instance()` is a fresh *realization* (one per facet). The recipe-vs-instance split exists; the incompleteness is *within* each instance. The full split formalizes what the Factory started.

**Two upgrades to the plan (scope otherwise unchanged):**
1. §8a adopts a graph-wide **three-bucket vocabulary — spec / realization / interaction** — applied incrementally to the participants interactivity touches (`PlotContext`, `Position<TKey>`) and documented so the rest inherit it when next edited. Consistent vocabulary is the down-payment; universal application stays opportunistic.
2. When the full split is triggered, **pilot the real extraction on `Position<TKey>`** — smallest self-contained participant, with the `Factory` seam to build on — before `PlotContext`.

### 8c. Is the split mechanical or a regression risk?

A barbell: **mechanical and self-verifying in volume, but real regression risk concentrated in ~4 seams — and those seams are exactly the ones the current test net cannot see.** The determining factor is not the refactor's difficulty; it is a coverage asymmetry.

**Why the bulk is low-risk (strong oracle).** `Render()` is idempotent by construction (`PlotContext.cs:357`) — the pipeline is already a near-pure function of *(spec + data) → output*, so the split *formalizes* a purity that already holds rather than imposing one. And the byte-pinned gallery (`tests/GGNet.Headless.Tests/Gallery/`, `XDocument.Parse` + byte-compare) is an output-equivalence oracle: field relocation, state-tree threading, the ~23-file `BuilderExtensions` rewrite, API surface (guarded by `PublicApiTests`/`OverloadConsistencyTests`) all self-verify — a byte-identical gallery proves behavior unchanged.

**Where the real risk lives — all net-blind, because the goldens are a single static headless render** (no multi-pass re-render, no circuit, no concurrency — the roadmap's Testing B+ gap):

1. **Per-participant persist-vs-clear lifecycle.** The design depends on *what survives `Reset()`*: `Clear()` nulls `_min/_max` but not `Limits`/`ViewRange` (`Position.cs:72`), and the semantics are per-type idiosyncratic — `DiscretePosition.Clear()` also clears `values`, scarred by "the append-across-passes invariant hid a SortedBuffer corruption bug once already" (`DiscretePosition.cs:94-97`). A naive rebuild-per-render loses this; a regression passes every golden yet breaks interactive re-render.
2. **Deliberate shared-reference invariants.** "Clear contents, never replace the instance: geoms capture the container reference at panel-build time, and default-path panels outlive passes" (`PlotContext.cs:415-417`). Rebuilding the realization tree per pass can break captured references — a bug class the author has already hit.
3. **Builder accumulation & defaulting order.** Reworking the fluent API from mutate-the-context to accumulate-then-freeze risks order-dependent side effects and conditional defaulting (e.g. `XLim`'s inject-default-scale-if-null, `BuilderExtensions.cs:119-122`); goldens catch it only if a gallery plot exercises that path.
4. **Concurrency / state isolation.** `InteractiveRenderModeHandler` mutates `PlotContext` on a background thread; "safe concurrency" requires getting shared-spec vs per-render-state exactly right. Misses are races the single-threaded headless goldens never exercise.

**Verdict:** mechanical in LOC, but genuine risk in a few historically bug-prone seams the gallery is blind to — so it is "largely mechanical" *if and only if* the coverage gap is closed first. Mitigation, in order: (a) **close the gap before touching the seams** — multi-pass re-render equivalence, interactive-circuit (bUnit/driven), and a concurrency stress test (the roadmap's Testing A− item; gate the split behind it); (b) **extract incrementally, scale-first (§8b), each step byte-verified against goldens** — a big-bang split maximizes net-blind risk, an incremental one converts most of it back to the mechanical/self-verifying category; (c) **do the §8a/§8b legibility pass first** — labeling persist/clear/interaction *is* the documentation of seam #1, captured while fresh.

---

## 9. Test strategy — xUnit / bUnit / Playwright

A three-layer pyramid, mapped to the build order (§6). Much of the §8c net-blind risk is *pipeline logic*, so it lands in the cheapest layer, not the browser.

**Layer 1 — plain xUnit (now, no browser).** Closes most §8c risk without new tooling: seam round-trips (`Unproject∘Project`, `Invert∘Coord`); `ViewRange` survives `Reset()` and recomputes correctly; **multi-pass render equivalence** (the property `PlotContext.cs:357` claims but nothing tests — the gallery renders once); builder ordering/defaulting (`XLim` inject-if-null); and **concurrency** (§8c #4) as a multithreaded stress test on `InteractiveRenderModeHandler` (last-writer-wins / no corruption). *Races are an xUnit concern — neither bUnit nor Playwright tests them well.* The Headless byte-pinned goldens remain the output-equivalence oracle.

**Layer 2 — bUnit (with A + C).** The roadmap's "bUnit or a driven circuit" A− item. In-memory, CI-friendly, for what needs the renderer + lifecycle: capture-group `@onwheel`/`@ondblclick` → `ViewRange` → re-render; the imperative API via `@ref` (`SetView`/`ResetView`/`ShowLast`); tooltip/legend/hit-strip re-render; and the **opt-in / Headless-purity gate** (render without `Interactive` → markup identical to golden; with it → capture group appears — the §0 acceptance test). Caveats: the background-thread render handler needs `WaitForState`/`WaitForAssertion` (or test `Render()` synchronously and the handler separately); **bUnit stubs JSInterop**, so it can verify the .NET side *calls* a JS module but never that the JS acted.

**Layer 3 — Playwright (with B only, kept thin).** The single tool that can execute the Tier-2 JS interop and fire real pointer/DOM events — validates the `transform`-preview moving on `pointermove`, real SVG event bubbling over the capture group (Step 3 §3's sibling-vs-ancestor assumption), and the pixel-gesture→commit round-trip end-to-end. Heavyweight (browser + hosted app in CI, slower/flakier); a smoke layer, not a broad suite, adopted only when B lands.

| Tool | Covers | When | Cost |
|---|---|---|---|
| xUnit (have) | seam, lifecycle, multi-pass equivalence, handler, concurrency | now | free |
| Headless goldens (have) | output equivalence | ongoing | free |
| **bUnit** (add) | discrete events, imperative API, re-render, opt-in gate | **A + C** | low; doesn't displace AwesomeAssertions/Moq |
| **Playwright** (add, deferred) | executed JS, real pointer, end-to-end gesture | **B** only | high; browser + running-app harness |

Neither bUnit nor Playwright is on the testing guide's allow/deny lists — they are test *infrastructure*, not assertion/mocking libraries, so they coexist with the "AwesomeAssertions/Moq exclusively" rule (use AwesomeAssertions for value assertions inside bUnit tests). Both are deliberate new dependencies (the repo has neither today); bUnit is low-friction, Playwright rides with the first-JS-asset decision.

---

*This closes Steps 1–3. The trilogy: `interactivity-inventory.md` (what to build, by user value), `interactivity-options.md` (how — preview-vs-commit, three packages, two non-negotiables), and this (where it lands, file by file, plus the architecture boundary §8 and test strategy §9). Implementation is a separate decision, triggered per the roadmap by a dashboard that needs it. The spec/realization split stays out of scope (§8) — interactivity is designed to migrate into it, not to require it.*
