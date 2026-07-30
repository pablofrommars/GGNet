namespace GGNet.Headless.Tests;

// The skill's example files are checked-in copies of gallery test code
// (extraction decided over generation, plan/PLAN.md 1.3): every csharp block
// under skills/ggnet/examples must appear verbatim (modulo whitespace) in
// GalleryTests.cs, so an API change that rewrites a gallery entry fails here
// until the example follows.
public class SkillExampleConsistencyTests
{
	private static string RepoRoot([CallerFilePath] string path = "")
		=> Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));

	private static string Normalize(string code)
		=> Regex.Replace(code, @"\s+", " ").Trim();

	[Fact]
	public void ExamplesMatchGallerySource()
	{
		// Arrange / Act

		var root = RepoRoot();
		var examples = Directory.GetFiles(Path.Combine(root, "skills", "ggnet", "examples"), "*.md");
		var gallery = Normalize(File.ReadAllText(Path.Combine(root, "tests", "GGNet.Headless.Tests", "GalleryTests.cs")));

		// Assert

		examples.Should().NotBeEmpty();

		using var _ = new AssertionScope();

		foreach (var example in examples)
		{
			var blocks = Regex.Matches(File.ReadAllText(example), "```csharp\n(.*?)```", RegexOptions.Singleline);

			blocks.Should().NotBeEmpty($"'{Path.GetFileName(example)}' must contain a csharp block");

			foreach (Match block in blocks)
			{
				gallery.Contains(Normalize(block.Groups[1].Value))
					.Should().BeTrue($"the csharp block of '{Path.GetFileName(example)}' must match GalleryTests.cs");
			}
		}
	}

	// The Tier-C compositions documented in geoms.md have no pinned gallery
	// entry (plan/PLAN.md 1.2); their chains must match TierCCompositionTests
	// so the reference can never document code that doesn't compile.
	[Fact]
	public void TierCRecipesMatchCompositionSource()
	{
		// Arrange

		var root = RepoRoot();
		var geoms = File.ReadAllText(Path.Combine(root, "skills", "ggnet", "reference", "geoms.md"));
		var section = geoms[geoms.IndexOf("## Multi-layer recipes", StringComparison.Ordinal)..];
		var source = Normalize(File.ReadAllText(Path.Combine(root, "tests", "GGNet.Headless.Tests", "TierCCompositionTests.cs")));

		// Act

		var blocks = Regex.Matches(section, "```csharp\n(.*?)```", RegexOptions.Singleline);

		// Assert

		blocks.Should().NotBeEmpty();

		using var _ = new AssertionScope();

		foreach (Match block in blocks)
		{
			source.Contains(Normalize(block.Groups[1].Value))
				.Should().BeTrue("every Tier-C chain in geoms.md must match TierCCompositionTests.cs");
		}
	}
}
