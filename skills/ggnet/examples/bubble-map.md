# Proportional symbol map

- Chart: `proportional_symbol_map`
- Pinned SVG: [`GalleryTests.BubbleMap.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.BubbleMap.verified.svg)
- When: A magnitude per location: sized circles over a base map.

```csharp
PlotContext.Build(sites, s => s.Longitude, s => s.Latitude)
	.Scale_Size_Continuous(s => s.Value, range: (4, 12))
	.Geom_Map(regions, r => r.Shapes)
	.Geom_Point()
	.Style()
```
Sites are the primary source so `Scale_Size_Continuous` can train on them; the polygons come in as a secondary layer source. `range:` is the radius in pixels.
