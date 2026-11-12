# Time-cursor inspection across panels

## Why this document exists

This spec hands off a feature proposed by the engineer on 2026-11-12, at the end of the
touchscreen-support Task (`plan/touchscreen-support/task-plan.md`, register row D-03). It carries
the proposal's intent, the in-session review of that proposal against the code, the facts the
review verified, and the device knowledge gained during physical touch QA. Its inputs are the
proposal text, that review, and the `net11-bump` working tree at `cd27877` plus the uncommitted
touchscreen changes.

It is not yet bound by a plan. The decisions listed under [Open decisions](#open-decisions) are the
engineer's; the plan that binds this spec records their rulings, and from then on this document is
immutable. Changed intent supersedes it.

It supersedes two items the engineer had approved but that were never built: an opt-in
chart-domain pinch and drag-to-scrub.

## Outcome

Someone touching a GGNet chart can select a moment on its shared time axis and read every related
series at that moment, across all compatible panels of one `Plot`, with honest treatment of
unevenly sampled and missing data. The selection stays pinned after the finger lifts, survives live
data and resizing, and is cleared only by an explicit control. The same inspection works with a
mouse and from the keyboard. GGNet stays domain-neutral: it knows named series with formatted
values, not brewing.

### Motivating use

A consuming dashboard on a portrait 10-inch touchscreen tracks fermentation over hours to weeks:
temperature (°C) with a changing target, specific gravity (e.g. `1.024`), pressure (bar), and
cooling or heating activity. Its operator asks questions such as "what was the temperature when
gravity began dropping?" or "did pressure rise before or after the temperature changed?". Detailed
inspection belongs to a larger focused chart; an overview chart can stay a navigation surface.

Illustrative readout, one shared time with per-sample honesty:

```text
Inspecting Tue 14:32:00
Temperature  18.7 °C   sample 14:32:08
Target       18.0 °C   step value in effect
Gravity      1.024     sample 2 min earlier
Pressure     0.62 bar  sample 8 s earlier
Ambient      No nearby reading
```

## Scope

### 1. Interaction

| Input | Action in time-cursor mode |
|---|---|
| One-finger tap in a plot panel | Select and pin that x-domain time |
| One-finger horizontal drag | Scrub: the cursor follows the finger; release pins the final time |
| One-finger vertical drag | Native page or container scroll (unchanged) |
| Two-finger drag / pinch | Pan / zoom the time window, only if enabled (Open decision OD-2) |
| Visible clear control | Clear the inspection |
| Visible reset control | Restore the authored time window |
| Mouse hover / click | Unpinned cursor follows the pointer / click pins |
| Keyboard | Plot focusable; Left/Right step to the previous/next sample time; Escape clears |

- A tap moves the pin; it never dismisses it. Scrolling, tapping elsewhere on the page, re-rendering
  and live data never clear it.
- Small finger movement is inspection input: it must not pan, reset, fire a mark callback or an
  ancestor click.
- One finger then a second: the scrub is abandoned and the previous pin restored; no new inspection
  starts until every finger has lifted. Lifting one finger of two never becomes a tap or click.
- Double-tap reset is off in this mode (two quick taps are two selections).

### 2. Shared time across panels

- Selecting a time in any participating panel selects it for the whole `Plot`. Each panel draws its
  cursor line at that domain value through its own projection, clipped to its own plot area — never
  through legends, titles, strips or other panels.
- Invariant: all participating panels inspect the same x-domain value; none picks its own nearest
  shape.
- Participating panels are every panel of the `Plot` except facets built with `freeX: true` and
  polar panels. Non-participating panels are never silently synchronized.
- Separate `Plot` components are not synchronized in version 1. Cross-chart sync later goes through
  the bindable inspection value (§6), never hidden global state.

### 3. Series and lookup

- Every inspectable series is named. A geom split by `colorBy` yields one series per group, named
  from the group key unless configured otherwise.
- Version 1 inspects sampled series on `Geom_Line`, `Geom_Point` and `Geom_Area`. Other geoms are
  not inspected, and this is stated in the docs and the readout model — never approximated.
- Lookup policies, chosen per series and never inferred from how the series is drawn:
  - **Nearest**, within a configured maximum distance.
  - **Previous** (last known reading at or before the time), within a maximum age.
  - **Step** (value in effect until the next change).
  - **Interpolated**, only when explicitly enabled, and always labelled as derived (Open decision OD-6).
- Samples whose y is NaN (the `piecewise` pen lift) are not readings.
- No suitable sample → a "missing" reading with configurable text ("No nearby reading"). A distant
  sample is never presented as simultaneous.
- Each reading reports: series name and group, the cursor time, the actual sample time (when
  observed), how it was obtained (observed, previous, step, interpolated, missing), and the value.
- Distances are typed per x: `Duration` for `Instant` and `LocalDateTime`, the value type for
  numeric x. GGNet hard-codes no freshness threshold.

### 4. State

- The pin is an x-domain value plus active/pinned state — never a pixel or array index.
- New samples do not move it; resizing reprojects it; view-window changes never select another time.
- A pin outside the visible window keeps its selection and readout, hides its line and flags the
  readout as outside the view; it is never clamped to the edge (recommended; OD-4).
- A pin is a time, not a snapshot: readings are recomputed when data changes (recommended; OD-3).
- `Plot` exposes the inspection as a bindable parameter (`@bind-Inspection`), so it survives
  `Context` replacement and is controllable by the host.
- Notifications distinguish "time or pinned state changed" from "readings changed under the same
  pin", so live data does not churn a consumer's readout.

### 5. Presentation

- GGNet supplies the structured inspection (§3, §4) and draws only: the cursor line per panel, and a
  marker at each returned sample's **actual** position (never moved onto the cursor line), with
  distinct treatment for missing and derived values. Paint uses `--ggnet-*` variables.
- The readout is the consumer's (placed above or below the panels so no finger covers it). A
  built-in default readout is Open decision OD-5.
- The clear and reset controls: GGNet exposes `ClearInspectionAsync` and keeps `ResetViewAsync`;
  whether GGNet also renders them is part of OD-5.

### 6. Architecture

- High-frequency input stays in the one shipped JS module (`src/GGNet/Components/Panel.razor.js`):
  the cursor line moves at frame rate client-side.
- Readings are computed server-side, where data, selectors and formatters live, and requested
  latest-wins: at most one request in flight; on its return the newest position is sent; release
  always sends one exact final request. The readout may show an updating state meanwhile. No
  per-move circuit round trip.
- Lookups use a per-series index sorted by x, rebuilt when the series' data changes.
- Time-cursor mode is a new opt-in on `InteractivityOptions`, carried in the existing single
  `initialize` payload (`PanelInteropOptions`). It is initialization-only like the other options.
- Combining it with one-finger `Pan` throws an actionable `GGNetUserException` at initialization.
  It supersedes `Crosshair` and `CursorTooltip` for touch in that `Plot`; how the flags combine is
  part of OD-1.

## Out of scope

- Editing points, equipment commands or any write action: inspection reads only.
- Interval and event inspection (cooling or heating active), unless OD-7 brings it in.
- Cross-`Plot` synchronization (§2).
- Domain-specific formatting, units or freshness thresholds.
- Changing non-interactive or headless output. With the option unset, static and headless SVG stay
  byte-identical, and the gallery goldens do not move.
- Changing the shipped touch behaviour for plots that do not opt in.

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
- No change to dependency versions without a separate decision.
- Physical acceptance on a real touchscreen is required. Chromium emulation is supporting evidence.

## Verified premises

Checked against the tree on 2026-11-12; re-verify before relying on them.

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
| Panels are keyed by context and index, so a new `Context` recreates them; `Plot` persists | `<Panel @key=@((Context, i))>` in `src/GGNet/Components/Plot.razor` |
| `Plot.Interactivity` is initialization-only and throws `GGNetUserException` on change | `src/GGNet/Components/Plot.razor.cs` |
| The crosshair is per panel, driven by hover strips marked `data-ggnet-strip` | `src/GGNet/Components/Panel.razor`, `Panel.razor.cs` (`OnCrosshairOver`) |
| Touch tap-to-inspect (nearest mark within 24 px) and click-follows-inspection already ship | `src/GGNet/Components/Panel.razor.js`; Blazor guide §5 |
| Interop options and touch policy | `src/GGNet/Components/PanelInterop.cs` (`PanelInteropOptions`, `TouchAction`) |

## Device and test knowledge from touch QA

- **QA device:** a 10-inch 1200×1920 portrait panel. Chromium 156 runs it at device scale 1, about
  1185 CSS px wide beside the scrollbar, so one CSS px is about 0.11 mm. Demo marks render about
  23 px across (about 2.6 mm), so targets are small.
- **Multi-touch:** the device delivers at least 9 simultaneous contacts. Over areas with default
  touch behaviour, Chromium cancels the page's pointer streams (`pointercancel`) as soon as two or
  more fingers are down, even when the fingers are still. The page only receives two-finger input
  where the panel's `touch-action` excludes pinch. Taking pinch away from the panel removes native
  page zoom wherever the chart is (OD-2).
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
- **Prior regressions** a new mode must not reintroduce, each pinned by tests in
  `tests/GGNet.E2ETests/`:
  - a stranded preview after the marks group is replaced
  - flashing on release before the committed frame
  - stale hover state
  - long-press text selection

## Open decisions

| ID | Decision | Recommendation |
|---|---|---|
| OD-1 | Option shape and naming: a flag or record on `InteractivityOptions` (for example `TimeCursor`), and how it combines with `Pan`, `Crosshair`, `CursorTooltip` | A record; it implies its own cursor; `Pan` alongside it throws unless two-finger pan is enabled |
| OD-2 | Two-finger pan and chart pinch in version 1? | Ship inspection first; two-finger navigation behind its own flag after physical QA of the page-zoom trade-off |
| OD-3 | Readings under a pin when data changes | Recompute; "readings changed" notification |
| OD-4 | Pin outside the visible window | Keep pin and readout, hide the line, flag "outside view" |
| OD-5 | Built-in readout and controls, or consumer-only | Structured data plus `ClearInspectionAsync` in version 1; a built-in readout later |
| OD-6 | Interpolation in version 1 | Out of version 1; nearest, previous and step only |
| OD-7 | Interval inspection (which interval contains the time) in version 1 | Out of version 1, stated as unsupported |
| OD-8 | x types in version 1 | `Instant`, `LocalDateTime` and numeric x; discrete x unsupported |
| OD-9 | API surface for series configuration: an `inspect:` parameter on the Line, Point and Area families, or a plot-level registration | `inspect:` on those three families, one `Inspect<T>` record each |

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
      Interactivity=@(new() { TimeCursor = new() })
      @bind-Inspection=@inspection />
```

## Acceptance criteria

| Criterion | Falsifiable condition |
|---|---|
| ACCEPTANCE-01 | Single panel, two series (measured and a step target): a tap shows both readings at one cursor time, each with its kind and sample time. |
| ACCEPTANCE-02 | Three time-aligned panels: a tap in any panel draws one cursor line per panel at the same domain value, each clipped to its own plot area, and the readout lists all series. |
| ACCEPTANCE-03 | Uneven sampling: the cursor time differs from each reading's reported sample time, and sample markers sit at their actual positions. |
| ACCEPTANCE-04 | Missing data: a series with no sample within its maximum distance reports "missing" with the configured text, never a distant value. |
| ACCEPTANCE-05 | Scrub: during a one-finger horizontal drag the line follows at frame rate with at most one readings request in flight; release pins the final time with an exact final request. |
| ACCEPTANCE-06 | Live append: new samples do not move the pin; readings under it update with a "readings changed" notification only. |
| ACCEPTANCE-07 | Resize and window changes keep the domain pin; a pin outside the view follows the ruled OD-4 policy. |
| ACCEPTANCE-08 | Arbitration: no tap or scrub causes a pan, a reset, a mark callback or an ancestor click; a second finger abandons the scrub and restores the previous pin; lifting one of two fingers starts nothing. |
| ACCEPTANCE-09 | Clear and lifetime: only the clear control clears; leaving the page and returning, and replacing `Context`, give predictable state with no duplicate or stale handlers. |
| ACCEPTANCE-10 | Mouse and keyboard reach the same inspection (hover, click to pin, arrows, Escape). |
| ACCEPTANCE-11 | With the option unset, static and headless output is byte-identical (goldens unchanged) and existing touch behaviour is unchanged (existing E2E suite green). |
| ACCEPTANCE-12 | Gates: build `-warnaserror`, `dotnet test`, both `dotnet format` checks, and `GGNET_E2E=1 dotnet test tests/GGNet.E2ETests` with every browser case executed. |
| ACCEPTANCE-13 | Physical QA on the touchscreen with the engineer: every item above that has a touch component is observed and recorded, including readability (no finger covers the information needed). |
