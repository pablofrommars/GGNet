namespace GGNet.Headless.Tests;

// The comment policy (csharp.instructions.md §12), held as a test rather than as a convention: every
// full-line `//` comment block under src/ starts with one of the sanctioned tags, so the sanctioned set
// is one grep. `///` XML docs are the public DSL's manual and are out of scope. So is a trailing
// annotation on a code line: it is bounded by the line it sits on and cannot accumulate into prose.
public class CommentPolicyTests
{
	private static readonly string[] tags = ["TODO: calibration", "RUNBOOK", "INVARIANT", "DEVIATION"];

	private static readonly Regex tagged = new(
		@"^\s*//\s*(TODO: calibration|RUNBOOK|INVARIANT|DEVIATION)\b",
		RegexOptions.CultureInvariant);

	private static readonly Regex comment = new(@"^\s*//(?!/)", RegexOptions.CultureInvariant);

	private static readonly Regex documentation = new(@"^\s*///", RegexOptions.CultureInvariant);

	private sealed record Block(string File, int Line, string Text);

	[Fact]
	public void EveryCommentUnderSrcCarriesASanctionedTag()
	{
		// Arrange

		var blocks = Blocks().ToList();

		// Act

		var untagged = blocks.Where(block => !tagged.IsMatch(block.Text)).ToList();

		// Assert

		untagged.Should().BeEmpty(
			"production code carries no untagged comments; offenders:\n{0}",
			string.Join("\n", untagged.Select(block => $"  {block.File}:{block.Line}: {block.Text.Trim()}")));
	}

	[Fact]
	public void TheGuardReallyReadsTheTree()
	{
		// Arrange — a guard that stopped finding files, stopped recognising tags, or started reading XML
		// docs as comments would pass or fail while proving nothing.

		var files = SourceFiles().ToList();

		// Act

		var blocks = Blocks().ToList();
		var documented = files.Count(file => File.ReadLines(file).Any(documentation.IsMatch));

		// Assert

		using var _ = new AssertionScope();

		files.Should().HaveCountGreaterThan(250, "the sweep covers the whole of src/");
		documented.Should().BePositive("the public DSL carries XML docs the sweep must skip");
		blocks.Should().NotContain(block => documentation.IsMatch(block.Text));
		blocks.Should().Contain(block => tagged.IsMatch(block.Text), "the sanctioned comments are still found and parsed");
	}

	[Fact]
	public void TheGuideDeclaresExactlyTheTagsThisGuardEnforces()
	{
		// Arrange — the policy and its guard drift apart silently otherwise: a tag added to the guide and
		// not here is unenforced, and one added here and not there is unexplained.

		var guide = File.ReadAllText(Path.Combine(RepoRoot(), ".github", "instructions", "csharp.instructions.md"));
		var section = guide[guide.IndexOf("## 12. Comments & Documentation", StringComparison.Ordinal)..];
		section = section[..section.IndexOf("## 13.", StringComparison.Ordinal)];

		// Act — read whatever the guide declares rather than looking for the tags already known here, or a
		// tag added to the guide alone would be invisible to this comparison.

		var declared = Regex.Matches(section, @"(?m)^-\s+\*\*`// (?<tag>[^`<]+)")
			.Select(match => match.Groups["tag"].Value.TrimEnd(' ', ':', '-', '—'))
			.Distinct(StringComparer.Ordinal)
			.Order(StringComparer.Ordinal)
			.ToList();

		// Assert

		declared.Should().Equal([.. tags.Order(StringComparer.Ordinal)]);
	}

	private static IEnumerable<Block> Blocks()
	{
		var root = RepoRoot();

		foreach (var file in SourceFiles())
		{
			var lines = File.ReadAllLines(file);
			var relative = Path.GetRelativePath(root, file);

			for (var index = 0; index < lines.Length; index++)
			{
				if (!comment.IsMatch(lines[index]))
				{
					continue;
				}

				yield return new Block(relative, index + 1, lines[index]);

				while (index + 1 < lines.Length && comment.IsMatch(lines[index + 1]))
				{
					index++;
				}
			}
		}
	}

	private static IEnumerable<string> SourceFiles()
		=> Directory
			.EnumerateFiles(Path.Combine(RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories)
			.Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
				&& !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

	private static string RepoRoot([CallerFilePath] string path = "")
		=> Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));
}
