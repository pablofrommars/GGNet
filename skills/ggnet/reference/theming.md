# Theming & export

One rule splits styling: **if it moves layout it's C# (`Style` — font sizes, margins, positions, because the server measures them); if it's paint it's CSS.**

## Layout — the `Style` record

Passed to the terminal call: `.Style(style)` (plus `axisY: Position.Left|Right`, `legend: Position.Right|Top|…`). Nested option groups: `StylePlot`, `StylePanel`, `StylePanelSpacing`, `StyleAxis`, `StyleAxisText`, `StyleAxisTitle`, `StyleLegend`, `StyleStrip`, `StyleStripText`, `StylePolar`. Reach for it only when layout must change; paint never goes here.

## Paint — CSS variables

Base rules (`Themes/Default.css`) read every paint through a `--ggnet-*` variable, scoped under `.ggnet[theme=name]`. A theme is a block of variable overrides, not a stylesheet fork — anything omitted degrades to the default:

```css
.ggnet[theme=mytheme] {
	--ggnet-bg: #1e1e1e;
	--ggnet-grid: #333;
	--ggnet-break-label: #9ca3af;
}
```

The full variable set (16): `--ggnet-axis-title`, `--ggnet-bg`, `--ggnet-break-label`, `--ggnet-break-title`, `--ggnet-caption`, `--ggnet-font`, `--ggnet-grid`, `--ggnet-legend-label`, `--ggnet-legend-title`, `--ggnet-spinner-accent`, `--ggnet-spinner-display`, `--ggnet-spinner-track`, `--ggnet-strip`, `--ggnet-sub-title`, `--ggnet-title`, `--ggnet-tooltip-bg`. The contract (every emitted class painted, every referenced variable defined, themes set only known variables) is test-enforced (`ThemeContractTests`).

Select the theme via the `Plot` component's `Theme` parameter (default `"default"`) or the export call's `theme:` argument.

Notes:
- Geom parameters accept CSS custom properties — `color: "var(--color-temperature)"` wires a layer to app design tokens.
- `--ggnet-font` affects rendering only: server-side text measurement assumes Inter.

## Hosting & export

Blazor: `<Plot Context="plot" RenderMode="..." Width="720" Height="576" Theme="default" />`; `SparkLine` for inline 150×50 plots.

Headless (`GGNet.Headless` package):

```csharp
string svg = await plot.AsStringAsync(width: 720, height: 576, theme: "default", selfContained: false);
await plot.SaveAsync("plot.svg" /*, same options */);
```

Output is pure, well-formed SVG. `selfContained: true` embeds the bundled theme CSS as a `<style>` element so the file renders standalone; the default (off) leaves paint to the hosting app's stylesheet.
