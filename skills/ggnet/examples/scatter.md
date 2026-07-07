# Scatter plot

- Chart: `scatter`
- Pinned SVG: [`GalleryTests.Point.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.Point.verified.svg)
- When: Relationship between two numeric variables; every observation is a mark.

```csharp
PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Point().Style()
```
Source: any record with two numeric properties (here `XY(double X, double Y)`).
