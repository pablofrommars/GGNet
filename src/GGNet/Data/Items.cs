using GGNet.Common;

namespace GGNet.Data;

internal sealed class Items : Buffer<(Dimension<string> label, Elements elements)>
{
	private readonly Theme.Theme theme;

	public Items(Theme.Theme theme) : base(8, 1) => this.theme = theme;

	public (Dimension<string> label, Elements elements) GetOrAdd(string label)
	{
		for (var i = 0; i < Count; i++)
		{
			var ret = Get(i);
			if (ret.label.Value == label)
			{
				return ret;
			}
		}

		var height = label.Height(theme.Legend.Labels.Size);

		var item = (
			label: new Dimension<string>
			{
				Value = label,
				Width = label.Width(theme.Legend.Labels.Size),
				Height = height
			},
			elements: new Elements(height)
		);

		Add(item);

		return item;
	}
}