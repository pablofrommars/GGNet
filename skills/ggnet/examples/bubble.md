# Bubble chart

- Chart: `bubble`
- Pinned SVG: [`GalleryTests.Bubble.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.Bubble.verified.svg)
- When: Scatter with a third numeric variable encoded as circle size.

```csharp
PlotContext.Build(xy, i => i.X, i => i.Y)
	.Scale_Size_Continuous(i => i.Y, range: (3, 9))
	.Geom_Point()
	.Style()
```
`range:` is the radius in **pixels** — the default `(0, 1)` renders sub-pixel bubbles; always pass an explicit range.
