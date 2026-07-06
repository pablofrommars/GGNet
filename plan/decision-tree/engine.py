"""Chart selection engine v4.

The JSON is config; this code IS the protocol. An LLM's only jobs are
(a) building the query object from data + intent, (b) presenting results.

Multi-function: query.functions is a list. A leaf matches if it covers
ANY requested function; leaves covering MORE of them rank higher.
"""
from __future__ import annotations
import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

AXIS_FIELDS = [
    "num_vars", "cat_vars", "cat_structure", "obs_per_group", "ordered_num",
    "is_relational", "is_spatial", "physical_subject", "cardinality",
    "metric_type", "spatial_grain",
]

# A leaf declaring one of these True REQUIRES the query to affirm it.
# Prevents spatial/relational/physical leaves leaking into ordinary queries
# where those fields are simply unknown (null).
GATING_FIELDS = {"is_relational", "is_spatial", "physical_subject"}


@dataclass
class Result:
    chart_id: str
    matched_fields: list[str]
    functions_covered: list[str]
    caveats: list[str] = field(default_factory=list)


def load_config(path: str | Path) -> dict:
    cfg = json.loads(Path(path).read_text())
    _validate_config(cfg)
    return cfg


def _validate_config(cfg: dict) -> None:
    axes = cfg["axes"]
    ids = set()
    for leaf in cfg["leaves"]:
        assert leaf["id"] not in ids, f"duplicate leaf id {leaf['id']}"
        ids.add(leaf["id"])
        for fn in leaf["functions"]:
            assert fn in axes["function"], f"{leaf['id']}: unknown function {fn}"
        for f in AXIS_FIELDS:
            if f in leaf:
                assert leaf[f] in axes[f], f"{leaf['id']}: bad {f}={leaf[f]}"
        for cf, allowed in leaf.get("constraints", {}).items():
            if cf == "max_num_series":
                assert isinstance(allowed, int)
            else:
                assert cf in axes, f"{leaf['id']}: constraint on unknown axis {cf}"
                for v in allowed:
                    assert v in axes[cf], f"{leaf['id']}: bad constraint value {v}"


def normalize_query(cfg: dict, raw: dict) -> dict:
    """Resolve aliases, validate values, coerce functions to a list."""
    q = dict(raw)
    fns = q.get("functions") or ([q["function"]] if q.get("function") else [])
    fns = [cfg["story_need_aliases"].get(f, f) for f in fns]
    for f in fns:
        if f not in cfg["axes"]["function"]:
            raise ValueError(f"unknown function {f!r}")
    if not fns:
        raise ValueError("at least one function is required")
    q["functions"] = list(dict.fromkeys(fns))
    for f in AXIS_FIELDS:
        v = q.get(f)
        if v is not None and v not in cfg["axes"][f]:
            raise ValueError(f"query.{f}={v!r} not in axis enum")
    return q


def _gate(cfg: dict, q: dict) -> list[str]:
    pre, errs = cfg["preconditions"], []
    c, n = q.get("completeness"), q.get("sample_size")
    if c is not None and c < pre["min_completeness"]:
        errs.append(f"completeness {c} < {pre['min_completeness']}")
    if n is not None and n < pre["min_sample_size"]:
        errs.append(f"sample_size {n} < {pre['min_sample_size']}")
    return errs


def _axis_match(leaf: dict, q: dict) -> tuple[bool, list[str]]:
    """Absent leaf field = wildcard; null query field = no constraint."""
    matched = []
    for f in AXIS_FIELDS:
        if f not in leaf:
            continue  # wildcard
        qv = q.get(f)
        if qv is None:
            if f in GATING_FIELDS and leaf[f] is True:
                return False, []  # gate: unknown does not open the door
            continue
        if qv != leaf[f]:
            return False, []
        matched.append(f)
    return True, matched


def _constraints(leaf: dict, q: dict) -> tuple[bool, list[str]]:
    """Allowlists: reject only on non-null query value outside the list."""
    caveats = []
    for cf, allowed in leaf.get("constraints", {}).items():
        if cf == "max_num_series":
            ns = q.get("num_series")
            if ns is None:
                caveats.append("unverified: num_series")
            elif ns > allowed:
                return False, []
        else:
            qv = q.get(cf)
            if qv is None:
                caveats.append(f"unverified: {cf}")
            elif qv not in allowed:
                return False, []
    return True, caveats


def _run_filter(cfg: dict, q: dict) -> list[Result]:
    out = []
    for leaf in cfg["leaves"]:
        covered = [f for f in q["functions"] if f in leaf["functions"]]
        if not covered:
            continue
        ok, matched = _axis_match(leaf, q)
        if not ok:
            continue
        ok, cav = _constraints(leaf, q)
        if not ok:
            continue
        cav = list(leaf.get("caveats", [])) + cav
        shape = q.get("distribution_shape")
        for rule in cfg["shape_caveat_rules"]:
            if rule["shape"] == shape and leaf["id"] in rule["applies_to"]:
                cav.append(rule["caveat"])
        out.append(Result(leaf["id"], matched, covered, cav))
    return out


def select(cfg: dict, raw_query: dict, top_n: int = 3) -> dict:
    q = normalize_query(cfg, raw_query)
    if errs := _gate(cfg, q):
        return {"error": "data_quality_insufficient", "reasons": errs}

    relaxed: list[str] = []
    results = _run_filter(cfg, q)
    for f in cfg["relax_order"]:
        if results:
            break
        if q.get(f) is not None:
            q = {**q, f: None}
            relaxed.append(f)
            results = _run_filter(cfg, q)

    if not results:
        return {"error": "no_match", "relaxed": relaxed,
                "hint": "data shape fits no known chart; check axis extraction"}

    # Rank: functions covered desc, specificity desc, specialization (fewer leaf functions) desc
    by_id = {l["id"]: l for l in cfg["leaves"]}
    results.sort(key=lambda r: (-len(r.functions_covered), -len(r.matched_fields),
                                len(by_id[r.chart_id]["functions"]), r.chart_id))
    return {
        "top_charts": [vars(r) for r in results[:top_n]],
        "total_matches": len(results),
        "relaxed": relaxed,
    }


if __name__ == "__main__":
    import sys
    cfg = load_config(Path(__file__).parent / "chart_selection.json")
    print(json.dumps(select(cfg, json.loads(sys.argv[1])), indent=2))
