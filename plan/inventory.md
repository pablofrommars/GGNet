# GGNet 2.0 API Inventory

*Phase 1 step 1.1 deliverable (PLAN.md). Extracted 2026-07-07 from source on `ai-skills-mcp-jul2026` (base `1f2bd83`) by script (`public static` declaration scan with balanced-paren capture) — signatures below are verbatim, not recalled. Feeds 1.5 (SKILL.md + reference files) and 1.6 (common-mistakes). Cross-checked against the pinned public surface (`tests/GGNet.Headless.Tests/Api/PublicApiTests.GGNet.verified.txt`, 74 types) — see §11.*

## 1. Entry points — `PlotContext.Build`

```csharp
PlotContext<T, double, double>        Build<T>(IReadOnlyList<T> source, Func<T, double>? x = null, Func<T, double>? y = null)
PlotContext<T, TX, TY>                Build<T, TX, TY>(IReadOnlyList<T> source, Func<T, TX> x, Func<T, TY> y)
PlotContext<T, TX, double>            Build<T, TX>(IReadOnlyList<T> source, Func<T, TX> x, Func<T, double>? y = null)
PlotContext<T, double, TY>            Build<T, TY>(IReadOnlyList<T> source, Func<T, double> x, Func<T, TY> y)
PlotContext<T, LocalDate, double>     Build<T>(IReadOnlyList<T> source, Func<T, LocalDate> x, Func<T, double>? y = null)
PlotContext<T, LocalDateTime, double> Build<T>(IReadOnlyList<T> source, Func<T, LocalDateTime> x, Func<T, double>? y = null)
PlotContext<T, Instant, double>       Build<T>(IReadOnlyList<T> source, Func<T, Instant> x, Func<T, double>? y = null)
PlotContext<NoData, double, double>   Build()                                     // empty-state plot
```

`Build` fixes the source and default `x`/`y` selectors; every geom can override per-layer. NodaTime overloads pre-select the matching position scale types. `Stat.*` outputs plug in as `source` (see §5).

## 2. Geoms — 21, canonical signatures

Overload families per geom (verified consistent by `OverloadConsistencyTests`): `PlotContext` and `PanelFactory` receivers × inherited source vs own source (`Source<T2>` / `IEnumerable<T2>` / `IReadOnlyList<T2>` — own-source variants prepend `source` and type the selectors on `T2`). Canonical form below is the `PlotContext`, inherited-source variant. Shared trailing params `(bool x, bool y)? scale = null, bool inherit = true` elided as `…` where present.

```csharp
// -- data marks --------------------------------------------------------------
Geom_Point(Func<T,TX>? x = null, Func<T,TY>? y = null, IAestheticMapping<T,double>? sizeBy = null,
    IAestheticMapping<T,string>? colorBy = null, onclick, onmouseover, onmouseout, tooltip,
    bool animation = false, double size = 5, string color = "#23d0fc", double opacity = 1.0, …)

Geom_Line(Func<T,TX>? x = null, Func<T,TY>? y = null, IAestheticMapping<T,string>? colorBy = null,
    IAestheticMapping<T,LineType>? lineTypeBy = null, onclick, onmouseover, onmouseout, tooltip,
    double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
    …, bool piecewise = false)

Geom_Bar(Func<T,TX>? x = null, Func<T,TY>? y = null, IAestheticMapping<T,string>? fillBy = null,
    onclick, onmouseover, onmouseout, tooltip, string fill = "#23d0fc", double fillOpacity = 1.0,
    string strokeColor = "inherit", double strokeOpacity = 1.0, double strokeWidth = 0.0,
    PositionAdjustment position = PositionAdjustment.Stack, double width = 0.9, bool animation = false, …)

Geom_Area(Func<T,TX>? x = null, Func<T,TY>? y = null, IAestheticMapping<T,string>? fillBy = null,
    onclick, onmouseover, onmouseout, tooltip, string fill = "#23d0fc", double fillOpacity = 1.0,
    PositionAdjustment position = PositionAdjustment.Identity, …)

Geom_Ribbon(Func<T,TX>? x = null, Func<T,TY>? ymin = null, Func<T,TY>? ymax = null,
    IAestheticMapping<T,string>? fillBy = null, onclick, onmouseover, onmouseout, tooltip,
    string fill = "#23d0fc", double fillOpacity = 1.0, …)

Geom_ErrorBar(Func<T,TX>? x = null, Func<T,TY>? y = null, Func<T,TY>? ymin = null, Func<T,TY>? ymax = null,
    IAestheticMapping<T,string>? colorBy = null, onclick, onmouseover, onmouseout, tooltip,
    double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
    double radius = 5, PositionAdjustment position = PositionAdjustment.Identity, bool animation = false, …)

Geom_Segment(Func<T,TX> x, Func<T,TX> xend, Func<T,TY> y, Func<T,TY> yend,   // all four required
    onclick, onmouseover, onmouseout, tooltip,
    double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid)

Geom_Tile(Func<T,TX> x, Func<T,TY> y, Func<T,double> width, Func<T,double> height,   // extent in data units
    IAestheticMapping<T,string>? fillBy = null, onclick, onmouseover, onmouseout, tooltip,
    string fill = "#23d0fc", double fillOpacity = 1.0, string strokeColor = "inherit",
    double strokeOpacity = 1.0, double strokeWidth = 0.0, …)

Geom_Hex(Func<T,TX>? x = null, Func<T,TY>? y = null, Func<T,TX>? dx = null, Func<T,TY>? dy = null,
    IAestheticMapping<T,string>? fillBy = null, onclick, onmouseover, onmouseout, tooltip,
    string fill = "#23d0fc", double opacity = 1.0, bool animation = false, …)

Geom_Radar(Func<T,TX>? x = null, Func<T,TY>? y = null, IAestheticMapping<T,string>? fillBy = null,
    onclick, onmouseover, onmouseout, tooltip, string fill = "#23d0fc", double fillOpacity = 0.25,
    double strokeWidth = 2.0, …)                              // implies polar coordinates

Geom_Map(Source<T2> source, Func<T2, Geospacial.Polygon[]> polygons,          // PlotContext<T1,double,double> only
    IAestheticMapping<T2,string>? fillBy = null, onclick, onmouseover, onmouseout,
    Func<T2,(Geospacial.Point point, RenderFragment content)>? tooltip = null, bool animation = false,
    string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double strokeWidth = 0, …)

// -- finance -----------------------------------------------------------------
Geom_Candlestick(Func<T,TX>? x = null, Func<T,TY>? open = null, Func<T,TY>? high = null,
    Func<T,TY>? low = null, Func<T,TY>? close = null, onclick, onmouseover, onmouseout,
    double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid)

Geom_OHLC(   /* identical parameter surface to Geom_Candlestick */ )

Geom_Volume(Func<T,TX>? x = null, Func<T,TY>? volume = null, onclick, onmouseover, onmouseout,
    string fill = "#23d0fc", double opacity = 1.0)

// -- statistical summaries (no event block) -----------------------------------
Geom_Boxplot(Func<T,TX>? x = null, Func<T,TY>? y = null, IAestheticMapping<T,string>? fillBy = null,
    double size = 0.8, string fill = "#23d0fc", double fillOpacity = 1.0, double strokeWidth = 2.0, …)
    // horizontal by data design: x carries the measurements, y the category

Geom_Violin(Func<T,TX>? x = null, Func<T,TY>? y = null, Func<T,double>? width = null,   // width = density profile, required
    IAestheticMapping<T,string>? fillBy = null, string fill = "#23d0fc", double fillOpacity = 1.0,
    string? stroke = null, PositionAdjustment position = PositionAdjustment.Identity, …)

Geom_RidgeLine(Func<T,TX>? x = null, Func<T,TY>? y = null, Func<T,double>? height = null,
    IAestheticMapping<T,string>? fillBy = null, string fill = "#23d0fc", double fillOpacity = 1.0, …)

// -- annotations (no event block) ----------------------------------------------
Geom_Text(Func<T,TX>? x = null, Func<T,TY>? y = null, Func<T,double>? angleBy = null, Func<T,TT>? text = null,
    IAestheticMapping<T,string>? colorBy = null, Size? size = null, Anchor anchor = Middle,
    string weight = "normal", string style = "normal", string color = "#23d0fc", double angle = 0.0, …)

Geom_ABLine(Func<T,double> a, Func<T,double> b, Func<T,string>? label = null,   // y = a·x + b
    (bool x, bool y)? transformation = null, double strokeWidth = 1.07, string color = "#23d0fc",
    double opacity = 1.0, LineType lineType = Solid, Size? size = null, Anchor anchor = End,
    string weight = "normal", string style = "normal")

Geom_HLine(Func<T,TY> y, Func<T,string> label, double strokeWidth = 1.07, string color = "#23d0fc",
    double opacity = 1.0, LineType lineType = Solid, Size? size = null, Anchor anchor = End,
    string weight = "normal", string style = "normal")

Geom_VLine(Func<T,TX> x, Func<T,string> label, /* same tail as Geom_HLine */)
```

Interactivity block (`onclick`/`onmouseover`/`onmouseout` are `Func<T, MouseEventArgs, Task>?`, `tooltip` is `Func<T, RenderFragment>?`): present on all data marks and finance geoms (finance: events only, no `tooltip` except `Geom_Map`'s positioned tooltip); absent on statistical summaries and annotations.

## 3. Composition, coordinates, labels

```csharp
Flip()                                                            // swap axes; bar statistics follow
Coord_Polar(double startAngle = -Math.PI / 2.0, bool clockwise = true)
Facet_Wrap(Func<T,TKey> selector, bool freeX = false, bool freeY = false, int? nrows = null, int? ncolumns = null)
Facet_Grid(Func<T,TRow> row, Func<T,TColumn> column, bool freeX = false, bool freeY = false)
Panel(Func<PanelFactory<T,TX,TY>, PanelFactory<T,TX,TY>> factory, double width = 1.0, double height = 1.0,
      Func<MouseEventArgs, Task>? onClick = null)                 // sub-panel (grid composition)
XLim(double? min = null, double? max = null)   // + LocalDate overload; YLim on context and panel
Title(string) SubTitle(string) Caption(string) XLab(string) YLab(string)   // all [StringSyntax("Markdown")]
Style(Style? style = null, Position axisY = Left, Position legend = Right) // terminal call → IPlotContext
```

## 4. Scales

Position (x: `PlotContext` only; y: `PlotContext` and per-`PanelFactory`):

```csharp
Scale_X_Continuous(ITransformation<double>? transformation = null, (double? min, double? max)? limits = null,
    (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
    IFormatter<double>? formatter = null, bool hide = false, bool includeMinorBreaks = true)
Scale_Y_Continuous( /* same shape */ )
Scale_X_Sqrt(...) Scale_X_Log10(...) Scale_Y_Sqrt(...) Scale_Y_Log10(...)   // Continuous with Sqrt/Log10 transformation
Scale_X_Discrete((TX? min, TX? max)? limits = null, expand = null, IFormatter<TX>? formatter = null,
    double offset = 0.0, bool hide = false)
Scale_Y_Discrete( /* same shape over TY */ )
Scale_X_Discrete_Date / Scale_X_Discrete_DateTime(limits = null, expand = null)   // day/month tick labeling
Scale_X_Instant(Instant? start = null, Instant? end = null, IFormatter<Instant>? formatter = null)
Scale_Longitude((double? min, double? max)? limits = null)   // default (-180, 180)
Scale_Latitude((double? min, double? max)? limits = null)    // default (-90, 90); context or panel
```

Aesthetic (all return the mapping consumed by the geom's `xxxBy` — the same call registers scale + legend):

```csharp
Scale_Color_Discrete(Func<T,TKey> selector, string[] palette, int direction = 1, bool guide = true, string? name = null)
Scale_Color_Discrete(Func<T,TKey> selector, Palettes.Discrete<TKey,string> palette, bool guide = true, string? name = null)
Scale_Color_Identity(Func<T,string> selector)                     // data carries literal colors; no legend
Scale_Fill_Discrete( /* same two shapes as Color */ )
Scale_Fill_Continuous(Func<T,double> selector, string[] palette, int m = 5, IFormatter<double>? formatter = null,
    bool guide = true, string? name = null)
Scale_Fill_Identity(Func<T,string> selector)
Scale_Size_Continuous(Func<T,double> selector, (double min, double max)? limits = null,
    (double min, double max)? range = null, bool oob = false, bool guide = true, string? name = null,
    IFormatter<double>? formatter = null)
Scale_Size_Identity(Func<T,double> selector)
Scale_LineType_Discrete(Func<T,TKey> selector, LineType[]? palette = null, int direction = 1, bool guide = true, string? name = null)
Scale_LineType_Discrete(Func<T,TKey> selector, Palettes.Discrete<TKey,LineType> palette, bool guide = true, string? name = null)
Scale_LineType_Identity(Func<T,LineType> selector)
```

Palettes: `Colors` (incl. `Colors.Brewer` — ColorBrewer sets as `FrozenDictionary<int, string[]>`, e.g. `Blues`), `Palettes.Discrete<TKey, TValue>` for explicit key→value maps.

## 5. Stats — typed sources (`StatSource<TOut>`, recomputed per render)

```csharp
Stat.Bin<T>(IReadOnlyList<T> source, Func<T,double> selector, int bins = 30)                    → StatSource<Bin>
Stat.Bin<T,TKey>(source, selector, Func<T,TKey> groupBy, int bins = 30)                        → StatSource<Bin<TKey>>
Stat.Density<T>(source, selector, double? bandwidth = null, int n = 512, double? from = null, double? to = null)
                                                                                               → StatSource<DensityPoint>
Stat.Density<T,TKey>(source, selector, groupBy, double? bandwidth = null, int n = 512)         → StatSource<DensityPoint<TKey>>
Stat.Count<T,TKey>(source, Func<T,TKey> selector)                                              → StatSource<Count<TKey>>
Stat.Summary<T>(source, Func<T,double> x, Func<T,double> y, double spread = 1.0)               → StatSource<Summary>
Stat.Summary<T,TKey>(source, x, y, groupBy, double spread = 1.0)                               → StatSource<Summary<TKey>>
Stat.Nrd0(IReadOnlyList<double> values)   // Silverman's rule-of-thumb bandwidth, exposed
```

Output records (grouped variants prepend `TKey Group`):

```csharp
Bin(double Min, double Mid, double Max, int Count, double Density)
DensityPoint(double At, double Density)
Count<TKey>(TKey Key, int N)
Summary(double X, double Center, double Lower, double Upper)
```

Draw-with pairs: `Bin` → `Geom_Bar(x: b => b.Mid, y: b => b.Count)`; `DensityPoint` → `Geom_Area/Line`, `Geom_Violin(width: d => d.Density)`; `Count` → `Geom_Bar`; `Summary` → `Geom_ErrorBar(y: s => s.Center, ymin: s => s.Lower, ymax: s => s.Upper)`. Per-facet statistics = `groupBy:` + facet on `Group` (key stated twice by design).

## 6. Formatters (`GGNet.Formats`)

`IFormatter<T>` (the only formatting concept — `format:`/`timezone:` strings retired in 2.0); implementations: `DoubleFormatter` (numeric format string), `InstantFormatter`, `Standard<T>`, `Labeller`/`DiscreteLabeller` (discrete break labels), `Longitude`/`Latitude` (degree formatting).

## 7. Theming

- Layout is C#: `Style` record with nested option groups `StylePlot`, `StylePanel`, `StylePanelSpacing`, `StyleAxis`, `StyleAxisText`, `StyleAxisTitle`, `StyleLegend`, `StyleStrip`, `StyleStripText`, `StylePolar` (source: `Style.*.cs` partials) — passed to the terminal `.Style(style)` call.
- Paint is CSS: base rules in `src/GGNet/Themes/Default.css` read every paint through a variable; a theme overrides variables under `.ggnet[theme=name]`. Full variable set (16): `--ggnet-axis-title`, `--ggnet-bg`, `--ggnet-break-label`, `--ggnet-break-title`, `--ggnet-caption`, `--ggnet-font`, `--ggnet-grid`, `--ggnet-legend-label`, `--ggnet-legend-title`, `--ggnet-spinner-accent`, `--ggnet-spinner-display`, `--ggnet-spinner-track`, `--ggnet-strip`, `--ggnet-sub-title`, `--ggnet-title`, `--ggnet-tooltip-bg`. Contract enforced by `ThemeContractTests`.
- Geom color parameters accept CSS custom properties (`color: "var(--color-temperature)"`).
- `--ggnet-font` changes rendering only; server-side text measurement assumes Inter.

## 8. Blazor components (`GGNet.Components`)

- `Plot<T,TX,TY>` — hosts a built plot. Parameters: `Context` (required), `RenderMode` (required; enum `RenderMode`), `Width = 720`, `Height = 576`, `Theme = "default"`.
- `SparkLine<T,TX,TY>` — compact inline variant: `Width = 150`, `Height = 50`; `SparkLineTooltip`.
- Supporting: `Tooltip`/`TooltipBase`, `Panel`, `Area`, `VerticalOpacityGradient`, `Zone`, `ICoord`, `ITooltip`.

## 9. Headless (`GGNet.Headless`)

```csharp
Task<string> AsStringAsync(this IPlotContext context, double width = 720, double height = 576,
                           string theme = "default", bool selfContained = false)
Task SaveAsync(this IPlotContext context, string fn, /* same tail */)
```

`selfContained: true` embeds the bundled theme CSS as a `<style>` element. Output is pure well-formed SVG (gallery tests `XDocument.Parse` it). Supporting types: `Host`, `SVGRenderer`, `StaticRenderer`, `ThemeCss`, `RenderedComponent`, `ContainerComponent`.

## 10. Shared vocabulary (enums & small types)

`Anchor` (Start/Middle/End…), `LineType` (Solid/Dashed/…), `Position` (Left/Right/…), `PositionAdjustment` (Identity/Stack/Dodge), `RenderMode`, `RenderTarget` (Render/Loading), `CoordSystem`, `PolarOptions`/`PolarRingType`, `Direction`, `Guide`, `Size`, `Units`, `Elements.Margin`/`Elements.Text`, `Geospacial.Point`/`Polygon`, `NoData`, `Source<T>`, exceptions `GGNetException`/`GGNetInternalException`/`GGNetUserException`.

## 11. Cross-check against the pinned public surface

All 74 types in `PublicApiTests.GGNet.verified.txt` are accounted for by §1–§10 (DSL: `BuilderExtensions`, `PlotContext`/`PlotContext<,,>`/`IPlotContext`, `PanelFactory<,,>`; stats: `Stat`, `StatSource<>`, `IStatSource`, 8 output records; scales: `IAestheticMapping`(+`<,>`), `ITransformation<>`; formats: 4 + `IFormatter<>`; palettes: `Colors`(+`Brewer`), `Palettes.Discrete<,>`; style: `Style` + 10 nested; components: 12; vocabulary: §10; internals surfaced for rendering: `Data.Panel<,,>`, `Rendering.IRenderModeHandler`/`IChildRenderModeHandler`, `_Imports`). README DSL tables (geoms/stats) agree with the extracted signatures — no discrepancies found.

Notables the earlier planning docs did not capture:
- **`Coord_Polar` is a released surface** (with `PolarOptions`, `StylePolar`) — polar coordinates exist; the *arc geoms* (pie/rose) remain backlog. `Geom_Radar` implies polar.
- Labels (`Title`/`SubTitle`/`Caption`/`XLab`/`YLab`) accept **Markdown**.
- `Geom_Bar` defaults to `position: Stack` (not Identity); `Geom_Boxplot` is horizontal by data design (x = measurements, y = category).
- `Panel(...)` sub-panel composition and `NoData`/`Build()` empty-state are part of the DSL.
- `Stat.Nrd0` (bandwidth rule) is public.
