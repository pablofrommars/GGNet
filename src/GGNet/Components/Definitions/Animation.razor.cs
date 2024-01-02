namespace GGNet.Components.Definitions;

using Theme = Theme.Theme;

public partial class Animation : ComponentBase
{
	[Parameter]
	public required string Id { get; init; }

	[Parameter]
	public required Theme Theme { get; init; }

	protected override bool ShouldRender() => false;
}