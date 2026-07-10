namespace GGNet.Evals;

// The skill's chart-selection reference was generated from the config once,
// then hand-edited — this gate keeps the two from drifting: every table row
// must name a real leaf, use only resolvable function terms, and agree with
// the config on supported/unsupported status.
public class SkillDocDriftEvals
{
	private static readonly JsonObject cfg = Selector.LoadConfig();

	[Fact]
	public void ChartTableAgreesWithConfig()
	{
		// Arrange

		var reference = File.ReadAllText(Path.Combine(RepoRoot(), "skills", "ggnet", "reference", "chart-selection.md"));

		var leaves = cfg["leaves"]!.AsArray()
			.Select(n => n!.AsObject())
			.ToDictionary(l => l["id"]!.GetValue<string>());

		var functions = cfg["axes"]!["function"]!.AsArray()
			.Select(n => n!.GetValue<string>())
			.ToHashSet();

		var aliases = cfg["story_need_aliases"]!.AsObject()
			.Select(kv => kv.Key)
			.ToHashSet();

		// Act

		var rows = Regex.Matches(reference, @"^\| `([a-z0-9_]+)` \| ([^|]+) \| (.+) \|$", RegexOptions.Multiline)
			.Where(m => leaves.ContainsKey(m.Groups[1].Value))
			.ToList();

		// Assert

		rows.Should().HaveCount(leaves.Count, "every leaf must appear in the table exactly once");

		using var _ = new AssertionScope();

		foreach (var row in rows)
		{
			var id = row.Groups[1].Value;
			var terms = Regex.Replace(row.Groups[2].Value, @"\([^)]*\)", "")
				.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

			foreach (var term in terms)
			{
				(functions.Contains(term) || aliases.Contains(term))
					.Should().BeTrue($"'{term}' in the '{id}' row must be a function or a registered alias");
			}

			var documentedUnsupported = row.Groups[3].Value.Contains('❌');
			var supported = leaves[id]["ggnet"]!["supported"]!.GetValue<bool>();

			documentedUnsupported.Should().Be(!supported,
				$"the '{id}' row's ❌ marker must agree with ggnet.supported");
		}
	}

	private static string RepoRoot([CallerFilePath] string path = "")
		=> Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));
}
