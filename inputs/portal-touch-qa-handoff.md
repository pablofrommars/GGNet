# Why this document exists

Hand the GGNet touch repair to the Portal session for integration and physical touchscreen QA. The candidate is on local branch `touch-ux-repair`, based on `a135e50b6d52b9b157d442325779a938eb5e3e93`. Resolve and record the branch's full commit SHA when selecting the dependency; this document is included in that commit. The branch has not been pushed or merged into main.

The user authorized committing the repair to a branch on 2026-11-19 so Portal can test it. No Portal files, dependency pins, device releases, database or equipment state were changed by this GGNet session. The last recorded Portal release, `ui03-touch-03`, predates this repair. Its earlier physical observations do not establish acceptance of this candidate.

## What changed

- Chart-owned touch previously consumed taps without offering native activation or reset. `TouchInteractionOptions.TapActivatesPanel` and `DoubleTapResetsView` now opt into those actions. Both default to false, preserving other consumers' policies. Activation invokes the panel callback without synthesizing browser or mark clicks.
- A capturing window blur handler also received descendant-element blur, which could discard the first tap during a normal focus change. Only actual window blur now cancels the gesture. Tests cover both element focus changes and window cancellation.
- Cursor labels previously used a fixed offset and repeated in each panel. Optional `Style.Cursor` uses rendered text measurements and native C# layout to place one label at the top of a shared-X stack, with clearance below it and uninterrupted lower-panel lines.
- Navigation CSS previously hid grid and tick context wholesale. Grid positions and tick anchors now follow the same preview transform as the series; text is not stretched. Out-of-view ticks are hidden and new ticks wait for a rendered frame. Mismatched panel/marks frames still hide context. Cursor labels, sample markers and stale host readings retain their acknowledgement rules.

## Exact configuration

```csharp
var overviewInteractivity = new InteractivityOptions
{
    Pan = true,
    Crosshair = true,
    Touch = new TouchInteractionOptions
    {
        Inspect = true,
        Navigation = TouchNavigation.PanAndPinchX,
        Ownership = TouchOwnership.Owned,
        TapActivatesPanel = true,
        DoubleTapResetsView = true
    }
};

var detailInteractivity = overviewInteractivity with
{
    Touch = overviewInteractivity.Touch! with { TapActivatesPanel = false }
};

var chartStyle = Style.Default(init: style =>
{
    style.Panel.Spacing.Y = 0;
    style.Cursor = new Style.StyleCursor
    {
        FirstPanelOnly = true,
        FontSize = 1.5,
        Padding = 4,
        Gap = 4
    };
});
```

Use `overviewInteractivity` for the overview Plot and `detailInteractivity` for every detail Plot. Apply the cursor and spacing properties to Portal's existing styles, preserving its other typography and layout settings; `.Style(chartStyle)` is the builder API for a new style. `FontSize = 1.5` uses GGNet's em units, equivalent to 24 SVG units. Keep the existing overview `.Panel(..., onActivate:, key:)` callbacks and route each key to the corresponding detail. Touch options are initialization-only: remount with a new `@key` if changing them on an existing Plot.

## Gesture and edge behavior

| Input | Overview | Detail |
| --- | --- | --- |
| Single tap | Invoke the panel callback after the 300 ms double-tap window | Pin inspection after the same window |
| Double-tap | Reset the view, retain the committed pin, do not activate first | Reset the view, retain the committed pin, do not pin either tap |
| Hold for 400 ms, then drag | Inspect; release pins | Inspect; release pins |
| Immediate horizontal movement of at least 10 screen pixels | Pan | Pan |
| Two contacts | Pan/pinch X | Pan/pinch X |
| Existing host × | Not applicable | Return to overview with window and pin retained |

The second press must begin within 300 ms of the first release, on the same panel and within 24 screen pixels. A second press that becomes a hold, drag or multi-contact gesture performs that gesture instead. Widely separated or different-panel taps discard the earlier pending action and start a new candidate. Slower double taps can resolve as separate single taps; the timing still needs physical acceptance.

Host viewport/selection commands, outside presses, resize, window blur, context replacement and disposal cancel pending tap work. Session, epoch and revision checks reject stale actions. Contact escalation, cancellation, draining and unavailable recovery retain their existing contracts.

`Style.Cursor = null` retains legacy geometry. When configured, the label centers on the selected X wherever it fits; near an edge the label shifts inward while the line retains the selected data coordinate. Over-wide text uses SVG `textLength` compression, and a panel too short for the label reduces its font size. Very small charts still need physical readability QA. The label appears after measurement. Lower panels have neither repeated labels nor label gaps; zero panel spacing connects the lines. Free-X and flipped local cursors retain their own labels.

## Portal integration

1. Read Portal's current instructions and tree before editing. Select the exact candidate SHA on a Portal QA branch; do not silently advance to another GGNet revision. A local submodule can fetch `touch-ux-repair` from `/Users/pablo/source/GGNet`; remote availability requires a separate push.
2. Apply the policies and native cursor style to the overview and all three details. Remove the external Expand/All controls, scoped `.crosshair-label { display: none; }` workaround and duplicate external selected-time row. Retain the in-chart ×, independent Clear, fixed readout geometry and per-series sample timestamps.
3. Preserve shared inspection and window transfer in both directions. Keep hidden plots from replacing the active selection/readout. Preserve active-chart pending/unavailable feedback; do not force stale numeric readings visible during preview.
4. Update calibration instructions to the settled gesture mapping. Use generated calibration samples before production telemetry acceptance.
5. Follow the owning Portal session's compatible packaging/deployment workflow. The source handoff identifies backend `da32dd9bd458e8ad9d40de4c0bed7f130ebbfb53` as the compatibility boundary: do not deploy current-tree U20 or migrate/reset the database as part of chart QA. Re-read actual device state and preserve configuration, data, kiosk profile and brightness. Keep browser page-pinch suppression separate.

The native-actions mode at GGNet Demo `/overview-detail` is the reference host composition. Portal remains responsible for which detail is open and for its close control.

## Verification evidence and limits

- User-run complete E2E suite: **235 passed, zero failures, zero skips**, in 9m 05s. This includes the actual Blazor host, real browser touch input, gesture arbitration, measured cursor geometry, preview/acknowledgement and recovery cases. It is automated browser evidence, not physical touchscreen acceptance.
- Solution build passes with zero warnings/errors using the sandbox-compatible in-process build command.
- Component suite: 223 passed. Headless suite: 1,448 passed, including gallery and public API checks. No gallery snapshots were changed. Deterministic eval subset: 52 passed; process-spawning MCP/validator evals remain excluded from the agent-side result.
- JavaScript syntax, changed-file whitespace and `git diff --check` pass. The normal solution test launcher and solution format checks encounter local IPC permission restrictions in this agent environment; no full solution-format pass is claimed. Run the repository's normal build/test/whitespace/style gates in an unrestricted terminal before merging.

The browser geometry assertion allows 0.0001 SVG units where serialized coordinates are compared with `getBBox()` measurements; the observed rounding difference was approximately 0.000006 units.

## Physical QA sequence

Verify the deployed candidate identity first. Present one check at a time, record the user's observation and release identity, and distinguish failures from untested behavior.

1. Tap each overview panel to open its correct detail; use × to return.
2. Hold, drag and release to pin. Confirm the timestamp and readings remain at the release position.
3. Pan, then double-tap overview: reset once, without opening detail or moving the committed pin. Repeat in detail without an intermediate new pin.
4. Inspect near both edges and the center. Confirm one readable enlarged label inside the chart, clearance below it and continuous lower-panel lines.
5. Pan and pinch while watching series, grid and ticks. Confirm context stays aligned and readings settle after release. Verify time navigation separately from browser-page zoom.
6. Transfer a panned window and pin through open/close. Move the pin offscreen, verify outside-view feedback, reset to recover it, then Clear and confirm it does not return on reopening.
7. Check a second contact during a hold, fingers on separate temperature/activity panels, a third contact and two-to-one draining. None may accidentally activate a panel.
8. Check gestures during open/close transitions and subsequent input recovery. Record pending/unavailable behavior if encountered; do not induce an outage solely to obtain a pass.

Portal integration, device deployment and these physical passes remain outstanding.
