namespace GGNet.Evals;

public class ChartSelectionWireEvals
{
	// Fixed responses captured before the selection representation changed. The
	// wire contract has distinct field sets and explicit null stat_bridge values.
	public static TheoryData<string, string, bool> HistoricalResponses => new()
	{
		{
			"""{"functions":["comparison"],"sample_size":2,"completeness":0.5}""",
			"""
			{"error":"data_quality_insufficient","reasons":["completeness 0.5 < 0.8","sample_size 2 < 5"]}
			""",
			false
		},
		{
			"""{"functions":["part_to_whole","trend_over_time"],"num_vars":"many","cat_vars":"1","cat_structure":"flat","obs_per_group":"one","ordered_num":true,"num_series":5}""",
			"""
			{
			  "top_charts":[{
			    "chart_id":"stream_graph",
			    "matched_fields":["num_vars","cat_vars","cat_structure","obs_per_group","ordered_num"],
			    "functions_covered":["part_to_whole","trend_over_time"],
			    "caveats":[],
			    "ggnet":{"supported":false,"alternatives":["stacked_area"],"note":"no wiggle/symmetric baseline"},
			    "stat_bridge":null,
			    "transforms":[]
			  }],
			  "total_matches":1,
			  "relaxed":[],
			  "structural_escapes":[{"chart_id":"stacked_area","grouping":"more than 3 series: keep the recipe, add Facet_Wrap on the series key (small multiples)"}],
			  "excluded":[{"chart_id":"stacked_area","reason":"num_series 5 exceeds max 3"}]
			}
			""",
			false
		},
		{
			"""{"functions":["comparison"],"num_vars":"1","cat_vars":"1","cat_structure":"flat","obs_per_group":"many","ordered_num":false}""",
			"""
			{"error":"no_match","relaxed":["ordered_num","obs_per_group","cat_structure","cat_vars","num_vars"],"structural_escapes":[],"excluded":[],"hint":"data shape fits no known chart; check axis extraction"}
			""",
			true
		}
	};

	[Theory]
	[MemberData(nameof(HistoricalResponses))]
	public void PreservesHistoricalResponses(string queryJson, string expectedJson, bool emptyCatalog)
	{
		// Arrange

		var cfg = Selector.LoadConfig();
		if (emptyCatalog)
		{
			cfg["leaves"] = new JsonArray();
		}

		var query = JsonNode.Parse(queryJson)!.AsObject();
		var originalConfig = cfg.ToJsonString();

		// Act

		var response = Selector.Select(cfg, query, topN: 1).ToJsonString();
		var repeated = Selector.Select(cfg, query, topN: 1).ToJsonString();

		// Assert

		using var _ = new AssertionScope();

		JsonNode.DeepEquals(JsonNode.Parse(response), JsonNode.Parse(expectedJson)).Should().BeTrue(response);
		repeated.Should().Be(response);
		cfg.ToJsonString().Should().Be(originalConfig);
		JsonNode.DeepEquals(query, JsonNode.Parse(queryJson)).Should().BeTrue();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void EmptyRecommendationLimitStillReportsMatches(int topN)
	{
		// Arrange

		var cfg = Selector.LoadConfig();
		var query = JsonNode.Parse("""{"functions":["comparison"]}""")!.AsObject();

		// Act

		var response = Selector.Select(cfg, query, topN);

		// Assert

		using var _ = new AssertionScope();

		response.ContainsKey("error").Should().BeFalse();
		response["top_charts"]!.AsArray().Should().BeEmpty();
		response["total_matches"]!.GetValue<int>().Should().BePositive();
	}
}
