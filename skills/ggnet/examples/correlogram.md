# Correlogram

- Chart: `correlogram`
- Pinned SVG: [`GalleryTests.Correlogram.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.Correlogram.verified.svg)
- When: Pairwise correlation matrix as a tile grid with a diverging fill.

```csharp
PlotContext.Build(correlations, c => c.Column, c => c.Row)
	.Scale_Fill_Continuous(c => c.R, ["#b2182b", "#f7f7f7", "#2166ac"])
	.Geom_Tile(c => c.Column, c => c.Row, c => 0.95, c => 0.95)
	.Style()
```
Composable recipe: the caller computes the correlation matrix (no `Stat.Cor`); GGNet draws it.
