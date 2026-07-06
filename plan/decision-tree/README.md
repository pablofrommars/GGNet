# chart-selector MCP

Deterministic chart-type recommendation. LLM builds the query; engine decides.

## Files
- `chart_selection.json` — config (axes, 47 leaves, constraints). Single source of truth.
- `engine.py` — matching logic (normalize → gate → filter → constrain → relax → rank).
- `mcp_server.py` — MCP wrapper: `select_chart`, `list_charts`, `explain_axes`.
- `eval.py` — 20-case regression suite + reachability check. Run after any config edit.

## Install & register
```bash
pip install mcp
# Claude Code:
claude mcp add chart-selector -- python /abs/path/mcp_server.py
# Claude Desktop (claude_desktop_config.json):
{"mcpServers": {"chart-selector": {"command": "python", "args": ["/abs/path/mcp_server.py"]}}}
```

## Usage contract for the calling LLM
1. Profile the dataset → query fields (unknown = omit, never guess).
2. Resolve user intent → `functions` list (aliases handled server-side).
3. Call `select_chart`; present top_charts with caveats verbatim.
4. On `data_quality_insufficient`, report the reasons — do not recommend a chart.

Uncertain how to map columns → call `explain_axes` first.
