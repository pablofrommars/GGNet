# Lollipop chart

- Chart: `lollipop`
- Pinned SVG: [`GalleryTests.Lollipop.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.Lollipop.verified.svg)
- When: Bar-chart alternative with less ink: a stem segment plus a point tip.

```csharp
PlotContext.Build(xy, i => i.X, i => i.Y)
	.Geom_Segment(i => i.X, i => i.X, i => 0.0, i => i.Y)
	.Geom_Point()
	.Style()
```
Two layers over one source; the stem runs from 0 to the value.
