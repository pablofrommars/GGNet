# Grouped (dodged) bar chart

- Chart: `grouped_barplot`
- Pinned SVG: [`GalleryTests.BarFlippedDodged.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.BarFlippedDodged.verified.svg)
- When: Compare subgroups side-by-side within categories.

```csharp
PlotContext.Build(dodged, i => i.Pos, i => i.Value)
	.Scale_Fill_Discrete(i => i.Series, ["#23d0fc", "#fc9d23"])
	.Geom_Bar(position: PositionAdjustment.Dodge)
	.Flip()
	.Style()
```
This pinned variant is also flipped (horizontal); drop `.Flip()` for vertical bars. `Scale_Fill_Discrete` supplies the series.
