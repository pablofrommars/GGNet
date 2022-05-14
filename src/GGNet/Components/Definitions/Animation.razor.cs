namespace GGNet.Components.Definitions;

using Theme = Theme.Theme;

public partial class Animation : ComponentBase
{
	[Parameter]
	public string Id { get; set; } = default!;

	[Parameter]
	public Theme Theme { get; set; } = default!;

	protected override bool ShouldRender() => false;
}