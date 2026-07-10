# GG.Net Interactivity Inventory

Inventory date: 2026-07-08

Scope: popular JS/TS plotting libraries, sorted by decreasing fit for GG.Net. Fit is judged for data exploration and dashboards, with GG.Net's current SVG/Blazor shape in mind.

## Local Fit Criteria

- GG.Net already emits SVG marks and has a uniform event block for data-mark geoms: `onclick`, `onmouseover`, `onmouseout`, and `tooltip`.
- Panel code already exposes projected coordinates and ranges through `ToX`, `ToY`, `Project`, `XRange`, and `YRange`.
- The roadmap explicitly treats discrete events over the Blazor circuit as acceptable, and high-frequency pointer movement as the JS boundary.
- Interactivity should preserve the current separation where geoms describe data marks and the panel/rendering layer owns viewport behavior.

## Ranked Feature Inventory

| Rank | Feature | Found In | GG.Net fit |
|---:|---|---|---|
| 1 | Data-snapped hover, crosshair, coordinate readout | Observable Plot pointer/crosshair marks; Chart.js nearest/index/x/y interaction modes and pixel-to-data helpers; ECharts axis-triggered tooltip/cross axis pointer. | Best fit. It matches the roadmap's invisible hit-strip idea and can avoid raw `mousemove` by snapping to discrete SVG surfaces or precomputed bands. |
| 2 | Wheel zoom plus double-click/reset | Plotly scroll zoom, reset-scale controls, double-click timing, and zoom events; Vega-Lite interval wheel zoom and default double-click clearing; D3 zoom wheel/pinch/drag with scale extents; Highcharts axis zoom, reset, pinch, and mouse-wheel zoom. | Very high fit. Wheel events are discrete enough for Tier 0 if zoom is represented as scale/view-window state rather than only a visual transform. |
| 3 | Legend item toggling / series visibility | ECharts click-to-show/hide legend categories; Plotly legend interactions; Chart.js and Highcharts legend configuration and events; uPlot series toggles. | High dashboard value, but less direct in GG.Net because legends are aesthetic-scale artifacts, not necessarily series objects. Requires deciding whether toggling hides mapped values, layers, or legend groups. |
| 4 | Shared/comparison tooltip | Plotly closest/compare hover modes; ApexCharts shared/follow-cursor/intersect/fixed tooltips and series highlighting; Chart.js tooltip modes and external HTML tooltip support. | Good fit when snapped to x/category/index. Pixel-following tooltip is lower fit because it wants continuous pointer updates. |
| 5 | Brush/range selection | Vega-Lite point/interval selections; D3 brush; Plotly select/lasso; ECharts brush/dataZoom; ApexCharts selection/zoomed area. | Medium-high fit if implemented as drag preview in JS or commit-on-release. Pure Blazor Server drag is a poor fit. Click, keyboard, or range-select APIs may fit earlier. |
| 6 | Linked charts / synchronized cursors and zoom | uPlot cursor sync; ApexCharts grouped/synchronized charts; Highcharts dashboard synchronization concepts. | Dashboard-relevant, but introduces cross-plot state and registration beyond one `PlotContext`. Likely later than single-plot interactions. |
| 7 | Pan | D3 zoom and Vega interval translate; Highcharts panning; Plotly pan mode. | Useful but lower fit than wheel zoom. Smooth drag-pan is exactly the high-frequency path that belongs behind JS. Button/key pan or wheel-modifier pan could fit earlier. |
| 8 | Toolbar / modebar controls | Plotly modebar for zoom, pan, select, lasso, reset, and image export; ApexCharts and Highcharts toolbar/navigation concepts. | Moderate fit. Useful as discoverability, but GG.Net's first interactivity should probably define behavior and API before adding a visible control surface. |
| 9 | Editable charts / draggable points / annotations | Plotly editable mode; ECharts invisible draggable graphics plus `convertFromPixel`; Highcharts annotations/freeform drawing modules. | Low fit for the current roadmap. This turns GG.Net from exploratory plotting into chart editing and needs continuous client interaction plus mutation semantics. |
| 10 | Accessibility, keyboard, and audio interactions | Highcharts accessibility modules; general event hooks and ARIA/accessibility guidance across libraries. | Important, but not the core dashboard-exploration seam. Should be designed alongside interaction state rather than treated as the first feature slice. |

## Library Notes

- Plotly is the broadest interaction reference: scroll zoom, modebar controls, pan/select/lasso, reset/autoscale, hover closest/compare, editable chart metadata, export, responsive sizing, and zoom events.
- Apache ECharts emphasizes componentized interaction: `dataZoom`, brush, legend selection, tooltip axis pointers, event/action dispatch, and custom invisible graphics for dragging.
- Highcharts is the closest SVG commercial reference: axis zooming, panning, reset buttons, pinch and wheel zoom, rich tooltips, legends, drilldown, annotations, Stock navigator/range selector, and accessibility.
- Chart.js is a useful interaction-mode reference despite being canvas: it clearly separates event lists, nearest/index/x/y modes, click/hover callbacks, tooltip modes, pixel-to-data conversion, and zoom/pan via the zoom plugin.
- Vega-Lite is the strongest declarative selection reference: point and interval selections, projection by encodings/fields, default double-click clearing, selection resolution across facets/views, interval translate, and interval wheel zoom.
- Observable Plot is the closest grammar-of-graphics interaction reference: pointer transforms, one-dimensional pointer modes, crosshair marks, click-to-stick, and multiple coordinated pointer-rendered marks.
- D3 is the low-level gesture reference: `d3-zoom` and `d3-brush` define the vocabulary for scale extents, translate extents, gesture filtering, reset transforms, and combining zoom/drag/brush behavior.
- ApexCharts is a pragmatic dashboard reference: axis zoom via drag/wheel/toolbar, zoomed-area styling, autoscale-y, selection events, grouped charts, shared tooltips, fixed/follow-cursor tooltips, and series highlighting.
- uPlot is the performance-focused time-series reference: cursor sync, focus closest series, live values in legends, zoom with autoscale, and an explicit preference for lean hooks/plugins over built-in broad interaction.

## Sources

- Plotly configuration options: https://plotly.com/javascript/configuration-options/
- Plotly hover text and formatting: https://plotly.com/javascript/hover-text-and-formatting/
- Plotly zoom events: https://plotly.com/javascript/zoom-events/
- Apache ECharts legend: https://echarts.apache.org/handbook/en/concepts/legend/
- Apache ECharts axis and axis pointer examples: https://echarts.apache.org/handbook/en/concepts/axis/
- Apache ECharts drag interaction: https://echarts.apache.org/handbook/en/how-to/interaction/drag/
- Highcharts zooming: https://www.highcharts.com/docs/chart-concepts/zooming
- Highcharts tooltip: https://www.highcharts.com/docs/chart-concepts/tooltip
- Highcharts legend: https://www.highcharts.com/docs/chart-concepts/legend
- Chart.js interactions: https://www.chartjs.org/docs/latest/configuration/interactions.html
- Chart.js tooltip: https://www.chartjs.org/docs/latest/configuration/tooltip.html
- Chart.js zoom plugin: https://www.chartjs.org/chartjs-plugin-zoom/latest/guide/
- Vega-Lite selections: https://vega.github.io/vega-lite/docs/selection.html
- Observable Plot pointer transform: https://observablehq.com/plot/interactions/pointer
- Observable Plot crosshair mark: https://observablehq.com/plot/interactions/crosshair
- D3 zoom: https://d3js.org/d3-zoom
- D3 brush: https://d3js.org/d3-brush
- ApexCharts zoom options: https://apexcharts.com/docs/options/chart/zoom/
- ApexCharts tooltip options: https://apexcharts.com/docs/options/tooltip/
- uPlot README: https://github.com/leeoniya/uPlot
