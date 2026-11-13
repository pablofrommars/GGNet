# Time-cursor inspection across panels

## Why this document exists

This spec hands off a feature proposed by the engineer on 2026-11-12, at the end of the
touchscreen-support Task (`plan/touchscreen-support/task-plan.md`, register row D-03). It carries
the proposal's intent, the in-session review of that proposal against the code, the facts the
review verified, and the device knowledge gained during physical touch QA.

It was revised on 2026-11-13, before binding, with the engineer's rulings on its nine open
decisions ([Rulings at binding](#rulings-at-binding)). Three rulings depart from the original
recommendations and reshape the scope: the feature is an upgrade of the existing `Crosshair`
rather than a separate mode (OD-1), two-finger pan and pinch are in version 1 (OD-2), and bars are
inspectable (OD-7). The version before the rulings is in Git history at `5a5e90e`.

It is bound by `plan/time-cursor-inspection/task-plan.md` and immutable from then on. Changed
intent supersedes it.

It supersedes two items the engineer had approved but that were never built: an opt-in
chart-domain pinch and drag-to-scrub. Both are now part of this spec.

## Outcome

Someone touching a GGNet chart can select a moment on its shared time axis and read every related
series at that moment, across all compatible panels of one `Plot`, with honest treatment of
unevenly sampled and missing data. The selection stays pinned after the finger lifts, survives live
data and resizing, and is cleared only by an explicit control. The same inspection works with a
mouse and from the keyboard. Navigation is not given up for it: with `Pan` on, two fingers pan and
pinch the time window. GGNet stays domain-neutral: it knows named series with formatted values,
not brewing.

The cursor is the existing `Crosshair`, upgraded for every plot that turns it on. The readout is
an opt-in on top of it.

### Motivating use

A consuming dashboard on a portrait 10-inch touchscreen tracks fermentation over hours to weeks:
temperature (°C) with a changing target, specific gravity (e.g. `1.024`), pressure (bar), and
cooling or heating activity drawn as bars. Its operator asks questions such as "what was the
temperature when gravity began dropping?" or "did pressure rise before or after the temperature
changed?". The device has no mouse, wheel or keyboard. Its time axes are `Instant`, and one page
uses numeric hours.

Illustrative readout, one shared time with per-sample honesty:

```text
Inspecting Tue 14:32:00
Temperature  18.7 °C   sample 14:32:08
Target       18.0 °C   step value in effect
Gravity      1.024     sample 2 min earlier
Pressure     0.62 bar  sample 8 s earlier
Cooling      on        value in effect
Ambient      No nearby reading
```

## Scope

### 1. Interaction

Everything in this section applies to a `Plot` whose `Interactivity` has `Crosshair` on.

| Input | Action |
|---|---|
| One-finger tap in a plot panel | Select and pin that x-domain time |
| One-finger horizontal drag | Scrub: the cursor follows the finger; release pins the final time |
| One-finger vertical drag | Native page or container scroll (unchanged) |
| Two-finger horizontal drag, `Pan` on | Pan the time window |
| Two-finger pinch, `Pan` on | Zoom the time window about the midpoint of the fingers |
| Mouse hover | With no pin, an unpinned cursor follows the pointer |
| Mouse click | Pin that time |
| Mouse drag, `Pan` on | Pan, as today |
| Wheel | Zoom, as today |
| Keyboard | Plot focusable; Left/Right step to the previous/next sample time; Escape clears |
| Host clear control | Clear the inspection (`ClearInspectionAsync`) |
| Host reset control | Restore the authored time window (`ResetViewAsync`, unchanged) |

- A tap or click moves the pin; it never dismisses it. Scrolling, tapping elsewhere on the page,
  re-rendering and live data never clear it.
- Small finger movement is inspection input: it must not pan, reset, fire a mark callback or an
  ancestor click.
- One finger then a second: the scrub is abandoned and the previous pin restored. With `Pan` on,
  the two fingers then navigate. No new inspection starts until every finger has lifted. Lifting
  one finger of two never becomes a tap or click.
- Two-finger navigation drives x only, and only when `Zoom` includes x. It previews client-side at
  frame rate in every panel sharing the x scale and commits once on release, like the shipped
  one-finger pan. With `AutoFitY`, y refits on commit.
- Without `Pan`, there is no two-finger chart gesture and the plot area keeps native page pinch.
- A touch double-tap does not reset the view (two quick taps are two selections). A mouse double
  click still resets.
- Panel activation (`onActivate`) never fires under `Crosshair`, as today.
- `CursorTooltip` combines with `Crosshair` as today: a tap also opens the tooltip of the nearest
  mark within reach. The cursor goes to the tapped time, not to that mark; a scrub closes the
  tooltip.

### 2. Shared time across panels

- Selecting a time in any participating panel selects it for the whole `Plot`. Each panel draws its
  cursor line at that domain value through its own projection, clipped to its own plot area — never
  through legends, titles, strips or other panels.
- Invariant: all participating panels inspect the same x-domain value; none picks its own nearest
  shape.
- Participating panels are every panel of the `Plot` except facets built with `freeX: true`, panels
  of a plot with `Flip`, and polar panels. Non-participating panels are never silently
  synchronized. `freeX` and flipped panels keep a local, transient cursor in the panel under the
  pointer, with no pin and no readout.
- On an x axis that inverts exactly (`Instant`, and the numeric scales) the cursor is continuous.
  On `LocalDateTime` and discrete x it snaps to the same positions as the shipped crosshair.
- The cursor's x label is formatted by the scale's own formatter, as today.
- Separate `Plot` components are not synchronized in version 1. Cross-chart sync later goes through
  the bindable inspection value (§4), never hidden global state.

### 3. Series and lookup

- A series joins the readout through an `inspect:` parameter on its geom. Every inspectable series
  is named. A geom split by `colorBy` or `fillBy` yields one series per group, named from the group
  key unless configured otherwise.
- Version 1 inspects `Geom_Line`, `Geom_Point`, `Geom_Area` and `Geom_Bar`. Other geoms are not
  inspected, and this is stated in the docs — never approximated.
- Readouts exist on `Instant` and numeric x. A plot with `Crosshair` and an inspected series on a
  `LocalDateTime` or discrete x axis throws an actionable `GGNetUserException` at initialization.
- Lookup policies, chosen per series and never inferred from how the series is drawn:
  - **Nearest**, within a configured maximum distance.
  - **Previous** (last known reading at or before the time), within a maximum age.
  - **Step** (value in effect until the next change).
  - **Containing**, on bars only: the bar whose x extent contains the time; no bar there is a
    missing reading. A bar series may also use the three policies above, with the bar's x as its
    sample time.
- There is no interpolation in version 1.
- Samples whose y is NaN (the `piecewise` pen lift) are not readings.
- No suitable sample → a "missing" reading with configurable text ("No nearby reading"). A distant
  sample is never presented as simultaneous.
- Each reading reports: series name and group, the cursor time, the actual sample time (when
  observed), how it was obtained (observed, previous, step, containing, missing), and the value.
- Distances are typed per x: `Duration` for `Instant`, the value type for numeric x. GGNet
  hard-codes no freshness threshold.
- Without `Crosshair`, `inspect:` has no effect on any output.

### 4. State

- The pin is an x-domain value plus active/pinned state — never a pixel or array index.
- New samples do not move it; resizing reprojects it; view-window changes, two-finger navigation
  included, never select another time.
- A pin outside the visible window keeps its selection and readout, hides its line and flags the
  readout as outside the view; it is never clamped to the edge.
- A pin is a time, not a snapshot: readings are recomputed when data changes.
- `Plot` exposes the inspection as a bindable parameter (`@bind-Inspection`), so it survives
  `Context` replacement and is controllable by the host. A plot with `Crosshair` and no binding
  still pins; Escape and `ClearInspectionAsync` clear it.
- Notifications distinguish "time or pinned state changed" from "readings changed under the same
  pin", so live data does not churn a consumer's readout.

### 5. Presentation

- GGNet supplies the structured inspection (§3, §4) and draws only: the cursor line and its x label
  per panel, and a marker at each returned sample's **actual** position (never moved onto the
  cursor line), with distinct treatment for missing readings. Paint uses `--ggnet-*` variables.
- The readout and the clear and reset controls are the consumer's (placed above or below the
  panels so no finger covers them). GGNet ships no built-in readout in version 1.

### 6. Architecture

- High-frequency input stays in the one shipped JS module (`src/GGNet/Components/Panel.razor.js`):
  the cursor line moves at frame rate client-side. The shipped crosshair's server round trip per
  hover strip is replaced.
- The x label and the readings are computed server-side, where data, selectors and formatters
  live, and requested latest-wins: at most one request in flight; on its return the newest position
  is sent; release always sends one exact final request. The readout may show an updating state
  meanwhile. No per-move circuit round trip.
- Lookups use a per-series index sorted by x, rebuilt when the series' data changes.
- There is no new mode on `InteractivityOptions`. Anything the module needs travels in the
  existing single `initialize` payload (`PanelInteropOptions`), and the options stay
  initialization-only.
- Under `Crosshair` the plot-area viewport (`svg[data-ggnet-viewport]`) is rendered with
  `touch-action: pan-y pinch-zoom`, or `pan-y` when `Pan` is on. Taking pinch from the plot area
  removes native page zoom there; that is the accepted cost of two-finger navigation.

## Out of scope

- Editing points, equipment commands or any write action: inspection reads only.
- Interpolated readings.
- Inspection of geoms other than the four named in §3 (ribbons, reference lines and the rest).
- A built-in readout or built-in clear and reset controls.
- Cross-`Plot` synchronization (§2).
- Two-finger navigation along y, and any two-finger chart gesture on a plot without `Crosshair`.
- Domain-specific formatting, units or freshness thresholds.
- Changing non-interactive or headless output. Static and headless SVG stay byte-identical, with
  or without `inspect:`, and the gallery goldens do not move.
- Changing the shipped behaviour of plots that do not turn `Crosshair` on.

## Constraints

- Repository contract: `AGENTS.md` and the scoped guides. In particular:
  - Blazor guide §5 — one JS module, one `initialize` payload, no per-move round trips, the opt-in
    gate.
  - C# guide §10 — invariant formatting (`CA1305`); readout values go through `IFormatter<T>` or
    explicit invariant formatting.
  - C# guide §6 — exhaustive switches over unions (`CS8509`).
  - DSL guide — new public surface carries `///` docs.
  - Comment policy — `CommentPolicyTests`.
- New public API changes `tests/GGNet.Headless.Tests/Api/PublicApiTests.GGNet.verified.txt`. That
  is a deliberate re-pin.
- A new parameter on a `Geom_*` overload family must be added to every overload of that family:
  `OverloadConsistencyTests` and `BuilderForwardingTests` guard the hand-synced families.
- New paint classes or variables update `src/GGNet/Themes/Default.css` (`ThemeContractTests`).
- Touch-policy changes stay scoped to the panel viewport (`svg[data-ggnet-viewport]`). There is no
  root or global `touch-action: none`, no blanket `preventDefault`, and browser zoom is not disabled
  globally.
- The shipped crosshair's behaviour changes by design. Tests that pin the old behaviour
  (`tests/GGNet.Components.Tests/CrosshairTests.cs`, the crosshair cases of
  `tests/GGNet.E2ETests/TouchDemoTests.cs`) are rewritten to the behaviour stated here, not
  weakened; every other existing test stays as it is.
- No change to dependency versions without a separate decision.
- Physical acceptance on a real touchscreen is required. Chromium emulation is supporting evidence.

## Verified premises

Checked against `main` at `5a5e90e` on 2026-11-13.

| Premise | Where |
|---|---|
| A `Plot` has one `TX` and one plot-wide x position; the view window lives there | `src/GGNet/PlotContext.Spec.cs` (`Positions`), `src/GGNet/PlotContext.State.cs` |
| Stacked sub-panels each own a y scale and y label | `src/GGNet/PanelFactory.cs` (`Y`, `YLab`), `BuilderExtensions.Panel` in `src/GGNet/BuilderExtensions.cs` |
| Facets can make x independent per panel | `freeX` on `Facet_Wrap`/`Facet_Grid` in `src/GGNet/BuilderExtensions.cs`; `Faceting.FreeX` in `src/GGNet/PlotContext.cs` |
| Each geom layer has its own source, so series sample independently | `Geom_Line(source, …)` in `src/GGNet/BuilderExtensions.Line.cs`; `Geom.source` in `src/GGNet/Geoms/Geom.cs` |
| A series' x/y selectors and colour grouping are on the geom | `Selectors`, `Aesthetics` in `src/GGNet/Geoms/Line/Line.cs` |
| NaN y breaks a line (`piecewise`) | `src/GGNet/BuilderExtensions.Line.cs`, `src/GGNet/Geoms/Line/Line.cs` |
| No step-line geom; `LineType` is a dash pattern only | `src/GGNet/BuilderExtensions.*.cs` (no step option) |
| Temporal positions exist for `Instant` and local date-times | `src/GGNet/Scales/InstantPosition.cs`, `src/GGNet/Scales/DateTimePosition.cs` |
| `LocalDateTime` x does not invert exactly: `Map` returns an index into a per-minute sample buffer and NaN for other values, `Unmap` rounds to the nearest buffered minute. `Instant` and the numeric scales invert exactly | `src/GGNet/Scales/DateTimePosition.cs` (`InvertsExactly`), `src/GGNet/Scales/InstantPosition.cs` |
| Panels are keyed by context and index, so a new `Context` recreates them; `Plot` persists | `<Panel @key=@((Context, i))>` in `src/GGNet/Components/Plot.razor` |
| `Plot.Interactivity` is initialization-only and throws `GGNetUserException` on change | `src/GGNet/Components/Plot.razor.cs` |
| The crosshair is a `bool` option. It is per panel, drawn server-side, driven by a `mouseover` round trip per hover strip (`data-ggnet-strip`), cleared on `mouseleave`, and carries an x label from the scale | `src/GGNet/InteractivityOptions.cs`, `src/GGNet/Components/Panel.razor`, `Panel.razor.cs` (`OnCrosshairOver`, `OnCrosshairLeave`) |
| Touch tap-to-inspect (nearest mark within 24 px) and click-follows-inspection already ship; a touch tap drives the crosshair by dispatching `mouseover` on a strip | `src/GGNet/Components/Panel.razor.js`; Blazor guide §5 |
| Interop options and touch policy. The viewport carrying `touch-action` is rendered only when `Pan` is on | `src/GGNet/Components/PanelInterop.cs` (`PanelInteropOptions`, `TouchAction`), `src/GGNet/Components/Panel.razor` |
| The module keeps a registry of a plot's panels; `sharedX` is true exactly when the plot has one x scale; interop is never initialized on polar panels | `group`, `sharedX`, `sharedY` in `Panel.razor.cs` (`OnAfterRenderAsync`), `Panel.razor.js` |
| Wheel zoom and drag pan commit a view window through `Plot.CommitViewAsync` with cause `Gesture`; pan previews client-side, also in linked panels | `OnWheelAsync`, `OnPanEndAsync` in `Panel.razor.cs`; `Panel.razor.js` |
| `Zoom` has no off value (`X`, `Y`, `Both`); wheel zoom is on whenever `Interactivity` is set | `src/GGNet/ZoomAxis.cs`, `src/GGNet/InteractivityOptions.cs` |
| `Geom_Bar` places a bar at its x with a width given as a fraction of the smallest key spacing | `src/GGNet/BuilderExtensions.Bar.cs`, `src/GGNet/Geoms/Bar/Bar.cs` |
| The four inspected families have 4 (`Geom_Line`), 6 (`Geom_Point`), 4 (`Geom_Area`) and 4 (`Geom_Bar`) public overloads | `src/GGNet/BuilderExtensions.{Line,Point,Area,Bar}.cs` |

## Device and test knowledge from touch QA

- **QA device:** a 10-inch 1200×1920 portrait panel. Chromium 156 runs it at device scale 1, about
  1185 CSS px wide beside the scrollbar, so one CSS px is about 0.11 mm. Demo marks render about
  23 px across (about 2.6 mm), so targets are small.
- **Multi-touch:** the device delivers at least 9 simultaneous contacts. Over areas with default
  touch behaviour, Chromium cancels the page's pointer streams (`pointercancel`) as soon as two or
  more fingers are down, even when the fingers are still. The page only receives two-finger input
  where the panel's `touch-action` excludes pinch.
- **Browser:** Chromium's touch adjustment does not retarget taps to GGNet marks, because Blazor
  delegates events to the root. Targeting therefore has to be GGNet's own.
- **Headless limits:** headless Chromium does not run the long-press selection gesture from DevTools
  touch input. A computed style is the observable there.
- **Emulation viewport:** demo-level browser tests should emulate the device viewport (1185×1920,
  `HasTouch`, not mobile), as `tests/GGNet.E2ETests/TouchDemoTests.cs` does. Swipe distances should
  scale with panel size.
- **Demo deployment runbook:** a self-contained `linux-arm64` publish, run as a user process on a
  loopback port, with build identity in the page. See `src/GGNet.Demo/README.md`. The touch pages
  and the two-layer contact diagnostic (touch events against pointer events) are reusable.
- **Prior regressions** the rewrite must not reintroduce, each pinned by tests in
  `tests/GGNet.E2ETests/`:
  - a stranded preview after the marks group is replaced
  - flashing on release before the committed frame
  - stale hover state
  - long-press text selection

## Rulings at binding

Ruled by the engineer on 2026-11-13. The plan's Decisions section carries the same rulings.

| ID | Decision | Ruling |
|---|---|---|
| OD-1 | Option shape, and how it combines with `Pan`, `Crosshair`, `CursorTooltip` | No separate mode. `Crosshair` itself is upgraded: synced across panels, continuous, draggable, moved client-side. The readout is an opt-in on top. Departs from the recommendation |
| OD-2 | Two-finger pan and chart pinch in version 1? | Both in. One finger scrubs, two fingers pan and pinch the time window when `Pan` is on. Page pinch-zoom over the plot area is given up there. Departs from the recommendation |
| OD-3 | Readings under a pin when data changes | Recompute; "readings changed" notification |
| OD-4 | Pin outside the visible window | Keep pin and readout, hide the line, flag "outside view" |
| OD-5 | Built-in readout and controls, or consumer-only | Consumer renders. GGNet gives structured data plus `ClearInspectionAsync` |
| OD-6 | Interpolation in version 1 | Out; nearest, previous and step only |
| OD-7 | Interval inspection in version 1 | `Geom_Bar` is inspectable, with a lookup for the bar containing the time. Departs from the recommendation |
| OD-8 | x types in version 1 | Readouts on `Instant` and numeric x only. The cursor line itself keeps working on `LocalDateTime` and discrete x, snapped as today. Departs from the recommendation |
| OD-9 | API surface for series configuration | `inspect:` on the four geom families, one `Inspect<T>` record each |
| — | Mouse mapping when `Pan` is on | Hover previews, click pins, drag pans |

Illustrative only, not binding:

```csharp
PlotContext.Build(temperature, r => r.Ts, r => r.Value)
	.Geom_Line(inspect: new Inspect<Sample>("Temperature",
		r => FormattableString.Invariant($"{r.Value:0.0} °C"),
		InspectPolicy.Nearest, maxDistance: Duration.FromMinutes(10)))
	.Panel(p => p.Geom_Line(gravity, r => r.Ts, r => r.Value,
		inspect: new Inspect<Sample>("Gravity", r => FormattableString.Invariant($"{r.Value:0.000}"),
			InspectPolicy.Previous, maxDistance: Duration.FromHours(1))))
	.Style();

<Plot Context=@context RenderMode=@RenderMode.Interactive
      Interactivity=@(new() { Crosshair = true, Pan = true })
      @bind-Inspection=@inspection />
```

## Acceptance criteria

| Criterion | Falsifiable condition |
|---|---|
| ACCEPTANCE-01 | Single panel, two series (measured and a step target): a tap shows both readings at one cursor time, each with its kind and sample time. |
| ACCEPTANCE-02 | Three time-aligned panels: a tap in any panel draws one cursor line per panel at the same domain value, each clipped to its own plot area, and the readout lists all series. |
| ACCEPTANCE-03 | Uneven sampling: the cursor time differs from each reading's reported sample time, and sample markers sit at their actual positions. |
| ACCEPTANCE-04 | Missing data: a series with no sample within its maximum distance reports "missing" with the configured text, never a distant value. |
| ACCEPTANCE-05 | Scrub: during a one-finger horizontal drag the line follows at frame rate with at most one request in flight; release pins the final time with an exact final request. |
| ACCEPTANCE-06 | Live append: new samples do not move the pin; readings under it update with a "readings changed" notification only. |
| ACCEPTANCE-07 | Resize and window changes keep the domain pin; a pin outside the view keeps its readout, hides its line and is flagged outside the view. |
| ACCEPTANCE-08 | Arbitration: no tap or scrub causes a pan, a reset, a mark callback or an ancestor click; a second finger abandons the scrub and restores the previous pin; lifting one of two fingers starts nothing. |
| ACCEPTANCE-09 | Clear and lifetime: only Escape and the clear command clear; leaving the page and returning, and replacing `Context`, give predictable state with no duplicate or stale handlers. |
| ACCEPTANCE-10 | Mouse and keyboard reach the same inspection (hover, click to pin, arrows, Escape); with `Pan` on, a mouse drag pans and does not pin. |
| ACCEPTANCE-11 | Without `Crosshair`, behaviour is unchanged: every existing browser case that does not involve the crosshair passes unmodified. Static and headless output is byte-identical with and without `inspect:` (goldens unchanged). |
| ACCEPTANCE-12 | Gates: build `-warnaserror`, `dotnet test`, both `dotnet format` checks, and `GGNET_E2E=1 dotnet test tests/GGNet.E2ETests` with every browser case executed. |
| ACCEPTANCE-13 | Physical QA on the touchscreen with the engineer: every item here that has a touch component is observed and recorded, including two-finger pan and pinch, and readability (no finger covers the information needed). |
| ACCEPTANCE-14 | Two-finger navigation: with `Crosshair` and `Pan`, a two-finger horizontal drag pans the time window and a pinch zooms it about the fingers' midpoint. Every panel sharing x previews before release; release raises one `ViewChanged` with cause `Gesture`; the pin keeps its domain value. Without `Pan`, the plot area's `touch-action` still allows page pinch. |
| ACCEPTANCE-15 | Bars: an inspected `Geom_Bar` series reports the bar containing the cursor time, and "missing" where no bar does; with a sampled policy it reads like any sampled series. |
| ACCEPTANCE-16 | Crosshair without a readout: with `Crosshair` on and no `inspect:`, the line and its x label appear in every participating panel, and pin on tap or click. On `LocalDateTime` and discrete x the line snaps to the positions the shipped crosshair used. In a `freeX` plot the line stays in the panel under the pointer and does not pin. |
| ACCEPTANCE-17 | Unsupported readouts fail loudly: `Crosshair` with an inspected series on `LocalDateTime` or discrete x throws an actionable `GGNetUserException` at initialization. |
| ACCEPTANCE-18 | Surface and docs: the public API snapshot gains only the inspection surface; `OverloadConsistencyTests` and `BuilderForwardingTests` pass with `inspect:` on every overload of the four families; Blazor guide §5, `README.md`, `skills/ggnet` and the demo README describe the upgraded crosshair, the gestures, the readout API and what is not inspected. |
