# Summary with error bars

- Chart: `stat bridge → barplot/dot_plot family`
- Pinned SVG: [`GalleryTests.SummaryErrorBar.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.SummaryErrorBar.verified.svg)
- When: Compare groups from raw observations: center plus spread interval per group.

```csharp
PlotContext.Build(Stat.Summary(readings, r => r.Tank, r => r.Value), s => s.X, s => s.Center)
	.Geom_ErrorBar(ymin: s => s.Lower, ymax: s => s.Upper)
	.Style()
```
The stat bridge in one line: `Stat.Summary` aggregates raw rows, `Geom_ErrorBar` draws center/lower/upper.

**Careful:** `Tank` is a `double` in this fixture. `Stat.Summary`'s x selector is `Func<T, double>` — with *string* categories, map each to a stable numeric slot (`Array.IndexOf(sortedKeys, r.Key) + 1.0`) and pass the name as `groupBy:` so the color scale and legend carry it (see patterns/common-mistakes.md).
