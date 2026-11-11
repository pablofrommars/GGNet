# Overview and detail focus primitives

## Why this document exists

This spec hands off a feature separate from time-cursor inspection
(`specs/time-cursor-inspection.md`). It comes from an in-session review on 2026-11-12 of how a
consuming dashboard switches between an overview chart and separately built detail charts. Its
inputs are the engineer's description of that consumer's current implementation and the GGNet tree
(`net11-bump` at `cd27877` plus the uncommitted touchscreen changes). No consumer source was
inspected for this spec, and its claims about the consumer are as described.

It is not yet bound by a plan. The engineer rules on the [open decisions](#open-decisions) when a
plan binds it; from then on this document is immutable, and changed intent supersedes it.

Its conclusion: GGNet should **not** build a native "maximize this panel" or a native
detail-composition switcher. It should provide the few primitives a consumer needs to run an
overview/detail lifecycle with explicit state hand-over and clean event ownership.

## Outcome

A consumer can show an overview chart and open a related detail chart in the same region, then
return. Each of the following follows a rule the consumer chooses and GGNet makes possible:

- whether the time window carries over in either direction;
- whether a pinned inspection time carries over;
- what activates "open";
- what activates "close".

None of it relies on clicks that GGNet may now consume for gestures or inspection.

## The consumer pattern this serves

Described by the engineer; illustrative and domain-neutral here.

- **Layout.** One chart region on a page, with other content around it. The overview `Plot` has
  three stacked sub-panels (`.Panel(…, height:)`), for example a 50/30/20 split.
- **Detail charts are separate.** Each overview panel has a separate detail `Plot` with its own
  `PlotContext`. A detail can add information absent from the overview. In the motivating case, a
  temperature detail adds a second sub-panel of equipment activity: bars at −1 or +1 while active, 0
  otherwise, with the y axis hidden. Maximizing the overview panel could not produce that.
- **All plots stay mounted.** The plots overlap. CSS toggles opacity, scale (a 0.8 to 1 transition,
  200–300 ms) and `pointer-events`, so opening a detail is cheap and instances survive transitions.
- **Shared data.** All contexts read the same mutable time series. Missing numeric readings are
  `double.NaN`. Live telemetry appends or updates entries, and the page then refreshes every plot,
  hidden ones included.
- **Open and close.** A sub-panel's `onClick` opens its detail. A click anywhere on the detail's
  wrapper closes it. The plots set no `Interactivity`.
- **No shared interaction state.** View windows, zoom and pan, crosshair and inspection are not
  synchronized between overview and details; only data, formatter and style are shared.

## Scope

### 1. Observable view window

- **Read.** `Plot` (or its `PlotContext`) exposes the current x and y windows in **data units**
  (`TX`, `TY`), or null when no window is set.
- **Notify.** `Plot` raises a change notification after any window change: wheel zoom, drag pan,
  double-tap or dblclick reset, AutoFitY, host commands (`ZoomToXAsync`, `ResetViewAsync`,
  `ShowLastAsync`, `SetXView`).
  - The notification says what caused the change (gesture, host command, reset).
  - It fires once per committed change, never per pointer move.
- With these, a consumer can carry a window from overview to detail on open, and back on close,
  under a policy it owns.

### 2. Inspection hand-over

- The bindable inspection value defined by `specs/time-cursor-inspection.md` §4 is the hand-over
  mechanism for a pinned time. This spec adds no second inspection state.
- If that feature has not shipped, this spec's version 1 delivers §1 and §3 only.

### 3. Reliable panel activation

- **Today.** A sub-panel's `onClick` is wired to its background rectangle only (`rect.panel` in
  `src/GGNet/Components/Panel.razor`). Taps on marks (which are siblings, not children), on grid
  lines drawn above the background, or on labels do not reach it.
- **The fix, one of two forms (OD-2):**
  - an activation that covers the whole plot area, marks and grid included; or
  - the rectangle-only limit documented on the DSL surface.
- **Under `Interactivity`.** Activation must not fire for input GGNet has claimed: a recognized drag,
  a click discarded after a drag, an inspecting tap, or a reset gesture. When an inspection mode is
  active, activation needs an explicit affordance and is never a plain tap (OD-3).
- **Payload.** The activation reports which sub-panel was activated, so one handler can serve every
  panel (OD-4).

### 4. Hidden plots (conditional)

- Only if measurement on the target device shows that refreshing hidden plots costs noticeable time.
- In that case, add a way to mark a `Plot` as not displayed: it skips composition and refreshes once
  when shown, with no lost data and no stale frame (OD-5).

## Out of scope

- Native maximize of an existing panel.
- Native detail compositions, or GGNet switching between compositions. Consumers already compose
  these with separate `PlotContext`s.
- Owning focus state. Which view is open is the consumer's state.
- Navigation chrome: close or back buttons, keyboard focus traps, transitions. These are consumer
  UI.
- Cross-`Plot` synchronization through hidden or global state.
- Changes to static or headless output.

## Constraints

- The repository contract (`AGENTS.md`, scoped guides) applies.
- New public surface carries `///` docs and re-pins
  `tests/GGNet.Headless.Tests/Api/PublicApiTests.GGNet.verified.txt` deliberately.
- Interaction stays within the single gesture module and its single `initialize` payload (Blazor
  guide §5).
- Change notifications are committed events, not per-move traffic.
- `Interactivity` stays initialization-only.
- Existing behaviour for plots without the new parameters is unchanged.
- Physical touch QA on the device is required for anything touching activation under touch.

## Verified premises

Checked against the tree on 2026-11-12; re-verify before relying on them.

| Premise | Where |
|---|---|
| Windows can be set but not read in data units; they are stored in mapped units on internal `Positions` | `src/GGNet/PlotContext.State.cs` (`SetXView`, `SetYView`, `ResetView`), `src/GGNet/PlotContext.Spec.cs` |
| Host window commands | `ZoomToXAsync`, `ZoomToYAsync`, `ResetViewAsync` in `src/GGNet/Components/Plot.razor.cs`; `ShowLastAsync` in `src/GGNet/ViewExtensions.cs` |
| `Plot` exposes no event or callback parameter for view or interaction changes | `src/GGNet/Components/Plot.razor.cs` (parameters: `Context`, `RenderMode`, `Width`, `Height`, `Theme`, `Interactivity`) |
| A sub-panel's `onClick` is on its background rectangle only | `src/GGNet/Components/Panel.razor` (`RenderBackground`), `src/GGNet/PanelFactory.cs` |
| Gestures cancel a pan whenever the panel's screen transform changes, so a CSS scale transition cancels pans that start mid-transition | `src/GGNet/Components/Panel.razor.js` (`#sameTransform`, `#watchTransform`) |
| After a recognized drag the module discards the trailing click before ancestors see it; a plain inspecting tap still produces a click that bubbles to ancestors | `src/GGNet/Components/Panel.razor.js`; Blazor guide §5 |
| A geom's `IReadOnlyList<T>` source is used as given, not snapshotted, and `Interactive` composition runs off the UI thread | `BuilderExtensions.*.cs` (`source as IReadOnlyList<T2> ?? new Source<T2>(source)`); Blazor guide §5 |
| NaN y values are skipped, so a line joins across them, unless `Geom_Line(piecewise: true)` | `src/GGNet/Geoms/Line/Line.cs`, `src/GGNet/BuilderExtensions.Line.cs` |
| `Width`/`Height` are viewBox units; the SVG carries no width or height and scales to its container | troubleshooting guide §12 |

## Cautions for the consumer pattern

These came out of the review. They are not GGNet requirements, but the plan should keep them in view
and the documentation may state them.

- **Gaps.** A NaN-as-gap convention draws gaps only with `piecewise: true`; otherwise the line
  bridges them.
- **Shared mutable source.** One mutable source behind several plots is safe only if mutations are
  marshalled onto the renderer's synchronization context before refreshing. Concurrent appends during
  an off-thread composition can fail enumeration or read a half-updated list.
- **Tap-to-close.** Closing on a wrapper click conflicts with any tap GGNet uses (inspection,
  click-follows-inspection). An explicit close control separates navigation from inspection.
- **Hidden interactive plots.** With `Interactivity`, hidden plots still start the gesture module.
  They ignore input correctly, because hit testing skips `pointer-events: none`, but they still
  receive window-level scroll and touch events.

## Open decisions

| ID | Decision | Recommendation |
|---|---|---|
| OD-1 | Window API shape: a bindable parameter (`@bind-XView`) or a read accessor plus an `EventCallback` | Read accessor plus a `ViewChanged` callback with a cause; binding later if hosts need two-way control |
| OD-2 | Panel activation: whole-area activation, or the rectangle-only limit documented | Whole-area activation as a separate panel-level callback, so the existing `onClick` keeps its meaning |
| OD-3 | Activation when an inspection mode is on | Explicit affordance only (for example a strip or title hit area, or consumer UI); never a plain tap |
| OD-4 | Panel identity in the activation payload | The sub-panel's index and an optional consumer-supplied key on `.Panel(...)` |
| OD-5 | Hidden-plot suspension | Measure first on the device; build only if refresh cost is noticeable |
| OD-6 | Ordering against time-cursor inspection | Ship §1 and §3 independently; §2 follows the time-cursor feature |

## Acceptance criteria

| Criterion | Falsifiable condition |
|---|---|
| ACCEPTANCE-01 | After wheel zoom, drag pan, reset and each host command, the window read back in data units equals the window shown, and exactly one change notification with the matching cause fires per committed change. |
| ACCEPTANCE-02 | No change notification fires during an in-progress drag preview; one fires on commit. |
| ACCEPTANCE-03 | A demo overview/detail page in `src/GGNet.Demo` (generated data, three sub-panels, one detail adding a sub-panel) carries the window from overview to detail on open and back on close under a demo-chosen policy, and keeps a pinned inspection time if §2 is in scope. |
| ACCEPTANCE-04 | Activation fires for taps on marks, grid lines and background inside the plot area, and never after a recognized drag, for a discarded click, for an inspecting tap, or for a reset gesture. |
| ACCEPTANCE-05 | The activation payload identifies the activated sub-panel. |
| ACCEPTANCE-06 | Without the new parameters, plots behave and render exactly as before: goldens unchanged, existing browser suite green. |
| ACCEPTANCE-07 | Gates: build `-warnaserror` (subject to `plan/deferred.md` D-04), `dotnet test`, both `dotnet format` checks, `GGNET_E2E=1 dotnet test tests/GGNet.E2ETests` with every browser case executed. |
| ACCEPTANCE-08 | Physical touch QA with the engineer on the device: open, close, window carry-over, activation against inspection, and a drag started during the open transition, observed and recorded. |
