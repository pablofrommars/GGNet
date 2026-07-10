# Stats — typed sources, not layers

Each `Stat.*` call returns a `StatSource<TOut>` that plugs into `PlotContext.Build(...)` as the source; any geom draws it unchanged, and it is **recomputed on every render pass** (streaming data stays current). There is no histogram/density/count geom — the stat produces, a plain geom draws.

## Signatures (extracted from source)

```csharp
Stat.Bin<T>(IReadOnlyList<T> source, Func<T,double> selector, int bins = 30)          → StatSource<Bin>
Stat.Bin<T,TKey>(source, selector, Func<T,TKey> groupBy, int bins = 30)               → StatSource<Bin<TKey>>

Stat.Density<T>(source, selector, double? bandwidth = null, int n = 512,
                double? from = null, double? to = null)                                → StatSource<DensityPoint>
Stat.Density<T,TKey>(source, selector, groupBy, double? bandwidth = null, int n = 512) → StatSource<DensityPoint<TKey>>
// default bandwidth: Stat.Nrd0 (Silverman's rule), itself public

Stat.Count<T,TKey>(source, Func<T,TKey> selector)                                      → StatSource<Count<TKey>>

Stat.Summary<T>(source, Func<T,double> x, Func<T,double> y, double spread = 1.0)       → StatSource<Summary>
Stat.Summary<T,TKey>(source, x, y, groupBy, double spread = 1.0)                       → StatSource<Summary<TKey>>
```

Output records — grouped variants prepend `TKey Group`:

```csharp
Bin(double Min, double Mid, double Max, int Count, double Density)
DensityPoint(double At, double Density)
Count<TKey>(TKey Key, int N)
Summary(double X, double Center, double Lower, double Upper)
```

## Draw-with pairs (all pinned in the gallery)

| Recipe | Chain |
|---|---|
| Histogram | `Build(Stat.Bin(readings, r => r.Value, bins: 12), b => b.Mid, b => b.Count).Geom_Bar(width: 1.0)` |
| Density | `Build(Stat.Density(readings, r => r.Value, n: 128), d => d.At, d => d.Density).Geom_Area()` |
| Count bar | `Build(Stat.Count(events, e => e.Category), c => c.Key, c => c.N).Geom_Bar()` |
| Summary + error bars | `Build(Stat.Summary(readings, r => r.Tank, r => r.Value), s => s.X, s => s.Center).Geom_ErrorBar(ymin: s => s.Lower, ymax: s => s.Upper)` |
| Violin from density | `Build(Stat.Density(readings, r => r.Value, r => r.Tank, n: 64), d => d.Group, d => d.At).Geom_Violin(width: d => d.Density)` |

## The two rules that matter

1. **Per-facet statistics are grouped statistics.** Compute with `groupBy:` and facet the output on the **same** key — the key is deliberately stated twice; a mismatch between them is almost certainly a bug:

```csharp
PlotContext.Build(Stat.Bin(readings, r => r.Value, r => r.Tank, bins: 10), b => b.Mid, b => b.Count)
	.Geom_Bar(width: 1.0)
	.Facet_Wrap(b => b.Group)
	.Style();
```

2. **Stats bridge raw data to aggregated charts.** A barplot/lollipop/dot-plot expects one row per group; when you have raw observations, don't pre-aggregate in LINQ — `Stat.Count` (frequencies) or `Stat.Summary` (center + spread, drawn with error bars) is the deterministic, render-refreshing way there. Stats run over the whole source (per group when grouped); stats that would depend on panel-trained state are out of scope by design.
