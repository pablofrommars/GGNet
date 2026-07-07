# Connected scatter plot

- Chart: `connected_scatter`
- Pinned SVG: [`GalleryTests.ConnectedScatter.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.ConnectedScatter.verified.svg)
- When: Trajectory through two numeric variables over an implicit order — line plus marks.

```csharp
PlotContext.Build(xy, i => i.X, i => i.Y)
	.Geom_Line()
	.Geom_Point()
	.Style()
```
Same source drives both layers; order comes from x.
