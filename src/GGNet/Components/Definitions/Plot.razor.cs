namespace GGNet.Components.Definitions;

using Theme = Theme.Theme;

public partial class Plot : ComponentBase
{
	[Parameter]
	public required string Id { get; init; }

	[Parameter]
	public required Theme Theme { get; set; }

	[Parameter]
	public required IChildRenderPolicy RenderPolicy { get; set; }

	protected override bool ShouldRender() => RenderPolicy.ShouldRender(RenderTarget.Theme);
}