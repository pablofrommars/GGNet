# GGNet — Interactivity Feature Inventory (Step 1)

*Compiled 2026-07-08. Step 1 of the interactivity session (inventory → options → blast radius). External-library research only — no GGNet source was read for this step; feasibility is an **annotation**, not the sort key, and gets grounded in the codebase in Step 2. Feature taxonomy verified against current docs for Plotly.js, Apache ECharts, Highcharts/Stock, Vega-Lite, Observable Plot, uPlot, D3 (`d3-zoom`/`d3-brush`), Recharts, and ApexCharts (sources at the foot). GGNet's existing interaction surface is taken from `ROADMAP.md` and `plan/inventory.md`: per-mark `tooltip`/`onclick`/`onmouseover`/`onmouseout` and `Panel` `onClick` are shipped; legends render as static guides.*

## How to read this

- **Sort axis = user value** for the two stated targets, **data exploration** and **dashboards** — decreasing down the list. A feature that is expensive to build but highly wanted still ranks high; feasibility does **not** move it.
- **Feasibility is annotation only.** The last column records where each feature sits under the roadmap's Blazor-Server tiers, so Step 2 has a starting map — it did not influence the ordering.
  - **Tier 0** — no JS; works with discrete circuit events (`click`, `dblclick`, `wheel`, per-element `mouseover`/`mouseout`). Data-snapped crosshair via invisible hit-strips lives here.
  - **Tier 2** — needs continuous `mousemove` tracking (drag, freeform, pixel-glued); this is the JS boundary. (Tier 1 = ~15 lines of measurement JS for responsive sizing; no interaction feature is *defined by* Tier 1.)
  - **Shipped** — already in GGNet's DSL today.
- Bands **A / B / C** group by value tier; ranks are linear within the whole list.

---

## Band A — core reading and the zoom loop

The interactions nearly every exploratory tool and dashboard ships. Losing any of them is felt immediately.

| # | Feature | What it does · exemplars | Value for exploration + dashboards | Feasibility (annotation only) |
|---|---|---|---|---|
| 1 | **Nearest-point hover tooltip** | Reveal exact values for the mark under the cursor. Plotly `hovermode:"closest"`, Plot `tip`+`pointer`, ECharts item tooltip, Recharts `Tooltip`. | The single most-used interaction — without it a chart is a picture. Baseline for both targets. | **Shipped** (per-mark `tooltip`, discrete `mouseover`; Tier 0). |
| 2 | **Axis/shared tooltip + crosshair** | Guide lines that track the cursor and read out all series' values at one x (or y). Plotly `x unified`+spikelines, ECharts `axisPointer`, Highcharts Stock crosshair (on by default), Plot `crosshair`, uPlot cursor. | The reading primitive for multi-series dashboards and time series. Highest-value gap GGNet has. | Not shipped. **Tier 0** — data-snapped via invisible hit-strips + coordinate readout (the roadmap's named seam). |
| 3 | **Zoom to region (box / rubber-band)** | Drag a rectangle → zoom the axes to it. Plotly `dragmode:"zoom"`, Highcharts `zoomType`, ECharts `dataZoom` select, `d3-brush`→zoom, ApexCharts selection zoom. | *The* exploratory "look closer" gesture; the default drag in most tools. | Not shipped. **Tier 2** — rectangle drag needs `mousemove`. |
| 4 | **Wheel zoom** | Scroll to zoom in/out around the cursor. Plotly `scrollZoom`, ECharts `dataZoom` inside, uPlot wheel, Vega-Lite `zoom`, `d3-zoom`. | Fast continuous "look closer"; this is the fermentation-dashboard case the roadmap calls out. | Not shipped. **Tier 0** — `wheel` is a discrete event. |
| 5 | **Reset / autoscale** | Return to the full data extent. Plotly double-click + `autoScale`/`resetScale`, Highcharts reset button, ECharts `restore`. | Mandatory companion to any zoom — the view is a trap without it. | Not shipped. **Tier 0** — `dblclick` / button. |
| 6 | **Drag pan** | Click-drag to translate the current view. Plotly `dragmode:"pan"`, ECharts `dataZoom` translate, Vega-Lite interval translate, `d3-zoom`. | Pairs with zoom to move around a zoomed view; high value once zoomed in. | Not shipped. **Tier 2** — translate needs `mousemove`. |

---

## Band B — selection, overview, and dashboard navigation

High value, and often the features that *define* a dashboard rather than merely decorate it. Selection (7–9) is one machine seen from three angles.

| # | Feature | What it does · exemplars | Value for exploration + dashboards | Feasibility (annotation only) |
|---|---|---|---|---|
| 7 | **Range slider / navigator (overview + detail)** | A mini overview chart with a draggable window driving the main view. Highcharts `navigator`, Plotly `rangeslider`, Recharts `Brush`, ECharts `dataZoom` slider, ApexCharts brush chart. | The time-series dashboard staple; overview+detail is a first-class exploration pattern. | Not shipped. Static overview render is Tier 0; the **handle drag is Tier 2**. |
| 8 | **Linked brushing / crossfilter** | A selection in one view filters or highlights the others. Vega-Lite `selection`→filter (its headline feature), D3 brushing-and-linking, ECharts `connect`, ApexCharts `group`, Recharts `syncId`. | The apex of exploratory dashboards — coordinated multiple views. Highest ceiling on this list. | Not shipped. Selection is **Tier 2** (drag); the cross-panel wiring is a GGNet composition concern (Step 2/3). |
| 9 | **Brush / interval selection (emit a range)** | Drag to select a 1D/2D region and expose it as a value. Vega-Lite interval, `d3-brush`, Plotly box/lasso `select` events. | The input to #8 and to "zoom-or-filter to selection." | Not shipped. **Tier 2** — region drag. |
| 10 | **Legend interactivity** | Click to toggle/isolate a series; hover to highlight. ECharts, Highcharts, Plotly, Recharts all ship it. | Manage which series show — ubiquitous, high-frequency, cheap. Dashboards lean on it constantly. | Not shipped (legend is a static guide today). **Tier 0** — click-toggle is discrete. |
| 11 | **Data-point click / drill** | Click a mark → select, navigate, or descend a hierarchy. Highcharts `drilldown`, Plotly/ECharts click events. | Dashboards act on clicks (navigate, drill to detail). | **Partly shipped** — per-mark `onclick` + `Panel` `onClick` exist (Tier 0). Hierarchical *drilldown as a pattern* is not built. |
| 12 | **Range-selector preset buttons** | Preset zoom windows (1D / 1W / 1M / YTD / All). Highcharts `rangeSelector`, Plotly `rangeselector`. | Fast, discoverable time-window navigation on dashboards. | Not shipped. **Tier 0** — buttons that set `Limits`. |

---

## Band C — targeted, meta, or boundary features

Valuable in specific domains, or utilities/affordances that follow whatever they wrap rather than standing on their own.

| # | Feature | What it does · exemplars | Value for exploration + dashboards | Feasibility (annotation only) |
|---|---|---|---|---|
| 13 | **Lasso / freeform point selection** | Select an arbitrary set of points. Plotly `lasso`, D3. | Scatter / point-cloud exploration; narrower than box-select (#3/#9). | Not shipped. **Tier 2** — freeform drag. |
| 14 | **Hover highlight / focus series** | Emphasize the hovered series, fade the rest. ECharts emphasis/blur, uPlot focus. | Legibility on dense multi-series; a nice-to-have over #2. | Not shipped. Per-series `mouseover` is **Tier 0**, but restyling siblings touches the render path. |
| 15 | **Synchronized cursor / zoom across panels** | Shared x-crosshair and/or zoom over stacked, aligned charts. uPlot sync, Highcharts synchronized, ECharts `connect`. | Multi-panel time-series dashboards (finance especially). | Not shipped. Crosshair-sync **Tier 0**; zoom-sync inherits #3/#4's tier. GGNet `Panel`/`Facet` is the natural host (Step 2/3). |
| 16 | **Coordinate readout** | Cursor position in data units, independent of any mark. Plotly hover coords, Plot crosshair labels. | "What value is here?" for exploration in sparse regions. | Not shipped. **Tier 0** — bundled with the crosshair seam; arguably part of #2. |
| 17 | **Roam (geo / graph pan-zoom)** | Pan/zoom a map or network surface. ECharts `roam`, Plotly geo. | Relevant to `Geom_Map`; narrow otherwise. | Not shipped. **Tier 2** — drag. |
| 18 | **Toolbar / modebar** | The affordance container (mode switch, reset, screenshot). Plotly `modebar`, ECharts `toolbox`. | Discoverability wrapper, not an interaction itself — inherits the tier of whatever it hosts. | Not shipped. **Tier 0** — buttons. |
| 19 | **Save-as-image / data view** | Export the chart, or reveal the table behind it. ECharts `saveAsImage`/`dataView`, Plotly `toImage`. | Dashboard utility. Export overlaps the roadmap's separate **PNG-export** backlog item. | Not shipped. "Data view" not built; PNG export tracked elsewhere. |
| 20 | **Editable annotations / draw-on-chart** | Draw lines, rects, paths on the plot. Plotly drawing `dragmode`s, Highcharts annotations. | Authoring/markup — tangential to exploration-*reading*. | Not shipped. **Tier 2** — drawing is drag. Low priority. |
| 21 | **Real-time / streaming append** | Live data updating the plot. | A dashboard staple, but a data-update concern more than a pointer interaction. Listed at the boundary of "interactivity." | GGNet already re-renders through the circuit (animation / refresh paths). Not a new interaction seam. |

---

## What GGNet already has (so Step 2/3 don't re-inventory it)

- **Hover tooltip** — per-mark `tooltip: Func<T, RenderFragment>` on all data marks and finance geoms (except finance's event-only block; `Geom_Map` has a positioned tooltip).
- **Click** — per-mark `onclick` and `Panel` `onClick` (`Func<…, MouseEventArgs, Task>`).
- **Hover enter/leave** — per-mark `onmouseover` / `onmouseout`.
- These are the discrete-event, Tier-0 primitives. Everything else in the list above is a gap.

## Reading ahead to Step 2

The Band-A/B zoom-pan-select cluster (#3, #4, #6, #7, #9) is **one machine**: an invertible **view window**. Every one of them reduces to "map pixels back to data, set a new axis window, re-render." That is exactly the roadmap's named seam — `Unproject(px, py)` on `ICoordinateSystem`, `Invert` on scales, and a view window as a dynamic `Limits` exempt from `Reset()`. The value ranking above and the tier annotation pull in different directions (e.g. box-zoom is high-value/Tier-2; wheel-zoom is high-value/Tier-0), which is precisely the CSS-transform-vs-Blazor-re-render trade Step 2 has to resolve. The Tier split is not per-feature-random: it tracks **discrete vs. continuous input**, and that is the real design question — not "which features," but "how a view-window update reaches the screen."

### Scope inputs for Step 2 (captured 2026-07-08)

Two constraints added by Pablo after Step 1, both widening the option space:

1. **The shipped event surface is refactorable, not frozen.** Step 2 may propose reworking the existing `tooltip` / `onclick` / `onmouseover` / `onmouseout` / `Panel.onClick` plumbing, not only adding interactions beside it. The "already shipped" markers above are the *current* state, not a fixed floor.
2. **An imperative external-control API is in scope.** Some controls are better owned by the host app (a "reset selection" button, a "zoom to" box, a "last 2 weeks" picker) but must drive a public GGNet surface — e.g. `ZoomTo(...)`, `ResetSelection()`, `ShowLast(Period)`. This makes the view window a seam with **two consumers**: in-chart pointer gestures *and* host commands, both mutating the same view-window / selection state. The imperative façade is largely **tier-independent** — a method call is neither Tier 0 nor Tier 2; it is the state model beneath both — so its design is orthogonal to the CSS-transform-vs-re-render question and can be settled on its own terms. Designing this API (shape, ownership of state, how host commands and pointer gestures reconcile) is a Step 2/3 deliverable.

---

## Sources

- Plotly.js — [Configuration options](https://plotly.com/javascript/configuration-options/), [layout reference (`dragmode`, `hovermode`, spikelines)](https://plotly.com/javascript/reference/layout/), [Zoom/Pan/Hover controls](https://plotly.com/chart-studio-help/zoom-pan-hover-controls/)
- Apache ECharts — [Features overview](https://echarts.apache.org/en/feature.html), [dataZoom tutorial](https://echarts.apache.org/handbook/en/how-to/interaction/drag/), [Toolbox component](https://echarts.apache.org/en/option.html)
- Highcharts — [Range selector](https://www.highcharts.com/docs/stock/range-selector), [Navigator](https://www.highcharts.com/docs/stock/navigator), [Tooltip](https://www.highcharts.com/docs/chart-concepts/tooltip), [Synchronized charts demo](https://www.highcharts.com/demo/synchronized-charts/sand-signika)
- Vega-Lite — [Selection parameters](https://vega.github.io/vega-lite/docs/selection.html), [Zooming an interval selection](https://vega.github.io/vega-lite-v4/docs/zoom.html), [Crossfilter example](https://vega.github.io/vega-lite/examples/interactive_crossfilter.html)
- Observable Plot — [Interactions](https://observablehq.com/plot/features/interactions), [Crosshair mark](https://observablehq.com/plot/interactions/crosshair), [Pointer transform](https://observablehq.com/plot/interactions/pointer), [Tip mark](https://observablehq.com/plot/marks/tip)
- uPlot — [Wheel zoom & drag demo](https://leeoniya.github.io/uPlot/demos/zoom-wheel.html), [cursor sync (issue #459)](https://github.com/leeoniya/uPlot/issues/459)
- D3 — [d3-brush](https://d3js.org/d3-brush), [Brush & Zoom (focus+context)](https://gist.github.com/mbostock/34f08d5e11952a80609169b7917d4172)
- Recharts — [Brush API](https://recharts.github.io/en-US/api/Brush/)
- ApexCharts — [Interactivity](https://apexcharts.com/docs/interactivity/), [Zoom](https://apexcharts.com/docs/options/chart/zoom/), [Brush](https://apexcharts.com/docs/options/chart/brush/), [Synchronized charts](https://apexcharts.com/docs/chart-types/synchronized-charts/)
