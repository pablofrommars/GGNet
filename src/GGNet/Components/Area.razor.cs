namespace GGNet.Components;

using Rendering;

public partial class Area<T, TX, TY> : ComponentBase
   where TX : struct
   where TY : struct
{
	[Parameter]
	public required Data.Panel<T, TX, TY> Panel { get; init; }

	[Parameter]
	public required IChildRenderModeHandler RenderModeHandler { get; init; }

	[Parameter]
	public required ICoord Coord { get; init; }

	[Parameter]
	public Zone Zone { get; set; }

	[Parameter]
	public required string Clip { get; init; }

	private readonly RenderFragment renderShapes;

	public Area()
	{
		renderShapes = RenderShapes;
	}

	protected override bool ShouldRender() => RenderModeHandler.ShouldRender();
}
