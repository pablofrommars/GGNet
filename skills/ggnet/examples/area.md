# Area chart

- Chart: `area`
- Pinned SVG: [`GalleryTests.Area.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.Area.verified.svg)
- When: Trend where the magnitude (area under the curve) matters.

```csharp
PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Area().Style()
```
