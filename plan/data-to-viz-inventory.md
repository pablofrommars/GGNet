# data-to-viz.com — exhaustive rule inventory

*Crawled 2026-07-07: 38 chart pages + 30 caveat pages via 7 parallel extraction agents (rules/thresholds/remedies only — definitions and prose excluded as learned consensus). Coverage: 66/68 (site 404s: graph/boxplot, graph/pie, graph/spider — content lives in their caveat twins; caveat/overplotting exceeded fetch size, remedies recovered from referencing pages; caveat/small_multiple is an unfinished stub). Numeric thresholds marked ≈ were paraphrased by the extraction model, not verbatim site quotes — treat as our edge to place. Provenance: guidance by Yan Holtz & Conor Healy; paraphrased rules only, no copied prose.*

Classification = bake target. Every rule lands in exactly one bucket.

## A. Already enforced — no work, worth knowing

| Site rule | Where we enforce it |
|---|---|
| Never map raw counts on a choropleth — normalize | `metric_type: count` blocks choropleth (config constraint) |
| Pie only for 2–3 categories with a dominant value; prefer bars | D11: pie/doughnut out by design, alternatives with reasons |
| Stacked area unreadable beyond few series | `max_num_series: 3` + structural escape to facets |
| Never connect unordered points | `line`/`area` leaves require `ordered_num: true` — structurally unreachable otherwise |
| Bubble size must encode **area**, not radius | `Scales/Size.cs` interpolates on `Sqrt(value)` — area-proportional by construction |
| Multiple values per group → boxplot/violin over bar+error-bar | ranking already places boxplot/violin above the bridged barplot (observed in evals) |
| Always show a size legend on bubbles | `Scale_Size_Continuous(guide: true)` is the default |
| 3D charts → never | GGNet has no 3D surface; nothing to refuse |

## B. Bake now — conditional `caveat_rules` (engine v6: generalize `shape_caveat_rules` to arbitrary `when` conditions)

| # | Rule | Condition → applies to | Payload | Status |
|---|---|---|---|---|
| B1 | Simpson's paradox | leaf axis defs already steer known-subgroup queries to `grouped_scatter`; the risky case is **unknown** subgroups → `when {cat_vars: null}` → scatter | caveat naming grouped-scatter + Simpson | ✅ baked (null-as-unknown semantics) |
| B2 | Overplotting at high n | `sample_size > 1000` (our edge) → scatter, grouped_scatter, bubble | caveat: opacity → `hexbin` recipe → facet → sample | ✅ baked |
| B3 | Spaghetti | `num_series > 4` (≈) → line, connected_scatter | caveat: highlight-one or `small-multiples` | ✅ baked |
| B4 | Too many overlaid distributions | — | structurally enforced: histogram/density declare `cat_vars: "0"`, grouped queries can't reach them (moved to bucket A) | ✅ nothing to do |
| B5 | Error-bar honesty | `stat_bridge` fires → bridged results | caveat carried in the bridge's config block (hide shape/n/multimodality; disclose SD/SE/CI) | ✅ baked |
| B6 | Ridgeline group floor | `cardinality: low_2_7` → ridgeline | caveat: below ~6 groups prefer violin/boxplot | ✅ baked |

All baked 2026-07-07 (schema v6, `caveat_rules` generalization); each has an eval case incl. a negative (known-no-subgroups stays quiet).

## C. Bake now — static leaf `caveats` entries (engine already surfaces these verbatim)

- `barplot`, `lollipop`: "no natural category order → sort by value; never alphabetical (temporal data stays chronological)"
- `lollipop`: "preferred over bars when many values are similar (less ink, less moiré); horizontal for long labels"
- `radar`: "normalize all dimensions to a common scale first; ≤ ~3 overlaid series — facet beyond that; radial axis starts at zero"
- `heatmap`: "normalize per column when scales differ; reorder rows/columns (clustering) — unordered heatmaps hide the pattern"
- `correlogram`: "beyond ~10 variables it stops reading; exploratory tool, not a presentation chart"
- `stacked_barplot`, `stacked_area`: "only the baseline series reads accurately; keep layer order constant; put the most important/stable series at the bottom"
- `histogram`: "bin count changes the story — try several (there is no single correct bin size)"
- `density`: "bandwidth changes the story — try several"
- `connected_scatter`: "time not flowing left-to-right → add arrows/date labels or the trajectory misreads"

## D. Bake now — skill-doc notes (no engine surface)

- **chart-selection.md**: zero-baseline rule pair — *bars always start at zero (length encodes value); lines/areas may truncate deliberately — label the cut, never truncate to exaggerate.* Mental-arithmetic rule: *plot the derived quantity (difference/ratio) directly rather than making readers compute it — a `Stat`-style prep step.*
- **theming.md**: color must communicate — groups, highlight, or gradient; otherwise one color (legend-less single-series is correct, our default). Item→color consistency across a report's charts — exactly what `Palettes.Discrete<TKey,string>` pins and CSS custom properties (`var(--color-x)`) carry across plots; say so.
- **geoms.md / examples**: long category labels → `.Flip()` (horizontal bars/lollipops) rather than rotated labels; annotation doctrine — explanatory charts highlight the one series the point is about (accent + gray context), print key values with `Geom_Text`.
- **common-mistakes.md**: dual y-axis request → refuse (plot one variable against the other, or index both — never two axes); grouped bars must visibly group (within-gap < between-gap — `Geom_Bar` dodge handles this, don't fake it with adjacent categories).

## E. Phase 3 spec enrichment (recorded into PLAN Phase 3 now, built later)

- `suggest_bins`: bin/bandwidth *sensitivity* is the point — recommend a set to try (FD + Sturges + one coarser), not a single number; log/sqrt pre-transform for skewed data before binning (confirms the log-space-binning note).
- `suggest_density_strategy`: the overplotting remedies ladder, ordered — size/opacity → sampling → hexbin/2d-density → facet; the site confirms the ladder shape.
- `profile_data`: **comb-pattern detection** (spike-gap-spike histogram = rounding artifact → data-quality flag, new signal); similar-heights detection (drives lollipop-over-bars advice).
- `suggest_comparison`: dual-axis refusal now doubly sourced (connectedscatter + line pages: "never dual Y — perceived relationship is an artifact of axis ranges").
- New candidate (small, maybe fold into `suggest_bins`): **aspect-ratio banking to ~45°** — width:height from average segment slope; deterministic, layout-level.
- `suggest_annotations`: spaghetti highlight rule (featured series thicker + accent, context gray) as an output mode.

## F. Not applicable — unsupported leaves (rules recorded only as alternative-notes)

Venn >3 sets → UpSet; treemap ≤2–3 annotated levels; sunburst outer-ring distortion; network hairball/layout-algorithm rules; sankey/chord/arc node-ordering rules; circular barplot inner-radius > ½ total and ~40+-levels-only rule; wordcloud "a pitfall on its own" (already ❌ → barplot). If any of these geoms ever ship, this section is their day-one caveat set.

## Post-bake audit (2026-07-07)

A re-walk of all seven extraction batches against the baked state found six rules the synthesis dropped — all closed same day as additional leaf caveats: boxplot/violin honesty (annotate n, overlay raw points — the leaves we rank *first* for raw data had no caveats at all), order-by-summary-stat extended to violin/ridgeline, sparse-line point markers, heatmap print-values-when-precise, parts-sum-to-whole on the part-to-whole leaves, bubble most-important-vars-on-axes. Plus one bucket-A addition (size legend by default). Remaining consciously unbaked: declutter/attention-span prose (consensus), per-page composition tips for composable recipes (example-note material, marginal), the error-bar n<30 threshold (folded conceptually into the boxplot caveat).

## Waves (on go)

1. **Wave 1 — engine v6**: generalize to `caveat_rules` (`when` on any axis + numeric thresholds), add B1–B6, evals for each, schema_version 6, docs regenerate via drift gate.
2. **Wave 2 — config + docs**: section C leaf caveats, section D doc notes (drift test extended where applicable).
3. **Wave 3 — PLAN Phase 3 edits**: section E spec lines (do now — it's a plan edit, not code).
