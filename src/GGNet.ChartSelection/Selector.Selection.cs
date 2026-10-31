using System.Text.Json.Serialization;

namespace GGNet.ChartSelection;

public static partial class Selector
{
	[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
	private readonly union Selection(Recommendations, InsufficientData, NoMatch);

	private readonly record struct Recommendations(
		ChartResult[] TopCharts,
		int TotalMatches,
		List<string> Relaxed,
		List<StructuralEscape> StructuralEscapes,
		List<ExcludedChart> Excluded);

	private readonly record struct InsufficientData(List<string> Reasons)
	{
		[JsonPropertyOrder(-1)]
		public string Error { get; } = "data_quality_insufficient";
	}

	private readonly record struct NoMatch(
		List<string> Relaxed,
		List<StructuralEscape> StructuralEscapes,
		List<ExcludedChart> Excluded)
	{
		[JsonPropertyOrder(-1)]
		public string Error { get; } = "no_match";

		public string Hint { get; } = "data shape fits no known chart; check axis extraction";
	}

	private readonly record struct ChartResult(
		string ChartId,
		List<string> MatchedFields,
		List<string> FunctionsCovered,
		List<string> Caveats,
		[property: JsonPropertyName("ggnet")] JsonNode? GGNet,
		string? StatBridge,
		List<string> Transforms);

	private readonly record struct StructuralEscape(string ChartId, JsonNode Grouping);

	private readonly record struct ExcludedChart(string ChartId, string? Reason);

	[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
	[JsonSerializable(typeof(Selection))]
	private sealed partial class SelectionJsonContext : JsonSerializerContext
	{
	}
}
