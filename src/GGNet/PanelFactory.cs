namespace GGNet;

using Scales;
using Facets;
using Geoms;

public sealed class PanelFactory<T, TX, TY>
	where TX : struct
	where TY : struct
{
	private readonly List<Func<Data.Panel<T, TX, TY>, Facet<T>?, IGeom>> geoms = new();

	private readonly double width;
	private readonly double height;

	public PanelFactory(Data<T, TX, TY> data, double width = 1, double height = 1)
	{
		Data = data;

		this.width = width;
		this.height = height;
	}

	internal Data<T, TX, TY> Data { get; }

	internal Func<Position<TY>>? Y { get; set; }

	internal string? YLab { get; set; }

	internal void Add(Func<Data.Panel<T, TX, TY>, Facet<T>?, IGeom> geom) => geoms.Add(geom);

	internal Data.Panel<T, TX, TY> Build((int, int) coord, Facet<T>? facet = null, double? width = null, double? height = null)
	{
		var panel = new Data.Panel<T, TX, TY>(coord, Data, width ?? this.width, height ?? this.height);

		foreach (var geom in geoms)
		{
			panel.Geoms.Add(geom(panel, facet));
		}

		return panel;
	}
}