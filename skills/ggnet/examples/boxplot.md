# Boxplot

- Chart: `boxplot`
- Pinned SVG: [`GalleryTests.Boxplot.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.Boxplot.verified.svg)
- When: Compare distributions across categories via quartiles and whiskers.

```csharp
PlotContext.Build(grouped, i => i.Value, i => i.Group).Geom_Boxplot().Style()
```
Boxplot is horizontal by data design: x carries the measurements, y the category.
