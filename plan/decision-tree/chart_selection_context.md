# Chart Selection System — LLM Context (v4)

**Purpose:** Deterministic chart recommendation from data shape + editorial intent.
**Architecture change in v4:** the protocol is now **code** (`engine.py`); the JSON (`chart_selection.json`) is pure config. The LLM's only jobs: (a) build the query object from data + user intent, (b) present results. It never executes matching logic itself.

## Files

| File | Role |
|---|---|
| `chart_selection.json` | Config: axes, 47 leaves, constraints, caveat rules. Single source of truth. |
| `engine.py` | Protocol: normalize → gate → filter → constrain → relax → rank. `select(cfg, query)` → results or error. |
| `eval.py` | 20 realistic queries + leaf-reachability check. Run after every config change. |

## v4 changes (from v3 review)

1. **Missing leaves added:** `dot_plot`, `dumbbell`, `small_multiples`, `table`, `calendar_heatmap`. `small_multiples` is the escape hatch when a single chart is overloaded (e.g., >3 stacked series).
2. **Validated:** 20/20 eval cases pass; every leaf proven reachable. Eval exposed and fixed a leakage bug (see 3).
3. **Protocol as code:** prose steps replaced by `engine.py`. New **gating rule** found via eval: leaves declaring `is_spatial`/`is_relational`/`physical_subject: true` require the query to affirm it — unknown no longer opens the door. Without this, `3d_globe` leaked into ordinary comparison queries.
4. **Multi-function queries:** `query.functions` is a list. Leaf matches on any overlap; leaves covering more requested functions rank first (e.g., `comparison`+`trend_over_time` → `slope`).

## Query object (built by the LLM)

```json
{
  "functions": ["comparison"],          // REQUIRED, 1+ of: comparison|correlation|distribution|part_to_whole|trend_over_time (aliases resolved)
  "num_vars": "1",                      // "0"|"1"|"2"|"3"|"many"
  "cat_vars": "1",                      // "0"|"1"|"2"|"many"
  "cat_structure": "flat",              // none|flat|subgroup|nested
  "obs_per_group": "one",               // one|many
  "ordered_num": false,                 // a numeric var is time/sequence
  "is_relational": null,                // entity links present
  "is_spatial": null,                   // geographic dimension
  "physical_subject": null,             // physical object/structure
  "spatial_grain": null,                // region|point|flow
  "cardinality": "low_2_7",             // low_2_7|medium_8_20|high_gt_20
  "metric_type": null,                  // count|rate|amount|score
  "num_series": null,                   // integer
  "sample_size": null,                  // integer
  "completeness": null,                 // 0-1
  "distribution_shape": null            // normal|skewed|bimodal|power_law|uniform|unknown
}
```
All fields except `functions` nullable; **null = unknown, never disqualifies** (produces `unverified:` caveats instead).

## Matching semantics (implemented, not advisory)

- Absent leaf field = wildcard.
- **Gating exception:** leaf `is_spatial/is_relational/physical_subject: true` requires query `true`.
- `constraints` are allowlists; reject only on non-null query value outside the list.
- Relaxation order (on zero matches): shape → spatial_grain → cardinality → metric_type → ordered_num → obs_per_group → cat_structure → cat_vars → num_vars. `functions` never relaxed.
- Rank: functions covered ↓, matched fields ↓, leaf specialization (fewer functions) ↑.
- Data gate: completeness < 0.8 or n < 5 (when known) → `data_quality_insufficient` error, no recommendation.

## Axis extraction hints (the actual hard part)

| Signal | Mapping |
|---|---|
| Date/timestamp column used as axis | `ordered_num: true` (do NOT count as cat_var) |
| Month/weekday as grouping, not sequence | `cat_vars` += 1, `cat_structure` per nesting |
| Edge list / from-to pairs | `is_relational: true` |
| Geo codes, lat/lon, region names as unit | `is_spatial: true`; polygons→region, coords→point, origin-dest→flow |
| Metric is a count of things | `metric_type: "count"` (blocks choropleth) |
| One row per group vs raw observations | `obs_per_group` |
| Distinct values of main categorical | 2–7→low, 8–20→medium, >20→high |

When a signal is ambiguous, leave the field null and let caveats surface the uncertainty — do not guess.

## Provenance (delta log)

- v1: nested YAML from data-to-viz SVG → rejected for LLM traversal fragility.
- v2: flat JSON, function-as-tag, disqualifiers (session synthesis + 2 Perplexity SKILLs: cardinality/shape/profiling gate; spatial axis/anti-patterns/aliases).
- v3: review fixes — allowlists, wildcard semantics, query schema, shape caveats wired, null-safe gates.
- v4: engine extracted to code; 5 leaves added; multi-function; gating rule; 20-case eval green; reachability proven.

## Rejected scope (unchanged)

Format families, page layout, editorial voice, typography, image licensing, SQL profiling, piece-level chart limits.

## Open work

- Axis-extraction eval (the residual risk lives there, not in matching).
- Seasonality/change-point sub-axes when `ordered_num=true`.
- Accessibility constraints per chart; `interactivity_required` axis; confidence scores.
