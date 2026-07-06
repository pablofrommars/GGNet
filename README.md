[![License](https://img.shields.io/github/license/BlazorExtensions/Storage.svg?longCache=true&style=flat-square)](https://github.com/pablofrommars/GGNet/blob/master/LICENSE.TXT)
[![Package Version](https://img.shields.io/badge/nuget-v1.4.0-blue.svg?longCache=true&style=flat-square)](https://www.nuget.org/packages/GGNet/1.4.0)
# GG.Net Data Visualization

GG.Net lets Data Scientists and Developers create interactive and flexible charts for .NET and [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) Web Apps.

Taking its inspiration from the highly popular [ggpplot2](https://ggplot2.tidyverse.org) R package, GG.Net provides natively rich features for your Data Analysis Workflow. Build publication quality charts with just a few lines of code in C# and F#.

[Learn more about GG.Net](https://pablofrommars.github.io/)

## The DSL

A plot is one fluent chain: `PlotContext.Build(source, x, y)` establishes the data source and default selectors, each `Geom_*` call adds a layer configured in place, `Scale_*` calls shape the axes and legends, and `.Style()` finishes the plot.

```csharp
var plot = PlotContext.Build(points, o => o.X, o => o.Y)
	.Geom_Line(strokeWidth: 2, color: "#23d0fc")
	.Geom_HLine([1.0], y: o => o, label: o => "Baseline", lineType: LineType.Dashed)
	.Scale_Y_Continuous(format: "N2")
	.Style();
```

### Conventions

- **`_`-prefix means data-driven.** `_color`, `_fill`, `_size`, `_lineType` take an aesthetic *mapping* (built by `Scale_Color_Discrete`, `Scale_Fill_Continuous`, …): the value is computed per item, trains a scale, and feeds the legend. The unprefixed twin (`color`, `fill`, `size`, `lineType`) is a constant applied to the whole layer. Set one or the other, not both.
- **Positional arguments stop at the selectors.** Source and selector parameters (`x`, `y`, `ymin`, `open`, …) may be passed positionally; every aesthetic, event, or option after them is passed by name. The signatures are wide by design — configuration lives in one call — and named arguments are what keep call sites readable and stable.
- **The vocabulary is SVG's.** `strokeWidth`, `opacity`, `fillOpacity`, `strokeOpacity`, `strokeColor` mean exactly what they mean in SVG. `width` and `height` are reserved for geometric extent in data units (`Geom_Bar`, `Geom_Tile`, `Geom_Violin`).
- **Interactivity is a uniform block.** Every data-mark geom takes `onclick`, `onmouseover`, `onmouseout`, and (where a hover surface makes sense) `tooltip`. When `tooltip` is set and no explicit hover handlers are given, the default hover shows it. Annotation geoms (`Geom_ABLine`, `Geom_HLine`, `Geom_VLine`, `Geom_Text`) and statistical summaries (`Geom_Boxplot`, `Geom_Violin`, `Geom_RidgeLine`) deliberately take no event block.

### Geoms

| Geom | Selectors | Mappings | Constants | Events | Tooltip |
|-|-|-|-|-|-|
| `Geom_Point` | `x`, `y` | `_size`, `_color` | `size`, `color`, `opacity` | ✓ | ✓ |
| `Geom_Line` | `x`, `y` | `_color`, `_lineType` | `strokeWidth`, `color`, `opacity`, `lineType`, `piecewise` | ✓ | ✓ |
| `Geom_Bar` | `x`, `y` | `_fill` | `fill`, `fillOpacity`, `strokeColor`, `strokeOpacity`, `strokeWidth`, `position`, `width` | ✓ | ✓ |
| `Geom_Area` | `x`, `y` | `_fill` | `fill`, `fillOpacity`, `position` | ✓ | ✓ |
| `Geom_Ribbon` | `x`, `ymin`, `ymax` | `_fill` | `fill`, `fillOpacity` | ✓ | ✓ |
| `Geom_ErrorBar` | `x`, `y`, `ymin`, `ymax` | `_color` | `strokeWidth`, `color`, `opacity`, `lineType`, `radius`, `position` | ✓ | ✓ |
| `Geom_Segment` | `x`, `xend`, `y`, `yend` | — | `strokeWidth`, `color`, `opacity`, `lineType` | ✓ | ✓ |
| `Geom_Tile` | `x`, `y`, `width`, `height` | `_fill` | `fill`, `fillOpacity`, `strokeColor`, `strokeOpacity`, `strokeWidth` | ✓ | ✓ |
| `Geom_Hex` | `x`, `y`, `dx`, `dy` | `_fill` | `fill`, `opacity` | ✓ | ✓ |
| `Geom_Radar` | `x`, `y` | `_fill` | `fill`, `fillOpacity`, `strokeWidth` | ✓ | ✓ |
| `Geom_Map` | `polygons` | `_fill` | `fill`, `fillOpacity`, `stroke`, `strokeWidth` | ✓ | ✓ |
| `Geom_Candlestick` | `x`, `open`, `high`, `low`, `close` | — | `strokeWidth`, `color`, `opacity`, `lineType` | ✓ | — |
| `Geom_OHLC` | `x`, `open`, `high`, `low`, `close` | — | `strokeWidth`, `color`, `opacity`, `lineType` | ✓ | — |
| `Geom_Volume` | `x`, `volume` | — | `fill`, `opacity` | ✓ | — |
| `Geom_Boxplot` | `x`, `y` | `_fill` | `size`, `fill`, `fillOpacity`, `strokeWidth` | — | — |
| `Geom_Violin` | `x`, `y`, `width` | `_fill` | `fill`, `fillOpacity`, `stroke`, `position` | — | — |
| `Geom_RidgeLine` | `x`, `y`, `height` | `_fill` | `fill`, `fillOpacity` | — | — |
| `Geom_Text` | `x`, `y`, `text`, `_angle` | `_color` | `size`, `anchor`, `weight`, `style`, `color`, `angle` | — | — |
| `Geom_ABLine` | `a`, `b`, `label` | — | `strokeWidth`, `color`, `opacity`, `lineType`, `size`, `anchor` | — | — |
| `Geom_HLine` | `y`, `label` | — | `strokeWidth`, `color`, `opacity`, `lineType`, `size`, `anchor` | — | — |
| `Geom_VLine` | `x`, `label` | — | `strokeWidth`, `color`, `opacity`, `lineType`, `size`, `anchor` | — | — |

### Examples Gallery

| | | |
|-|-|-|
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/scatterplot.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/bubbleplot.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/barchart.png)
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/candlestick.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/linechart.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/areachart.png)
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/barplot.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/stacked.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/hbarplot.png)
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/lolipop.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/errorbar.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/violin.png)
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/hex.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/ridgeline.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/choropleth.png)
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/sparkline.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/CFR.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/abline.png)
 
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/bubblemap.png)
