"""Eval: 20 realistic queries. Each asserts expected charts in top-3
and/or charts that must NOT appear."""
import json
from engine import load_config, select

cfg = load_config("chart_selection.json")

CASES = [
    ("revenue by 5 business units",
     {"functions": ["comparison"], "num_vars": "1", "cat_vars": "1", "cat_structure": "flat",
      "obs_per_group": "one", "ordered_num": False, "cardinality": "low_2_7"},
     {"expect_any": ["barplot", "lollipop", "dot_plot"], "forbid": ["pie"]}),

    ("market share of 4 competitors",
     {"functions": ["part_to_whole"], "num_vars": "1", "cat_vars": "1", "cat_structure": "flat",
      "obs_per_group": "one", "ordered_num": False, "cardinality": "low_2_7"},
     {"expect_any": ["pie", "doughnut", "waffle", "barplot"]}),

    ("market share of 25 competitors — pie must be excluded",
     {"functions": ["part_to_whole"], "num_vars": "1", "cat_vars": "1", "cat_structure": "flat",
      "obs_per_group": "one", "ordered_num": False, "cardinality": "high_gt_20"},
     {"forbid": ["pie", "doughnut", "waffle"], "expect_any": ["barplot"]}),

    ("salary distribution across 6 departments",
     {"functions": ["distribution"], "num_vars": "1", "cat_vars": "1", "cat_structure": "flat",
      "obs_per_group": "many", "ordered_num": False},
     {"expect_any": ["boxplot", "violin", "ridgeline", "beeswarm"]}),

    ("bimodal latency distribution per region — boxplot must carry caveat",
     {"functions": ["distribution"], "num_vars": "1", "cat_vars": "1", "cat_structure": "flat",
      "obs_per_group": "many", "ordered_num": False, "distribution_shape": "bimodal"},
     {"expect_any": ["violin", "ridgeline"], "caveat_on": ("boxplot", "bimodality")}),

    ("ARR vs headcount across portfolio companies",
     {"functions": ["correlation"], "num_vars": "2", "cat_vars": "0", "cat_structure": "none",
      "obs_per_group": "many", "ordered_num": False},
     {"expect_any": ["scatter", "density_2d"]}),

    ("MRR trend, single series, monthly",
     {"functions": ["trend_over_time"], "num_vars": "1", "cat_vars": "0", "cat_structure": "none",
      "obs_per_group": "one", "ordered_num": True},
     {"expect_any": ["area", "line"]}),

    ("daily active users over 2 years — seasonality",
     {"functions": ["trend_over_time"], "num_vars": "1", "cat_vars": "0", "cat_structure": "none",
      "obs_per_group": "one", "ordered_num": True},
     {"expect_any": ["calendar_heatmap", "area"]}),

    ("revenue mix over time, 5 product lines — stacked_area must be excluded",
     {"functions": ["part_to_whole", "trend_over_time"], "num_vars": "many", "cat_vars": "1",
      "cat_structure": "flat", "obs_per_group": "one", "ordered_num": True, "num_series": 5},
     {"forbid": ["stacked_area"], "expect_any": ["stream_graph", "small_multiples"]}),

    ("revenue mix over time, 3 product lines — stacked_area allowed",
     {"functions": ["part_to_whole", "trend_over_time"], "num_vars": "many", "cat_vars": "1",
      "cat_structure": "flat", "obs_per_group": "one", "ordered_num": True, "num_series": 3},
     {"expect_any": ["stacked_area"]}),

    ("NPS before vs after per segment",
     {"functions": ["comparison"], "num_vars": "2", "cat_vars": "1", "cat_structure": "flat",
      "obs_per_group": "one", "ordered_num": False},
     {"expect_any": ["dumbbell"]}),

    ("exact KPI values for 10 companies, several metrics",
     {"functions": ["comparison"], "num_vars": "many", "cat_vars": "1", "cat_structure": "flat",
      "obs_per_group": "one", "ordered_num": False, "cardinality": "medium_8_20"},
     {"expect_any": ["table", "parallel_plot"], "forbid": ["radar"]}),

    ("org budget hierarchy, one value per leaf",
     {"functions": ["part_to_whole"], "num_vars": "1", "cat_vars": "many", "cat_structure": "nested",
      "obs_per_group": "one", "ordered_num": False},
     {"expect_any": ["treemap", "sunburst"]}),

    ("unemployment RATE by county",
     {"functions": ["comparison"], "is_spatial": True, "spatial_grain": "region", "metric_type": "rate"},
     {"expect_any": ["choropleth"]}),

    ("store COUNT by county — choropleth must be excluded",
     {"functions": ["comparison"], "is_spatial": True, "metric_type": "count"},
     {"forbid": ["choropleth"], "expect_any": ["proportional_symbol_map"]}),

    ("migration flows between cities",
     {"functions": ["trend_over_time"], "is_spatial": True, "spatial_grain": "flow"},
     {"expect_any": ["flow_map"]}),

    ("capital flows between fund stages",
     {"functions": ["part_to_whole"], "is_relational": True},
     {"expect_any": ["sankey", "chord"]}),

    ("co-investment network among 40 VCs — chord must be excluded",
     {"functions": ["correlation"], "is_relational": True, "cardinality": "high_gt_20"},
     {"forbid": ["chord"], "expect_any": ["network", "arc"]}),

    ("multi-intent: compare segments AND show trend",
     {"functions": ["comparison", "trend_over_time"], "num_vars": "1", "cat_vars": "1",
      "cat_structure": "flat", "obs_per_group": "one", "ordered_num": True},
     {"expect_any": ["slope"]}),

    ("garbage data gate",
     {"functions": ["comparison"], "sample_size": 2},
     {"expect_error": "data_quality_insufficient"}),
]

passed = failed = 0
for name, query, exp in CASES:
    res = select(cfg, query)
    errors = []
    if "expect_error" in exp:
        if res.get("error") != exp["expect_error"]:
            errors.append(f"expected error {exp['expect_error']}, got {res}")
    else:
        if "error" in res:
            errors.append(f"unexpected error: {res}")
        else:
            top = [c["chart_id"] for c in res["top_charts"]]
            if "expect_any" in exp and not set(exp["expect_any"]) & set(top):
                errors.append(f"none of {exp['expect_any']} in top3 {top}")
            if "forbid" in exp:
                allm = [c["chart_id"] for c in res["top_charts"]]
                bad = set(exp["forbid"]) & set(allm)
                if bad:
                    errors.append(f"forbidden {bad} present in {allm}")
            if "caveat_on" in exp:
                cid, substr = exp["caveat_on"]
                full = select(cfg, query, top_n=99)
                hit = [c for c in full["top_charts"] if c["chart_id"] == cid]
                if not hit or not any(substr in cv for cv in hit[0]["caveats"]):
                    errors.append(f"missing caveat '{substr}' on {cid}")
    if errors:
        failed += 1
        print(f"FAIL {name}")
        for e in errors:
            print(f"     {e}")
        if "error" not in res:
            print(f"     top3: {[c['chart_id'] for c in res.get('top_charts', [])]}"
                  f" (of {res.get('total_matches')}, relaxed={res.get('relaxed')})")
    else:
        passed += 1
        detail = res.get("error") or [c["chart_id"] for c in res["top_charts"]]
        print(f"PASS {name} -> {detail}")

print(f"\n{passed}/{passed+failed} passed")

# Reachability: every leaf must be attainable by SOME query
print("\n--- reachability ---")
unreached = []
for leaf in cfg["leaves"]:
    q = {"functions": [leaf["functions"][0]]}
    for f in ["num_vars", "cat_vars", "cat_structure", "obs_per_group", "ordered_num",
              "is_relational", "is_spatial", "physical_subject", "spatial_grain"]:
        if f in leaf:
            q[f] = leaf[f]
    for cf, allowed in leaf.get("constraints", {}).items():
        if cf != "max_num_series":
            q[cf] = allowed[0]
    res = select(cfg, q, top_n=99)
    ids = [c["chart_id"] for c in res.get("top_charts", [])]
    if leaf["id"] not in ids:
        unreached.append(leaf["id"])
print("all leaves reachable" if not unreached else f"UNREACHABLE: {unreached}")
