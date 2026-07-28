# Step 2 - Interactivity Implementation Options

Evaluation date: 2026-07-08

Scope: implementation options for GG.Net interactivity, including possible refactors of existing tooltip/event handling and API design for host-owned controls.

## Framing

Interactivity should be evaluated as two related surfaces:

1. Built-in plot gestures: wheel zoom, double-click reset, snapped hover, brush, pan.
2. Host-app state primitives: read/set/reset view state, read/set/clear selection state, and subscribe to changes. Domain-specific commands such as "last N days/weeks" are host conveniences built on those primitives.

The core decision is not "CSS transforms vs Blazor re-rendering" in isolation. It is where canonical interaction state lives.

## Recommendation

Canonical interaction state should live in .NET/GG.Net, and rendered output should be produced by the existing render pipeline.

CSS/SVG transforms should only be used as transient presentation for gestures that need per-frame feedback.

GG.Net already realizes plots through trained scales, committed ranges, grid composition, and coordinate projection. If interaction changes the data viewport, the truthful result is a re-rendered plot, not a transformed old SVG.

The broader spec/realization split should be treated as an architectural constraint for interactivity, but not as a prerequisite for the first slice. Interactivity should be designed as:

```text
plot specification + optional interaction state -> realization
```

This keeps authored plot configuration, trained/rendered state, and interaction state conceptually separate even before the full immutable plot-spec refactor exists.

## Option 1 - Blazor Re-render From Canonical View State

Verdict: best fit for Tier 0 and host-app APIs.

Use for:

- Wheel zoom.
- Double-click reset.
- Set view to an explicit x/y range.
- Reset selection/view.
- Legend/category/layer toggles.
- Snapped crosshair when driven by discrete hit strips.
- Commit-after-selection workflows.

Strengths:

- Axes, ticks, grid, labels, clipping, tooltips, and geoms remain consistent.
- State can be exposed to host controls and synchronized across plots.
- It works with the existing render pipeline instead of bypassing it.
- It preserves the idea that the final rendered plot is a plot state, not a browser-only transform.

Caveat:

- Do not reuse scale `Limits` as interaction state. `Limits` are user-authored scale configuration. Interactive view windows need separate state applied during scale commit, with reset semantics that restore the user's configured limits.

## Option 2 - CSS/SVG Transform As Canonical State

Verdict: poor fit as the main approach.

This is visually fast because a panel's data marks could be wrapped in a `<g>` and transformed with `translate/scale`. As source of truth, it breaks too much:

- Axes and grid stay stale.
- Stroke widths and text can scale incorrectly.
- Tooltip and hit testing drift from data coordinates.
- Clipping and panel bounds become subtle.
- Facets and polar coordinates make transform semantics less uniform.
- Host controls cannot query or set meaningful data-domain state unless the transform is mirrored back into .NET anyway.

Conclusion: reject this as the canonical zoom/pan implementation.

## Option 3 - Hybrid JS Preview, Blazor Commit

Verdict: best fit for Tier 2.

Use JS for high-frequency pointer physics:

- Drag-pan preview.
- Rubber-band rectangle drawing.
- Pixel-glued crosshair.
- Smooth touch/pinch gestures.
- Possible inertial or animated interaction later.

Then commit the final domain/window back to .NET, and let Blazor re-render the truthful SVG.

This keeps the roadmap boundary intact: continuous motion stays client-side; durable plot state stays in GG.Net.

## Option 4 - Dedicated Overlay/Hit-Test Layer

Verdict: likely refactor point for existing interactivity.

Today tooltip behavior is embedded in geoms. For example, point geoms build default tooltip handlers during `Init` and call the panel tooltip directly from a per-item mouseover path. Rendered screen primitives carry raw click/mouseover/mouseout delegates.

That works for mark-local hover, but it is not the right long-term center for crosshair, selection, viewport commands, or sync.

Direction:

- Introduce a panel-level interaction layer.
- Let it render invisible hit strips, crosshair marks, selection rectangles, and coordinate readouts.
- Avoid pushing crosshair/selection behavior into every geom.
- Keep per-geom click/hover support where it is useful, but stop treating it as the only interaction mechanism.

## Host-Control API Direction

External controls should not simulate DOM events. They should call or bind to GG.Net interaction state.

A full host API design is not settled here, but a subset of primitives is already clear:

- `GetView()` or equivalent current view state: read the effective x/y data-domain window.
- `SetView(...)`: set the x/y data-domain window explicitly.
- `ResetView()`: clear interaction view state and return to authored scale limits or data-trained range.
- `Zoom(...)`: apply a relative zoom around a data point or panel center.
- `Pan(...)`: shift the current view by data-domain deltas or fractions of the current span.
- `GetSelection()` or equivalent current selection state: read the selected data-domain interval/region.
- `SetSelection(...)`: set a data-domain interval/region explicitly.
- `ClearSelection()`: remove selection.
- `ViewChanged` and `SelectionChanged`: notify hosts and enable plot sync.

These are state primitives, not UX commands. "Last 2 weeks", "zoom to anomaly", or "show batch 42" should be app logic that calls `SetView` or `SetSelection`.

The concrete shape may be component methods, bindable parameters, or a controller object. The important decision is that commands operate on data-domain interaction state, not pixels. The likely first stable slice is `ViewState`, `GetView`, `SetView`, `ResetView`, and `ViewChanged`; selection can follow once brush and cross-filter semantics are clearer.

## Spec/Realization Scope

The preview review's spec/realization split is relevant because interactivity adds another state layer to an already mutable `PlotContext`. Adding view windows, cursor state, selection, and visibility directly into the same mutable bucket would deepen the existing problem.

In scope now:

- Design interactivity around `plot specification + interaction state -> realization`.
- Keep authored scale limits separate from interactive view state.
- Introduce durable view/selection state that survives render reset.
- Avoid public APIs that depend on current mutable internals of `PlotContext`.
- Keep headless capable of rendering authored spec plus optional interaction state, without browser gestures.

Not required for the first interactive slice:

- A complete immutable plot-spec type.
- Rewriting every builder to produce a pure description.
- Reworking all panels, geoms, and scales into a fully separate realization graph.
- Plot-spec serialization.

The pragmatic path is a narrow realization seam first. Keep the fluent API mostly as-is, but stop treating trained/rendered state and interaction state as ordinary builder state. That seam can later become the migration path to the full immutable spec/realization split.

## Working Conclusion

Options 3 and 4 are the working path:

- Canonical state in GG.Net/.NET: data-domain view window, selection, cursor/snap target, and visibility state.
- Panel-level interaction layer: owns overlays, hit strips, crosshair/readout, selection visuals, and routes interaction events.
- JS only for high-frequency preview: drag-pan, rubber-band, pixel-glued cursor/touch gestures; it commits final state back to GG.Net.
- Blazor re-render on commit: axes, grid, geoms, and tooltips become truthful again after the interaction settles.
- Host-control API over the same state primitives: view, reset view, selection, clear selection, and change notifications for sync.

Option 4 is the architectural seam. Option 3 is the physics boundary for gestures that cannot live comfortably over a Blazor Server circuit.

Use Blazor re-rendering as the canonical implementation.

Use CSS/SVG transforms only as transient preview, and only when JS owns the high-frequency gesture loop.

Refactor existing tooltip/event plumbing if needed so interactivity is no longer exclusively per-geom. The durable seam should be panel/plot-level interaction state plus host-callable commands.
