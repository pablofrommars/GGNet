# Heatmap

- Chart: `heatmap`
- Pinned SVG: [`GalleryTests.Tile.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.Tile.verified.svg)
- When: A value over two discrete-ish dimensions, encoded as tile fill.

```csharp
PlotContext.Build(xy, i => i.X, i => i.Y)
	.Geom_Tile(i => i.X, i => i.Y, i => 0.9, i => 0.8).Style()
```
`width`/`height` are geometric extent in data units. Add `Scale_Fill_Continuous` to map a value to fill (see `calendar-heatmap`).
