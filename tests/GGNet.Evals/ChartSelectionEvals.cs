namespace GGNet.Evals;

// Behavior pin for the chart-selection engine: 22 realistic queries ported
// from the retired Python eval suite. Each case asserts charts expected in
// top-3, charts that must not appear, and the v5 ggnet behaviors
// (alternatives, structural escapes, stat bridges, transforms).
public class ChartSelectionEvals
{
	private static readonly JsonObject cfg = Selector.LoadConfig();

	private sealed record Case(
		string Query,
		string[]? ExpectAny = null,
		string[]? Forbid = null,
		string? ExpectError = null,
		(string Chart, string Fragment)? CaveatOn = null,
		string? AlternativesOn = null,
		string? EscapeOn = null,
		(string Chart, string Fragment)? BridgeOn = null,
		(string Chart, string Fragment)? TransformOn = null,
		(string Chart, string Fragment)? ExcludedOn = null);

	private static readonly Dictionary<string, Case> cases = new()
	{
		["revenue by 5 business units"] = new(
			"""{"functions": ["comparison"], "num_vars": "1", "cat_vars": "1", "cat_structure": "flat", "obs_per_group": "one", "ordered_num": false, "cardinality": "low_2_7"}""",
			ExpectAny: ["barplot", "lollipop", "dot_plot"], Forbid: ["pie"]),

		["market share of 4 competitors"] = new(
			"""{"functions": ["part_to_whole"], "num_vars": "1", "cat_vars": "1", "cat_structure": "flat", "obs_per_group": "one", "ordered_num": false, "cardinality": "low_2_7"}""",
			ExpectAny: ["pie", "doughnut", "waffle", "barplot"], AlternativesOn: "pie"),

		["market share of 25 competitors — pie must be excluded"] = new(
			"""{"functions": ["part_to_whole"], "num_vars": "1", "cat_vars": "1", "cat_structure": "flat", "obs_per_group": "one", "ordered_num": false, "cardinality": "high_gt_20"}""",
			ExpectAny: ["barplot"], Forbid: ["pie", "doughnut", "waffle"],
			ExcludedOn: ("pie", "cardinality")),

		["salary distribution across 6 departments"] = new(
			"""{"functions": ["distribution"], "num_vars": "1", "cat_vars": "1", "cat_structure": "flat", "obs_per_group": "many", "ordered_num": false}""",
			ExpectAny: ["boxplot", "violin", "ridgeline", "beeswarm"]),

		["bimodal latency distribution per region — boxplot must carry caveat"] = new(
			"""{"functions": ["distribution"], "num_vars": "1", "cat_vars": "1", "cat_structure": "flat", "obs_per_group": "many", "ordered_num": false, "distribution_shape": "bimodal"}""",
			ExpectAny: ["violin", "ridgeline"], CaveatOn: ("boxplot", "bimodality")),

		["ARR vs headcount across portfolio companies"] = new(
			"""{"functions": ["correlation"], "num_vars": "2", "cat_vars": "0", "cat_structure": "none", "obs_per_group": "many", "ordered_num": false}""",
			ExpectAny: ["scatter", "density_2d"]),

		["MRR trend, single series, monthly"] = new(
			"""{"functions": ["trend_over_time"], "num_vars": "1", "cat_vars": "0", "cat_structure": "none", "obs_per_group": "one", "ordered_num": true}""",
			ExpectAny: ["area", "line"]),

		["daily active users over 2 years — seasonality"] = new(
			"""{"functions": ["trend_over_time"], "num_vars": "1", "cat_vars": "0", "cat_structure": "none", "obs_per_group": "one", "ordered_num": true}""",
			ExpectAny: ["calendar_heatmap", "area"]),

		["revenue mix over time, 5 product lines — stacked_area must be excluded"] = new(
			"""{"functions": ["part_to_whole", "trend_over_time"], "num_vars": "many", "cat_vars": "1", "cat_structure": "flat", "obs_per_group": "one", "ordered_num": true, "num_series": 5}""",
			ExpectAny: ["stream_graph", "small_multiples"], Forbid: ["stacked_area"], EscapeOn: "stacked_area",
			ExcludedOn: ("stacked_area", "num_series")),

		["revenue mix over time, 3 product lines — stacked_area allowed"] = new(
			"""{"functions": ["part_to_whole", "trend_over_time"], "num_vars": "many", "cat_vars": "1", "cat_structure": "flat", "obs_per_group": "one", "ordered_num": true, "num_series": 3}""",
			ExpectAny: ["stacked_area"]),

		["NPS before vs after per segment"] = new(
			"""{"functions": ["comparison"], "num_vars": "2", "cat_vars": "1", "cat_structure": "flat", "obs_per_group": "one", "ordered_num": false}""",
			ExpectAny: ["dumbbell"]),

		["exact KPI values for 10 companies, several metrics"] = new(
			"""{"functions": ["comparison"], "num_vars": "many", "cat_vars": "1", "cat_structure": "flat", "obs_per_group": "one", "ordered_num": false, "cardinality": "medium_8_20"}""",
			ExpectAny: ["table", "parallel_plot"], Forbid: ["radar"]),

		["org budget hierarchy, one value per leaf"] = new(
			"""{"functions": ["part_to_whole"], "num_vars": "1", "cat_vars": "many", "cat_structure": "nested", "obs_per_group": "one", "ordered_num": false}""",
			ExpectAny: ["treemap", "sunburst"]),

		["unemployment RATE by county"] = new(
			"""{"functions": ["comparison"], "is_spatial": true, "spatial_grain": "region", "metric_type": "rate"}""",
			ExpectAny: ["choropleth"]),

		["store COUNT by county — choropleth must be excluded"] = new(
			"""{"functions": ["comparison"], "is_spatial": true, "metric_type": "count"}""",
			ExpectAny: ["proportional_symbol_map"], Forbid: ["choropleth"]),

		["migration flows between cities"] = new(
			"""{"functions": ["trend_over_time"], "is_spatial": true, "spatial_grain": "flow"}""",
			ExpectAny: ["flow_map"]),

		["capital flows between fund stages"] = new(
			"""{"functions": ["part_to_whole"], "is_relational": true}""",
			ExpectAny: ["sankey", "chord"]),

		["co-investment network among 40 VCs — chord must be excluded"] = new(
			"""{"functions": ["correlation"], "is_relational": true, "cardinality": "high_gt_20"}""",
			ExpectAny: ["network", "arc"], Forbid: ["chord"]),

		["multi-intent: compare segments AND show trend"] = new(
			"""{"functions": ["comparison", "trend_over_time"], "num_vars": "1", "cat_vars": "1", "cat_structure": "flat", "obs_per_group": "one", "ordered_num": true}""",
			ExpectAny: ["slope"]),

		["garbage data gate"] = new(
			"""{"functions": ["comparison"], "sample_size": 2}""",
			ExpectError: "data_quality_insufficient"),

		["compare tanks from raw readings — barplot reachable via stat bridge"] = new(
			"""{"functions": ["comparison"], "num_vars": "1", "cat_vars": "1", "cat_structure": "flat", "obs_per_group": "many", "ordered_num": false}""",
			ExpectAny: ["boxplot", "violin"], BridgeOn: ("barplot", "Stat.Count")),

		["skewed response-time histogram — concrete log-scale call attached"] = new(
			"""{"functions": ["distribution"], "num_vars": "1", "cat_vars": "0", "cat_structure": "none", "obs_per_group": "many", "ordered_num": false, "distribution_shape": "skewed"}""",
			ExpectAny: ["histogram", "density"], TransformOn: ("histogram", "Log10")),
	};

	public static TheoryData<string> CaseNames => [.. cases.Keys];

	[Theory]
	[MemberData(nameof(CaseNames))]
	public void Select(string name)
	{
		var c = cases[name];
		var query = JsonNode.Parse(c.Query)!.AsObject();

		var result = Selector.Select(cfg, query);

		using var _ = new AssertionScope();

		if (c.ExpectError is not null)
		{
			result["error"]?.GetValue<string>().Should().Be(c.ExpectError);
			result.ContainsKey("error").Should().BeTrue();
			return;
		}

		result.ContainsKey("error").Should().BeFalse($"unexpected error: {result["error"]}");

		var top = result["top_charts"]!.AsArray()
			.Select(n => n!["chart_id"]!.GetValue<string>())
			.ToArray();

		if (c.ExpectAny is not null)
		{
			top.Should().Contain(chart => c.ExpectAny.Contains(chart));
		}

		if (c.Forbid is not null)
		{
			top.Should().NotContain(chart => c.Forbid.Contains(chart));
		}

		if (c.CaveatOn is (var caveatChart, var caveatFragment))
		{
			var hit = Hit(c.Query, caveatChart);

			hit.Should().NotBeNull();
			hit!["caveats"]!.AsArray().Select(n => n!.GetValue<string>())
				.Should().Contain(caveat => caveat.Contains(caveatFragment));
		}

		if (c.AlternativesOn is not null)
		{
			var hit = Hit(c.Query, c.AlternativesOn);

			hit.Should().NotBeNull();
			hit!["ggnet"]!["supported"]!.GetValue<bool>().Should().BeFalse();
			hit["ggnet"]!["alternatives"]!.AsArray().Should().NotBeEmpty();
		}

		if (c.EscapeOn is not null)
		{
			result["structural_escapes"]!.AsArray()
				.Select(n => n!["chart_id"]!.GetValue<string>())
				.Should().Contain(c.EscapeOn);
		}

		if (c.BridgeOn is (var bridgeChart, var bridgeFragment))
		{
			var hit = Hit(c.Query, bridgeChart);

			hit.Should().NotBeNull();
			hit!["stat_bridge"]!.GetValue<string>().Should().Contain(bridgeFragment);
		}

		if (c.ExcludedOn is (var excludedChart, var excludedFragment))
		{
			var entry = result["excluded"]!.AsArray()
				.Select(n => n!.AsObject())
				.FirstOrDefault(o => o["chart_id"]!.GetValue<string>() == excludedChart);

			entry.Should().NotBeNull();
			entry!["reason"]!.GetValue<string>().Should().Contain(excludedFragment);
		}

		if (c.TransformOn is (var transformChart, var transformFragment))
		{
			var hit = Hit(c.Query, transformChart);

			hit.Should().NotBeNull();
			hit!["transforms"]!.AsArray().Select(n => n!.GetValue<string>())
				.Should().Contain(transform => transform.Contains(transformFragment));
		}
	}

	private static JsonObject? Hit(string query, string chartId)
		=> Selector.Select(cfg, JsonNode.Parse(query)!.AsObject(), topN: 99)["top_charts"]!.AsArray()
			.Select(n => n!.AsObject())
			.FirstOrDefault(o => o["chart_id"]!.GetValue<string>() == chartId);

	// Every leaf must be attainable by SOME query built from its own fields.
	[Fact]
	public void EveryLeafReachable()
	{
		string[] reachFields =
		[
			"num_vars", "cat_vars", "cat_structure", "obs_per_group", "ordered_num",
			"is_relational", "is_spatial", "physical_subject", "spatial_grain",
		];

		var unreached = new List<string>();

		foreach (var leaf in cfg["leaves"]!.AsArray().Select(n => n!.AsObject()))
		{
			var query = new JsonObject
			{
				["functions"] = new JsonArray(leaf["functions"]![0]!.DeepClone())
			};

			foreach (var f in reachFields)
			{
				if (leaf.ContainsKey(f))
				{
					query[f] = leaf[f]!.DeepClone();
				}
			}

			if (leaf["constraints"] is JsonObject constraints)
			{
				foreach (var (cf, allowed) in constraints)
				{
					if (cf != "max_num_series")
					{
						query[cf] = allowed![0]!.DeepClone();
					}
				}
			}

			var ids = Selector.Select(cfg, query, topN: 99)["top_charts"]?.AsArray()
				.Select(n => n!["chart_id"]!.GetValue<string>())
				.ToList() ?? [];

			if (!ids.Contains(leaf["id"]!.GetValue<string>()))
			{
				unreached.Add(leaf["id"]!.GetValue<string>());
			}
		}

		unreached.Should().BeEmpty();
	}

	// Every ggnet recipe must resolve to a skill example file — the ids are
	// shared vocabulary between the selector and skills/ggnet/examples.
	[Fact]
	public void RecipesResolveToExampleFiles()
	{
		var examples = Directory.GetFiles(Path.Combine(RepoRoot(), "skills", "ggnet", "examples"), "*.md")
			.Select(Path.GetFileNameWithoutExtension)
			.ToHashSet();

		using var _ = new AssertionScope();

		foreach (var leaf in cfg["leaves"]!.AsArray().Select(n => n!.AsObject()))
		{
			if (leaf["ggnet"]!["recipe"] is JsonNode recipe)
			{
				examples.Should().Contain(recipe.GetValue<string>(),
					$"leaf '{leaf["id"]}' names it as its recipe");
			}
		}
	}

	// Config-load validation (block presence, alternatives→supported, bridge
	// axes) runs inside LoadConfig; this pins that the embedded config passes.
	[Fact]
	public void ConfigLoadsAndValidates()
	{
		var loaded = Selector.LoadConfig();

		loaded["leaves"]!.AsArray().Should().HaveCount(47);
	}

	private static string RepoRoot([CallerFilePath] string path = "")
		=> Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));
}
