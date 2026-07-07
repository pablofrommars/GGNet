# Small multiples (faceted histogram)

- Chart: `small_multiples`
- Pinned SVG: [`GalleryTests.HistogramFaceted.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.HistogramFaceted.verified.svg)
- When: One panel per group when a single chart would overload — the structural escape hatch.

```csharp
PlotContext.Build(Stat.Bin(readings, r => r.Value, r => r.Tank, bins: 10), b => b.Mid, b => b.Count)
	.Geom_Bar(width: 1.0)
	.Facet_Wrap(b => b.Group)
	.Style()
```
Per-facet statistics are grouped statistics: `Stat.Bin(..., groupBy)` then `Facet_Wrap` on the **same** key — the key is deliberately stated twice.
