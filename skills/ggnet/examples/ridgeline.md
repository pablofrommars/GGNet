# Ridgeline plot

- Chart: `ridgeline`
- Pinned SVG: [`GalleryTests.RidgeLine.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.RidgeLine.verified.svg)
- When: Many distributions stacked as overlapping profiles, one row each.

```csharp
PlotContext.Build(xy, i => i.X, i => i.Y)
	.Geom_RidgeLine(height: i => 0.8).Style()
```
`height` is the profile height above each row's baseline, in y-axis data units.
