using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using VerifyTests;
using VerifyXunit;

namespace GGNet.Static.Tests;

public static class VerifyConfig
{
	[ModuleInitializer]
	public static void Init()
	{
		// Per-instance plot ids (gg<hash>) and Blazor CSS scope attributes are the
		// only nondeterministic parts of the output.
		VerifierSettings.AddScrubber(sb =>
		{
			var scrubbed = Regex.Replace(sb.ToString(), @"gg[A-Za-z0-9_-]+", "ggID");
			scrubbed = Regex.Replace(scrubbed, @" b-[a-z0-9]+(?=[ =>])", " b-SCOPE");

			sb.Clear();
			sb.Append(scrubbed);
		});

		Verifier.DerivePathInfo((sourceFile, projectDirectory, type, method) =>
			new(Path.Combine(projectDirectory, "Gallery"), type.Name, method.Name));
	}
}
