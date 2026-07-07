namespace GGNet.ChartSelection;

// Deterministic chart-type recommendation. The embedded JSON config is the
// knowledge (axes, leaves, constraints, ggnet renderability blocks); this
// class is the protocol: normalize → gate → filter (stat bridges) →
// constrain (structural escapes) → relax → rank. An agent's only jobs are
// building the query object and presenting results — it never executes
// matching logic itself. Behavior is pinned by GGNet.Evals.
public static class Selector
{
	private static readonly string[] axisFields =
	[
		"num_vars", "cat_vars", "cat_structure", "obs_per_group", "ordered_num",
		"is_relational", "is_spatial", "physical_subject", "cardinality",
		"metric_type", "spatial_grain",
	];

	// A leaf declaring one of these true REQUIRES the query to affirm it —
	// unknown never opens a gated door (keeps spatial/relational/physical
	// leaves out of ordinary queries).
	private static readonly string[] gatingFields = ["is_relational", "is_spatial", "physical_subject"];

	public static JsonObject LoadConfig()
	{
		using var stream = typeof(Selector).Assembly.GetManifestResourceStream("GGNet.ChartSelection.chart_selection.json")
			?? throw new InvalidOperationException("Embedded 'chart_selection.json' not found.");

		var cfg = JsonNode.Parse(stream)!.AsObject();

		Validate(cfg);

		return cfg;
	}

	public static JsonObject Select(JsonObject cfg, JsonObject query, int topN = 3)
	{
		var q = NormalizeQuery(cfg, query);

		var gate = Gate(cfg, q);
		if (gate.Count > 0)
		{
			return new JsonObject
			{
				["error"] = "data_quality_insufficient",
				["reasons"] = new JsonArray([.. gate.Select(r => (JsonNode)r)])
			};
		}

		var relaxed = new List<string>();
		var (candidates, escapes, excluded) = RunFilter(cfg, q);

		foreach (var f in cfg["relax_order"]!.AsArray().Select(n => n!.GetValue<string>()))
		{
			if (candidates.Count > 0)
			{
				break;
			}

			if (q[f] is null)
			{
				continue;
			}

			q[f] = null;
			relaxed.Add(f);
			(candidates, escapes, excluded) = RunFilter(cfg, q);
		}

		if (candidates.Count == 0)
		{
			return new JsonObject
			{
				["error"] = "no_match",
				["relaxed"] = ToArray(relaxed),
				["structural_escapes"] = escapes,
				["excluded"] = excluded,
				["hint"] = "data shape fits no known chart; check axis extraction"
			};
		}

		var ranked = candidates
			.OrderByDescending(c => c.Covered.Count)
			.ThenByDescending(c => c.Matched.Count)
			.ThenBy(c => c.LeafFunctionCount)
			.ThenBy(c => c.ChartId, StringComparer.Ordinal);

		return new JsonObject
		{
			["top_charts"] = new JsonArray([.. ranked.Take(topN).Select(ToResult)]),
			["total_matches"] = candidates.Count,
			["relaxed"] = ToArray(relaxed),
			["structural_escapes"] = escapes,
			["excluded"] = excluded,
		};
	}

	private sealed record Candidate(
		string ChartId,
		List<string> Matched,
		List<string> Covered,
		List<string> Caveats,
		JsonNode? GGNet,
		string? StatBridge,
		List<string> Transforms,
		int LeafFunctionCount);

	private static (List<Candidate> Candidates, JsonArray Escapes, JsonArray Excluded) RunFilter(JsonObject cfg, JsonObject q)
	{
		var candidates = new List<Candidate>();
		var escapes = new JsonArray();
		var excluded = new JsonArray();
		var functions = q["functions"]!.AsArray().Select(n => n!.GetValue<string>()).ToList();
		var shape = q["distribution_shape"]?.GetValue<string>();

		foreach (var leaf in cfg["leaves"]!.AsArray().Select(n => n!.AsObject()))
		{
			var id = leaf["id"]!.GetValue<string>();
			var leafFunctions = leaf["functions"]!.AsArray().Select(n => n!.GetValue<string>()).ToList();
			var covered = functions.Where(leafFunctions.Contains).ToList();
			if (covered.Count == 0)
			{
				continue;
			}

			var (ok, matched, mismatched) = AxisMatch(leaf, q);
			string? statBridge = null;
			if (!ok)
			{
				statBridge = Bridge(leaf, q, mismatched);
				if (statBridge is null)
				{
					continue;
				}
			}

			var (allowed, caveats, reason) = Constraints(leaf, q);
			if (!allowed)
			{
				// The leaf matched intent and shape but broke a limit: report
				// it as excluded-with-reason (never silently dropped), and
				// when a structural way out exists (facet the same recipe),
				// as an escape too.
				excluded.Add(new JsonObject
				{
					["chart_id"] = id,
					["reason"] = reason
				});

				if (leaf["ggnet"]?["grouping"] is JsonNode grouping)
				{
					escapes.Add(new JsonObject
					{
						["chart_id"] = id,
						["grouping"] = grouping.DeepClone()
					});
				}

				continue;
			}

			if (leaf["caveats"] is JsonArray declared)
			{
				caveats.InsertRange(0, declared.Select(n => n!.GetValue<string>()));
			}

			var transforms = new List<string>();
			foreach (var rule in cfg["shape_caveat_rules"]!.AsArray().Select(n => n!.AsObject()))
			{
				if (rule["shape"]!.GetValue<string>() == shape
					&& rule["applies_to"]!.AsArray().Any(n => n!.GetValue<string>() == id))
				{
					caveats.Add(rule["caveat"]!.GetValue<string>());
					if (rule["transform"] is JsonNode transform)
					{
						transforms.Add(transform.GetValue<string>());
					}
				}
			}

			candidates.Add(new Candidate(id, matched, covered, caveats,
				leaf["ggnet"], statBridge, transforms, leafFunctions.Count));
		}

		return (candidates, escapes, excluded);
	}

	// Absent leaf field = wildcard; null query field = no constraint. Collects
	// every mismatched field (rather than failing fast) so the caller can
	// decide whether a stat bridge covers the whole mismatch. Gating stays a
	// hard fail: unknown never opens a gated door, bridged or not.
	private static (bool Ok, List<string> Matched, List<string> Mismatched) AxisMatch(JsonObject leaf, JsonObject q)
	{
		List<string> matched = [], mismatched = [];

		foreach (var f in axisFields)
		{
			if (!leaf.ContainsKey(f))
			{
				continue;
			}

			var qv = q[f];
			if (qv is null)
			{
				if (gatingFields.Contains(f) && leaf[f]?.GetValueKind() == JsonValueKind.True)
				{
					return (false, [], []);
				}

				continue;
			}

			if (JsonNode.DeepEquals(qv, leaf[f]))
			{
				matched.Add(f);
			}
			else
			{
				mismatched.Add(f);
			}
		}

		return (mismatched.Count == 0, matched, mismatched);
	}

	// Allowlists: reject only on a non-null query value outside the list.
	// A rejection carries its reason so it can be reported, not silently
	// dropped — the caller's refusal is only as strong as its explanation.
	private static (bool Ok, List<string> Caveats, string? Reason) Constraints(JsonObject leaf, JsonObject q)
	{
		var caveats = new List<string>();

		if (leaf["constraints"] is not JsonObject constraints)
		{
			return (true, caveats, null);
		}

		foreach (var (cf, allowed) in constraints)
		{
			if (cf == "max_num_series")
			{
				if (q["num_series"] is not JsonNode series)
				{
					caveats.Add("unverified: num_series");
				}
				else if (series.GetValue<int>() > allowed!.GetValue<int>())
				{
					return (false, [], $"num_series {series} exceeds max {allowed}");
				}
			}
			else if (q[cf] is not JsonNode qv)
			{
				caveats.Add($"unverified: {cf}");
			}
			else if (!Contains(allowed!.AsArray(), qv))
			{
				return (false, [], $"{cf} {qv.ToJsonString()} outside allowed {allowed!.ToJsonString()}");
			}
		}

		return (true, caveats, null);
	}

	// A stat bridge admits the leaf when it covers the ENTIRE axis mismatch:
	// the query's raw shape is exactly what the declared Stat.* pre-processing
	// turns into the leaf's expected shape.
	private static string? Bridge(JsonObject leaf, JsonObject q, List<string> mismatched)
	{
		if (leaf["ggnet"]?["stat_bridge"] is not JsonObject bridge)
		{
			return null;
		}

		var when = bridge["when"]!.AsObject();
		if (!mismatched.All(when.ContainsKey))
		{
			return null;
		}

		if (!when.All(kv => JsonNode.DeepEquals(q[kv.Key], kv.Value)))
		{
			return null;
		}

		return "pre-process with " + string.Join(" or ", bridge["via"]!.AsArray().Select(n => n!.GetValue<string>()));
	}

	private static JsonObject NormalizeQuery(JsonObject cfg, JsonObject raw)
	{
		var q = raw.DeepClone().AsObject();
		var axes = cfg["axes"]!.AsObject();
		var aliases = cfg["story_need_aliases"]!.AsObject();

		var functions = new List<string>();
		if (q["functions"] is JsonArray many)
		{
			functions.AddRange(many.Select(n => n!.GetValue<string>()));
		}
		else if (q["function"] is JsonNode single)
		{
			functions.Add(single.GetValue<string>());
		}

		functions = [.. functions.Select(f => aliases[f]?.GetValue<string>() ?? f)];
		if (functions.Count == 0)
		{
			throw new ArgumentException("At least one function is required.");
		}

		var known = axes["function"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet();
		foreach (var f in functions)
		{
			if (!known.Contains(f))
			{
				throw new ArgumentException($"Unknown function '{f}'.");
			}
		}

		q["functions"] = new JsonArray([.. functions.Distinct().Select(f => (JsonNode)f)]);

		foreach (var f in axisFields)
		{
			if (q[f] is JsonNode v && !Contains(axes[f]!.AsArray(), v))
			{
				throw new ArgumentException($"query.{f}={v} not in axis enum.");
			}
		}

		return q;
	}

	private static List<string> Gate(JsonObject cfg, JsonObject q)
	{
		var pre = cfg["preconditions"]!.AsObject();
		var errors = new List<string>();

		if (q["completeness"] is JsonNode completeness
			&& completeness.GetValue<double>() < pre["min_completeness"]!.GetValue<double>())
		{
			errors.Add($"completeness {completeness} < {pre["min_completeness"]}");
		}

		if (q["sample_size"] is JsonNode sampleSize
			&& sampleSize.GetValue<double>() < pre["min_sample_size"]!.GetValue<double>())
		{
			errors.Add($"sample_size {sampleSize} < {pre["min_sample_size"]}");
		}

		return errors;
	}

	private static void Validate(JsonObject cfg)
	{
		var axes = cfg["axes"]!.AsObject();
		var leaves = cfg["leaves"]!.AsArray().Select(n => n!.AsObject()).ToList();
		var ids = new HashSet<string>();
		var supported = leaves
			.Where(l => l["ggnet"]?["supported"]?.GetValueKind() == JsonValueKind.True)
			.Select(l => l["id"]!.GetValue<string>())
			.ToHashSet();

		foreach (var leaf in leaves)
		{
			var id = leaf["id"]!.GetValue<string>();
			if (!ids.Add(id))
			{
				throw new InvalidOperationException($"Duplicate leaf id '{id}'.");
			}

			foreach (var fn in leaf["functions"]!.AsArray())
			{
				if (!Contains(axes["function"]!.AsArray(), fn))
				{
					throw new InvalidOperationException($"{id}: unknown function {fn}.");
				}
			}

			foreach (var f in axisFields)
			{
				if (leaf.ContainsKey(f) && !Contains(axes[f]!.AsArray(), leaf[f]))
				{
					throw new InvalidOperationException($"{id}: bad {f}={leaf[f]}.");
				}
			}

			if (leaf["constraints"] is JsonObject constraints)
			{
				foreach (var (cf, allowed) in constraints)
				{
					if (cf == "max_num_series")
					{
						if (allowed?.GetValueKind() != JsonValueKind.Number)
						{
							throw new InvalidOperationException($"{id}: max_num_series must be a number.");
						}
					}
					else if (!axes.ContainsKey(cf))
					{
						throw new InvalidOperationException($"{id}: constraint on unknown axis {cf}.");
					}
					else
					{
						foreach (var v in allowed!.AsArray())
						{
							if (!Contains(axes[cf]!.AsArray(), v))
							{
								throw new InvalidOperationException($"{id}: bad constraint value {v}.");
							}
						}
					}
				}
			}

			if (leaf["ggnet"] is not JsonObject ggnet)
			{
				throw new InvalidOperationException($"{id}: missing ggnet block.");
			}

			if (ggnet["supported"] is not JsonNode isSupported
				|| isSupported.GetValueKind() is not (JsonValueKind.True or JsonValueKind.False))
			{
				throw new InvalidOperationException($"{id}: ggnet.supported must be a boolean.");
			}

			if (ggnet["alternatives"] is JsonArray alternatives)
			{
				foreach (var alt in alternatives.Select(n => n!.GetValue<string>()))
				{
					if (!supported.Contains(alt))
					{
						throw new InvalidOperationException($"{id}: alternative '{alt}' is not a supported leaf.");
					}
				}
			}

			if (ggnet["stat_bridge"] is JsonObject bridge)
			{
				foreach (var (bf, _) in bridge["when"]!.AsObject())
				{
					if (!axisFields.Contains(bf))
					{
						throw new InvalidOperationException($"{id}: stat_bridge on unknown axis {bf}.");
					}
				}
			}
		}
	}

	private static JsonObject ToResult(Candidate c)
		=> new()
		{
			["chart_id"] = c.ChartId,
			["matched_fields"] = ToArray(c.Matched),
			["functions_covered"] = ToArray(c.Covered),
			["caveats"] = ToArray(c.Caveats),
			["ggnet"] = c.GGNet?.DeepClone(),
			["stat_bridge"] = c.StatBridge,
			["transforms"] = ToArray(c.Transforms),
		};

	private static JsonArray ToArray(List<string> values)
		=> new([.. values.Select(v => (JsonNode)v)]);

	private static bool Contains(JsonArray values, JsonNode? value)
		=> values.Any(v => JsonNode.DeepEquals(v, value));
}
