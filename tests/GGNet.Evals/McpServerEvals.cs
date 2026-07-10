namespace GGNet.Evals;

// End-to-end contract walk over the real stdio server (spawned via dotnet
// run, exercising the same path a registered agent uses): tool discovery,
// selection with measurement override, live introspection, validation.
public class McpServerFixture : IAsyncLifetime
{
	public McpClient Client { get; private set; } = null!;

	public async Task InitializeAsync()
	{
		var project = Path.Combine(RepoRoot(), "src", "GGNet.Mcp");

		var transport = new StdioClientTransport(new StdioClientTransportOptions
		{
			Name = "GGNet.Evals",
			Command = "dotnet",
			Arguments = ["run", "--project", project],
		});

		Client = await McpClient.CreateAsync(transport);
	}

	public async Task DisposeAsync()
	{
		await Client.DisposeAsync();
	}

	private static string RepoRoot([CallerFilePath] string path = "")
		=> Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));
}

public class McpServerEvals(McpServerFixture fixture) : IClassFixture<McpServerFixture>
{
	[Fact]
	public async Task ListsExpectedTools()
	{
		// Arrange / Act

		var tools = await fixture.Client.ListToolsAsync();

		// Assert

		var names = tools.Select(t => t.Name).ToArray();

		names.Should().Contain(["select_chart", "list_charts", "explain_axes", "list_geoms", "list_scales", "validate_plot"]);
	}

	[Fact]
	public async Task MeasuredCardinalityBeatsSuppliedOne()
	{
		// Arrange

		// The caller claims a pie-friendly cardinality; the raw column says 25
		// distinct categories (one row each, so the aggregated shape holds) —
		// measurement wins and pie lands in excluded with the cardinality reason.
		var categories = Enumerable.Range(0, 25).Select(i => $"c{i}").ToArray();

		// Act

		var result = await CallAsJson("select_chart", new Dictionary<string, object?>
		{
			["query"] = """{"functions": ["part_to_whole"], "num_vars": "1", "cat_vars": "1", "cat_structure": "flat", "ordered_num": false, "cardinality": "low_2_7"}""",
			["categories"] = categories,
			["topN"] = 99,
		});

		// Assert

		var excluded = result["excluded"]!.AsArray()
			.Select(n => n!.AsObject())
			.FirstOrDefault(o => o["chart_id"]!.GetValue<string>() == "pie");

		using var _ = new AssertionScope();

		excluded.Should().NotBeNull();
		excluded!["reason"]!.GetValue<string>().Should().Contain("cardinality");
	}

	[Fact]
	public async Task StatBridgeSurfacesOverTheWire()
	{
		// Arrange / Act

		var result = await CallAsJson("select_chart", new Dictionary<string, object?>
		{
			["query"] = """{"functions": ["comparison"], "num_vars": "1", "cat_vars": "1", "cat_structure": "flat", "obs_per_group": "many", "ordered_num": false}""",
			["topN"] = 99,
		});

		// Assert

		var barplot = result["top_charts"]!.AsArray()
			.Select(n => n!.AsObject())
			.First(o => o["chart_id"]!.GetValue<string>() == "barplot");

		barplot["stat_bridge"]!.GetValue<string>().Should().Contain("Stat.Count");
	}

	[Fact]
	public async Task IntrospectionIsLive()
	{
		// Arrange / Act

		var geoms = await CallAsJson("list_geoms", []);
		var scales = await CallAsJson("list_scales", []);

		// Assert

		using var _ = new AssertionScope();

		geoms.ContainsKey("Geom_Point").Should().BeTrue();
		geoms.ContainsKey("Geom_Candlestick").Should().BeTrue();
		scales.ContainsKey("Scale_Y_Log10").Should().BeTrue();
		scales.ContainsKey("Facet_Wrap").Should().BeTrue();
	}

	[Fact]
	public async Task ValidatePlotCompilesAndRenders()
	{
		// Arrange / Act

		var result = await CallAsJson("validate_plot", new Dictionary<string, object?>
		{
			["snippet"] =
				"""
				var xy = new[] { (X: 1.0, Y: 2.0), (X: 2.0, Y: 3.5) };
				var plot = PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Point().Style();
				Console.WriteLine((await plot.AsStringAsync()).Length);
				""",
		});

		// Assert

		result["ok"]!.GetValue<bool>().Should().BeTrue(result["errors"]?.GetValue<string>());
	}

	private async Task<JsonObject> CallAsJson(string tool, Dictionary<string, object?> args)
	{
		var result = await fixture.Client.CallToolAsync(tool, args);

		var text = result.Content.OfType<TextContentBlock>().First().Text;

		return JsonNode.Parse(text)!.AsObject();
	}
}
