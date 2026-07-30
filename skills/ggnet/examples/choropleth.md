# Choropleth map

- Chart: `choropleth`
- Pinned SVG: [`GalleryTests.Map.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.Map.verified.svg)
- When: A rate/score per geographic region, encoded as polygon fill.

```csharp
PlotContext.Build(regions)
	.Geom_Map(r => r.Shapes).Style()
```
One `Geospacial.Polygon[]` per item; all polygons of a layer emit as one multi-subpath `<path>`. Mark an inner ring `Hole = true` to punch a cut-out — composition winds it against its exterior, so your source winding does not matter. Add `Scale_Fill_Continuous(fillBy: ...)` for the value; `Scale_Longitude`/`Scale_Latitude` for degree axes.
