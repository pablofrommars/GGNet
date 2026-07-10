# Calendar heatmap

- Chart: `calendar_heatmap`
- Pinned SVG: [`GalleryTests.CalendarHeatmap.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.CalendarHeatmap.verified.svg)
- When: Daily values over weeks: week × weekday grid, value as fill.

```csharp
PlotContext.Build(days, d => d.Week, d => d.Weekday)
	.Scale_Fill_Continuous(d => d.Value, ["#132b43", "#56b1f7"])
	.Geom_Tile(d => d.Week, d => d.Weekday, d => 0.95, d => 0.95)
	.Style()
```
Composable recipe: the caller computes the week/weekday coordinates; GGNet draws tiles and trains the fill scale.
