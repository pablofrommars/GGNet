# Geoms — the 21 layers

All signatures extracted from source (GGNet 2.0). Canonical form shown: `PlotContext` receiver, inherited source. Own-source overloads prepend a `source` parameter and type the selectors on `T2`; source parameter types vary by family and receiver. Shared tail `(bool x, bool y)? scale = null, bool inherit = true` elided as `…`. Events block = `onclick`, `onmouseover`, `onmouseout` (`Func<T, MouseEventArgs, Task>?`) and `tooltip` (`Func<T, RenderFragment>?`).

## Source-preserving overloads

The following parameter types are extracted from the current `BuilderExtensions.*.cs` members. `Source<T2>` and `StatSource<T2>` implement `IReadOnlyList<T2>` and bind to that overload.

| Family | `PlotContext` source types | `PanelFactory` source types |
|---|---|---|
| `Geom_Point` | `IReadOnlyList<T2>`, `IEnumerable<T2>` | `IReadOnlyList<T2>`, `IEnumerable<T2>` |
| `Geom_Tile` | `IReadOnlyList<T2>`, `IEnumerable<T2>` | `IReadOnlyList<T2>` |
| `Geom_Segment` | `IReadOnlyList<T2>`, `IEnumerable<T2>` | `IReadOnlyList<T2>` |
| `Geom_ABLine` | `IReadOnlyList<T2>`, `IEnumerable<T2>` | `IReadOnlyList<T2>`, `IEnumerable<T2>` |
| `Geom_ErrorBar` | `IReadOnlyList<T2>`, `IEnumerable<T2>` | `IReadOnlyList<T2>`, `IEnumerable<T2>` |
| `Geom_Hex` | `IReadOnlyList<T2>`, `IEnumerable<T2>` | `IReadOnlyList<T2>`, `IEnumerable<T2>` |
| `Geom_Map` | `IReadOnlyList<T2>`, `IEnumerable<T2>` | `IReadOnlyList<T2>`, `IEnumerable<T2>` |
| `Geom_Ribbon` | `IReadOnlyList<T2>`, `IEnumerable<T2>` | `IReadOnlyList<T2>`, `IEnumerable<T2>` |
| `Geom_RidgeLine` | `IReadOnlyList<T2>`, `IEnumerable<T2>` | `IReadOnlyList<T2>`, `IEnumerable<T2>` |
| `Geom_Violin` | `IReadOnlyList<T2>`, `IEnumerable<T2>` | `IReadOnlyList<T2>`, `IEnumerable<T2>` |

These families retain `IReadOnlyList<T2>` objects through geom construction, including inherited root sources. An `IEnumerable<T2>` argument that implements `IReadOnlyList<T2>` at runtime is retained too; other enumerables are materialized once when the chain is constructed. List mutations remain visible on later renders. Creating `new Source<T2>(items)` still explicitly copies the items.

Facet matching uses the existing `plotSource.Equals(layerSource)` check. For arrays and ordinary lists, passing the same object partitions the layer with the plot; a distinct `rows.ToList()` repeats its contents in every facet. Custom equality and boxed value-type lists retain their own `Equals` semantics.

Candlestick, OHLC, and Volume retain their existing `Source<T2>` explicit-source context signatures; this change does not add adapters to those families. See [beta migration notes](../patterns/common-mistakes.md#source-identity-corrections-in-20-beta) for the binary compatibility break.

## Explicit-source aesthetic inheritance

For the fourteen families exposing `inherit` (Area, Bar, Boxplot, ErrorBar, Hex, Line, Map, Point, Radar, Ribbon, RidgeLine, Text, Tile and Violin), missing mappings resolve when the layer is added, including with an explicit source. Explicit mappings win per aesthetic; `inherit: false` disables all fallback. Only aesthetics used by that geom are considered.

Inherited mappings must implement `IAestheticMapping<T2, TValue>` for the layer's row type. The interface is invariant: matching properties, a shared base class, or a shared interface do not make two row types interchangeable. An incompatible requested mapping throws `GGNetUserException` at the layer call, naming the aesthetic and row types; supply a layer mapping or opt out. A custom mapping implementing both required interfaces is compatible.

Explicit sources still require their positional selectors; `inherit` governs aesthetics, not selectors. Sourceless overloads retain their existing default selectors, including when `inherit: false`. Configure scales before adding layers; replacing a plot mapping later does not replace a mapping already captured by a layer.

## Data marks (full events block)

```csharp
Geom_Point(x?, y?, IAestheticMapping<T,double>? sizeBy, IAestheticMapping<T,string>? colorBy,
    events…, bool animation = false, double size = 5, string color = "#23d0fc", double opacity = 1.0, …)

Geom_Line(x?, y?, colorBy?, IAestheticMapping<T,LineType>? lineTypeBy, events…,
    double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0,
    LineType lineType = Solid, …, bool piecewise = false)

Geom_Bar(x?, y?, fillBy?, events…, string fill = "#23d0fc", double fillOpacity = 1.0,
    string strokeColor = "inherit", double strokeOpacity = 1.0, double strokeWidth = 0.0,
    PositionAdjustment position = PositionAdjustment.Stack,   // ← Stack by default!
    // Stack: negative values pile downward from the baseline; the axis covers both directions
    double width = 0.9, bool animation = false, …)

Geom_Area(x?, y?, fillBy?, events…, string fill = "#23d0fc", double fillOpacity = 1.0,
    PositionAdjustment position = PositionAdjustment.Identity, …)

Geom_Ribbon(x?, ymin?, ymax?, fillBy?, events…, fill, fillOpacity, …)   // band between ymin and ymax

Geom_ErrorBar(x?, y?, ymin?, ymax?, colorBy?, events…, strokeWidth = 1.07, color, opacity,
    lineType, double radius = 5, PositionAdjustment position = Identity, animation, …)

Geom_Segment(x, xend, y, yend,          // all four REQUIRED
    events…, strokeWidth = 1.07, color, opacity, lineType)

Geom_Tile(x, y, width, height,          // extent selectors REQUIRED, in data units
    fillBy?, events…, fill, fillOpacity, strokeColor = "inherit", strokeOpacity, strokeWidth = 0.0, …)

Geom_Hex(x?, y?, dx?, dy?, fillBy?, events…, fill, opacity, animation, …)   // hexagon per item

Geom_Radar(x?, y?, fillBy?, events…, fill, double fillOpacity = 0.25, strokeWidth = 2.0, …)
    // closed polygon over angular categories; implies polar coordinates

Geom_Map(source, Func<T2, Geospacial.Polygon[]> polygons, fillBy?, events…,
    Func<T2,(Geospacial.Point point, RenderFragment content)>? tooltip,   // positioned tooltip
    animation, fill, fillOpacity, string strokeColor = "#000000", double strokeWidth = 0, …)
    // PlotContext<T, double, double> only; all polygons of a layer emit as ONE multi-subpath <path>
    // Geospacial.Polygon { Longitude, Latitude, Hole } — Hole = true rings render as cut-outs, any winding
```

## Finance (events, no tooltip)

```csharp
Geom_Candlestick(x?, open?, high?, low?, close?, events…, strokeWidth = 1.07, color, opacity, lineType)
Geom_OHLC(       /* identical parameter surface */ )
Geom_Volume(x?, volume?, events…, fill, opacity)
```

## Statistical summaries (no events)

```csharp
Geom_Boxplot(x?, y?, fillBy?, double size = 0.8, fill, fillOpacity, strokeWidth = 2.0, …)
    // HORIZONTAL by data design: x carries the measurements, y the category

Geom_Violin(x?, y?, Func<T,double>? width,   // width = density profile at y — REQUIRED in practice
    fillBy?, fill, fillOpacity, string? strokeColor = null, PositionAdjustment position = Identity, …)
    // feed from Stat.Density: Geom_Violin(width: d => d.Density)

Geom_RidgeLine(x?, y?, Func<T,double>? height, fillBy?, fill, fillOpacity, …)
    // area of `height` (y-data units) above each row baseline
```

## Annotations (no events; one item per line/label)

```csharp
Geom_Text(x?, y?, Func<T,double>? angleBy, Func<T,TT>? text, colorBy?, Size? size = null,
    Anchor anchor = Middle, string weight = "normal", string style = "normal",
    string color = "#23d0fc", double angle = 0.0, …)

Geom_ABLine(a, b, label?, (bool x, bool y)? transformation = null,   // y = a·x + b
    strokeWidth = 1.07, color, opacity, lineType, Size? size, Anchor anchor = End, weight, style)

Geom_HLine(y, label, strokeWidth = 1.07, color, opacity, lineType, Size? size, Anchor anchor = End, weight, style)
Geom_VLine(x, label, /* same tail; label rotated ±90° */)
```

Annotations usually draw from their own small source:

```csharp
.Geom_HLine(new[] { 3.0 }, v => v, v => "level")
```

Annotation doctrine (explanatory charts, not exploratory): highlight the one series the point is about — accent color on it, constant gray on the rest — and print the key values with `Geom_Text` so the reader never computes. Long category labels: `.Flip()` to horizontal bars/lollipops instead of rotating labels; rotated axis text is a last resort.

## Multi-layer recipes

Layers stack in call order (first call = bottom). One source can feed several layers (`connected-scatter` = `Geom_Line` + `Geom_Point`); a layer can bring its own source (annotations, `bubble-map`'s polygons).

Tier-C compositions — compile- and render-verified in `TierCCompositionTests` (categorical axes need a struct type: enums or numeric slots, **not** `string`):

**Dot plot** — value on x, category on y (`Kpi(Team Team, double Value)`, `Team` an enum):

```csharp
PlotContext.Build(kpis, k => k.Value, k => k.Team)
	.Geom_Point()
	.Style()
```

**Dumbbell** — one segment per entity between before/after, a point layer per endpoint (`Change(Team Team, double Before, double After)`):

```csharp
PlotContext.Build(changes, c => c.Before, c => c.Team)
	.Geom_Segment(c => c.Before, c => c.After, c => c.Team, c => c.Team)
	.Geom_Point()
	.Geom_Point(x: c => c.After)
	.Style()
```

**Waffle** — caller computes the unit grid (`Unit(double Column, double Row, string Part)`, one row per cell, parts assigned by share):

```csharp
PlotContext.Build(units, u => u.Column, u => u.Row)
	.Scale_Fill_Discrete(u => u.Part, ["#23d0fc", "#fc9d23", "#8b5cf6"])
	.Geom_Tile(u => u.Column, u => u.Row, u => 0.95, u => 0.95)
	.Style()
```
