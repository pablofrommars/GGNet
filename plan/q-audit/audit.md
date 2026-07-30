# GGNet Core Audit

## Purpose

This document records an evidence-based review of `src/GGNet` so correctness and security defects can be resolved before release. It intentionally excludes style preferences and issues not grounded in the current code.

## General assessment

The library is thoughtfully structured around a typed grammar-of-graphics API, a separated compose/render pipeline, invariant SVG formatting, and a substantial automated test suite. The solution builds without warnings and all 290 enabled tests pass. However, the review found several uncovered correctness defects in rerendering, transformed scales, temporal axes, statistical aggregation, faceting, and map rendering, plus one injection vulnerability. The current recommendation is **fix-then-ship**.

## Findings

### Must-fix

#### 1. Plot labels permit arbitrary SVG/HTML injection

- **Location:** `src/GGNet/Markdown.cs:17-47`, `src/GGNet/Components/Panel.razor:148,165,280,298,316`
- **Evidence:** `Markdown.Text` copies unmatched text and matched group values without encoding. Titles and axis labels are then rendered as `MarkupString`.
- **Impact:** If a label contains untrusted data, it can inject arbitrary SVG elements or event attributes, creating a DOM-XSS path.
- **Suggested fix:** HTML-encode all user text before inserting the library-owned `<tspan>` markup, or construct the tspans through `RenderTreeBuilder`. Add hostile-input tests for titles and axis labels.

#### 2. Transformed-scale limits and additive expansion are interpreted in transformed space despite being documented as data units

- **Location:** `src/GGNet/Scales/Extended.cs:30-35`, `src/GGNet/Scales/Log10.cs:25-30`, `src/GGNet/Scales/Position.cs:43-48`, `src/GGNet/BuilderExtensions.cs:67-83,111-116,213-240`
- **Evidence:** Data values are mapped through `transformation.Apply`, but `Commit` passes raw `Limits.min/max` directly to `SetRange`. For example, log limits `(1, 1000)` produce range `1..1000`, while plotted values occupy log-space `0..3`. `SetRange` also adds `expand.minAdd/maxAdd` after transformation even though the public API documents those values as data units.
- **Impact:** `Scale_*_Sqrt`, `Scale_*_Log10`, `XLim`, and `YLim` can compress or place data incorrectly whenever explicit limits or additive expansion are used.
- **Suggested fix:** Define the scale-space conversion in one place: transform explicit limit endpoints, and either convert additive expansion from data space at each endpoint or correct the public contract if transformed units are intended. Add tests for direct scale limits, `XLim`/`YLim`, and non-zero additive expansion.

#### 3. Unmapped ribbons disappear after the first render

- **Location:** `src/GGNet/Geoms/Ribbon/Ribbon.cs:126-141,220-225`
- **Evidence:** `Clear` removes `Layer` contents but does not reset `cachedArea`. On the next pass, `cachedArea` is non-null, so it is reused without being added back to `Layer`; its mutable `Points` buffer also persists.
- **Impact:** A normal rerender drops a constant-fill ribbon and accumulates stale points off-layer, violating the pipeline's idempotence guarantee.
- **Suggested fix:** Reset `cachedArea` during `Clear`, or recreate the area once per pass. Extend `RenderTwiceIdentical` coverage to ribbons.

#### 4. Continuous fill and size scales fail for constant data and mishandle an initial zero

- **Location:** `src/GGNet/Scales/FillContinuous.cs:17-30,62-76`, `src/GGNet/Scales/Size.cs:43-57,86-122`
- **Evidence:** Both scales use `(0,0)` as an uninitialized sentinel. A first value of zero is therefore discarded when a later value arrives. For a genuinely constant range, fill returns `string.Empty`, while size divides by a zero span and returns `NaN`.
- **Impact:** Constant-fill layers disappear; size-mapped points emit invalid `NaN` radii. Zero-valued observations may also be incorrectly treated as out of range.
- **Suggested fix:** Track initialization with a separate boolean and define a deterministic midpoint mapping for degenerate ranges.

#### 5. Default stacked bars emit invalid geometry for negative values

- **Location:** `src/GGNet/Geoms/Bar/Bar.cs:246-272`, `src/GGNet/Scene/ShapeComposer.cs:89-101`
- **Evidence:** `Stack` passes a negative value directly as rectangle height and uses one accumulator for both signs. Projection consequently produces a negative SVG height.
- **Impact:** Negative bar segments do not render correctly, and mixed-sign stacks use incorrect baselines and scale extents.
- **Suggested fix:** Maintain separate positive and negative accumulators; emit every rectangle with a non-negative height and train the scale over both resulting endpoints.

#### 6. Cross-month discrete date axes can index past the end of the buffer

- **Location:** `src/GGNet/Scales/DiscreteDates.cs:124-135`, `src/GGNet/Scales/DiscretePosition.cs:58-100`
- **Evidence:** `Labeling` receives an exclusive `end`, but `DayMonth` loops with `i <= end` and reads `values[i]`. For Jan 30, Jan 31, Feb 1, and Feb 2, the month-transition branch reaches `values[4]`.
- **Impact:** Ordinary short series spanning a month boundary can throw `ArgumentOutOfRangeException` from the underlying list during rendering. With a limited view whose exclusive `end` is still inside the buffer, it can instead emit a tick outside the requested window.
- **Suggested fix:** Use `i < end` and add boundary tests covering month/year transitions and limited view windows.

#### 7. Date-time scales silently discard out-of-order observations

- **Location:** `src/GGNet/Scales/DateTimePosition.cs:30-82,181-189`
- **Evidence:** For same-day values, training only appends minute samples after `values[^1]`. If 10:00 is trained before 09:00, the loop adds nothing and the 09:00 key is never inserted; `Map(09:00)` then returns `NaN`.
- **Impact:** Input order changes the rendered data, with earlier observations silently missing.
- **Suggested fix:** Always retain observed keys, then construct any minute sampling from the sorted extent during commit.

#### 8. Faceting an empty source crashes instead of rendering an empty plot

- **Location:** `src/GGNet/Facets/Utils.cs:23-26`, `src/GGNet/Facets/Faceting1D.cs:18-25,37-41`, `src/GGNet/PlotContext.cs:170-184,277-299,319-322`
- **Evidence:** `DimWrap(0)` returns zero rows and `Facets()` returns an empty array, so no panels are created. The floating-point panel-height calculation yields infinity rather than throwing; the actual crash occurs when faceted legend construction unconditionally indexes `Panels[0]`.
- **Impact:** A streaming or filtering scenario that temporarily produces no rows fails rendering.
- **Suggested fix:** Define an empty-facet state, typically one empty panel, and guard all first-panel access. Also validate explicit `nrows` and `ncolumns` as positive.

#### 9. Boxplots discard duplicate observations before computing percentiles

- **Location:** `src/GGNet/Buffers/SortedBuffer.cs:16-24`, `src/GGNet/Geoms/Boxplot/Boxplot.cs:108-116,245-259`
- **Evidence:** `SortedBuffer.Add` ignores an item when `BinarySearch` finds an equal value. Boxplot samples use this buffer directly, so `[1,1,1,10]` becomes `[1,10]` before percentile calculation.
- **Impact:** Quartiles, median, and whiskers are statistically wrong whenever measurements repeat.
- **Suggested fix:** Use a sorted collection that preserves multiplicity for boxplot samples, without changing set-like consumers of `SortedBuffer`.

#### 10. Map polygon holes are ignored

- **Location:** `src/GGNet/Geospacial/Polygon.cs:3-9`, `src/GGNet/Scene/ShapeComposer.cs:251-274`, `src/GGNet/Components/Area.razor:130-141`
- **Evidence:** Public polygons expose `Hole`, but it is never read. All rings become ordinary subpaths, and the emitted path has no explicit fill rule.
- **Impact:** Declared holes can render filled; results depend accidentally on caller-provided winding direction.
- **Suggested fix:** Carry hole semantics into composition. Either normalize hole winding and use `nonzero`, or use `evenodd` only with a representation that prevents overlapping exterior rings from cancelling each other. Add map tests containing inner rings and multiple exterior polygons.

#### 11. Stacked bars train the grouping-axis extent at twice their drawn half-width

- **Location:** `src/GGNet/Geoms/Bar/Bar.cs:246-267,276-309`
- **Evidence:** A stacked rectangle spans `x - delta / 2` through `x + delta / 2`, but `Stack` trains the position scale with `x - delta` through `x + delta`. `Dodge` trains the exact rectangle bounds.
- **Impact:** Default stacked bar charts receive excess range on both sides, shrinking the marks and adding avoidable outer whitespace. The error is especially visible for a small number of bars.
- **Suggested fix:** Train the grouping-axis scale with `x - delta / 2` and `x + delta / 2`, matching the emitted rectangle, and pin the one-bar and stacked-bar extents in tests.

### Should-fix

#### 1. Insufficient or empty palettes fail silently or with low-level exceptions

- **Location:** `src/GGNet/Palettes/Utils.cs:5-28`, `src/GGNet/Palettes/Discrete.cs:28-40`, `src/GGNet/Scales/FillContinuous.cs:62-71`
- **Evidence:** If category count exceeds palette length, `Sample` returns null and `Set` leaves every mapping at `default`; color/fill geoms then skip all marks. An empty continuous palette eventually indexes `colors[0]`.
- **Impact:** A configuration problem appears as an empty chart or `IndexOutOfRangeException`, with no actionable diagnosis.
- **Suggested fix:** Validate palettes at the public scale methods and throw `GGNetUserException`, or deliberately support cycling or interpolation.

### Consider

No clarity-only findings worth recording.

**Verdict: fix-then-ship.**
