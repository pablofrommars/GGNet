using GGNet.Common;

namespace GGNet.Data;

internal sealed class Items(Theme.Theme theme) : Buffer<(Dimension<string> label, Elements elements)>(8, 1)
{
	private readonly Theme.Theme theme = theme;

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