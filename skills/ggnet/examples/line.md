# Line chart

- Chart: `line`
- Pinned SVG: [`GalleryTests.Line.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.Line.verified.svg)
- When: Trend over an ordered (time or sequence) axis.

```csharp
PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Line().Style()
```
For NodaTime axes use the `Build` overloads for `LocalDate`/`LocalDateTime`/`Instant` and the matching `Scale_X_*` scales.
