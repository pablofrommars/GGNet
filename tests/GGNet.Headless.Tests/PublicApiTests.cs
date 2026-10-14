namespace GGNet.Headless.Tests;

// Diffable public-API manifest: the exported types of each assembly, pinned by
// snapshot. A change here is a deliberate surface decision, not an accident.
public class PublicApiTests
{
	private static string Manifest(Assembly assembly)
		=> string.Join('\n', assembly
			.GetExportedTypes()
			.Select(type => type.FullName)
			.OrderBy(name => name, StringComparer.Ordinal));

	[Fact]
	public async Task GGNet()
	{
		// Arrange

		var assembly = typeof(PlotContext).Assembly;

		// Act

		var manifest = Manifest(assembly);

		// Assert

		await Verifier.Verify(manifest, extension: "txt");
	}

	[Fact]
	public async Task Headless()
	{
		// Arrange

		var assembly = typeof(Host).Assembly;

		// Act

		var manifest = Manifest(assembly);

		// Assert

		await Verifier.Verify(manifest, extension: "txt");
	}
}
