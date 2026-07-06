# GGNet Skills & MCP — Plan

*Started 2026-07-06. This file is the persistent planning record for shipping GGNet with a set of skills and, later, an MCP server. Session artefacts: [graph-composition/HANDOVER.md](graph-composition/HANDOVER.md) (skill design, regrounded to the repo) and [decision-tree/](decision-tree/) (chart-selector prototype: config + Python engine + 20-case eval, all green).*

## Decisions (closed — do not relitigate)

| # | Decision | Rationale |
|---|---|---|
| D1 | **Skills ship before MCP, but the two are designed together.** | Skill delivers value with zero hosting; the shared vocabulary (selector leaf ids ↔ geom recipes, validator ↔ `validate_plot`) is fixed now so the MCP slots in without rework. |
| D2 | **C# all the way for the MCP.** | GGNet is a single-toolchain .NET repo (dotnet-only gates). The Python `engine.py`/`mcp_server.py` are the validated prototype; production uses the official `ModelContextProtocol` C# SDK. `chart_selection.json` carries over verbatim; the eval ports to xUnit. |
| D3 | **GGNet capability annotations live in `chart_selection.json`.** | ~20 of 47 leaves aren't renderable by GGNet today, and new geoms arrive demand-driven. Per-leaf annotations (`supported`, geom recipe, nearest supported alternative) keep the selector the single source of truth; the engine returns `supported: false` + alternatives instead of silently recommending the unrenderable. |
| D4 | **Examples are compiled gallery entries.** | Each example is a real entry in `tests/GGNet.Headless.Tests` (built `-warnaserror`, rendered via Headless, byte-pinned). Skill `examples/` files are extracted from them — CI makes drift impossible. |
| D5 | **Chart selection is distilled into the skill during the skills-only phase.** | `reference/chart-selection.md` generated from `chart_selection.json`; the MCP later becomes the deterministic path with identical vocabulary. |
| D6 | **Skills live at `skills/ggnet/`, repo root, tracked in git.** | Skills target frontier models across major vendors — vendor-neutral markdown, not a `.claude/`-only asset. |
| D7 | **The skill documents the 2.0 surface.** | Published 1.4.0 docs are stale; 1.x parameter names go in `common-mistakes.md` alongside R-ggplot2 priors. |
| D8 | **Target agents: Claude and Codex, in that priority.** | Skill content is formulated to work for both (plain markdown, no vendor-only mechanics in the body); packaging may be vendor-specific (SKILL.md frontmatter for Claude; Codex adapter as needed). |
| D9 | **Planning stays in PLAN.md; no ROADMAP.md entry.** | ROADMAP.md remains the library's own open-items file; skills/MCP scope is tracked here only. |

## Phase 0 — Grounding (done 2026-07-06)

- [x] Review both session artefacts against the repo; flag inconsistencies.
- [x] Reground `graph-composition/HANDOVER.md` (fluent DSL not component markup; stats added; theming model corrected; resolved open questions).
- [x] Correct doc drift: leaf count 45→47 in `decision-tree/README.md` and `chart_selection_context.md`.
- [x] Persist this plan.

## Phase 1 — The skill (`skills/ggnet/`)

Ordered; each step names its deliverable and its verification. Grounding facts used below: the gallery already holds **31 byte-pinned entries** (`tests/GGNet.Headless.Tests/Gallery/GalleryTests.<Name>.verified.svg`, snapshot-tested via Verify with a shared `VerifyPlot` helper); Headless export is `AsStringAsync(width, height, theme, selfContained)` / `SaveAsync(fn, ...)` in `src/GGNet.Headless/IPlotContextExtensions.cs`; the public surface is pinned by `PublicApiTests` (74 exported types).

### 1.1 API inventory

- **Do:** extract the authoritative 2.0 surface from source: the 21 geom signatures (`src/GGNet/BuilderExtensions.*.cs` partials — selectors, mappings, constants, events/tooltip per geom), the `Scale_*` family (position: Continuous/Discrete/Log10/Sqrt/date-time variants/Longitude/Latitude; aesthetic: Color/Fill/Size/LineType × Discrete/Continuous/Identity), `Stat.Bin/Density/Count/Summary` with output types, `Facet_Wrap`/`Facet_Grid` (`freeX`/`freeY`, `nrows`/`ncolumns`), `Flip`, `PanelFactory` sub-panel composition, the `Style` surface (`Style.*.cs`), formatters (`Formats/`), the `--ggnet-*` variable set (`Themes/Default.css`), Headless export.
- **Deliverable:** `tmp/skills-mcp/inventory.md` — the single source for steps 1.5–1.6; not shipped.
- **Verify:** cross-check against the README DSL tables and the `PublicApiTests` manifest — every public DSL entry point accounted for.

### 1.2 Coverage matrix + gallery gaps

- **Input:** [leaf-inventory.md](leaf-inventory.md) — all 47 leaves mapped to repo capability (24 supported / 4 composable / 19 unsupported with alternatives), including which gallery entries exist and which are gaps.
- **Do:** build a matrix: target example ↔ chart-selection leaf(s) ↔ geoms/stats/scales exercised. Mark which of the 31 existing gallery entries cover it (Point, Line, Bar, BarFlipped, BarFlippedDodged, Area, Boxplot, Candlestick, DensityArea, ErrorBar, Hex, Histogram, HistogramFaceted, HLine/VLine/ABLine, Legend variants, Map, OHLC, Radar, Ribbon, RidgeLine, Segment, SummaryErrorBar, Text, Tile, Violin, ViolinFromDensity, Volume, SelfContained) and which need new entries — likely candidates: stacked bar/area (`position:`), bubble (`sizeBy`), lollipop (segment+point), dumbbell, slope, connected scatter, calendar heatmap (`Geom_Tile`), grouped scatter with color legend, a `SparkLine`, a themed plot.
- **Deliverable:** the matrix (checked into `tmp/skills-mcp/`); final example list of 15–20 with stable ids — these ids are the recipe ids used by D3 annotations (1.4) and the MCP later.
- **Verify:** every supported selector leaf maps to ≥1 example; every geom and stat appears in ≥1 example. New gallery entries follow repo gates — each pin is a deliberate, eyeballed decision, one commit per entry batch.

### 1.3 Extraction step (decides Q2)

- **Do:** choose between (a) checked-in `examples/` copies guarded by a consistency test that compares each example's code block to the gallery test source, and (b) script extraction from `GalleryTests.cs` via sentinel markers at build/CI time. Bias: (a) — no build machinery in the skill, drift caught by the same test project.
- **Deliverable:** the mechanism + `skills/ggnet/examples/` populated: one file per example — the C# chain, a one-line "when to use", the selector-leaf id, and a pointer to the pinned SVG.
- **Verify:** consistency check green in CI; deleting or renaming a gallery entry fails the build until the example follows.

### 1.4 Annotate `chart_selection.json` (D3 — pulled forward from Phase 2)

Must precede 1.5's `chart-selection.md`; the Python prototype stays the executable spec until the C# port.

- **Input:** [leaf-inventory.md](leaf-inventory.md) is the authored mapping the annotations encode.
- **Do:** add a per-leaf `ggnet` block, tri-state per the inventory: `{"supported": true, "recipe": "<example-id>"}` (24 leaves); `{"supported": true, "recipe": "<example-id>", "caveat": "<caller computes …>"}` for the 4 composable leaves; `{"supported": false, "alternatives": ["<leaf-id>", ...], "note": "<why / backlog trigger>"}` for the 19 unsupported (e.g. `pie` → `barplot`/`waffle`, note pointing at the arc-geometry backlog trigger). Additionally an optional `grouping` hint (see inventory §grouping): maps `cat_structure`/series pressure to GGNet's orthogonal mechanisms — aesthetic mapping (`colorBy`/`fillBy` + discrete scale), `position: Dodge|Stack`, `Facet_Wrap`/`Facet_Grid` — so constraint overflows (stacked area >3 series, high cardinality) resolve *structurally* (same recipe + facet) rather than only by relaxation. Extend `engine.py` to surface the block in results; extend `eval.py`: every leaf has a `ggnet` block, every `alternatives` entry points to a supported leaf, every `recipe` matches an example id from 1.2, plus new cases asserting (a) unsupported recommendations carry alternatives (never a bare dead-end) and (b) an overflow case surfaces the facet escape hatch.
- **Verify:** `eval.py` green (existing 20 + reachability + new annotation cases).

### 1.5 SKILL.md + reference files

- **SKILL.md** (< 500 lines, index-style): frontmatter classifier — name GGNet; triggers: chart/plot requests in C#/Blazor/.NET context, grammar-of-graphics, ggplot-style, `.razor` dashboards; anti-triggers: Plotly/Chart.js/ApexCharts/ScottPlot/matplotlib. Body: mental model (`Build(source, selectors) + Geom_* + Stat.* sources + Scale_* + Facet_* + Style/theme`), one minimal verified example, the 4-step decision flow (data shape → chart via chart-selection.md → mappings → facets/scales/theme), quick tables (geoms, stats, scales), the four DSL conventions from the README (xxxBy vs constant; positional stops at selectors; SVG vocabulary; uniform interactivity block), validation instructions (run `scripts/`), pointers to reference files.
- **`reference/geoms.md`** — per geom: selectors, mappings, constants, events/tooltip, one-line snippet. **`stats.md`** — the four stats, output types, draw-with table, grouped-stat + facet-same-key pattern. **`scales.md`** — position scales incl. NodaTime variants, aesthetic scales, formatters (`IFormatter<T>`, `DoubleFormatter`), facets, `Flip`, sub-panel composition. **`theming.md`** — layout-is-C#/paint-is-CSS rule, the `--ggnet-*` variable contract (`ThemeContractTests` enforces it), `Theme` parameter, CSS custom properties in geom params, self-contained export. **`chart-selection.md`** — distilled from the annotated config: functions vocabulary, axis-extraction hints, constraints, per-leaf GGNet recipe or alternatives, data-quality gate, and the grouping ladder (constant → aesthetic mapping → position adjustment → facet) from the leaf inventory.
- **R→GGNet translation** — 5–10 side-by-side pairs (in SKILL.md or `patterns/`).
- **Verify:** every code snippet in SKILL.md and reference files is either an extracted example or passes the 1.7 validator; no invented API names (spot-check against `inventory.md`).

### 1.6 `patterns/common-mistakes.md`

- **Do:** seed from three sources: R-ggplot2 priors (`aes()`, `+` chaining, `geom_histogram`, `theme_minimal()`), GGNet 1.x priors (the ROADMAP rename list: `width`→`strokeWidth`, `alpha`→`opacity`, `_color`→`colorBy`, `format:`→`formatter:`, …), DSL conventions (positional past selectors; dead constant beside its mapping; stats-are-sources; grouped-stat key stated twice). Then extend with *observed* errors from the 1.8 baseline run.
- **Verify:** each mistake entry shows wrong → right as a compilable pair.

### 1.7 `scripts/` validator

- **Do:** a script that takes a generated C# plot snippet, compiles it in a scratch project referencing `src/GGNet` + `src/GGNet.Headless` under `-warnaserror`, executes it to render SVG via `AsStringAsync`, and reports compile errors or render exceptions. Agent-agnostic (plain `dotnet` invocation — works for Claude and Codex, D8).
- **Deliverable:** `skills/ggnet/scripts/` + usage documented in SKILL.md. This is the seed of the MCP `validate_plot` tool (D1).
- **Verify:** validator green on all 1.3 examples; red on a deliberately broken snippet (1.x parameter name, R syntax).

### 1.8 Evaluation loop

- **Do:** baseline first — generate plots for the 20 `eval.py` scenarios (used as natural-language prompts) plus a handful of dashboard-flavoured asks *without* the skill; record failures (feeds 1.6). Then re-run with the skill on both target agents (Claude, Codex packaging per D8).
- **Metrics:** trigger accuracy (fires on chart-in-C# asks, silent on Plotly asks), compile pass rate, render pass rate, chart-choice agreement with the selector.
- **Verify/iterate:** loop until compile+render pass is stable; fold every recurring failure into 1.6 or SKILL.md.

**Phase 1 exit criteria:** coverage matrix fully backed by pinned gallery entries; consistency check + validator green in CI; `eval.py` green with annotations; SKILL.md triggers verified on both agents; repo gates green (`dotnet build GGNet.slnx -warnaserror`, full suite, format verify).

## Phase 2 — The MCP server (C#)

Design fixed now (D1, D2); execution after Phase 1 ships. The annotated config and Python eval from 1.4 are the executable spec being ported.

### 2.1 Engine port

- **Do:** port `engine.py` `select(cfg, query)` to C# with identical semantics — normalize → gate (spatial/relational/physical require affirmative) → filter → constrain (allowlists, reject only non-null-outside-list) → relax (fixed order, `functions` never relaxed) → rank (functions covered ↓, matched fields ↓, specialization ↑) — plus the data-quality gate (completeness < 0.8 or n < 5 → `data_quality_insufficient`). `chart_selection.json` ships verbatim as an embedded resource; records model Query/Leaf/Result. Pure, deterministic, no I/O.
- **Verify:** xUnit port of the full eval corpus (20 base + reachability + 1.4 annotation cases), plus a **parity harness**: run Python and C# over the same query corpus, diff the JSON outputs — parity green is the retirement precondition for the prototype.

### 2.2 Server project

- **Do:** new project (naming per Q3, e.g. `src/GGNet.Mcp`) on the official `ModelContextProtocol` C# SDK, stdio transport. Tools: `select_chart`, `list_charts`, `explain_axes` (ported contracts — results include the `ggnet` block: recipe for supported, alternatives for not, grouping hint on overflow); `validate_plot` (in-process compile+render via Roslyn scripting or the 1.7 script path — decide by measuring both; must reference `GGNet.Headless` directly, no second renderer); `list_geoms`/`list_scales` (live introspection over the GGNet assembly — never stale, cross-checked against `inventory.md` once at build time).
- **Conventions:** repo C# guide applies; the MCP boundary is platform code (exceptions at the edge, engine itself returns result types).
- **Verify:** register in a live agent session (`claude mcp add` equivalent) and run the usage contract end-to-end: profile → `select_chart` → compose from recipe → `validate_plot`. Unsupported chart request returns alternatives; garbage query returns the data-quality error.

### 2.3 Skill/MCP integration

- **Do:** update `skills/ggnet/` for the hybrid: SKILL.md points selection and validation at the MCP tools when available, falls back to `chart-selection.md` + `scripts/` when not. Same vocabulary by construction (leaf ids, recipe ids, axis enums — all from the shared config).
- **Verify:** both paths (skill-only, skill+MCP) produce the same chart choice on the eval scenarios.

### 2.4 Prototype retirement

- **Do:** once 2.1 parity is green and 2.2 is live, remove the Python engine/eval/server from `tmp/skills-mcp/decision-tree/`; keep `chart_selection.json` at its production home (owned by the C# project); leave a pointer in the decision-tree docs.

**Phase 2 exit criteria:** xUnit eval + parity harness green; live-session contract walk-through passes on both target agents; unsupported requests always carry alternatives; repo gates green with the new project included in `GGNet.slnx`.

## Open questions

- **Q2 — Extraction mechanics** (Phase 1 step 3): build-time extraction vs checked-in copies + consistency test. Decide when building it.
- **Q3 — MCP project naming/layout** (`src/GGNet.Mcp` vs tooling dir) and whether it ships as a NuGet tool — decide at Phase 2 start (NuGet packaging is currently out of scope per ROADMAP).

*Resolved: Q1 → D8 (Claude + Codex, vendor-specific packaging allowed). Q4 → D9 (PLAN.md only).*
