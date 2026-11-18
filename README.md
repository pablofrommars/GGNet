[![License](https://img.shields.io/github/license/BlazorExtensions/Storage.svg?longCache=true&style=flat-square)](https://github.com/pablofrommars/GGNet/blob/main/LICENSE.TXT)
[![Package Version](https://img.shields.io/badge/nuget-v1.4.0-blue.svg?longCache=true&style=flat-square)](https://www.nuget.org/packages/GGNet/1.4.0)
# GG.Net Data Visualization

GG.Net lets Data Scientists and Developers create interactive and flexible charts for .NET and [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) Web Apps.

Taking its inspiration from the highly popular [ggpplot2](https://ggplot2.tidyverse.org) R package, GG.Net provides natively rich features for your Data Analysis Workflow. Build publication quality charts with just a few lines of code in C# and F#.

[Learn more about GG.Net](https://pablofrommars.github.io/)

## Install

`2.0` builds publish to **GitHub Packages**. That feed requires authentication even for public packages, so add a `nuget.config` next to your solution with a [personal access token](https://github.com/settings/tokens) carrying the `read:packages` scope:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
	<packageSources>
		<add key="ggnet" value="https://nuget.pkg.github.com/pablofrommars/index.json" />
	</packageSources>
	<packageSourceCredentials>
		<ggnet>
			<add key="Username" value="USERNAME" />
			<add key="ClearTextPassword" value="TOKEN" />
		</ggnet>
	</packageSourceCredentials>
</configuration>
```

```
dotnet add package GGNet --prerelease
```

The `1.4.0` release remains on [nuget.org](https://www.nuget.org/packages/GGNet/1.4.0).

## The DSL

A plot is one fluent chain: `PlotContext.Build(source, x, y)` establishes the data source and default selectors, each `Geom_*` call adds a layer configured in place, `Scale_*` calls shape the axes and legends, and `.Style()` finishes the plot.

```csharp
var plot = PlotContext.Build(points, o => o.X, o => o.Y)
	.Geom_Line(strokeWidth: 2, color: "#23d0fc")
	.Geom_HLine([1.0], y: o => o, label: o => "Baseline", lineType: LineType.Dashed)
	.Scale_Y_Continuous(formatter: new DoubleFormatter("N2"))
	.Style();
```

### Conventions

- **`xxxBy` means data-driven.** `colorBy`, `fillBy`, `sizeBy`, `lineTypeBy` take an aesthetic *mapping* (built by `Scale_Color_Discrete`, `Scale_Fill_Continuous`, …): the value is computed per item, trains a scale, and feeds the legend. The unsuffixed twin (`color`, `fill`, `size`, `lineType`) is a constant applied to the whole layer. When a mapping is present it wins for its own aesthetic; the constant then still serves as the base for *other* aesthetics' legend swatches (a line-type legend draws its swatches in the constant color), so setting both is meaningful rather than an error.
- **Positional arguments stop at the selectors.** Source and selector parameters (`x`, `y`, `ymin`, `open`, …) may be passed positionally; every aesthetic, event, or option after them is passed by name. The signatures are wide by design — configuration lives in one call — and named arguments are what keep call sites readable and stable.
- **The vocabulary is SVG's.** `strokeWidth`, `opacity`, `fillOpacity`, `strokeOpacity`, `strokeColor` mean exactly what they mean in SVG. `width` and `height` are reserved for geometric extent in data units (`Geom_Bar`, `Geom_Tile`, `Geom_Violin`, `Geom_RidgeLine`).
- **Interactivity is a uniform block.** Every data-mark geom takes `onclick`, `onmouseover`, `onmouseout`, and (where a hover surface makes sense) `tooltip`. When `tooltip` is set and no explicit hover handlers are given, the default hover shows it. Annotation geoms (`Geom_ABLine`, `Geom_HLine`, `Geom_VLine`, `Geom_Text`) and statistical summaries (`Geom_Boxplot`, `Geom_Violin`, `Geom_RidgeLine`) deliberately take no event block.

### Position ranges and updates

Log10 positions require positive finite data, limits, and view bounds. Nonpositive values and infinities throw `GGNetUserException`; filter those rows or choose a continuous scale. Existing NaN missing-point and line-gap behavior is retained. Nonfinite position extents and view windows are rejected before tick generation.

Automatic area ranges include their drawn baseline, which is zero in transformed coordinates (raw value one on a Log10 axis). Explicit limits and view windows still take precedence. Large finite numeric ranges use finite expansion and overflow-safe projection; very small, very large, or narrowly offset ranges use at most five evenly spaced major ticks. Expansion saturates at finite double endpoints. Equal view bounds receive singleton padding.

In Blazor, changing `Context`, `Width`, or `Height` requests a refresh in both interactive modes and preserves view windows when resizing. `Static` retains its initial frame. A context may be reused sequentially after headless export; use separate contexts for concurrently mounted plots.

### Overview and detail charts

GGNet does not maximize a panel or switch between compositions. A page that shows an overview and opens a related detail chart builds two plots and owns which one is showing. Three primitives make the hand-over explicit:

- `context.XView` / `context.YView` read the current view window in data units (null when no window is set, and on discrete and `LocalDateTime` axes).
- `Plot.ViewChanged` fires once per committed window change with its cause: `Gesture`, `HostCommand` or `Reset`. Name `Plot`'s type arguments when binding it to a method.
- `.Panel(…, onActivate:, key:)` fires for a click anywhere in a sub-panel's plot area and reports which sub-panel it was. Mouse clicks on a plot with `Interactivity` wait about 200 ms so double-click reset wins. Enabled chart touch gestures consume browser clicks. Opt into `Touch.TapActivatesPanel` to invoke the panel callback natively, including with `Crosshair`; this does not dispatch mark clicks.

```razor
<Plot T="Reading" TX="Instant" TY="double" @ref=overview Context=@overviewContext RenderMode=@RenderMode.Interactive ViewChanged=@OnOverviewViewChanged />
```

```csharp
// open: carry the overview's window to the detail chart
if (overviewContext.XView is { } window)
{
	await detail.ZoomToXAsync(window.min, window.max);
}
```

Things to keep in mind when several plots share a page:

- **Gaps.** A `double.NaN` reading draws a gap only with `Geom_Line(piecewise: true)`; otherwise the line joins across it.
- **One mutable source behind several plots.** Interactive composition runs off the UI thread and reads the list you gave it. Append or update on the renderer's synchronization context (`InvokeAsync`) before refreshing, or a render can enumerate a half-updated list.
- **Closing.** Closing a detail on any click of its wrapper competes with taps the chart uses. An explicit close button keeps navigation apart from inspection.
- **Hidden plots.** A plot hidden with CSS and `pointer-events: none` ignores input, but with `Interactivity` its gesture module still runs and still hears window-level scroll and touch events.

The demo's `/overview-detail` page is a working example.

### Crosshair and readout

`Interactivity = new() { Crosshair = true }` gives an interactive plot one inspected x position, drawn as a line with its x label in every panel that shares the x scale. A mouse moves it by hovering and pins it with a click; a finger pins it with a tap and enters inspection after holding for 400 ms within 10 screen pixels, then scrubs and pins on release. Active touch interaction owns the plot area by default; `TouchOwnership.ScrollY` permits native vertical scrolling. Left and Right step between sample times and Escape clears. A pin is a data value: it survives new data, resizing, view changes and a new `Context`, and only Escape, `ClearInspectionAsync` or the host end it.

A layer joins the readout with `inspect:`. Each series finds its own reading at the inspected position, under a policy you choose per series, and says which sample it used:

```csharp
PlotContext.Build(temperature, r => r.At, r => r.Value)
	.Scale_X_Instant()
	.Panel(p => p
		.Geom_Line(inspect: new("Temperature", r => FormattableString.Invariant($"{r.Value:0.0} °C")) { MaxDuration = Duration.FromMinutes(10) })
		.Geom_Line(target, r => r.At, r => r.Value, inspect: new("Target", r => FormattableString.Invariant($"{r.Value:0.0} °C"), InspectPolicy.Step)))
	.Panel(p => p
		.Geom_Point(gravity, r => r.At, r => r.Value, inspect: new("Gravity", r => FormattableString.Invariant($"{r.Value:0.000}"), InspectPolicy.Previous) { MaxDuration = Duration.FromHours(3) }))
	.Style();
```

```razor
<Plot T="Sample" TX="Instant" TY="double" @ref=plot Context=@context RenderMode=@RenderMode.Interactive
      Interactivity=@(new() { Crosshair = true, Pan = true })
      @bind-Inspection=inspection ReadingsChanged=@OnReadingsChanged />
```

- **Policies.** `Nearest` takes the closest sample on either side, `Previous` the last one at or before the position, both within `MaxDuration` (`Instant` x) or `MaxDistance` (numeric x). `Step` takes the last one however old: a value in effect. `Containing`, on `Geom_Bar` only, takes the bar whose x extent holds the position. Nothing is interpolated. A series with no qualifying sample reports `ReadingKind.Missing` with its `Missing` text, never a distant value.
- **The readout is yours.** `ReadingsChanged` hands you `InspectionReadings<TX>`: the position, one `Reading<TX>` per series (name, group, the sample's own x, how it was obtained, the formatted value) and whether the position lies outside the visible window. GGNet draws the line, its label and a marker on each sample that was read; it renders no readout and no clear button.
- **Two notifications.** `InspectionChanged` fires when the position or its pinned state changes through a gesture, a key or a clear. `ReadingsChanged` fires when the readout changes, which includes live data arriving under a pin that did not move.
- **Navigation.** With `Pan` and an X wheel axis, an immediate one-finger horizontal drag pans and two fingers pan/pinch X, whether or not a crosshair is visible. A hold enters inspection instead. Explicit `Touch` options control navigation independently of mouse `Pan` and `Zoom`. Opt into `Touch.DoubleTapResetsView` for double-tap reset that retains the committed inspection.
- **Where readings exist.** On `Instant` and numeric x, for `Geom_Line`, `Geom_Point`, `Geom_Area` and `Geom_Bar`. Other geoms are not read. An inspected layer on a `LocalDateTime` or discrete x axis throws when the plot mounts with `Crosshair`; the line itself works there, snapped. Facets with `freeX` and flipped plots get a line local to one panel, with no pin and no readout. Separate plots are not synchronized: carry `Inspection` between them yourself.

Four cautions for a page that uses it:

- Give the readout a fixed height and put it where no finger covers it. A readout that grows when a reading arrives moves the chart under a resting pointer, and a pointer that leaves the plot area ends an unpinned crosshair.
- By default, under a shared crosshair a touch tap pins and goes no further: it does not reach a mark's `onclick` or a click handler around the chart. A mouse click reaches both.
- A marker sits at the row's own value. In a stacked area or bar it is not on the stacked outline.
- Name `Plot`'s type arguments when binding `ReadingsChanged` or `ViewChanged` to a method.

### Touch policy and beta migration

The beta touch contract replaces immediate drag-to-scrub with hold-to-scrub. A second contact
abandons a provisional scrub and restores the committed pin before navigating; a third contact
cancels. After two fingers become one, the remaining finger cannot start another gesture until
all contacts lift. Pinch distance remains Euclidean, so fingers may sit on different linked panels.

Use one explicit policy when the inferred `Pan`/`Crosshair` settings do not describe your host:

```csharp
new InteractivityOptions
{
    Crosshair = true,
    Pan = true,
    Touch = new()
    {
        Navigation = TouchNavigation.PanAndPinchX,
        Inspect = true,
        Ownership = TouchOwnership.ScrollY
    }
}
```

`TouchNavigation.TwoFingerX` reserves navigation for two fingers. `None` disables navigation.
`Inspect` enables tap/hold inspection independently, including a host readout without a visible
crosshair. `Touch = new()` disables chart touch gestures entirely while retaining mouse/pen
configuration. Options remain initialization-only: remount with a new `@key` to change them.

While a touch policy is on, the chart owns its touch taps: they reach no mark `onclick`, panel
activation wrapper or surrounding click handler, and `CursorTooltip` stays a mouse feature.
`TapActivatesPanel` invokes only the native panel callback instead of pinning; hold inspection still works. Only a chart
with touch off keeps the browser's tap-as-click, and its tap standing in for hover on a cursor
tooltip; it then inspects no position by touch.

A touch gesture still in flight when the host commands the viewport (`ZoomToXAsync`,
`ZoomToYAsync`, `ResetViewAsync`, `ShowLastAsync`, a window set before `RefreshAsync`, or a new
`Context` whose x window differs from the one it replaces) is discarded: the host wins. So is one in flight when the host clears or sets the selection on a
plot with touch inspection or native tap actions; with navigation alone a selection change leaves touch navigation alone. A wheel, mouse or pen view change made while a touch gesture is still in flight wins the
same way. A mouse or pen pan keeps its preview until the frame of its own commit, or of a
later host viewport command, is rendered; a frame that only brings new data does not end it. If no
such frame arrives within ten seconds the preview is dropped and the chart stays usable. A `RefreshAsync` that only brings new data keeps the gesture and the pin. Replacing the `Context` discards an in-flight touch gesture in the browser whatever its window: the plot's panels are keyed by context, so their touch state is disposed with them. Only the server's acceptance rule tells a same-window replacement apart, and no browser client can use it. A dropped circuit is noticed only when the ten-second watchdog
fires, since Blazor neither completes nor rejects a call in flight across a disconnect.

Beta change: `ZoomToXAsync`, `ZoomToYAsync`, `ResetViewAsync` and `RefreshAsync` given an
already-cancelled token now render nothing and throw `TaskCanceledException` under both
`RenderMode.Interactive` and `RenderMode.InteractiveAuto`. `InteractiveAuto` previously ignored
the token and rendered anyway. A cancelled command still leaves its window on the context, and
the next refresh renders it.

`Owned` (the default) installs `touch-action: none` on the plot-area viewport. `ScrollY` permits
vertical page scrolling and may cancel a hold or scrub; it permits page pinch only when chart
pinch is disabled. Neither policy changes the surrounding page. There is no touch Y navigation
in this contract; `ZoomAxis.Y` remains meaningful for mouse/pen, and AutoFitY follows committed X
changes. Explicit enabled touch requires unflipped Cartesian shared-X charts without free-X facets;
unsupported default configurations leave those touch capabilities disabled.

For in-chart overview/detail actions, configure the overview with both `TapActivatesPanel = true`
and `DoubleTapResetsView = true`; configure detail with `TapActivatesPanel = false` and
`DoubleTapResetsView = true`. Keep `Inspect = true` and `Navigation = TouchNavigation.PanAndPinchX`
in both. Overview taps activate; detail taps pin. Keep the host's in-chart × and independent Clear.
Carry `Inspection` and the viewport between compositions. Reset retains the committed inspection.
An offscreen selection stays a data coordinate rather than being clamped to an edge.

Both new options default to false. With double-tap reset enabled, the single action waits 300 ms
from release. A second press on the same panel within that interval and 24 screen pixels cancels
the first action; releasing that press without a hold or drag resets once. Holding for 400 ms,
10-pixel movement, additional contacts, a different panel, an outside press, a host command,
resize, context replacement or disposal cancels pending tap work. A second press that becomes
a pan, scrub or pinch performs that gesture, with no activation or reset. Widely separated taps
cancel the earlier pending action and start a new candidate. No browser clicks are synthesized.
These thresholds need physical touchscreen calibration; a slower double tap is two single taps.
Changing touch options requires remounting the Plot with a different `@key`.

### Measured cursor labels

Set `Style.Cursor` to opt into native in-chart label layout:

```csharp
var style = Style.Default(init: s =>
{
    s.Panel.Spacing.Y = 0;
    s.Cursor = new() { FirstPanelOnly = true, FontSize = 1.5, Padding = 4, Gap = 4 };
});
```

Pass this style to `.Style(style)`. `FontSize` uses GGNet's `Size` units (1.5 em is 24 SVG units);
null inherits X-axis text. The existing module measures SVG text after fonts load and C# computes
the label position and line clearance from those bounds. The label appears after its measurement
returns. It is centered on the selected X where it fits and shifts inward at an edge; only the label
moves. An over-wide timestamp is horizontally compressed with SVG `textLength`; a label too tall
for its panel reduces its font size. Tiny charts still need enough physical space to read text.
`FirstPanelOnly` labels the top panel of each shared-X column; lower lines begin at their panel's
top. Zero panel spacing connects the lines. Free-X and flipped local cursors retain their own labels.
Null `Style.Cursor` preserves the legacy geometry and repeated labels. Paint remains theme CSS.

Touch feedback is local: the plot root carries `data-ggnet-interaction` with `inspecting`,
`navigating`, `pending` or `unavailable`, and removes it when settled. Provisional lines do not
carry old timestamps or sample markers. Navigation translates/scales existing grid positions and translates tick anchors with the same
preview as the marks, without stretching tick text. Out-of-view ticks are hidden; new ticks wait
for a matching rendered frame. Y context stays accurate against the currently drawn Y range,
including while a future AutoFitY refit is pending. A mismatched panel/marks frame hides context
until the pair matches. Sample values and cursor labels/markers remain hidden until acknowledged. A mouse or pen pan gives the same feedback: `navigating` from the moment the drag
is recognized, then `pending` from release until its frame is rendered, with cursor
label and markers hidden and a pinned line travelling with the marks. Its ten-second bound only
drops the preview and the feedback; it never makes the chart `unavailable`. A wheel step, a key
and a double-click reset set no feedback. A ten-second unresolved touch operation enters
`unavailable`; remount the plot to resume.
So does a touch commit whose call fails: an exception escaping `ViewChanged`, `InspectionChanged`
or `ReadingsChanged` during a touch commit puts the chart in `unavailable` until remount, although
the server has already committed the change. An exception escaping `ViewChanged` during a mouse or
pen pan is not surfaced: the pan stays committed and the chart stays usable.
In `unavailable` the last acknowledged cursor label, markers and frame stay visible beside the
indication, since they are the last state the server confirmed. A mouse or pen pan still works
there, and for the length of its preview the label and markers are hidden while ticks follow the preview.

Place the plot and its readout inside a common wrapper to hide stale host values immediately,
without waiting for a server callback, and to keep the last acknowledged values on show once the
plot is `unavailable`. For example, give that wrapper `class="chart-inspection"`
and wrap the values inside `.readout-values`:

```css
.chart-inspection:has([data-ggnet-interaction]:not([data-ggnet-interaction="unavailable"])) .readout-values {
    visibility: hidden;
}
.chart-inspection:has([data-ggnet-interaction]:not([data-ggnet-interaction="unavailable"])) .readout::after {
    content: "Updating readings…";
}
.chart-inspection:has([data-ggnet-interaction="unavailable"]) .readout::after {
    content: "Last readings — reload to reconnect";
}
```

Keep the readout's dimensions stable so feedback does not move the chart under a held pointer.
`ReadingsChanged` continues to report the actual selection belonging to its values.

### Stats

Stats are sources, not layers: each `Stat.*` call returns a typed source that any geom draws unchanged, recomputed on every render pass so streaming data stays current.

```csharp
// a histogram is Stat.Bin + Geom_Bar — there is no Histogram geom
PlotContext.Build(Stat.Bin(readings, r => r.Value, bins: 20), b => b.Mid, b => b.Count)
	.Geom_Bar(width: 1.0)
	.Style();
```

| Stat | Output | Draw with |
|-|-|-|
| `Stat.Bin` | `Bin` / `Bin<TKey>` (min, mid, max, count, density) | `Geom_Bar(x: b => b.Mid, y: b => b.Count)` |
| `Stat.Density` | `DensityPoint` / `DensityPoint<TKey>` (at, density) | `Geom_Area`, `Geom_Line`, `Geom_Violin(width: d => d.Density)` |
| `Stat.Count` | `Count<TKey>` (key, n) | `Geom_Bar` over categories |
| `Stat.Summary` | `Summary` / `Summary<TKey>` (x, center, lower, upper) | `Geom_ErrorBar(y: s => s.Center, ymin: s => s.Lower, ymax: s => s.Upper)` |

**Per-facet statistics are grouped statistics.** Compute with `groupBy:` and facet the output on the same key — the key is deliberately stated twice; a mismatch between them is almost certainly a bug:

```csharp
PlotContext.Build(Stat.Bin(readings, r => r.Value, r => r.Tank, bins: 10), b => b.Mid, b => b.Count)
	.Geom_Bar(width: 1.0)
	.Facet_Wrap(b => b.Group)
	.Style();
```

Statistics run over the whole source (per group when grouped). Stats that would depend on panel-trained state — a function traced over each panel's free-scale range — are out of scope by design.

### Geoms

| Geom | Selectors | Mappings | Constants | Events | Tooltip |
|-|-|-|-|-|-|
| `Geom_Point` | `x`, `y` | `sizeBy`, `colorBy` | `size`, `color`, `opacity` | ✓ | ✓ |
| `Geom_Line` | `x`, `y` | `colorBy`, `lineTypeBy` | `strokeWidth`, `color`, `opacity`, `lineType`, `piecewise` | ✓ | ✓ |
| `Geom_Bar` | `x`, `y` | `fillBy` | `fill`, `fillOpacity`, `strokeColor`, `strokeOpacity`, `strokeWidth`, `position`, `width` | ✓ | ✓ |
| `Geom_Area` | `x`, `y` | `fillBy` | `fill`, `fillOpacity`, `position` | ✓ | ✓ |
| `Geom_Ribbon` | `x`, `ymin`, `ymax` | `fillBy` | `fill`, `fillOpacity` | ✓ | ✓ |
| `Geom_ErrorBar` | `x`, `y`, `ymin`, `ymax` | `colorBy` | `strokeWidth`, `color`, `opacity`, `lineType`, `radius`, `position` | ✓ | ✓ |
| `Geom_Segment` | `x`, `xend`, `y`, `yend` | — | `strokeWidth`, `color`, `opacity`, `lineType` | ✓ | ✓ |
| `Geom_Tile` | `x`, `y`, `width`, `height` | `fillBy` | `fill`, `fillOpacity`, `strokeColor`, `strokeOpacity`, `strokeWidth` | ✓ | ✓ |
| `Geom_Hex` | `x`, `y`, `dx`, `dy` | `fillBy` | `fill`, `opacity` | ✓ | ✓ |
| `Geom_Radar` | `x`, `y` | `fillBy` | `fill`, `fillOpacity`, `strokeWidth` | ✓ | ✓ |
| `Geom_Map` | `polygons` | `fillBy` | `fill`, `fillOpacity`, `strokeColor`, `strokeWidth` | ✓ | ✓ |
| `Geom_Candlestick` | `x`, `open`, `high`, `low`, `close` | — | `strokeWidth`, `color`, `opacity`, `lineType` | ✓ | — |
| `Geom_OHLC` | `x`, `open`, `high`, `low`, `close` | — | `strokeWidth`, `color`, `opacity`, `lineType` | ✓ | — |
| `Geom_Volume` | `x`, `volume` | — | `fill`, `opacity` | ✓ | — |
| `Geom_Boxplot` | `x`, `y` | `fillBy` | `size`, `fill`, `fillOpacity`, `strokeWidth` | — | — |
| `Geom_Violin` | `x`, `y`, `width` | `fillBy` | `fill`, `fillOpacity`, `strokeColor`, `position` | — | — |
| `Geom_RidgeLine` | `x`, `y`, `height` | `fillBy` | `fill`, `fillOpacity` | — | — |
| `Geom_Text` | `x`, `y`, `angleBy`, `text` | `colorBy` | `size`, `anchor`, `weight`, `style`, `color`, `angle` | — | — |
| `Geom_ABLine` | `a`, `b`, `label` | — | `strokeWidth`, `color`, `opacity`, `lineType`, `size`, `anchor`, `weight`, `style` | — | — |
| `Geom_HLine` | `y`, `label` | — | `strokeWidth`, `color`, `opacity`, `lineType`, `size`, `anchor`, `weight`, `style` | — | — |
| `Geom_VLine` | `x`, `label` | — | `strokeWidth`, `color`, `opacity`, `lineType`, `size`, `anchor`, `weight`, `style` | — | — |

### Theming

Styling is split by one rule: **if it moves layout it's C# (`Style` — font sizes, margins, positions, because the server measures them); if it's paint it's CSS.** Paint targets stable semantic classes (`panel`, `x-break`, `legend-title`, …) scoped under `.ggnet[theme=name]`, selected by the `Theme` parameter on the `Plot` component.

A theme is a block of variable overrides, not a stylesheet fork:

```css
.ggnet[theme=mytheme] {
	--ggnet-bg: #1e1e1e;
	--ggnet-grid: #333;
	--ggnet-break-label: #9ca3af;
}
```

The base rules in `Themes/Default.css` read every paint through a `--ggnet-*` variable (backgrounds, grid, labels, titles, strips, legend, spinner — the file documents the full set), so a theme overrides only what it changes, anything it omits degrades to the default instead of rendering unstyled, and classes added by future GGNet versions are painted automatically. A test (`ThemeContractTests`) enforces the contract: every emitted class painted, every referenced variable defined, theme files only setting known variables.

Notes:

- Geom parameters accept css custom properties — `color: "var(--color-temperature)"` wires a layer to your design tokens.
- Changing `--ggnet-font` affects rendering only: server-side text measurement assumes Inter until font metrics ship with the theme.
- **Self-contained export**: `plot.AsStringAsync(selfContained: true)` / `SaveAsync(..., selfContained: true)` embeds the bundled theme as a `<style>` element so the SVG renders standalone; off by default — app-hosted output is styled by the app's stylesheet.

## Agent Skill

The repo ships a model-facing skill for AI coding agents under [skills/ggnet/](skills/ggnet/) — the DSL manual, a data-shape → chart selection guide, 30 compile- and render-verified example recipes, and a snippet validator ([skills/ggnet/scripts/validate.cs](skills/ggnet/scripts/validate.cs)). Nothing in it is hand-maintained prose: signatures are extracted from source, examples are pinned gallery tests, and drift is caught by the test suite — the skill version *is* the library version.

The repo is its own single-plugin marketplace ([`.claude-plugin/marketplace.json`](.claude-plugin/marketplace.json)), so consuming projects install the skill straight from it.

**Claude Code**, from GitHub:

```
/plugin marketplace add pablofrommars/GGNet
/plugin install ggnet@ggnet
```

**Claude Code**, from a local checkout (e.g. a repo that vendors GGNet):

```
/plugin marketplace add path/to/GGNet
/plugin install ggnet@ggnet
```

**Codex** consumes the same skill via the [`.codex-plugin/plugin.json`](.codex-plugin/plugin.json) manifest; the agent-neutral manifest at the root ([plugin.json](plugin.json)) follows the [agentskills.io](https://agentskills.io) layout for everything else.

Two pieces of the skill are live tooling and need the GGNet source tree present (a clone or a vendored copy — not just the installed skill): `scripts/validate.cs` compiles snippets against the in-repo projects, and the MCP server is registered with

```
claude mcp add ggnet -- dotnet run --project path/to/GGNet/src/GGNet.Mcp
```

### Examples Gallery

| | | |
|-|-|-|
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/scatterplot.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/bubbleplot.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/barchart.png)
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/candlestick.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/linechart.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/areachart.png)
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/barplot.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/stacked.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/hbarplot.png)
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/lolipop.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/errorbar.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/violin.png)
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/hex.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/ridgeline.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/choropleth.png)
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/CFR.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/abline.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/bubblemap.png)
