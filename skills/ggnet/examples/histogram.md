# Histogram

- Chart: `histogram`
- Pinned SVG: [`GalleryTests.Histogram.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.Histogram.verified.svg)
- When: Distribution of one numeric variable from raw observations.

```csharp
PlotContext.Build(Stat.Bin(readings, r => r.Value, bins: 12), b => b.Mid, b => b.Count)
	.Geom_Bar(width: 1.0)
	.Style()
```
There is no Histogram geom: `Stat.Bin` computes bins, `Geom_Bar(width: 1.0)` draws them.
