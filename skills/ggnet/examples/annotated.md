# Annotated line chart

- Chart: `cross-cutting (annotations)`
- Pinned SVG: [`GalleryTests.HLine.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.HLine.verified.svg)
- When: Reference levels and marks over a data layer.

```csharp
PlotContext.Build(xy, i => i.X, i => i.Y)
	.Geom_Line()
	.Geom_HLine(new[] { 3.0 }, v => v, v => "level").Style()
```
Annotation geoms (`Geom_HLine`, `Geom_VLine`, `Geom_ABLine`) take no event block; each draws one line per item of its own small source (both siblings pinned).
