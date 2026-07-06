"""Chart Selector MCP server.

Exposes the deterministic chart-selection engine as MCP tools.
The LLM builds a query object from data + intent and calls select_chart;
all matching logic runs here, versioned and testable.

Run (stdio): python mcp_server.py
Register (Claude Code): claude mcp add chart-selector -- python /path/to/mcp_server.py
"""
from __future__ import annotations
from pathlib import Path
from typing import Literal

from mcp.server.fastmcp import FastMCP

from engine import load_config, select, AXIS_FIELDS

CFG = load_config(Path(__file__).parent / "chart_selection.json")
mcp = FastMCP("chart-selector")

Function = Literal["comparison", "correlation", "distribution", "part_to_whole", "trend_over_time"]


@mcp.tool()
def select_chart(
    functions: list[str],
    num_vars: str | None = None,
    cat_vars: str | None = None,
    cat_structure: str | None = None,
    obs_per_group: str | None = None,
    ordered_num: bool | None = None,
    is_relational: bool | None = None,
    is_spatial: bool | None = None,
    physical_subject: bool | None = None,
    spatial_grain: str | None = None,
    cardinality: str | None = None,
    metric_type: str | None = None,
    num_series: int | None = None,
    sample_size: int | None = None,
    completeness: float | None = None,
    distribution_shape: str | None = None,
    top_n: int = 3,
) -> dict:
    """Recommend chart types for a dataset + editorial intent.

    REQUIRED: functions — one or more of comparison | correlation | distribution
    | part_to_whole | trend_over_time (story-need aliases like 'change_over_time'
    are resolved automatically).

    All other fields describe the data shape; pass null/omit when unknown —
    unknown never disqualifies, it only adds 'unverified' caveats. Enum values:
    num_vars: 0|1|2|3|many; cat_vars: 0|1|2|many; cat_structure: none|flat|subgroup|nested;
    obs_per_group: one|many; spatial_grain: region|point|flow;
    cardinality: low_2_7|medium_8_20|high_gt_20; metric_type: count|rate|amount|score;
    distribution_shape: normal|skewed|bimodal|power_law|uniform|unknown.

    Extraction hints: a time axis is ordered_num=true (not a cat_var); edge lists
    → is_relational=true; geo units → is_spatial=true; counts block choropleth.

    Returns {top_charts: [{chart_id, matched_fields, functions_covered, caveats}],
    total_matches, relaxed} or {error, reasons} on data-quality failure.
    """
    query = {k: v for k, v in locals().items() if k not in ("top_n",)}
    return select(CFG, query, top_n=top_n)


@mcp.tool()
def list_charts(function: str | None = None) -> dict:
    """List all chart types in the catalog, optionally filtered by function.
    Returns each chart's id, functions, data-shape requirements, constraints, caveats."""
    leaves = CFG["leaves"]
    if function:
        fn = CFG["story_need_aliases"].get(function, function)
        leaves = [l for l in leaves if fn in l["functions"]]
    return {"count": len(leaves), "charts": leaves}


@mcp.tool()
def explain_axes() -> dict:
    """Return the full axis vocabulary and matching semantics — use when unsure
    how to map a dataset's columns into a select_chart query."""
    return {
        "axes": CFG["axes"],
        "aliases": CFG["story_need_aliases"],
        "semantics": {
            "wildcard": "Absent leaf field matches any query value.",
            "gating": "Leaves with is_spatial/is_relational/physical_subject=true require the query to affirm it.",
            "constraints": "Allowlists; only a known out-of-list value rejects. Unknown → caveat.",
            "relax_order": CFG["relax_order"],
            "never_relaxed": "functions",
        },
        "extraction_hints": {
            "timestamp_as_axis": "ordered_num=true, not a cat_var",
            "month_as_grouping": "cat_var, not ordered_num",
            "edge_list": "is_relational=true",
            "geo_unit": "is_spatial=true; polygon→region, coords→point, origin-dest→flow",
            "count_metric": "metric_type=count (blocks choropleth)",
            "ambiguous": "leave null; never guess",
        },
    }


if __name__ == "__main__":
    mcp.run()
