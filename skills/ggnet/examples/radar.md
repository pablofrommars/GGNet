# Radar chart

- Chart: `radar`
- Pinned SVG: [`GalleryTests.Radar.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.Radar.verified.svg)
- When: Few entities compared over a small set of shared metrics; implies polar coordinates.

```csharp
PlotContext.Build(rated, i => i.Metric, i => i.Value).Geom_Radar().Style()
```
The angular axis is the category (here an enum); the radial axis starts at zero.
