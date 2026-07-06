
namespace GGNet.Data;

internal sealed class Items(Style style) : List<(Dimension<string> label, Elements elements)>
{
	private readonly Style style = style;

	public (Dimension<string> label, Elements elements) GetOrAdd(string label)
	{
		for (var i = 0; i < Count; i++)
		{
			var ret = this[i];
			if (ret.label.Value == label)
			{
				return ret;
			}
		}

		var height = label.Height(style.Legend.Labels.FontSize);

		var item = (
		  label: new Dimension<string>
		  {
			  Value = label,
			  Width = label.Width(style.Legend.Labels.FontSize),
			  Height = height
		  },
		  elements: new Elements(height)
		);

		Add(item);

		return item;
	}
}
