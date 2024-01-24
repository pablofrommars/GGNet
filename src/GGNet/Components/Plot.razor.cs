using GGNet.Common;

namespace GGNet.Components;

using Rendering;

using static Position;

public partial class Plot<T, TX, TY> : PlotBase<T, TX, TY>
	where TX : struct
	where TY : struct
{
	[Parameter]
	public double Width { get; set; } = 720;

	[Parameter]
	public double Height { get; set; } = 576;

	[Parameter]
	public string? Class { get; set; }

	public Zone Title;
	public Zone SubTitle;

	public Zone Legend;

	public Zone Caption;

	private Zone wrapper;

	private IChildRenderPolicy definitionsPolicy = default!;

	private bool loading;

	public string? DynamicClass => (Class, loading) switch
	{
		(string @class, true) => $"{@class} loading",
		(null, true) => "loading",
		_ => Class
	};

	protected override void OnInitialized()
	{
		base.OnInitialized();

		Context.Init();

		definitionsPolicy = Policy.Child();

		Render(true, RenderTarget.All);
	}

	protected void Render(bool firstRender, RenderTarget target)
	{
		if (target == RenderTarget.Loading)
		{
			loading = true;
			return;
		}

		loading = false;

		Context.Render(firstRender);

		wrapper.X = 0;
		wrapper.Y = 0;
		wrapper.Width = Width;
		wrapper.Height = Height;

		if (!string.IsNullOrEmpty(Context.Title))
		{
			var width = Context.Title.Height(Context.Theme!.Plot.Title.Size);
			var height = Context.Title.Height(Context.Theme!.Plot.Title.Size);

			Title.X = Context.Theme!.Plot.Title.Margin.Left;
			Title.Y = Context.Theme!.Plot.Title.Margin.Top + height;
			Title.Width = Context.Theme!.Plot.Title.Margin.Left + width + Context.Theme.Plot.Title.Margin.Right;
			Title.Height = Context.Theme!.Plot.Title.Margin.Top + height + Context.Theme.Plot.Title.Margin.Bottom;

			wrapper.Y += Title.Height;
			wrapper.Height -= Title.Height;
		}

		if (!string.IsNullOrEmpty(Context.SubTitle))
		{
			var width = Context.SubTitle.Height(Context.Theme!.Plot.SubTitle.Size);
			var height = Context.SubTitle.Height(Context.Theme!.Plot.SubTitle.Size);

			SubTitle.X = Context.Theme!.Plot.SubTitle.Margin.Left;
			SubTitle.Y = Title.Height + Context.Theme!.Plot.SubTitle.Margin.Top + height;
			SubTitle.Width = Context.Theme!.Plot.SubTitle.Margin.Left + width + Context.Theme!.Plot.SubTitle.Margin.Right;
			SubTitle.Height = Context.Theme!.Plot.SubTitle.Margin.Top + height + Context.Theme!.Plot.SubTitle.Margin.Bottom;

			wrapper.Y += SubTitle.Height;
			wrapper.Height -= SubTitle.Height;
		}

		if (!string.IsNullOrEmpty(Context.Caption))
		{
			var width = Context.Caption.Height(Context.Theme!.Plot.Caption.Size);
			var height = Context.Caption.Height(Context.Theme!.Plot.Caption.Size);

			Caption.Y = Height - Context.Theme!.Plot.Caption.Margin.Bottom;
			Caption.Width = Context.Theme!.Plot.Caption.Margin.Left + width + Context.Theme!.Plot.Caption.Margin.Right;
			Caption.Height = Context.Theme!.Plot.Caption.Margin.Top + height + Context.Theme!.Plot.Caption.Margin.Bottom;

			wrapper.Height -= Caption.Height;
		}

		if (Context.Legends.Count > 0)
		{
			var (width, height) = Context.Legends.Dimension();

			if (width > 0 && height > 0)
			{
				if (Context.Theme!.Legend.Position == Right)
				{
					Legend.X = Width - width - Context.Theme!.Legend.Margin.Right;
					Legend.Y = wrapper.Y + (wrapper.Height - height) / 2.0;
					Legend.Width = Context.Theme!.Legend.Margin.Left + width + Context.Theme!.Legend.Margin.Right;
					Legend.Height = wrapper.Height;

					wrapper.Width -= Legend.Width;
				}
				else if (Context.Theme!.Legend.Position == Left)
				{
					Legend.X = Context.Theme!.Legend.Margin.Left;
					Legend.Y = wrapper.Y + (wrapper.Height - height) / 2.0; ;
					Legend.Width = Context.Theme!.Legend.Margin.Left + width + Context.Theme!.Legend.Margin.Right;
					Legend.Height = wrapper.Height;

					wrapper.X += Legend.Width;
					wrapper.Width -= Legend.Width;
				}
				else if (Context.Theme!.Legend.Position == Top)
				{
					Legend.X = wrapper.X + (wrapper.Width - width) / 2.0;
					Legend.Y = wrapper.Y + Context.Theme!.Legend.Margin.Top;
					Legend.Width = wrapper.Width;
					Legend.Height = Context.Theme!.Legend.Margin.Top + height + Context.Theme!.Legend.Margin.Bottom;

					wrapper.Y += Legend.Height;
					wrapper.Height -= Legend.Height;
				}
				else if (Context.Theme!.Legend.Position == Bottom)
				{
					Legend.X = wrapper.X + (wrapper.Width - width) / 2.0;
					Legend.Y = wrapper.Y + wrapper.Height - height - Context.Theme!.Legend.Margin.Bottom;
					Legend.Width = wrapper.Width;
					Legend.Height = Context.Theme!.Legend.Margin.Top + height + Context.Theme!.Legend.Margin.Bottom;

					wrapper.Height -= Legend.Height;
				}
			}
		}

		if (Caption.Width > 0)
		{
			Caption.X = wrapper.X + wrapper.Width - Context.Theme!.Plot.Caption.Margin.Right;
		}

		if (!firstRender)
		{
			definitionsPolicy?.Refresh(target);

			for (var i = 0; i < Context.Panels.Count; i++)
			{
				Context.Panels[i].Component?.Refresh(target);
			}
		}
	}

	public override void Render(RenderTarget target) => Render(false, target);
}
