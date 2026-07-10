# Slope chart

- Chart: `slope`
- Pinned SVG: [`GalleryTests.Slope.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.Slope.verified.svg)
- When: Before/after comparison per entity: one segment from the first to the second period.

```csharp
PlotContext.Build(changes, c => 0.0, c => c.Before)
	.Geom_Segment(c => 0.0, c => 1.0, c => c.Before, c => c.After)
	.Geom_Point()
	.Geom_Point(x: c => 1.0, y: c => c.After)
	.Geom_Text(x: c => 1.1, y: c => c.After, text: c => c.Name, anchor: Anchor.Start)
	.Style()
```
Source: `Change(string Name, double Before, double After)`; endpoints drawn as points, names as trailing labels.
