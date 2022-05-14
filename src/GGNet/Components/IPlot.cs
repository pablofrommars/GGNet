namespace GGNet.Components;

public interface IPlot
{
	RenderPolicyBase Policy { get; }

	void Render();

	Task StateHasChangedAsync();
}
