# Scales, facets, composition

All signatures extracted from source (GGNet 2.0).

## Position scales

X scales chain on the `PlotContext`; Y scales exist on both `PlotContext` and per-panel `PanelFactory`.

```csharp
Scale_X_Continuous(ITransformation<double>? transformation = null,
    (double? min, double? max)? limits = null,
    (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
    IFormatter<double>? formatter = null, bool hide = false, bool includeMinorBreaks = true)
Scale_Y_Continuous( /* same shape */ )

Scale_X_Log10(...) / Scale_Y_Log10(...) / Scale_X_Sqrt(...) / Scale_Y_Sqrt(...)
    // Continuous with the Log10/Sqrt transformation pre-applied — use for skewed / power-law data

Scale_X_Discrete((TX? min, TX? max)? limits = null, expand = null,
    IFormatter<TX>? formatter = null, double offset = 0.0, bool hide = false)
Scale_Y_Discrete( /* same over TY */ )

Scale_X_Discrete_Date(...)      // LocalDate axis, day/month tick labeling
Scale_X_Discrete_DateTime(...)  // LocalDateTime axis
Scale_X_Instant(Instant? start = null, Instant? end = null, IFormatter<Instant>? formatter = null)

Scale_Longitude((double? min, double? max)? limits = null)   // default (-180, 180), degree labels
Scale_Latitude((double? min, double? max)? limits = null)    // default (-90, 90); context or panel

XLim(min, max) / YLim(min, max)   // shorthand for limits only
```

NodaTime rules the time axes: `Build` has dedicated overloads for `LocalDate`, `LocalDateTime`, `Instant` selectors that pre-select the axis type. Never `DateTime`.

## Aesthetic scales — one call registers scale + legend + mapping

The `Scale_Color/Fill/Size/LineType_*` call on the chain creates the mapping that the geoms' `colorBy`/`fillBy`/`sizeBy`/`lineTypeBy` parameters inherit (`inherit: true` default). You rarely pass `xxxBy:` explicitly — chain the scale before the geom.

```csharp
Scale_Color_Discrete(Func<T,TKey> selector, string[] palette, int direction = 1, bool guide = true, string? name = null)
Scale_Color_Discrete(selector, Palettes.Discrete<TKey,string> palette, guide, name)   // explicit key→color map
Scale_Color_Identity(Func<T,string> selector)        // data carries literal colors; no legend

Scale_Fill_Discrete( /* same two shapes */ )
Scale_Fill_Continuous(Func<T,double> selector, string[] palette, int m = 5,
    IFormatter<double>? formatter = null, bool guide = true, string? name = null)
Scale_Fill_Identity(Func<T,string> selector)

Scale_Size_Continuous(Func<T,double> selector, (double min, double max)? limits = null,
    (double min, double max)? range = null,           // RADIUS IN PIXELS — default (0, 1) is sub-pixel!
    bool oob = false, bool guide = true, string? name = null, IFormatter<double>? formatter = null)
Scale_Size_Identity(Func<T,double> selector)

Scale_LineType_Discrete(selector, LineType[]? palette = null, direction, guide, name)
Scale_LineType_Identity(Func<T,LineType> selector)
```

Palettes: pass explicit `string[]`, or use `Colors` (including `Colors.Brewer` ColorBrewer sets keyed by class count), or `Palettes.Discrete<TKey,TValue>` for fixed key→value maps. Geom color parameters also accept CSS custom properties: `color: "var(--color-temperature)"`.

## Formatters

`IFormatter<T>` is the only formatting concept (no `format:`/`timezone:` strings): `DoubleFormatter("N2")`, `InstantFormatter`, `Standard<T>`, `Labeller`/`DiscreteLabeller`, `Longitude`/`Latitude`. When you transform an axis (`_Log10`, `_Sqrt`), keep the formatter honest — ticks show data values, the transform changes spacing.

## Facets & composition

```csharp
Facet_Wrap(Func<T,TKey> selector, bool freeX = false, bool freeY = false, int? nrows = null, int? ncolumns = null)
Facet_Grid(Func<T,TRow> row, Func<T,TColumn> column, bool freeX = false, bool freeY = false)
Flip()                              // swap axes; bar statistics follow the flip
Coord_Polar(double startAngle = -Math.PI / 2.0, bool clockwise = true)
Panel(factory, double width = 1.0, double height = 1.0, onClick?)   // explicit sub-panels (grid composition)
Title("…") SubTitle("…") Caption("…") XLab("…") YLab("…")           // Markdown supported
Style(Style? style = null, Position axisY = Left, Position legend = Right)   // terminal call
```

The grouping ladder — escalate only under pressure:
1. constant aesthetics (one series);
2. `colorBy`/`fillBy`/`lineTypeBy` + discrete scale (few series, one panel, legend);
3. `position: PositionAdjustment.Dodge | Stack` (subgroups on bar/area);
4. `Facet_Wrap`/`Facet_Grid` (too many series for one panel; pair with grouped stats on the same key).
