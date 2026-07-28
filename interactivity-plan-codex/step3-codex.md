# Step 3 - Refined Blast Radius

Evaluation date: 2026-07-08

Scope: blast radius for the chosen working path: Option 4 as the architectural seam, Option 3 as the JS/high-frequency gesture boundary.

## Non-Negotiable Constraints

- Interactivity must be optional.
- Headless rendering is an important feature and must remain first-class.
- Non-interactive plots should not require JS.
- Non-interactive plots should avoid extra behavior and extra markup where possible.
- Browser-only mechanics must not be required to render a plot.
- Snapshot/gallery stability should be preserved for default non-interactive plots.

This means the design must separate canonical state from gesture handling:

- Canonical state is plain .NET/GG.Net state: view window, selection, cursor/snap target, visibility state.
- Gesture handling is optional Blazor/browser behavior that mutates canonical state.
- JS is only an enhancement for live preview and high-frequency gesture physics.
- Headless can render a plot with an explicit view/selection state, but does not participate in browser gestures.
- Interactivity should be designed as `plot specification + optional interaction state -> realization`, even before the full immutable spec/realization refactor exists.

## Core Blast Radius

| Area | Impact | Why |
|---|---|---|
| `PlotContext` | High | Needs durable optional interaction state: view window, selection, cursor/snap target, visibility/toggle state, change notifications, reset semantics. Current `Render()` resets trained state each pass, so interaction state must survive outside scale training/reset. This should move toward a `spec + interaction state -> realization` seam rather than deepening `PlotContext` as one mutable bucket. |
| Position scales | High | View windows must be applied during commit, separately from authored `Limits`. Continuous/log/date/discrete scales all commit ranges differently, so this is the main correctness surface. |
| Coordinate systems | Medium-high | Need pixel/data inversion or at least pixel/fraction conversion. Current interface only projects fractions to pixels; it has no inverse path. Cartesian is straightforward, polar needs deliberate semantics. |
| `Panel` component | High | Becomes the optional interaction host: event receivers, overlay rendering, hit strips, crosshair/readout, selection visuals, JS attachment points, and commit-to-plot refresh. |
| Tooltip/event layer | Medium-high | Existing per-geom tooltip/event handling can remain for mark-local hover, but panel-level crosshair/selection should not be implemented by expanding every geom. A refactor path should separate mark events from panel interactions. |
| Public host-control API | High | In scope. Needs a stable way for app controls to get/set/reset view state, get/set/clear selection state, and subscribe to changes without simulating DOM events. This API must work independently of built-in gestures. |
| JS asset | Medium | Only needed for Tier 2 gestures: drag-pan preview, rubber-band preview, pixel-glued cursor/touch. It should send committed data-domain changes back to GG.Net. It must not be required for default/headless rendering. |
| Render mode handlers | Low-medium | Existing interactive render queue may be enough for committed updates. It may need light integration for debouncing/coalescing interaction commits. |

## Limited / Avoided Blast Radius

| Area | Expected Impact | Notes |
|---|---|---|
| Geoms | Low for viewport interactions; medium only if tooltip refactor is aggressive | Geoms already map through panel scales, so zoom/view-window should not require geom changes. Centralizing all default tooltip behavior would touch many geoms, so avoid doing that in the first slice. |
| `ShapeComposer` / screen primitives | Low-medium | Existing primitives carry raw mouse handlers. Overlay primitives can be added separately rather than changing all existing primitives. |
| Headless rendering | Low if state is cleanly separated | Headless should not know about gestures or JS. It can render canonical state when provided, such as a view-windowed plot. |
| Gallery snapshots | Low initially | No change if interactivity is opt-in and default state is identity. Snapshot churn only if base markup changes for all plots. |
| Existing DSL geom builders | Low | Avoid adding interaction parameters to every geom. Prefer plot/panel-level interaction configuration and host-control state. |

## Feature-Specific Radius

| Feature | Required Touches | Risk |
|---|---|---|
| Wheel zoom plus double-click reset | optional `Panel` events, interaction state, scale view-window commit, refresh path, coordinate inversion | Medium. Good first slice if Cartesian continuous/date axes are targeted first. |
| Host API primitives: get/set/reset view, view changed | public interaction state/API, scale view-window commit, refresh path | Medium-high because API shape matters, but implementation is clean if state is canonical. Domain-specific commands like "last N days/weeks" should be host logic built on these primitives. |
| Host API primitives: get/set/clear selection, selection changed | public selection state/API, optional panel overlay, downstream filtering/highlighting decision | Medium-high. Best after view primitives unless an immediate brush/cross-filter need appears. |
| Data-snapped crosshair/readout | optional panel overlay, hit strips or snap model, tooltip/readout component, optional selection state | Medium. Avoid per-pixel pointer traffic unless JS-backed. |
| Brush/rubber-band selection | optional panel overlay, selection state, JS preview for drag, Blazor commit | High. Good Tier 2 candidate after the view-window state exists. |
| Drag-pan | JS preview, coordinate inversion, view-window commit | High. Should not be pure Blazor Server. |
| Legend/category toggles | legend model, visibility state, render pipeline filtering/training decision | Medium-high. Semantics are harder than mechanics: hide layer, aesthetic value, or legend group? |
| Synced plots | shared host-control state, change notifications, loop prevention | Medium-high. Best built after single-plot command API stabilizes. |

## Critical Boundary

Do not mutate authored scale `Limits` for interaction.

`Limits` are part of the plot specification. Interaction needs a separate view state that is applied after training and before final range/break commit.

This boundary also protects headless rendering:

- A normal plot renders from authored spec only.
- A headless view-windowed plot can render from authored spec plus explicit canonical interaction state.
- Browser gestures are just one way to mutate that state.

## Spec/Realization Scope

The full spec/realization split is in scope as an architectural constraint, not as a first-slice prerequisite.

In scope now:

- Design interactivity around `plot specification + interaction state -> realization`.
- Keep authored scale limits separate from interactive view state.
- Introduce durable view/selection state that survives render reset.
- Avoid public APIs that depend on current mutable internals of `PlotContext`.
- Preserve a headless path that can render authored spec plus optional interaction state.

Not required for the first interactive slice:

- A complete immutable plot-spec type.
- Rewriting every builder to produce a pure description.
- Reworking all panels, geoms, and scales into a fully separate realization graph.
- Plot-spec serialization.

Blast-radius implication: the first implementation should create a narrow realization seam and avoid making `PlotContext` more stateful than it already is. That seam can later become the migration path to the full immutable spec/realization split.

## Pragmatic Sequencing

1. Introduce a narrow realization seam: authored plot state plus optional interaction state produces the render-time realization.
2. Add optional canonical view state and host API primitives for get/set/reset view on Cartesian x/y.
3. Apply that state in position-scale commit without touching geoms.
4. Add optional panel-level wheel zoom and double-click reset.
5. Add optional panel overlay for snapped crosshair/readout.
6. Add JS preview only when drag/rubber-band/pixel-glued behavior is needed.

The likely first stable API slice is view-focused: `ViewState`, `GetView`, `SetView`, `ResetView`, and `ViewChanged` or equivalent bindable/controller semantics. Selection primitives should follow once brush and cross-filter semantics are refined.

## Working Conclusion

The blast radius is broader than the original roadmap line because API design and existing interactivity refactors are in scope, but it remains concentrated.

The durable seam is plot/panel-level interaction state plus optional panel overlays. The JS boundary is only for high-frequency preview. Headless stays safe because rendering depends on canonical .NET state, not browser gestures.
