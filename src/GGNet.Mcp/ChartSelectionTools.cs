namespace GGNet.Mcp;

[McpServerToolType]
public static class ChartSelectionTools
{
	// Parsed and validated once; the stdio server handles one client and the
	// engine only ever reads the config (queries are cloned per call).
	private static readonly Lazy<JsonObject> config = new(Selector.LoadConfig);

	[McpServerTool(Name = "select_chart"), Description(
		"Deterministic chart-type recommendation for GGNet. Build the query from the data's shape and the analytical intent; unknown fields are omitted, never guessed. Results carry per-chart ggnet blocks (recipe or alternatives), stat bridges, transforms, structural escapes, and excluded-with-reason entries — present caveats verbatim and do not override them. Pass raw samples when available: measured fields beat estimated ones.")]
	public static string SelectChart(
		[Description("""Query JSON. Required: "functions" (1+ of comparison|correlation|distribution|part_to_whole|trend_over_time; aliases resolve). Optional shape fields: num_vars, cat_vars, cat_structure, obs_per_group, ordered_num, is_relational, is_spatial, physical_subject, cardinality, metric_type, spatial_grain, num_series, sample_size, completeness, distribution_shape.""")] string query,
		[Description("Optional raw numeric samples of the main measure (nulls allowed). When given, sample_size, completeness and distribution_shape are measured server-side and override the query.")] double?[]? values = null,
		[Description("Optional raw category values of the main grouping (nulls allowed). When given, cardinality and obs_per_group are measured server-side and override the query.")] string?[]? categories = null,
		[Description("Number of ranked recommendations to return.")] int topN = 3)
	{
		var q = JsonNode.Parse(query)!.AsObject();

		if (values is not null || categories is not null)
		{
			q = Profiler.Apply(q, Profiler.Profile(values, categories));
		}

		return Selector.Select(config.Value, q, topN).ToJsonString();
	}

	[McpServerTool(Name = "list_charts"), Description(
		"Catalog of all chart types the selector knows, with their functions and GGNet renderability (supported + recipe, or alternatives). Optionally filtered by function.")]
	public static string ListCharts(
		[Description("Optional function filter: comparison|correlation|distribution|part_to_whole|trend_over_time.")] string? function = null)
	{
		var leaves = config.Value["leaves"]!.AsArray()
			.Select(n => n!.AsObject())
			.Where(l => function is null || l["functions"]!.AsArray().Any(f => f!.GetValue<string>() == function))
			.Select(l => new JsonObject
			{
				["id"] = l["id"]!.GetValue<string>(),
				["functions"] = l["functions"]!.DeepClone(),
				["ggnet"] = l["ggnet"]!.DeepClone(),
			});

		return new JsonArray([.. leaves]).ToJsonString();
	}

	[McpServerTool(Name = "explain_axes"), Description(
		"The axis vocabulary the query is built from (every field's allowed values) plus extraction hints for mapping columns to fields. Call this first when the column mapping is unclear.")]
	public static string ExplainAxes()
	{
		return new JsonObject
		{
			["axes"] = config.Value["axes"]!.DeepClone(),
			["aliases"] = config.Value["story_need_aliases"]!.DeepClone(),
			["hints"] = new JsonArray(
				"date/timestamp column used as axis -> ordered_num: true (do NOT count it as a categorical)",
				"month/weekday as grouping, not sequence -> categorical; structure per nesting",
				"one row per group vs raw observations -> obs_per_group: one|many",
				"distinct values of the main categorical: 2-7 low_2_7, 8-20 medium_8_20, >20 high_gt_20",
				"geo codes / lat-lon / regions as unit -> is_spatial: true; polygons->region, coords->point, origin-dest->flow",
				"metric is a count of things -> metric_type: count (blocks choropleth; use rates there)",
				"ambiguous signal -> leave the field unset; unknown never disqualifies, it caveats"),
		}.ToJsonString();
	}
}
