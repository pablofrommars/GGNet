# Bar chart

- Chart: `barplot`
- Pinned SVG: [`GalleryTests.Bar.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.Bar.verified.svg)
- When: Compare one value per category. One row per category (pre-aggregated; else see `summary-errorbar`).

```csharp
PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Bar().Style()
```
Horizontal variant: append `.Flip()` (pinned as `GalleryTests.BarFlipped.verified.svg`).
