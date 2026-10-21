# Stats — typed sources, not layers

Each stat factory returns a `StatSource<TOut>` with its initial items computed eagerly. It plugs into `PlotContext.Build(...)` or an explicit geom source; rendering refreshes it so streaming data stays current. There is no histogram/density/count geom — the stat produces, a plain geom draws.

A render pass refreshes the root stat and its directly composed built-in dependencies before constructing facets, then discovers additional sources from the actual active geom layers before training. Each source object refreshes once per pass, upstream first, including the first export. Explicit panels shadow default layers when not faceting; facets use the default layers. Sources belonging only to shadowed factories or absent facet panels are not refreshed.

Direct `Recompute()` remains shallow: it updates only that source from the currently cached upstream items. Built-in factories retain direct `IStatSource` inputs by identity. Custom `IStatSource` implementations are opaque leaves whose existing `Recompute()` is invoked; dependencies hidden inside selectors, custom closures, or copied adapters are not discovered. If a built-in source's recomputation fails, its exception propagates and its previously cached items are preserved. A later render starts a fresh traversal and can retry; already-refreshed upstream sources are not rolled back.

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

Both `Stat.Density` overloads require an explicit `bandwidth` to be finite and strictly positive. Invalid values throw `GGNetUserException` at the call, including for empty input or `n < 2`. Omit `bandwidth` or pass `null` for automatic selection with `Stat.Nrd0`, independently per group. Explicit `from` and `to` must also be finite, validated after bandwidth in that order even when no output is requested.

Density grids keep their exact endpoints, including signed zero, and approximate equal spacing in binary64. Descending grids and repeated rounded coordinates are accepted. Safe endpoint/interpolation arithmetic avoids rejecting finite results merely because an intermediate overflows. Nonfinite required coordinates and densities outside finite representation throw actionable `GGNetUserException`; adjust bounds/bandwidth or rescale values. Final zero tails are allowed; scaled arithmetic preserves representable small densities lost by premature kernel underflow or denominator overflow. These calculations use bounded numerical precision, not a full-domain correctly rounded Gaussian guarantee.

All samples are validated before calculation. Grouped densities prepare bandwidth and grid for each sorted group before evaluating any density, so a later group's invalid coordinate precedes an earlier density failure. Within preparation, bandwidth selection precedes that group's coordinates. Failed construction publishes no source; a failed refresh preserves the entire previous output, including all groups, and can recover on a later valid refresh. Empty sources and `n < 2` keep their existing empty output after explicit-argument/sample validation.

Both `Stat.Bin` overloads emit exactly the requested number of bins for nonempty input and `bins >= 1`. Boundaries approximate equal spacing in binary64 and are shared, finite, and strictly increasing; nonconstant data keeps its exact minimum and maximum. Membership uses the emitted intervals: lower bounds are included, upper bounds excluded, except that the final interval includes the maximum. `Density` is probability mass divided by each emitted interval's width; wide intervals remain supported even when subtracting their endpoints would overflow.

For constant data, padding first attempts unit-width bins above the value, preserving ordinary behavior, then below if the upper boundaries collapse or overflow. If neither direction works, the step is `max(1, 2 * localSpacing)`, where `localSpacing` is the gap from the absolute value to its next larger finite double (the inward gap at `double.MaxValue`); padding again tries above, then below. This bounds the nominal span by `bins * max(1, 2 * localSpacing)`; boundaries still must pass strict finite ordering. Counts follow the same interval membership convention, including when padding goes below the constant.

NaN/infinite selected values, requested subdivisions whose nominal rounded boundaries collapse, and required positive densities that cannot be represented as finite positive doubles throw actionable `GGNetUserException`; use fewer bins or rescale/filter the selected values. Nominal approximately equal-width subdivisions must be distinct even when an uneven redistribution could fit the requested count. Requested bins are never silently redistributed, reduced, or deduplicated. Validation occurs at initial construction and recomputation. An invalid group fails the whole computation; a failed refresh retains the previous output and can be retried after correcting the input. Empty input or `bins < 1` retains the existing empty-output behavior. Selected values are captured once per computation.

Both `Stat.Summary` overloads require finite selected x/y values and finite `spread`, checked during construction and recomputation. Nonfinite spread is rejected even for empty input. Zero spread gives the mean alone; negative finite spread reverses the endpoints. A required endpoint outside finite-double representation throws `GGNetUserException` advising a smaller spread or rescaled values. An intermediate variance, standard deviation, or spread product outside double range does not by itself reject a representable requested endpoint.

`Stat.Nrd0` and both `Stat.Density` overloads reject nonfinite selected samples, including singleton input and density requests with `n < 2`. Empty and finite singleton samples give `Nrd0` bandwidth 1; samples are validated, not filtered. For constant samples of size `n >= 2`, the fallback is `0.9 * n^(-0.2)`. Repeated decimal constants such as `[0.1, 0.1, 0.1]` now have their exact common mean and zero standard deviation: an artificial tiny deviation from naive summation no longer creates a narrow density spike. This deliberate correction can materially change constant-group density/violin output.

Zero IQR falls back to sample standard deviation, then to 1 when that deviation is zero. Type-7 IQR calculation retains positive fractional differences that separately rounded quartiles could lose, so tied near-equal samples can also select a different bandwidth. A computed bandwidth outside positive finite-double representation throws an actionable exception suggesting rescaling or an explicit bandwidth; positive subnormal bandwidths remain supported. These checks run again during recomputation. Range-safe calculations do not promise correctly rounded results across the entire binary64 domain; fused endpoint arithmetic can change ordinary last bits.

Finite automatic bandwidth does not guarantee finite requested endpoints or densities; these are validated separately. The absolute constant-sample fallback can produce repeated grid locations near `1e308`, which are intentionally retained. Endpoint pinning and safe arithmetic can change the last coordinate's rounding; ordinary density arithmetic is preserved where it is range-safe.

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
