namespace GGNet.ChartSelection;

public static class Profiler
{
	public static JsonObject Profile(IReadOnlyList<double?>? values, IReadOnlyList<string?>? categories)
	{
		var profile = new JsonObject();

		if (values is { Count: > 0 })
		{
			var present = values
				.Where(v => v.HasValue)
				.Select(v => v!.Value)
				.Where(double.IsFinite)
				.ToArray();

			profile["sample_size"] = present.Length;
			profile["completeness"] = (double)present.Length / values.Count;

			// TODO: calibration — |skewness| > 1 is the textbook "highly skewed" cut; tune it against real column samples through ProfilerEvals (SkewedSampleIsDetected, SymmetricSampleClaimsNoShape).
			if (present.Length >= 3 && Math.Abs(Skewness(present)) > 1.0)
			{
				profile["distribution_shape"] = "skewed";
			}
		}

		if (categories is { Count: > 0 })
		{
			var present = categories.Where(c => c is not null).Select(c => c!).ToArray();
			var distinct = present.Distinct().Count();

			if (distinct >= 2)
			{
				profile["cardinality"] = distinct <= 7 ? "low_2_7" : distinct <= 20 ? "medium_8_20" : "high_gt_20";
				profile["obs_per_group"] = present.Length > distinct ? "many" : "one";
			}
		}

		return profile;
	}

	public static JsonObject Apply(JsonObject query, JsonObject profile)
	{
		var merged = query.DeepClone().AsObject();

		foreach (var (field, value) in profile)
		{
			merged[field] = value?.DeepClone();
		}

		return merged;
	}

	private static double Skewness(double[] values)
	{
		var n = values.Length;
		var mean = values.Average();
		var variance = values.Sum(v => (v - mean) * (v - mean)) / n;

		if (variance == 0)
		{
			return 0;
		}

		var sigma = Math.Sqrt(variance);

		return values.Sum(v => Math.Pow((v - mean) / sigma, 3)) / n;
	}
}
