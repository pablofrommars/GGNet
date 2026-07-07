# Common mistakes

Three failure sources: R ggplot2 priors, GGNet 1.x priors (the published NuGet docs are 1.4.0 — stale), and DSL-convention violations. Each entry: wrong → right.

## R ggplot2 leaking in

```csharp
// WRONG — aes() and + composition do not exist
ggplot(data, aes(x: X, y: Y)) + geom_point()
// RIGHT — one fluent chain
PlotContext.Build(data, i => i.X, i => i.Y).Geom_Point().Style()
```

```csharp
// WRONG — there is no histogram geom
.Geom_Histogram(bins: 12)
// RIGHT — a histogram is Stat.Bin + Geom_Bar
PlotContext.Build(Stat.Bin(readings, r => r.Value, bins: 12), b => b.Mid, b => b.Count)
	.Geom_Bar(width: 1.0)
```

```csharp
// WRONG — theme objects do not exist
.Theme_Minimal()  /  .Style(theme: "minimal")
// RIGHT — layout via Style record, paint via CSS variables (.ggnet[theme=name] { --ggnet-bg: …; })
//         theme picked on the Plot component (Theme="…") or export (theme: "…")
```

```csharp
// WRONG — string aesthetics
.Geom_Point(colorBy: "Category")
// RIGHT — a mapping built by a scale call, usually inherited from the chain
.Scale_Color_Discrete(i => i.Category, ["#23d0fc", "#fc9d23"])
.Geom_Point()
```

## GGNet 1.x priors (renamed/removed in 2.0)

| 1.x (WRONG now) | 2.0 |
|---|---|
| `width:` for stroke width on line-family geoms | `strokeWidth:` (`width` = geometric extent in data units, only on Bar/Tile/Violin) |
| `alpha:` | `opacity:` / `fillOpacity:` / `strokeOpacity:` |
| `_color:` / `_fill:` / `_size:` mapping parameters | `colorBy:` / `fillBy:` / `sizeBy:` / `lineTypeBy:` |
| `format: "N2"` / `timezone:` string parameters | `formatter: new DoubleFormatter("N2")` — `IFormatter<T>` everywhere |
| `GGNet.Static` namespace/package | `GGNet.Headless` |
| `.Set(...)` | `.Commit(...)` |
| `IWaiver` | `NoData` |
| HTML-fragment export | pure SVG only (`AsStringAsync`/`SaveAsync`) |

## DSL conventions violated

```csharp
// WRONG — positional arguments past the selectors
.Geom_Point(i => i.X, i => i.Y, null, null, null, null, null, null, false, 3)
// RIGHT — selectors positional, everything after by name
.Geom_Point(i => i.X, i => i.Y, size: 3)
```

```csharp
// WRONG — grouped stat faceted on a different key
Build(Stat.Bin(r, x => x.V, x => x.Tank), b => b.Mid, b => b.Count).Facet_Wrap(b => b.Count)
// RIGHT — facet the SAME key the stat grouped by
Build(Stat.Bin(r, x => x.V, x => x.Tank), b => b.Mid, b => b.Count).Facet_Wrap(b => b.Group)
```

```csharp
// WRONG — pre-aggregating raw data in LINQ (stale on re-render, loses the stat contract)
var avgs = readings.GroupBy(r => r.Tank).Select(g => new { Tank = g.Key, Avg = g.Average(r => r.Value) });
// RIGHT — stats are sources, recomputed every render
PlotContext.Build(Stat.Summary(readings, r => r.Tank, r => r.Value), s => s.X, s => s.Center)
	.Geom_ErrorBar(ymin: s => s.Lower, ymax: s => s.Upper)
```

```csharp
// WRONG — events on annotation/summary geoms (they take no event block)
.Geom_Boxplot(onclick: ...)  /  .Geom_HLine(y, label, tooltip: ...)
// RIGHT — interactivity lives on data-mark geoms only
```

```csharp
// WRONG — Stat.Summary's x selector is Func<T, double>; a string category does not convert
Stat.Summary(readings, r => r.Tank, r => r.GravityPoints)          // CS0029 when Tank is string
// RIGHT — give each category a stable numeric slot; the groupBy key carries the name
var tanks = readings.Select(r => r.Tank).Distinct().Order().ToArray();
Stat.Summary(readings, r => Array.IndexOf(tanks, r.Tank) + 1.0, r => r.GravityPoints, r => r.Tank)
// then color/legend by s => s.Group, and hide the numeric x axis if it carries no meaning
// (the pinned summary-errorbar example has a numeric Tank — don't copy its x selector for string categories)
```

## Sharp edges found the hard way

```csharp
// WRONG — invisible bubbles: Scale_Size_Continuous's range is the RADIUS IN PIXELS, default (0, 1)
.Scale_Size_Continuous(i => i.Value)
// RIGHT — always pass an explicit pixel range
.Scale_Size_Continuous(i => i.Value, range: (3, 9))
```

```csharp
// WRONG — expecting Geom_Bar to overlay by default
.Geom_Bar()   // with a fill scale, series STACK — Stack is the default position
// RIGHT — be explicit when you want something else
.Geom_Bar(position: PositionAdjustment.Dodge)   // or Identity
```

## Observed in baseline runs (agents without this skill, 2026-07-07)

```csharp
// WRONG — invented entry points; there is no Plot.New and no GGNet.Data<T,TX,TY>
var plot = Plot.New(items, x: i => i.X, y: i => i.Y)...
GGNet.Data<TankGravity, string, double>? plot;
// RIGHT — the entry point is PlotContext.Build; the built plot is IPlotContext
IPlotContext? plot;
plot = PlotContext.Build(items, i => i.X, i => i.Y)....Style();
```

```csharp
// WRONG — no Theme() on the chain
.Theme(dark: false)
// RIGHT — theme is picked at the hosting/export boundary, not in the DSL
<Plot Context="plot" RenderMode="..." Theme="default" />   // or AsStringAsync(theme: "...")
```

```razor
@* WRONG — invented component parameters *@
<GGNet.Components.Plot Data=@plot T=Item TX=string TY=double RenderPolicy=RenderPolicy.Auto />
@* RIGHT — Context + RenderMode (required), Width/Height/Theme optional *@
<Plot Context="plot" RenderMode="..." Width="720" Height="432" />
```

```csharp
// WRONG — discrete scales need a palette; Geom_Text's first positional is the X SELECTOR, not the text
.Scale_Fill_Discrete(i => i.Tank)
.Geom_Text(i => $"{i.Value:F1}")
// RIGHT
.Scale_Fill_Discrete(i => i.Tank, ["#23d0fc", "#fc9d23"])
.Geom_Text(text: i => $"{i.Value:F1}")
```

- GGNet 2.0 **does** have `Coord_Polar()` — it's the arc *geoms* (pie/rose wedges) that don't exist. Don't claim polar coordinates are missing; do refuse pies.

```csharp
// WRONG — dual y-axes to compare two series (the perceived relationship is an
// artifact of the two axis ranges; GGNet deliberately has no second y-axis)
// RIGHT — plot one against the other, index both to 100, or facet
PlotContext.Build(pairs, p => p.SeriesA, p => p.SeriesB).Geom_Point()   // connected-scatter for trajectories
```

```csharp
// WRONG — faking grouped bars with adjacent categories (grouping needs proximity cues)
// RIGHT — dodge does the within-group/between-group spacing correctly
.Scale_Fill_Discrete(i => i.Series, palette)
.Geom_Bar(position: PositionAdjustment.Dodge)
```

## Sharp edges (cont.)

- `Geom_Boxplot` is **horizontal by data design**: x carries the measurements, y the category — `Build(grouped, i => i.Value, i => i.Group)`.
- `Geom_Violin` draws a *precomputed* profile: `width:` is the density at each y; feed it from `Stat.Density` (`width: d => d.Density`).
- Time axes are NodaTime (`LocalDate`/`LocalDateTime`/`Instant` `Build` overloads) — never `DateTime`.
- Labels (`Title`, `XLab`, …) render Markdown; escape user text if it may contain Markdown syntax.
