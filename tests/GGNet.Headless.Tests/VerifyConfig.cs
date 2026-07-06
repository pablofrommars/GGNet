namespace GGNet.Headless.Tests;

public static class VerifyConfig
{
	[ModuleInitializer]
	public static void Init()
	{
		// Per-instance plot ids (gg<hash>, plus -plot/-0_0 style suffixes) are
		// the only nondeterministic part of the output; the ggnet- exclusion
		// keeps --ggnet-* theme variables out of the scrub.
		VerifierSettings.AddScrubber(sb =>
		{
			var scrubbed = Regex.Replace(sb.ToString(), @"gg(?!net-)[A-Za-z0-9_-]+", "ggID");

			sb.Clear();
			sb.Append(scrubbed);
		});

		Verifier.DerivePathInfo((sourceFile, projectDirectory, type, method) =>
			new(Path.Combine(projectDirectory, type.Name == nameof(PublicApiTests) ? "Api" : "Gallery"), type.Name, method.Name));
	}
}
