# Grouped scatter plot

- Chart: `grouped_scatter`
- Pinned SVG: [`GalleryTests.GroupedScatter.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.GroupedScatter.verified.svg)
- When: Scatter with a categorical series: the color scale trains the legend.

```csharp
PlotContext.Build(dodged, i => i.Pos, i => i.Value)
	.Scale_Color_Discrete(i => i.Series, ["#23d0fc", "#fc9d23"])
	.Geom_Point()
	.Style()
```
Source: `Grouped2(double Pos, double Value, double Series)` — the series selector feeds `Scale_Color_Discrete`.
