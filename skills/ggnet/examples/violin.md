# Violin plot

- Chart: `violin`
- Pinned SVG: [`GalleryTests.ViolinFromDensity.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.ViolinFromDensity.verified.svg)
- When: Compare full distribution shapes across categories — shows what summaries hide (e.g. bimodality).

```csharp
PlotContext.Build(Stat.Density(readings, r => r.Value, r => r.Tank, n: 64), d => d.Group, d => d.At)
	.Geom_Violin(width: d => d.Density)
	.Style()
```
`Stat.Density(groupBy)` produces the profile; `Geom_Violin(width: d => d.Density)` draws it. A precomputed profile works too (pinned as `GalleryTests.Violin.verified.svg`).
