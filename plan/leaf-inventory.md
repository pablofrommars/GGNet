# Chart-selection leaves × GGNet capability inventory

*2026-07-06. Maps all 47 leaves in `decision-tree/chart_selection.json` to what the repo can render today. This is the working input for PLAN.md step 1.4 (the `ggnet` annotation block) and step 1.2 (coverage matrix). Three states:*

- **supported** — a direct geom/stat recipe exists.
- **composable** — GGNet draws it, but the caller computes layout or statistics the library deliberately doesn't own (stats run over sources; layout algorithms are out of scope).
- **unsupported** — not renderable; nearest supported alternatives listed. Demand-driven geoms (ROADMAP backlog) noted.

*Gallery = existing byte-pinned entry in `tests/GGNet.Headless.Tests/Gallery/`; "gap" = new entry needed (PLAN 1.2).*

## Supported (24)

| Leaf | Recipe | Gallery |
|---|---|---|
| barplot | `Geom_Bar` | Bar, BarFlipped |
| grouped_barplot | `Geom_Bar(fillBy, position: Dodge)` | BarFlippedDodged |
| stacked_barplot | `Geom_Bar(fillBy, position: Stack)` | gap |
| lollipop | `Geom_Segment` + `Geom_Point` | gap |
| dot_plot | `Geom_Point` on discrete axis (flipped) | gap |
| dumbbell | `Geom_Segment` + 2× `Geom_Point` | gap |
| small_multiples | any geom + `Facet_Wrap` / `Facet_Grid` | HistogramFaceted |
| radar | `Geom_Radar` | Radar |
| heatmap | `Geom_Tile(fillBy)` + `Scale_Fill_Continuous` | Tile |
| scatter | `Geom_Point` | Point |
| grouped_scatter | `Geom_Point(colorBy)` + `Scale_Color_Discrete` | gap (Legend variants exercise the scale) |
| bubble | `Geom_Point(sizeBy)` + `Scale_Size_Continuous` | gap |
| histogram | `Stat.Bin` + `Geom_Bar` | Histogram |
| density | `Stat.Density` + `Geom_Area`/`Geom_Line` | DensityArea |
| boxplot | `Geom_Boxplot` | Boxplot |
| violin | `Geom_Violin` (raw or via `Stat.Density`) | Violin, ViolinFromDensity |
| ridgeline | `Geom_RidgeLine` | RidgeLine |
| line | `Geom_Line` | Line |
| area | `Geom_Area` | Area |
| stacked_area | `Geom_Area(fillBy, position: Stack)` | gap |
| connected_scatter | `Geom_Line` + `Geom_Point` | gap |
| slope | `Geom_Segment`/`Geom_Line` between two ordinals + `Geom_Text` | gap |
| choropleth | `Geom_Map(fillBy)` | Map |
| proportional_symbol_map | `Geom_Map` + `Geom_Point(sizeBy)` (the README bubblemap) | gap |

## Composable — caller computes, GGNet draws (4)

| Leaf | How | Caveat |
|---|---|---|
| density_2d | `Geom_Hex(dx, dy)` hexbin variant | true contour densities not available; hex binning computed by caller | 
| correlogram | `Geom_Tile(fillBy)` over a pairwise matrix | correlation stat computed by caller (no `Stat.Cor`) |
| calendar_heatmap | `Geom_Tile` with week/weekday coordinates | calendar layout computed by caller |
| waffle | `Geom_Tile` over a computed grid | unit-grid layout computed by caller |

## Unsupported (19)

| Leaf | Nearest supported alternatives | Note |
|---|---|---|
| pie | barplot, waffle (composable) | arc geometry is ROADMAP backlog ("trigger: an actual chart need") |
| doughnut | barplot, waffle (composable) | same backlog slot as pie |
| sunburst | stacked_barplot, small_multiples | arc + hierarchy layout |
| treemap | barplot, heatmap | hierarchy layout algorithm out of scope |
| circular_packing | bubble | packing layout out of scope |
| dendrogram | — (report unsupported) | hierarchy layout; precomputed segments possible but not a recipe |
| beeswarm | violin, boxplot | no swarm layout / jitter position |
| parallel_plot | slope, small_multiples | multi-axis layout not modeled |
| pca | scatter (after external PCA) | analysis technique, not a chart; render components as scatter |
| wordcloud | barplot | text layout algorithm out of scope |
| table | — (report unsupported) | not a chart; out of rendering scope |
| stream_graph | stacked_area | no wiggle/symmetric baseline |
| network | — (report unsupported) | no force layout; precomputed Segment+Point possible but not a recipe |
| chord | — (report unsupported) | arc + ribbon geometry |
| arc | — (report unsupported) | curved links unavailable |
| sankey | stacked_barplot | flow ribbons unavailable |
| flow_map | proportional_symbol_map | flow lines on maps unavailable |
| isometric_cutaway | — (report unsupported) | illustration, not data graphics |
| 3d_globe | choropleth, proportional_symbol_map | no 3D projection |

## Grouping mechanisms — orthogonal to the leaf (input for the decision tree)

GGNet expresses *data groupings* through three orthogonal mechanisms, and the selector's shape axes (`cat_structure`, `num_series`, `cardinality`) map onto them directly:

| Grouping pressure | GGNet mechanism | Selector signal |
|---|---|---|
| Few series, one panel | aesthetic mapping — `colorBy`/`fillBy`/`lineTypeBy` + `Scale_*_Discrete` (trains the legend) | `cat_structure: subgroup`, low `num_series` |
| Subgroups side-by-side or summed | `position: Dodge` / `Stack` | `cat_structure: subgroup` on bar/area families |
| Too many series for one panel | `Facet_Wrap`/`Facet_Grid` (`freeX`/`freeY`), grouped stats (`groupBy:` + facet on the same key) | constraint overflow (e.g. stacked area ≤3 series), `cardinality: high` |

Consequence for the `ggnet` annotation (PLAN 1.4): recipes are not only leaf-level. The block should carry an optional `grouping` hint so the engine can answer constraint overflows *structurally* instead of just relaxing — e.g. stacked_area with >3 series → same geom + `Facet_Wrap` (the config already encodes `small_multiples` as the escape hatch; GGNet makes that escape a one-call transformation of the *same* recipe rather than a different chart). The grouping ladder (constant → aesthetic mapping → position adjustment → facet) also becomes a section of `reference/chart-selection.md`.
