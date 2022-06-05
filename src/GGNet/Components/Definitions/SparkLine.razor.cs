namespace GGNet.Components.Definitions;

using Theme = Theme.Theme;

public partial class SparkLine : ComponentBase
{
	[Parameter]
	public string Id { get; set; } = default!;

	[Parameter]
	public Theme Theme { get; set; } = default!;

	[Parameter]
	public IChildRenderPolicy RenderPolicy { get; set; } = default!;

	protected override bool ShouldRender() => RenderPolicy.ShouldRender(RenderTarget.Theme);
}