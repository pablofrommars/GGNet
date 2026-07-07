# Chart selection — data shape → GGNet recipe

Distilled from `src/GGNet.ChartSelection/chart_selection.json` (schema v5, the single source of truth — engine: `GGNet.ChartSelection.Selector`, behavior-pinned by `tests/GGNet.Evals`). **Prefer the `ggnet` MCP server's `select_chart` when registered** — it serves this deterministically, with measured profiling; this file is the equivalent manual fallback. Recipe names refer to files in [../examples/](../examples/).

## 1. Resolve intent → functions

One or more of: `comparison`, `correlation`, `distribution`, `part_to_whole`, `trend_over_time`. Natural phrasings map onto these ("evolution"/"over time" → trend_over_time; "share of"/"makeup" → part_to_whole; "related"/"vs" → correlation).

## 2. Profile the data → shape fields

Unknown = leave unset, never guess. Key extraction rules:

| Signal | Mapping |
|---|---|
| Date/timestamp column used as axis | `ordered_num: true` (do NOT count it as a categorical) |
| Month/weekday as grouping, not sequence | categorical; structure per nesting (`flat`/`subgroup`/`nested`) |
| One row per group vs raw observations | `obs_per_group: one` vs `many` |
| Distinct values of the main categorical | 2–7 low · 8–20 medium · >20 high |
| Geo codes / lat-lon / regions as unit | spatial (region→choropleth family, points→symbol maps) |
| Metric is a count of things | counts block choropleth (use rates there) |

**Data gate**: completeness < 0.8 or n < 5 → don't chart; report the data-quality problem.

## 3. Pick the leaf → GGNet recipe or alternative

`recipe` = verified example file. ❌ = GGNet cannot render it → use the listed supported alternatives (never silently draw something else — say why).

| Chart | Functions | GGNet |
|---|---|---|
| `barplot` | comparison, part_to_whole | `barplot` |
| `lollipop` | comparison | `lollipop` |
| `dot_plot` | comparison | Geom_Point on a discrete axis (flipped); variant of the barplot/lollipop recipes |
| `dumbbell` | comparison, trend_over_time | Geom_Segment + two Geom_Point layers; variant of the lollipop/slope recipes |
| `table` | comparison | ❌ not a chart; out of rendering scope |
| `grouped_barplot` | comparison | `grouped-barplot` |
| `stacked_barplot` | comparison, part_to_whole | `stacked-barplot` |
| `small_multiples` | comparison, trend_over_time, distribution | `small-multiples` |
| `radar` | comparison | `radar` |
| `parallel_plot` | comparison, correlation | ❌ → `slope`, `small_multiples` (multi-axis layout not modeled) |
| `heatmap` | comparison, correlation | `heatmap` |
| `wordcloud` | comparison | ❌ → `barplot` (text layout out of scope) |
| `scatter` | correlation | `scatter` |
| `grouped_scatter` | correlation | `grouped-scatter` |
| `bubble` | correlation | `bubble` |
| `density_2d` | correlation, distribution | `hexbin` — hex binning computed by the caller |
| `correlogram` | correlation | `correlogram` — correlation matrix computed by the caller |
| `pca` | correlation | ❌ → `scatter` after external PCA (analysis technique, not a chart) |
| `histogram` | distribution | `histogram` |
| `density` | distribution | `density` |
| `boxplot` | distribution, comparison | `boxplot` |
| `violin` | distribution, comparison | `violin` |
| `ridgeline` | distribution, comparison | `ridgeline` |
| `beeswarm` | distribution | ❌ → `violin`, `boxplot` (no swarm layout) |
| `pie` | part_to_whole | ❌ by design → `barplot`, or `waffle` for part-to-whole (angle comparison is perceptually inferior to aligned position — Cleveland–McGill) |
| `doughnut` | part_to_whole | ❌ by design → `barplot`, or `waffle` (arc length reads no better than angle) |
| `waffle` | part_to_whole | composable — unit grid computed by the caller, drawn with Geom_Tile |
| `treemap` | part_to_whole | ❌ → `barplot`, `heatmap` (hierarchy layout out of scope) |
| `sunburst` | part_to_whole | ❌ → `stacked-barplot`, `small-multiples` |
| `circular_packing` | part_to_whole | ❌ → `bubble` |
| `dendrogram` | part_to_whole | ❌ (hierarchy layout out of scope) |
| `line` | trend_over_time | `line` |
| `area` | trend_over_time | `area` |
| `stacked_area` | part_to_whole, trend_over_time (≤3 series) | `stacked-area` |
| `stream_graph` | trend_over_time, part_to_whole | ❌ → `stacked-area` (no wiggle baseline) |
| `connected_scatter` | trend_over_time, correlation | `connected-scatter` |
| `slope` | trend_over_time, comparison | `slope` |
| `calendar_heatmap` | trend_over_time | `calendar-heatmap` — calendar layout computed by the caller |
| `network` | correlation | ❌ (no force layout) |
| `chord` | correlation, part_to_whole | ❌ (arc + ribbon unavailable) |
| `arc` | correlation | ❌ (curved links unavailable) |
| `sankey` | part_to_whole, trend_over_time | ❌ → `stacked-barplot` (flow ribbons unavailable) |
| `choropleth` | comparison (rates, not counts) | `choropleth` |
| `proportional_symbol_map` | comparison | `bubble-map` |
| `flow_map` | trend_over_time, comparison | ❌ → `bubble-map` (flow lines unavailable) |
| `isometric_cutaway` | part_to_whole | ❌ (illustration, not data graphics) |
| `3d_globe` | comparison | ❌ → `choropleth`, `bubble-map` (no 3D projection) |

**If the user insists on a ❌ chart:** it is never drawn — GGNet cannot render it, and for pie/doughnut the exclusion is doctrine, not a gap (humans judge aligned position far more accurately than angles or areas; bars beat pies at the pie's own game, and a waffle covers the part-to-whole gestalt by unit counting). Restate the reason once, offer the alternatives, stop. For *renderable* charts that carry caveats (bimodal boxplot, >7-slice anything, skewed axes): state the caveat once, then the user's choice wins.

## 4. Stat bridges — raw data reaching aggregated charts

Barplot-family charts expect **one row per group**. With raw observations, do not pre-aggregate in LINQ — bridge with a stat (recomputed per render):

| Raw shape | Bridge | Then |
|---|---|---|
| many rows per category, want frequencies | `Stat.Count(src, key)` | `barplot`/`lollipop` recipe over `(Key, N)` |
| many observations per group, want level + spread | `Stat.Summary(src, x, y)` | `summary-errorbar` recipe (bars/points + error bars) — a peer of boxplot/violin |

## 5. Grouping ladder — structural answers to overflow

constant → `colorBy`/`fillBy` + discrete scale → `position: Dodge|Stack` → `Facet_Wrap`. Constraint overflow keeps the recipe and facets it: stacked area with >3 series = **same recipe + `Facet_Wrap` on the series key** (see `small-multiples`); dodging past a handful of series → facet instead.

## 6. Shape transforms — executable, not advisory

| Distribution shape | Do |
|---|---|
| skewed | `Scale_Y_Log10()` — or `Scale_X_Log10()`, per the skewed axis |
| power_law | `Scale_X_Log10()` + `Scale_Y_Log10()` |
| bimodal | structural, not a transform: prefer `violin`/`ridgeline` over summaries |
| long-tailed counts | consider `Scale_Y_Sqrt()` |

Transforms change tick spacing, not tick values — pair with an honest `formatter:`.

**Histograms of multi-decade data** (e.g. latencies spanning ms→s): bin in log space instead of transforming the axis — pass `Math.Log10(value)` as the `Stat.Bin` selector so every decade gets equal resolution and equal-width bars. Keep the axis honest: say log₁₀ in the axis label, drop `Geom_VLine` marks at real values (100 ms, 1 s, …), inverse-transform in tooltips, and guard `Log10` against zero/negative inputs.
