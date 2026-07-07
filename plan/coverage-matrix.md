# Coverage matrix — examples × leaves × API surface

*Phase 1 step 1.2 deliverable (PLAN.md). Drafted 2026-07-07. Maps the target example set to chart-selection leaves and the API they exercise. Example ids are the stable recipe ids used by the 1.4 `ggnet` annotations and later by the MCP. Gallery = pinned entry in `tests/GGNet.Headless.Tests/Gallery/`; **NEW** = pin to add (each a deliberate, eyeballed decision).*

## Matrix

| Example id | Leaf (selector) | Gallery entry | Geoms / stats / scales exercised |
|---|---|---|---|
| `scatter` | scatter | Point | `Geom_Point` |
| `grouped-scatter` | grouped_scatter | **NEW** GroupedScatter | `Geom_Point(colorBy)` + `Scale_Color_Discrete` |
| `bubble` | bubble | **NEW** Bubble | `Geom_Point(sizeBy)` + `Scale_Size_Continuous` |
| `barplot` | barplot | Bar, BarFlipped | `Geom_Bar`, `Flip` |
| `grouped-barplot` | grouped_barplot | BarFlippedDodged | `Geom_Bar(fillBy, position: Dodge)` + `Scale_Fill_Discrete` |
| `stacked-barplot` | stacked_barplot | **NEW** BarStacked | `Geom_Bar(fillBy)` (Stack is the default) |
| `lollipop` | lollipop | **NEW** Lollipop | `Geom_Segment` + `Geom_Point` layering |
| `dot-plot` | dot_plot | **NEW** DotPlot | `Geom_Point` on discrete axis, `Flip` |
| `dumbbell` | dumbbell | **NEW** Dumbbell | `Geom_Segment` + 2 point layers |
| `slope` | slope | **NEW** Slope | `Geom_Segment` + `Geom_Text`, discrete x |
| `line` | line | Line | `Geom_Line` |
| `connected-scatter` | connected_scatter | **NEW** ConnectedScatter | `Geom_Line` + `Geom_Point` on shared source |
| `area` | area | Area | `Geom_Area` |
| `stacked-area` | stacked_area | **NEW** AreaStacked | `Geom_Area(fillBy, position: Stack)` |
| `histogram` | histogram | Histogram | `Stat.Bin` + `Geom_Bar` |
| `density` | density | DensityArea | `Stat.Density` + `Geom_Area` |
| `boxplot` | boxplot | Boxplot | `Geom_Boxplot` |
| `violin` | violin | Violin, ViolinFromDensity | `Geom_Violin`, `Stat.Density(groupBy)` |
| `ridgeline` | ridgeline | RidgeLine | `Geom_RidgeLine` |
| `summary-errorbar` | (stat bridge: barplot/dot_plot ← raw) | SummaryErrorBar | `Stat.Summary` + `Geom_ErrorBar` |
| `heatmap` | heatmap | Tile | `Geom_Tile(fillBy)` + `Scale_Fill_Continuous` |
| `calendar-heatmap` | calendar_heatmap (composable) | **NEW** CalendarHeatmap | `Geom_Tile`, caller-computed week/day coords |
| `correlogram` | correlogram (composable) | **NEW** Correlogram | `Geom_Tile(fillBy)`, caller-computed matrix |
| `hexbin` | density_2d (composable) | Hex | `Geom_Hex` |
| `waffle` | waffle (composable) | **NEW** Waffle | `Geom_Tile`, caller-computed unit grid |
| `small-multiples` | small_multiples | HistogramFaceted | `Stat.Bin(groupBy)` + `Facet_Wrap` (key stated twice) |
| `radar` | radar | Radar | `Geom_Radar` (implies polar) |
| `choropleth` | choropleth | Map | `Geom_Map(fillBy)` + `Scale_Longitude/Latitude` |
| `bubble-map` | proportional_symbol_map | **NEW** BubbleMap | `Geom_Map` + `Geom_Point(sizeBy)` |
| `candlestick` | (no leaf — GGNet strength) | Candlestick | `Geom_Candlestick` (+`Volume`, `OHLC` pinned) |
| `annotated` | (cross-cutting) | HLine, VLine, ABLine | annotation geoms, `LineType.Dashed` |
| `themed` | (cross-cutting) | SelfContained | `Theme` + `--ggnet-*` vars, self-contained export |

Not exercised by any example (accepted, documented in reference only): `Geom_Ribbon` (pinned: Ribbon), `Geom_Text` standalone (pinned: Text), `Geom_OHLC`/`Geom_Volume` (pinned), `SparkLine` component, `Panel` sub-panel composition, `Coord_Polar` directly, NodaTime scale variants beyond what `line`/`candlestick` use, `Scale_*_Identity`, `Log10`/`Sqrt` (documented in scales.md + chart-selection transforms; a transform variant can ride on `histogram` if wanted).

## New pins required, by priority

- **Tier A — config-critical, common charts (6):** BarStacked, AreaStacked, Bubble, GroupedScatter, Lollipop, ConnectedScatter. Each unlocks a supported leaf that currently has no pinned recipe.
- **Tier B — composable + map (4):** CalendarHeatmap, Correlogram, BubbleMap, Slope. Higher authoring effort (layout math in the example), high selector value.
- **Tier C — near-duplicates of existing recipes (3):** DotPlot, Dumbbell, Waffle. Variants of Point/Segment/Tile patterns; could be reference-file snippets instead of pins.

Verification per PLAN 1.2: every supported leaf maps to ≥1 example (Tier C leaves fall back to sibling recipes if skipped); every geom and stat appears in ≥1 pinned entry already (see "not exercised" list — all have existing pins).
