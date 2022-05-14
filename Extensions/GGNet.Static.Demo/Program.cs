using GGNet;
using GGNet.Datasets;
using GGNet.Static;

var plot = Plot.New(Iris.Load(), x: o => o.SepalLength, y: o => o.SepalWidth)
	.Geom_Point()
	.Scale_Color_Discrete(o => o.Species, Colors.Viridis, name: "Species")
	.Title("Fisher's Iris")
	.XLab("Sepal Lenght (cm)")
	.YLab("Sepal Width (cm)")
	.Theme(dark: false);

await plot.SaveAsync("iris.svg");