namespace GGNet.Evals;

// The profiler is the anti-gaming half of the data-quality gate: objective
// shape fields measured from raw samples, overriding whatever the caller
// supplied (plan/PLAN.md 2.2).
public class ProfilerEvals
{
	[Fact]
	public void SkewedSampleIsDetected()
	{
		var rng = new Random(7);
		var lognormal = Enumerable.Range(0, 500)
			.Select(_ => (double?)Math.Exp(rng.NextDouble() * 3))
			.ToArray();

		var profile = Profiler.Profile(lognormal, null);

		using var _ = new AssertionScope();

		profile["distribution_shape"]!.GetValue<string>().Should().Be("skewed");
		profile["sample_size"]!.GetValue<int>().Should().Be(500);
		profile["completeness"]!.GetValue<double>().Should().Be(1.0);
	}

	[Fact]
	public void SymmetricSampleClaimsNoShape()
	{
		var symmetric = Enumerable.Range(0, 100)
			.Select(i => (double?)Math.Sin(i * 0.7))
			.ToArray();

		var profile = Profiler.Profile(symmetric, null);

		profile.ContainsKey("distribution_shape").Should().BeFalse();
	}

	[Fact]
	public void MissingValuesLowerCompleteness()
	{
		double?[] values = [1.0, null, 2.0, null, 3.0, 4.0, null, 5.0, 6.0, 7.0];

		var profile = Profiler.Profile(values, null);

		using var _ = new AssertionScope();

		profile["completeness"]!.GetValue<double>().Should().Be(0.7);
		profile["sample_size"]!.GetValue<int>().Should().Be(7);
	}

	[Theory]
	[InlineData(3, "low_2_7")]
	[InlineData(12, "medium_8_20")]
	[InlineData(25, "high_gt_20")]
	public void CardinalityBuckets(int distinct, string expected)
	{
		var categories = Enumerable.Range(0, distinct * 4)
			.Select(i => (string?)$"cat-{i % distinct}")
			.ToArray();

		var profile = Profiler.Profile(null, categories);

		using var _ = new AssertionScope();

		profile["cardinality"]!.GetValue<string>().Should().Be(expected);
		profile["obs_per_group"]!.GetValue<string>().Should().Be("many");
	}

	[Fact]
	public void OneRowPerCategoryIsAggregated()
	{
		string?[] categories = ["a", "b", "c", "d"];

		var profile = Profiler.Profile(null, categories);

		profile["obs_per_group"]!.GetValue<string>().Should().Be("one");
	}

	[Fact]
	public void MeasuredFieldsOverrideSuppliedOnes()
	{
		// The gaming scenario: the caller claims a pie-friendly cardinality,
		// the raw column says otherwise — measurement wins.
		var query = JsonNode.Parse("""{"functions": ["part_to_whole"], "cardinality": "low_2_7"}""")!.AsObject();
		var categories = Enumerable.Range(0, 50).Select(i => (string?)$"c{i % 25}").ToArray();

		var merged = Profiler.Apply(query, Profiler.Profile(null, categories));

		merged["cardinality"]!.GetValue<string>().Should().Be("high_gt_20");
	}
}
