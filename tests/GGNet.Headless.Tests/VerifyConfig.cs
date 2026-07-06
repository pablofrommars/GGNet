using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using VerifyTests;
using VerifyXunit;

namespace GGNet.Headless.Tests;

public static class VerifyConfig
{
	[ModuleInitializer]
	public static void Init()
	{
		// Per-instance plot ids (gg<hash>) are the only nondeterministic part of the output.
		VerifierSettings.AddScrubber(sb =>
		{
			var scrubbed = Regex.Replace(sb.ToString(), @"gg[A-Za-z0-9_-]+", "ggID");

			sb.Clear();
			sb.Append(scrubbed);
		});

		Verifier.DerivePathInfo((sourceFile, projectDirectory, type, method) =>
			new(Path.Combine(projectDirectory, "Gallery"), type.Name, method.Name));
	}
}
