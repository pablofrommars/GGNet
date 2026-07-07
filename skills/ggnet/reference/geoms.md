# Geoms — the 21 layers

All signatures extracted from source (GGNet 2.0). Canonical form shown: `PlotContext` receiver, inherited source. Every geom also has `PanelFactory` receivers and own-source overloads (`Source<T2>` / `IEnumerable<T2>` / `IReadOnlyList<T2>` prepend a `source` parameter and type the selectors on `T2`). Shared tail `(bool x, bool y)? scale = null, bool inherit = true` elided as `…`. Events block = `onclick`, `onmouseover`, `onmouseout` (`Func<T, MouseEventArgs, Task>?`) and `tooltip` (`Func<T, RenderFragment>?`).

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
    animation, fill, fillOpacity, string stroke = "#000000", double strokeWidth = 0, …)
    // PlotContext<T, double, double> only; all polygons of a layer emit as ONE multi-subpath <path>
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
    fillBy?, fill, fillOpacity, string? stroke = null, PositionAdjustment position = Identity, …)
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
