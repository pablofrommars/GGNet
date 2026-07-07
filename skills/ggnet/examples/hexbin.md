# Hexbin plot

- Chart: `density_2d`
- Pinned SVG: [`GalleryTests.Hex.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.Hex.verified.svg)
- When: Density of many points over two numeric variables, binned into hexagons.

```csharp
PlotContext.Build(xy, i => i.X, i => i.Y)
	.Geom_Hex(dx: i => 0.5, dy: i => 0.4).Style()
```
The caller supplies hex centers and `dx`/`dy` extents; GGNet draws the hexagons.
