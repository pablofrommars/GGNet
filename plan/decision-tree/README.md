# chart-selector

Deterministic chart-type recommendation. The agent builds the query; the engine decides.

The Python prototype that lived here was retired on 2026-07-07 (decision: no Python in this repo — see [../PLAN.md](../PLAN.md)). The system's homes are now:

- `src/GGNet.ChartSelection/chart_selection.json` — config (axes, 47 leaves, constraints, per-leaf `ggnet` renderability blocks). Single source of truth, embedded in the assembly.
- `src/GGNet.ChartSelection/Selector.cs` — matching logic (normalize → gate → filter [stat bridges] → constrain [structural escapes] → relax → rank). `Selector.Select(cfg, query)` → results or error.
- `tests/GGNet.Evals/ChartSelectionEvals.cs` — 22-case regression suite + reachability + ggnet-annotation integrity, run by `dotnet test` (a repo gate, no manual step).
- MCP server — Phase 2 (see PLAN.md 2.2): C# `ModelContextProtocol` SDK exposing `select_chart`, `list_charts`, `explain_axes` over the same engine.

This folder keeps the design documentation: [chart_selection_context.md](chart_selection_context.md) (axes vocabulary, query object, matching semantics, version history).

## Usage contract for the calling LLM

1. Profile the dataset → query fields (unknown = omit, never guess).
2. Resolve user intent → `functions` list (aliases handled engine-side).
3. Call the selector; present top_charts with caveats verbatim.
4. On `data_quality_insufficient`, report the reasons — do not recommend a chart.
5. Read the `ggnet` block per result: `supported: false` → offer the `alternatives`; `stat_bridge` → state the `Stat.*` pre-processing step; `transforms` → apply the named `Scale_*` call; `structural_escapes` → offer "same chart, faceted" for overflows.
6. When the user asks "why not X": check `excluded` — constraint rejections come back as `{chart_id, reason}`. Quote the reason; do not re-adjudicate it.

Until the MCP server ships, the skill's [chart-selection reference](../../skills/ggnet/reference/chart-selection.md) is the distilled manual path.
