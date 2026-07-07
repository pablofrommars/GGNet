# Stacked bar chart

- Chart: `stacked_barplot`
- Pinned SVG: [`GalleryTests.BarStacked.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.BarStacked.verified.svg)
- When: Part-to-whole across categories; series sum to the bar total.

```csharp
PlotContext.Build(dodged, i => i.Pos, i => i.Value)
	.Scale_Fill_Discrete(i => i.Series, ["#23d0fc", "#fc9d23"])
	.Geom_Bar()
	.Style()
```
`Stack` is `Geom_Bar`'s default `position` — the fill scale alone makes it stacked.
