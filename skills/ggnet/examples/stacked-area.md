# Stacked area chart

- Chart: `stacked_area`
- Pinned SVG: [`GalleryTests.AreaStacked.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.AreaStacked.verified.svg)
- When: Part-to-whole evolving over an ordered axis. Keep it to few series; facet when crowded.

```csharp
PlotContext.Build(dodged, i => i.Pos, i => i.Value)
	.Scale_Fill_Discrete(i => i.Series, ["#23d0fc", "#fc9d23"])
	.Geom_Area(position: PositionAdjustment.Stack)
	.Style()
```
`Scale_Fill_Discrete` supplies the series; `position: PositionAdjustment.Stack` piles them on a common baseline.
