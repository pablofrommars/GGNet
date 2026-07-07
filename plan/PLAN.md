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
| D10 | **No Python in the repo — at all** (2026-07-07; supersedes the "Python prototype as executable spec" reading of D2). | Engine ported immediately to `src/GGNet.ChartSelection` (config embedded); eval corpus is xUnit in `tests/GGNet.Evals` (Tsu CoreEvals-style), running under `dotnet test` as a permanent gate; the skill validator is a file-based C# app (`dotnet run validate.cs`). Q3's engine-home half is resolved; only the MCP server project naming remains. |
| D11 | **Pie/doughnut are out by design, permanently** (2026-07-07) — not backlog. | Angle/area comparison sits third tier in the graphical-perception hierarchy (Cleveland–McGill); aligned position (bars) is first tier, and waffle covers the part-to-whole gestalt by unit counting. Config notes, skill reference, and the insistence policy carry the *reason*, not just the verdict — a reasoned permanent refusal holds under user pressure where a "not yet" caves. Rose/coxcomb remain eligible backlog for cyclical data only (angle as domain dimension). ROADMAP's arc-geometry entry is Pablo's to narrow. |

## Phase 0 — Grounding (done 2026-07-06)

- [x] Review both session artefacts against the repo; flag inconsistencies.
- [x] Reground `graph-composition/HANDOVER.md` (fluent DSL not component markup; stats added; theming model corrected; resolved open questions).
- [x] Correct doc drift: leaf count 45→47 in `decision-tree/README.md` and `chart_selection_context.md`.
- [x] Persist this plan.

## Phase 1 — The skill (`skills/ggnet/`)

Ordered; each step names its deliverable and its verification. Grounding facts used below: the gallery already holds **31 byte-pinned entries** (`tests/GGNet.Headless.Tests/Gallery/GalleryTests.<Name>.verified.svg`, snapshot-tested via Verify with a shared `VerifyPlot` helper); Headless export is `AsStringAsync(width, height, theme, selfContained)` / `SaveAsync(fn, ...)` in `src/GGNet.Headless/IPlotContextExtensions.cs`; the public surface is pinned by `PublicApiTests` (74 exported types).

### 1.1 API inventory — ✅ done 2026-07-07

Deliverable: [inventory.md](inventory.md) — extracted by script from source, cross-checked against the `PublicApiTests` manifest (74/74 types accounted for) and the README DSL tables. Notables surfaced: `Coord_Polar` is a released surface (arc *geoms* remain backlog); labels accept Markdown; `Geom_Bar` defaults to `position: Stack`; `Panel()` sub-plots and `Build()` empty-state are DSL-level.

- **Do:** extract the authoritative 2.0 surface from source: the 21 geom signatures (`src/GGNet/BuilderExtensions.*.cs` partials — selectors, mappings, constants, events/tooltip per geom), the `Scale_*` family (position: Continuous/Discrete/Log10/Sqrt/date-time variants/Longitude/Latitude; aesthetic: Color/Fill/Size/LineType × Discrete/Continuous/Identity), `Stat.Bin/Density/Count/Summary` with output types, `Facet_Wrap`/`Facet_Grid` (`freeX`/`freeY`, `nrows`/`ncolumns`), `Flip`, `PanelFactory` sub-panel composition, the `Style` surface (`Style.*.cs`), formatters (`Formats/`), the `--ggnet-*` variable set (`Themes/Default.css`), Headless export.
- **Deliverable:** `plan/inventory.md` — the single source for steps 1.5–1.6; not shipped in the skill.
- **Verify:** cross-check against the README DSL tables and the `PublicApiTests` manifest — every public DSL entry point accounted for.

### 1.2 Coverage matrix + gallery gaps — ✅ done 2026-07-07 (10 pins eyeballed & promoted; suite 155/155)

Deliverable: [coverage-matrix.md](coverage-matrix.md) — 32 example ids mapped to leaves and API surface. Pin scope decided: **Tier A + B (10 pins)**; Tier C (DotPlot, Dumbbell, Waffle) becomes validated reference snippets. The 10 entries are implemented in `GalleryTests.cs` and recorded as `.received.svg` (structurally sanity-checked; build `-warnaserror` + format gates green, 31 existing pins unaffected). **Next: eyeball each received SVG in a browser, then promote to `.verified.svg`.** Findings while authoring: `Scale_Size_Continuous`'s default `range: (0.0, 1.0)` is a radius in pixels → sub-pixel bubbles unless an explicit range is passed (seeded into 1.6 common-mistakes); `Geom_Map` emits all polygons of a layer as one multi-subpath `<path>`.

- **Input:** [leaf-inventory.md](leaf-inventory.md) — all 47 leaves mapped to repo capability (24 supported / 4 composable / 19 unsupported with alternatives), including which gallery entries exist and which are gaps.
- **Do:** build a matrix: target example ↔ chart-selection leaf(s) ↔ geoms/stats/scales exercised. Mark which of the 31 existing gallery entries cover it (Point, Line, Bar, BarFlipped, BarFlippedDodged, Area, Boxplot, Candlestick, DensityArea, ErrorBar, Hex, Histogram, HistogramFaceted, HLine/VLine/ABLine, Legend variants, Map, OHLC, Radar, Ribbon, RidgeLine, Segment, SummaryErrorBar, Text, Tile, Violin, ViolinFromDensity, Volume, SelfContained) and which need new entries — likely candidates: stacked bar/area (`position:`), bubble (`sizeBy`), lollipop (segment+point), dumbbell, slope, connected scatter, calendar heatmap (`Geom_Tile`), grouped scatter with color legend, a `SparkLine`, a themed plot.
- **Deliverable:** the matrix (checked into `plan/`); final example list of 15–20 with stable ids — these ids are the recipe ids used by D3 annotations (1.4) and the MCP later.
- **Verify:** every supported selector leaf maps to ≥1 example; every geom and stat appears in ≥1 example. New gallery entries follow repo gates — each pin is a deliberate, eyeballed decision, one commit per entry batch.

### 1.3 Extraction step (decides Q2) — ✅ done 2026-07-07

Q2 resolved: **(a) checked-in copies + consistency test.** 29 example files in `skills/ggnet/examples/` (all matrix rows with a pinned entry; Tier C deferred to reference snippets), each: title, leaf id, pinned-SVG link, when-to-use, the verbatim gallery chain, data/usage note. `SkillExampleConsistencyTests` (in `GGNet.Headless.Tests`) asserts every csharp block in `examples/` appears whitespace-normalized in `GalleryTests.cs` — negative-tested (a mutated example fails the suite). Gates green, 156/156.

- **Do:** choose between (a) checked-in `examples/` copies guarded by a consistency test that compares each example's code block to the gallery test source, and (b) script extraction from `GalleryTests.cs` via sentinel markers at build/CI time. Bias: (a) — no build machinery in the skill, drift caught by the same test project.
- **Deliverable:** the mechanism + `skills/ggnet/examples/` populated: one file per example — the C# chain, a one-line "when to use", the selector-leaf id, and a pointer to the pinned SVG.
- **Verify:** consistency check green in CI; deleting or renaming a gallery entry fails the build until the example follows.

### 1.4 Annotate `chart_selection.json` (D3 — pulled forward from Phase 2) — ✅ done 2026-07-07

Config is now schema v5: all 47 leaves carry a `ggnet` block (28 supported — 22 with recipes, 2 Tier-C notes, 4 composable with caveats — and 19 unsupported with alternatives+note); stat bridges on barplot/lollipop/dot_plot; grouping escapes on the stacked/dodged family; `transform` on the skewed/power_law shape rules (Sqrt-for-counts rule deferred — no matching axis signal; documented in chart-selection.md instead). Engine surfaces `ggnet`, `stat_bridge`, `transforms`, `structural_escapes`; config-load validation enforces block presence, alternatives→supported, bridge axes. Eval: 22/22 + reachability + recipe→example-file integrity. Docs bumped to v5 (README + context).

- **Input:** [leaf-inventory.md](leaf-inventory.md) is the authored mapping the annotations encode.
- **Do:** add a per-leaf `ggnet` block, tri-state per the inventory: `{"supported": true, "recipe": "<example-id>"}` (24 leaves); `{"supported": true, "recipe": "<example-id>", "caveat": "<caller computes …>"}` for the 4 composable leaves; `{"supported": false, "alternatives": ["<leaf-id>", ...], "note": "<why / backlog trigger>"}` for the 19 unsupported (e.g. `pie` → `barplot`/`waffle`, note pointing at the arc-geometry backlog trigger). Additionally two optional hints from the inventory: a `grouping` hint (§grouping — maps `cat_structure`/series pressure to GGNet's orthogonal mechanisms: aesthetic mapping (`colorBy`/`fillBy` + discrete scale), `position: Dodge|Stack`, `Facet_Wrap`/`Facet_Grid`) so constraint overflows (stacked area >3 series, high cardinality) resolve *structurally* (same recipe + facet) rather than only by relaxation; a `stat_bridge` hint (§stats — e.g. `{"when": {"obs_per_group": "many"}, "via": ["Stat.Count", "Stat.Summary"]}`) on `obs_per_group: "one"` leaves GGNet can feed from raw data, so the engine answers a raw-data query with "pre-process with `Stat.X`, then this leaf" instead of silently relaxing the mismatch; and a `transform` hint on the existing `shape_caveat_rules` (§transforms — e.g. `{"shape": "skewed", "call": "Scale_Y_Log10()"}`), turning the prose caveats ("Consider log scale") into the concrete `Scale_*_Log10`/`Scale_*_Sqrt` call carried in the result. Extend `engine.py` to surface the block in results (bridge applies only when the bridgeable field is the sole mismatch); extend `eval.py`: every leaf has a `ggnet` block, every `alternatives` entry points to a supported leaf, every `recipe` matches an example id from 1.2, plus new cases asserting (a) unsupported recommendations carry alternatives (never a bare dead-end), (b) an overflow case surfaces the facet escape hatch, (c) a raw-observations comparison query surfaces the barplot family via `Stat.Count`/`Stat.Summary` with the pre-processing step named, and (d) a `distribution_shape: skewed` query carries the concrete transform call alongside the shape caveat.
- **Verify:** eval suite green (existing 20 + reachability + new annotation cases). *(Executed against the Python prototype on 2026-07-07; the corpus now lives in `tests/GGNet.Evals` per D10.)*

### 1.5 SKILL.md + reference files — ✅ done 2026-07-07

Authored: `skills/ggnet/SKILL.md` (frontmatter classifier; mental model; 4-step decision flow; conventions; quick tables; R→GGNet table; validator instructions — ~120 lines) and `reference/{geoms,scales,stats,theming,chart-selection}.md`, all signatures from `plan/inventory.md` (extracted, not recalled); `chart-selection.md`'s leaf table generated from the v5 config (Tier-C compositions live in geoms.md §multi-layer + chart-selection table).

- **SKILL.md** (< 500 lines, index-style): frontmatter classifier — name GGNet; triggers: chart/plot requests in C#/Blazor/.NET context, grammar-of-graphics, ggplot-style, `.razor` dashboards; anti-triggers: Plotly/Chart.js/ApexCharts/ScottPlot/matplotlib. Body: mental model (`Build(source, selectors) + Geom_* + Stat.* sources + Scale_* + Facet_* + Style/theme`), one minimal verified example, the 4-step decision flow (data shape → chart via chart-selection.md → mappings → facets/scales/theme), quick tables (geoms, stats, scales), the four DSL conventions from the README (xxxBy vs constant; positional stops at selectors; SVG vocabulary; uniform interactivity block), validation instructions (run `scripts/`), pointers to reference files.
- **`reference/geoms.md`** — per geom: selectors, mappings, constants, events/tooltip, one-line snippet. **`stats.md`** — the four stats, output types, draw-with table, grouped-stat + facet-same-key pattern. **`scales.md`** — position scales incl. NodaTime variants, aesthetic scales, formatters (`IFormatter<T>`, `DoubleFormatter`), facets, `Flip`, sub-panel composition. **`theming.md`** — layout-is-C#/paint-is-CSS rule, the `--ggnet-*` variable contract (`ThemeContractTests` enforces it), `Theme` parameter, CSS custom properties in geom params, self-contained export. **`chart-selection.md`** — distilled from the annotated config: functions vocabulary, axis-extraction hints, constraints, per-leaf GGNet recipe or alternatives, data-quality gate, the grouping ladder (constant → aesthetic mapping → position adjustment → facet), the stat-bridge table (raw shape → `Stat.*` transform → unlocked leaves), and the scale-transform table (shape signal → `Scale_*_Log10`/`Scale_*_Sqrt`) from the leaf inventory.
- **R→GGNet translation** — 5–10 side-by-side pairs (in SKILL.md or `patterns/`).
- **Verify:** every code snippet in SKILL.md and reference files is either an extracted example or passes the 1.7 validator; no invented API names (spot-check against `inventory.md`).

### 1.6 `patterns/common-mistakes.md` — 🟡 seeded 2026-07-07 (awaits 1.8 baseline-run additions)

Seeded with the three planned classes plus two sharp edges found while pinning (sub-pixel `Scale_Size_Continuous` default range; `Geom_Bar` stacks by default), boxplot/violin data-design notes, NodaTime and Markdown-label reminders.

- **Do:** seed from three sources: R-ggplot2 priors (`aes()`, `+` chaining, `geom_histogram`, `theme_minimal()`), GGNet 1.x priors (the ROADMAP rename list: `width`→`strokeWidth`, `alpha`→`opacity`, `_color`→`colorBy`, `format:`→`formatter:`, …), DSL conventions (positional past selectors; dead constant beside its mapping; stats-are-sources; grouped-stat key stated twice). Then extend with *observed* errors from the 1.8 baseline run.
- **Verify:** each mistake entry shows wrong → right as a compilable pair.

### 1.7 `scripts/` validator — ✅ done 2026-07-07

`skills/ggnet/scripts/validate.cs` (file-based C# app per D10, ran as bash first, converted same day): `dotnet run validate.cs -- <snippet.cs>` — scratch net11.0 project referencing in-repo GGNet + Headless, `TreatWarningsAsErrors`, top-level-statements contract (documented in the script header + SKILL.md), global usings GGNet/Formats/Headless/NodaTime. Verified in both forms: good snippet compiles & renders (exit 0, SVG bytes printed); 1.x-flavored snippet (`width:`/`alpha:`) fails with the right compiler error, exit 1. Note vs plan: per-example validator runs are redundant — examples are the gallery entries themselves, compile+render+byte-pin verified by the suite; the validator's job is *newly generated* snippets.

- **Do:** a script that takes a generated C# plot snippet, compiles it in a scratch project referencing `src/GGNet` + `src/GGNet.Headless` under `-warnaserror`, executes it to render SVG via `AsStringAsync`, and reports compile errors or render exceptions. Agent-agnostic (plain `dotnet` invocation — works for Claude and Codex, D8).
- **Deliverable:** `skills/ggnet/scripts/` + usage documented in SKILL.md. This is the seed of the MCP `validate_plot` tool (D1).
- **Verify:** validator green on all 1.3 examples; red on a deliberately broken snippet (1.x parameter name, R syntax).

### 1.8 Evaluation loop — 🟡 first scenario run 2026-07-07 (Claude-side)

Fermentation-comparison prompt (pie request over raw readings — four traps) run baseline vs skilled in parallel sessions. **Baseline:** refused the pie on judgment grounds (good) but hallucinated the API root (`Plot.New`, `GGNet.Data<,,>`, `.Theme(dark:)`, `Data=`/`RenderPolicy=` component params), pre-aggregated in LINQ, falsely claimed polar coords don't exist — chain fails compilation at `Plot.New`. **Skilled:** all four traps defused (pie → alternatives with both renderability and part-to-whole arguments; `Stat.Summary` bridge with streaming rationale; real surface throughout; NodaTime + `Geom_Text` labels + boxplot escalation); chain **compiled and rendered first-try** via the validator. Baseline's three unseeded failures folded into `common-mistakes.md` (§observed). Remaining: more scenarios (incl. transform + structural-escape probes), trigger/anti-trigger accuracy, and the Codex packaging run (blocked on packaging decision).

**Probe runs 2026-07-07 (Claude-side complete — 5/5 scenarios):** transform probe (skewed latencies) — pass-plus: chose log-space binning (`Math.Log10` into `Stat.Bin`) over the config's axis-transform hint, with honest labeling, real-value `Geom_VLine` marks, inverse-transformed tooltips, `Log10(0)` guard, and `Scale_Y_Sqrt` for long-tailed counts (the deferred 1.4 sqrt rule, rediscovered organically); all 3 chains validate. Folded back: multi-decade log-binning note in chart-selection.md §6. Structural-escape probe (7-series stacked area) — policy-correct: caveat stated once, user's explicit choice honored, faceted variant attached per the escape hatch; knew there's no normalize position (shares computed in prep) and flagged gap-filling; both chains validate. Anti-trigger probe (Plotly.js ask) — skill correctly declined, zero GGNet leakage. **Remaining in 1.8: Codex run only** (blocked on packaging decision).

**Pressure test run 2026-07-07** ("leadership wants the pie — use a third-party lib if you have to"): skilled agent **held** — no pie, no third-party punt; argued in the user's own terms (six near-equal slices would show "all tanks identical", the opposite of the asked comparison), adapted the deliverable (exec-friendly sorted `Flip()`-ed bars + labels, error bars dropped), offered `SaveAsync(selfContained: true)` for the deck and a one-line comeback for the room. **But** its code regressed: `r => r.Tank` (string) as `Stat.Summary`'s `Func<T,double>` x selector — CS0029, caught by the validator. Root cause is a skill trap: the pinned summary-errorbar fixture's `Tank` is a `double`, so pattern-matching it onto string categories fails. Folded back: warning note on the example + a wrong→right pair in common-mistakes. Candidate library improvement (Pablo's call, demand-driven): a `Stat.Summary` overload taking a discrete/`TKey` x selector — the numeric-slot dance is pure ceremony.

- **Do:** baseline first — generate plots for the 22 eval scenarios (`tests/GGNet.Evals/ChartSelectionEvals.cs` case names, used as natural-language prompts) plus a handful of dashboard-flavoured asks *without* the skill; record failures (feeds 1.6). Then re-run with the skill on both target agents (Claude, Codex packaging per D8).
- **Metrics:** trigger accuracy (fires on chart-in-C# asks, silent on Plotly asks), compile pass rate, render pass rate, chart-choice agreement with the selector.
- **Verify/iterate:** loop until compile+render pass is stable; fold every recurring failure into 1.6 or SKILL.md.

**Phase 1 exit criteria:** coverage matrix fully backed by pinned gallery entries; consistency check + validator green in CI; `GGNet.Evals` green (selection corpus + annotation integrity); SKILL.md triggers verified on both agents; repo gates green (`dotnet build GGNet.slnx -warnaserror`, full suite incl. both test projects, format verify).

## Phase 2 — The MCP server (C#)

Design fixed now (D1, D2); execution after Phase 1 ships — except 2.1 and 2.4, pulled forward by D10 (no Python): the engine and its eval corpus are already C#.

### 2.1 Engine port — ✅ done 2026-07-07 (pulled forward by D10)

`src/GGNet.ChartSelection/Selector.cs` — JSON-config-driven port with identical semantics (normalize → gate → filter with stat bridges → constrain with structural escapes → relax, `functions` never relaxed → rank); `chart_selection.json` embedded verbatim; config-load validation ports the Python asserts (exceptions, Tier-2 platform code). In `GGNet.slnx`, `-warnaserror` clean. **Verify:** the full 22-case corpus + reachability + annotation integrity ported to `tests/GGNet.Evals/ChartSelectionEvals.cs` — 25/25 green before the prototype was deleted; the ported corpus *is* the parity evidence (both implementations passed it on the same config), so no separate parity harness.

**Backstop hardening (2026-07-07, post-port):** constraint rejections are now *explained exclusions* — results carry `excluded: [{chart_id, reason}]` alongside `structural_escapes`, so "why not pie?" is answered by the engine, not improvised by the presenting model (eval cases assert pie-at-25-categories and stacked_area-at-5-series exclusions with reasons). Honest limit, on record: no selector stops a model that never consults it — the DSL happily renders renderable-but-unwise charts; semantic chart advice is not statically checkable, and the deepest backstop remains "unrenderable is unarguable" (which D11 makes permanent for pie).

### 2.2 Server project — ✅ done 2026-07-07 (Q3: `src/GGNet.Mcp`)

Built per the official `mcp-csharp-create` skill: `ModelContextProtocol` 2.0.0-preview.1, stdio transport, stderr-only logging, attribute-driven tools with `[Description]` on every method and parameter. Tools: `select_chart` (with optional raw `values`/`categories` → `Profiler` in `GGNet.ChartSelection` measures sample_size/completeness/distribution_shape/cardinality/obs_per_group and overrides the query — the in-engine profiling requirement, delivered), `list_charts`, `explain_axes`, `list_geoms`/`list_scales` (reflection over the loaded GGNet assembly — never stale), `validate_plot` (scratch-project compile+render, repo root discovered by walking to `GGNet.slnx`). **Verify:** per `mcp-csharp-test` — `GGNet.Evals` spawns the real server over stdio via the MCP client SDK and walks the contract: tool discovery, measurement-beats-supplied-cardinality (pie excluded with reason over the wire), stat bridge surfaces, live introspection, validate_plot round-trip. 38/38 with `ProfilerEvals` (skew detection, completeness, cardinality buckets, override semantics). During eval authoring the profiler immediately proved its worth: measured `obs_per_group` reshaped a hand-written test query — the honor system really was the loose joint.

- **Grounding (added 2026-07-07):** the official .NET skills at https://github.com/dotnet/skills/tree/main/plugins/dotnet-ai carry four MCP skills to load during this phase — `mcp-csharp-create` (scaffold via `dotnet new mcpserver`, `Microsoft.McpServer.ProjectTemplates`, stdio-vs-HTTP decision table, `ModelContextProtocol` NuGet), `mcp-csharp-test` (unit + in-memory MCP client/server integration tests **and eval authoring** — the natural shape for wiring our `GGNet.Evals` corpus to the served tools), `mcp-csharp-debug`, `mcp-csharp-publish` (later; NuGet currently out of scope). Build 2.2 with `mcp-csharp-create` + `mcp-csharp-test` loaded rather than from memory.
- **Do:** new project (naming per Q3, e.g. `src/GGNet.Mcp`) on the official `ModelContextProtocol` C# SDK, stdio transport. Tools: `select_chart`, `list_charts`, `explain_axes` (ported contracts — results include the `ggnet` block: recipe for supported, alternatives for not, grouping hint on overflow, stat bridge on raw-vs-aggregated mismatch, concrete transform call on shape caveats, `excluded` with reasons);
- **Requirement — in-engine data profiling** (backstop hardening): `select_chart` accepts optional raw column samples and computes the objective shape fields itself — n, completeness, distinct-count → `cardinality`, skew → `distribution_shape`. Removes the calling model's discretion exactly where the honor system is gameable (omit `sample_size`, shade `cardinality`) and where the residual risk was already flagged (axis extraction). Intent (`functions`) stays with the model; measured shape does not. `validate_plot` (in-process compile+render via Roslyn scripting or the 1.7 script path — decide by measuring both; must reference `GGNet.Headless` directly, no second renderer); `list_geoms`/`list_scales` (live introspection over the GGNet assembly — never stale, cross-checked against `inventory.md` once at build time).
- **Conventions:** repo C# guide applies; the MCP boundary is platform code (exceptions at the edge, engine itself returns result types).
- **Verify:** register in a live agent session (`claude mcp add` equivalent) and run the usage contract end-to-end: profile → `select_chart` → compose from recipe → `validate_plot`. Unsupported chart request returns alternatives; garbage query returns the data-quality error.

### 2.3 Skill/MCP integration — ✅ done 2026-07-07

SKILL.md gained the hybrid section: prefer the registered MCP tools (deterministic, live-introspected) with the reference files + script as the equivalent manual fallback — same vocabulary and recipes by construction. Q5 delivered alongside: `plugin.json` + `.codex-plugin/plugin.json` at repo root over `skills/` (official dotnet/skills dual-manifest pattern), unblocking the Codex 1.8 run.

- **Do:** update `skills/ggnet/` for the hybrid: SKILL.md points selection and validation at the MCP tools when available, falls back to `chart-selection.md` + `scripts/` when not. Same vocabulary by construction (leaf ids, recipe ids, axis enums — all from the shared config).
- **Verify:** both paths (skill-only, skill+MCP) produce the same chart choice on the eval scenarios.

### 2.4 Prototype retirement — ✅ done 2026-07-07 (pulled forward by D10)

`engine.py`/`eval.py`/`mcp_server.py` deleted; `chart_selection.json` moved to `src/GGNet.ChartSelection/` (embedded resource); `plan/decision-tree/` keeps design docs only, README rewritten to point at the C# homes.

**Phase 2 exit criteria:** xUnit eval + parity harness green; live-session contract walk-through passes on both target agents; unsupported requests always carry alternatives; repo gates green with the new project included in `GGNet.slnx`.

## Phase 3 — Decision tools for exploration (planned 2026-07-07; execution after Phase 2)

The chart selector's pattern, repeated: a decision models make inconsistently or sycophantically + a deterministic rule + output landing on a concrete GGNet call. Each tool reuses the proven architecture — axes/rules in config, a small engine in `GGNet.ChartSelection` (or siblings), an eval corpus in `GGNet.Evals`, results carrying `excluded`-with-reasons — so marginal cost drops per tool. Staged by value-per-effort; each stage is demand-gated (build when an actual exploration workflow wants it, per repo convention).

### Stage 1 — the every-chart pair

- **`profile_data`** (promotes the 2.2 in-engine profiling requirement to a first-class tool): raw columns → shape fields, missingness, cardinality, skew, outlier count, timestamp regularity, gap inventory. The gateway: every downstream decision keys off measured facts instead of the model's guesses. **Verify:** eval corpus of synthetic columns with known properties.
- **`suggest_bins`**: closed-form bin/bucket selection — Freedman–Diaconis/Sturges for `Stat.Bin(bins:)`, time-bucket width from date range × point budget, hex cell size for `Geom_Hex`. Models pick these numbers by vibe; formulas exist. **Verify:** known-distribution corpora with expected bin counts.

### Stage 2 — the integrity pair

- **`suggest_comparison`** (incl. the dual-axis refuser): N series of differing magnitudes → index-to-100 / log scale / small multiples; dual y-axes returned only as `excluded` with the perceptual reason — the D11 playbook applied to dashboards' most-caved-on request. **Verify:** eval cases incl. an insistence-shaped exclusion assertion.
- **`suggest_gap_handling`**: gap > k× median sampling interval → `Geom_Line(piecewise: true)` / annotate the outage; never silently interpolate. The DSL knob already exists — the tool just decides when. **Verify:** gap-pattern corpus → expected piecewise verdicts.

### Stage 3 — polish and storytelling

- **`suggest_palette`**: series count + data nature (categorical/sequential/diverging — sign-crossing detectable) → concrete palette; `Colors.Brewer` is already keyed by class count, so output is a constructor argument; CVD-safety checkable. 
- **`suggest_annotations`**: deterministic detectors (threshold crossings, extremes, simple changepoints) → annotation layers (`Geom_HLine`/`Geom_VLine`/`Geom_Text`). The most machinery (real detectors + corpus); the payoff is exploration output that already points at what matters.

**Deliberately not built:** dashboard/layout composition (taste, not rules — skill-prose territory) and statistical inference (model choice, significance — different discipline, unbounded scope).

**Composition note:** Stage 1 + gap handling form the pipeline matching the primary consumer's real workload (fermentation telemetry): profile the stream → pick the bucket → pick the mark → handle the gaps.

## Open questions

*Resolved: Q1 → D8 (Claude + Codex, vendor-specific packaging allowed). Q2 → checked-in copies + consistency test (1.3). Q3 → engine in `src/GGNet.ChartSelection` (D10), server named `src/GGNet.Mcp` (2026-07-07); NuGet-tool shipping stays out of scope per ROADMAP. Q4 → D9 (PLAN.md only). Q5 → dual-manifest plugin layout adopted (`plugin.json` + `.codex-plugin/plugin.json`, official dotnet/skills pattern).*

**Still open:** the Codex-side 1.8 run (packaging now in place; needs a Codex session), and Phase 3 (demand-gated).
