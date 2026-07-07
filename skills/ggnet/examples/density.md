# Density plot

- Chart: `density`
- Pinned SVG: [`GalleryTests.DensityArea.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.DensityArea.verified.svg)
- When: Smooth distribution of one numeric variable from raw observations.

```csharp
PlotContext.Build(Stat.Density(readings, r => r.Value, n: 128), d => d.At, d => d.Density)
	.Geom_Area()
	.Style()
```
`Stat.Density` (kernel density, bandwidth via `Stat.Nrd0` by default) feeds `Geom_Area`.
