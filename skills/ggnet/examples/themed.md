# Themed, self-contained export

- Chart: `cross-cutting (theming/export)`
- Pinned SVG: [`GalleryTests.SelfContained.verified.svg`](../../../tests/GGNet.Headless.Tests/Gallery/GalleryTests.SelfContained.verified.svg)
- When: Standalone SVG output: embeds the bundled theme css so the file renders outside the app.

```csharp
var svg = await PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Point().Style()
	.AsStringAsync(selfContained: true);
```
App-hosted output omits `selfContained` (default false) and is painted by the app's stylesheet — themes override `--ggnet-*` variables under `.ggnet[theme=name]`; pick the theme via the `Plot` component's `Theme` parameter or the export call's `theme:` argument.
