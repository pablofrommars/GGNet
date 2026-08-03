# Radar chart

- Chart: `radar`
- Pinned SVG: [`GalleryTests.Radar.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.Radar.verified.svg)
- When: Few entities compared over a small set of shared metrics; implies polar coordinates.

```csharp
PlotContext.Build(rated, i => i.Metric, i => i.Value).Geom_Radar().Style()
```
The angular axis is the category (here an enum); the radial axis starts at zero.

## With a status line under each axis label

- Pinned SVG: [`GalleryTests.RadarBreakTitles.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.RadarBreakTitles.verified.svg)

```csharp
PlotContext.Build(rated, i => i.Metric, i => i.Value)
	.Scale_X_Discrete(expand: (0.0, 0.0, 0.0, 1.0), titles: MetricStatus.Instance)
	.Geom_Radar()
	.Style()
```
`titles:` takes an `IFormatter<TX>` and stacks a second, independently styled line (class `x-break-title`, sized by `Style.Axis.Title.X.FontSize`, painted by `--ggnet-break-title`) beneath each break label. Setting the scale explicitly opts out of polar's expansion hints, so `expand: (0.0, 0.0, 0.0, 1.0)` must be restated.
