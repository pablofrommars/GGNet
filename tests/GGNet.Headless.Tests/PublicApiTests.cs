namespace GGNet.Headless.Tests;

// Diffable public-API manifest: the exported types of each assembly, pinned by
// snapshot. A change here is a deliberate surface decision, not an accident.
public class PublicApiTests
{
	private static Task VerifyExportedTypes(Assembly assembly)
	{
		var manifest = string.Join('\n', assembly
			.GetExportedTypes()
			.Select(type => type.FullName)
			.OrderBy(name => name, StringComparer.Ordinal));

		return Verifier.Verify(manifest, extension: "txt");
	}

	[Fact]
	public Task GGNet() => VerifyExportedTypes(typeof(PlotContext).Assembly);

	[Fact]
	public Task Headless() => VerifyExportedTypes(typeof(Host).Assembly);
}
