# Overview and detail focus primitives

- Profile: Task
- Status: completed
- Cadence: continuous
- Spec context:
  - @specs/overview-detail-focus-r3.md

## Contents

- [Steps](#steps)
- [Acceptance](#acceptance)
- [Authority](#authority)
- [Decisions](#decisions)
- [Assumptions and open questions](#assumptions-and-open-questions)
- [Deferred issues and accepted gaps](#deferred-issues-and-accepted-gaps)
- [Gates](#gates)
- [Material notes](#material-notes)

## Steps

| ID | Checkable result | Affected surfaces | Acceptance | Verification |
|---|---|---|---|---|
| ✅ STEP-01 · completed | `PlotContext` exposes the current x and y windows in data units (`TX`, `TY`): null when unset and null on a discrete or `LocalDateTime` axis. Round trip holds for numeric, log and `Instant` axes. | `src/GGNet/PlotContext.State.cs`, `src/GGNet/Scales/*.cs`, `tests/GGNet.Headless.Tests/ViewCommandTests.cs`, `tests/GGNet.Headless.Tests/Api/PublicApiTests.GGNet.verified.txt` | ACCEPTANCE-01 | Done 2026-11-12: nine new `ViewCommandTests` cases failed to compile against the missing accessor, then `dotnet test --project tests/GGNet.Headless.Tests` passed 1358/1358 with no golden change. No public type added, so the API snapshot is unchanged. |
| ✅ STEP-02 · completed | `Plot` has a `ViewChanged` callback carrying the cause (gesture, host command, reset) and both windows. It fires once per committed change: wheel, pan end, reset, AutoFitY as part of its x change, `ZoomToXAsync`, `ZoomToYAsync`, `ResetViewAsync`, `ShowLastAsync`, and a direct `SetXView`/`SetYView` on the next refresh. It runs on the renderer's synchronization context. | `src/GGNet/Components/Plot.razor.cs`, `src/GGNet/Components/Panel.razor.cs`, `src/GGNet/ViewExtensions.cs`, `src/GGNet/ViewChange.cs`, `src/GGNet/ViewChangeCause.cs`, `tests/GGNet.Components.Tests`, public API snapshot | ACCEPTANCE-01, ACCEPTANCE-02 | Done 2026-11-12: eleven `PlotViewChangedTests` cases failed to compile against the missing callback, then `dotnet test --project tests/GGNet.Components.Tests` passed 110/110. The API snapshot was re-pinned for exactly `GGNet.ViewChange` and `GGNet.ViewChangeCause`. |
| ✅ STEP-03 · completed | `.Panel(...)` takes an activation callback and an optional key; the payload carries the sub-panel index and key. Activation follows DECISION-02. Without the callback the markup is unchanged. | `src/GGNet/BuilderExtensions.cs`, `src/GGNet/PanelFactory.cs`, `src/GGNet/Data/Panel.cs`, `src/GGNet/Components/Panel.razor`, `src/GGNet/Components/Panel.razor.cs`, `src/GGNet/Components/Panel.razor.js` only if the browser cases need it, `tests/GGNet.Components.Tests`, `tests/GGNet.Headless.Tests/OverloadConsistencyTests.cs`, public API snapshot | ACCEPTANCE-04, ACCEPTANCE-05, ACCEPTANCE-06 | Done 2026-11-12: `PanelActivationTests` (12 cases) ran 7 failed before the component change (the 5 that assert nothing fires passed vacuously), then `tests/GGNet.Components.Tests` passed 122/122 and `tests/GGNet.Headless.Tests` 1358/1358 with goldens unchanged. The API snapshot was re-pinned for exactly `GGNet.PanelActivation`. `Panel.razor.js` was not touched. |
| ✅ STEP-04 · completed | Demo page `OverviewDetail`: generated data, three sub-panels, one detail that adds a sub-panel, both plots mounted, x window carried on open and back on close, an explicit close control. Browser cases cover carry-over and activation against drag, discarded click, inspecting tap and reset. | `src/GGNet.Demo/Components/Pages/OverviewDetail.razor*`, `src/GGNet.Demo/Data/Telemetry.cs`, demo navigation, `src/GGNet.Demo/README.md`, `tests/GGNet.E2ETests` | ACCEPTANCE-02, ACCEPTANCE-03, ACCEPTANCE-04, ACCEPTANCE-05 | Done 2026-11-12: `OverviewDetailTests` 12/12 (13/13 with the later measurement case) executed and passed against the rebuilt demo (`GGNET_E2E=1 dotnet test --project tests/GGNet.E2ETests --filter-class GGNet.E2ETests.OverviewDetailTests`). The touch double-tap case passes, which settles the double-tap assumption. The full browser run is STEP-06. |
| ✅ STEP-05 · completed | Documentation matches the code: Blazor guide §5 (view notification, activation rules), `skills/ggnet` where it describes panels and view commands, and the four consumer cautions from the spec. | `.github/instructions/blazor.instructions.md`, `README.md`, `skills/ggnet/reference/scales.md`, `src/GGNet.Demo/README.md` | ACCEPTANCE-06 | Done 2026-11-12: Blazor guide §3 and §5, root `README.md` (primitives and the four cautions), `skills/ggnet/reference/scales.md`, demo README; covered by the STEP-06 run. |
| ✅ STEP-06 · completed | All local gates green on the final tree. | read-only | ACCEPTANCE-06, ACCEPTANCE-07 | Done 2026-11-12 on `HEAD` `7c1855a` plus the uncommitted tree: build `-warnaserror` 0 warnings; `dotnet test --solution GGNet.slnx` 1540 passed, 89 browser cases skipped by design; `dotnet format whitespace` and `style` `--verify-no-changes` exit 0; `GGNET_E2E=1 dotnet test --project tests/GGNet.E2ETests` 91/91 executed and passed. Rerun after the measurement control was added and after the ARM64 publish. |
| ✅ STEP-07 · completed | Demo deployed to the QA device; refresh time of the hidden plots measured there (DECISION-01, OD-5). | device only | ACCEPTANCE-08 | Done 2026-11-13: the device page header shows build `7c1855a.dirty.9bf3517f`, as published. The engineer ran **Measure refresh** on the device and read back a mean extra of 3.5 ms per refresh with the hidden detail (only the extra figure was reported). Whether that is noticeable is the engineer's ruling (GATE-08). |
| ✅ STEP-08 · completed | Physical touch QA with the engineer: open, close, window carry-over, activation against inspection, a drag started during the open transition. | — | ACCEPTANCE-08 | Engineer's observations of 2026-11-13 are in Material notes. Every listed check was observed. Two findings went to GATE-09 and GATE-10 and continue as STEP-09 and STEP-10. |
| ✅ STEP-09 · completed | Activation delay is 200 ms. During a drag, every other panel of the same plot sharing the panned axis previews the same shift, scaled to its plot area, with the same hold and cancel rules. Docs match. All local gates green. | `src/GGNet/Components/Panel.razor.cs`, `src/GGNet/Components/Panel.razor.js`, `src/GGNet/Components/PanelInterop.cs`, `tests/GGNet.E2ETests`, `tests/GGNet.Components.Tests`, `.github/instructions/blazor.instructions.md` | ACCEPTANCE-07, ACCEPTANCE-09 | Done 2026-11-13 on `HEAD` `7c1855a` plus the uncommitted tree. Six new browser cases (`ADragPreviewsOnEveryPanelOfThePlotSharingThePannedAxis` ×4, `ASiblingPreviewIsHeldAfterReleaseUntilItsOwnCommittedFrame`, `ADragMovesEverySubPanelBeforeRelease`): 5 failed against the module at `7c1855a` (the case expecting no movement passed), 6/6 pass after. Build `-warnaserror` 0 warnings; `dotnet test --solution GGNet.slnx` 1540 passed, 95 browser cases skipped by design; both `dotnet format` checks exit 0; `GGNET_E2E=1 dotnet test --project tests/GGNet.E2ETests` 97/97 executed and passed. |
| ✅ STEP-10 · completed | Fixed build redeployed to the device; the engineer retests the 200 ms delay (feel and slow double-taps) and the pan preview across sub-panels. | device only | ACCEPTANCE-08, ACCEPTANCE-09 | Done 2026-11-13: the device page reported build `7c1855a.dirty.bfc788f0`, as published. The engineer's retest is in Material notes: linked preview as expected from any panel and in the detail; 200 ms feels better; a double tap can open the detail, accepted by ruling (DECISION-06). |

## Acceptance

| Criterion | Evidence |
|---|---|
| ✅ ACCEPTANCE-01 · satisfied | `ViewCommandTests` (round trip on numeric, log, sqrt and `Instant` axes; null when unset, on discrete and on `LocalDateTime`; the fitted y window equals the scale range shown) and `PlotViewChangedTests` (one notification with the matching cause for wheel, pan, reset gesture, `ZoomToXAsync`, `ZoomToYAsync`, `ResetViewAsync`, `ShowLastAsync` and a context-set window; none for an unchanged window). In the browser, `OverviewDetailTests` sees exactly one `Gesture` after a drag and one `Reset` after a double click or double tap. |
| ✅ ACCEPTANCE-02 · satisfied | `PanReportsOneGestureOnCommitAndNothingBefore` (bUnit) and `ADragPanRaisesOneGestureChangeAndNoActivation` (browser): the change count is 1 after a ten-move swipe and still 1 after 900 ms. |
| ✅ ACCEPTANCE-03 · satisfied | `/overview-detail` in `src/GGNet.Demo`: generated telemetry, three sub-panels, a temperature detail adding an activity panel. `TheWindowTravelsToTheDetailOnOpenAndBackOnClose` passes. Inspection hand-over is out of scope (DECISION-01, OD-6). |
| ✅ ACCEPTANCE-04 · satisfied | `PanelActivationTests` (bUnit) and `OverviewDetailTests` (browser): activation on a mark, a grid line and the background; none after a drag and its discarded click, after a mouse double click or touch double tap, or for an inspecting tap under crosshair and cursor tooltip. |
| ✅ ACCEPTANCE-05 · satisfied | `TheActivationNamesItsSubPanel` (bUnit) and `ATapOnASubPanelReportsThatSubPanelAndOnlyTemperatureOpensItsDetail` (browser, all three panels). |
| ✅ ACCEPTANCE-06 · satisfied | No golden changed (1358/1358 headless); the 78 pre-existing browser cases pass unchanged; `WithoutTheCallbackNoActivationTargetIsRendered` pins the unchanged markup. The API snapshot gained only `PanelActivation`, `ViewChange` and `ViewChangeCause`. |
| ✅ ACCEPTANCE-07 · satisfied | STEP-09: all four local gates and the full browser run (97/97), 2026-11-13, `HEAD` `7c1855a` plus the uncommitted tree. The earlier run of STEP-06 (91/91) predates the pan-preview fix. |
| ✅ ACCEPTANCE-08 · satisfied | Physical touch QA by the engineer on the device, 2026-11-13, builds `7c1855a.dirty.9bf3517f` and `7c1855a.dirty.bfc788f0`: open, close, window carry-over both ways, activation against inspection, and a drag started during the open transition, each observed and recorded in Material notes. |
| ✅ ACCEPTANCE-09 · satisfied | `ADragPreviewsOnEveryPanelOfThePlotSharingThePannedAxis` (shared x, shared y, both, neither; a panel of another plot never moves; cancellation clears every preview), `ASiblingPreviewIsHeldAfterReleaseUntilItsOwnCommittedFrame`, and `ADragMovesEverySubPanelBeforeRelease` on the demo page. Confirmed by the engineer on the device, on the overview from each panel and on the detail. |

## Authority

- 2026-11-12 — The engineer authorized deployment over SSH for device QA (closes GATE-05):
  publish `linux-arm64` self-contained locally, copy to `~/ggnet-focus-demo` on
  `ziggybrew.local`, run it as a plain user process on loopback port 15236, and open a second
  kiosk Chromium window above the existing one. No service, package or system change. The grant
  covers redeploying fixes found during this QA and the cleanup at its end, nothing else.
- 2026-11-12 — The engineer named the key for the device, `~/.ssh/id_ziggybrew_ed25519`, used with
  the `ziggybrew` host alias (`ziggy@192.168.1.100`). This closed GATE-07; the default identities
  had been refused.

## Decisions

All ruled by the engineer on 2026-11-12 at planning entry; the spec's "Rulings at binding" section
carries the same text.

- DECISION-01 — Spec OD-1, OD-2, OD-4, OD-5 and OD-6: the recommendations stand as written.
  - OD-1: a read accessor plus a `ViewChanged` callback with a cause; no `@bind`.
  - OD-2: whole-area activation as a new panel-level callback; the existing `onClick` keeps its
    meaning (background rectangle only).
  - OD-4: the payload carries the sub-panel's index and an optional key set on `.Panel(...)`.
  - OD-5: measure hidden-plot refresh on the device first. Suspension (spec §4) is built only if
    the cost is noticeable; that would enter as a new step.
  - OD-6: §1 and §3 ship now. §2 (inspection hand-over) waits for the time-cursor feature (D-03).
- DECISION-02 — OD-3, activation under `Interactivity`.
  - Without `Interactivity`: a click in the plot area activates at once.
  - With `Interactivity` and neither `CursorTooltip` nor `Crosshair`: a click activates after the
    double-click interval unless a reset follows. The interval is a calibration value, about
    300 ms, to be confirmed in physical QA.
  - With `CursorTooltip` or `Crosshair`: taps and clicks never activate; the consumer supplies its
    own control.
- DECISION-03 — The data-unit read returns null on a discrete axis and on a `LocalDateTime` axis,
  and the docs say so; the change notification still fires on both. The `LocalDateTime` half
  closed GATE-06: the scale maps a value to its index in a per-minute sample buffer and inverts
  only to the nearest sample, which grounding had missed.
- DECISION-04 — Assurance interrogation.
  - Independent audit: not scheduled, by the engineer's ruling. Verification is fail-first tests,
    the public API snapshot, the four local gates, the full browser run and physical QA.
  - Paid or external evaluation runs: not scheduled. GGNet's evals are deterministic and run
    under `dotnet test`; nothing here depends on model behaviour.

- DECISION-05 — Ruled by the engineer on 2026-11-13 after physical QA.
  - Hidden plots: 3.5 ms extra per refresh is not noticeable. Suspension (spec §4) is not built.
  - Activation delay: 200 ms instead of 300 ms, retested on the device for feel and for slow
    double-taps.
  - Pan preview across sub-panels: fixed in this Task, although the defect predates it.

- DECISION-06 — Ruled by the engineer on 2026-11-13 after the retest: the activation delay
  stays at 200 ms although a double tap can then open the detail before it resets. Documented
  as a known limit.

## Assumptions and open questions

- The spec's premises were re-verified against `main` at `7c1855a` on 2026-11-12. The spec was
  written against `net11-bump` at `cd27877` plus uncommitted changes, which that commit contains.
  All hold.
- Assumption: `InstantPosition.Unmap` truncates to milliseconds, so an `Instant` window reads
  back to the millisecond. Stated in the accessor's docs; a consumer needing finer resolution
  would settle it.
- Assumption: a touch double-tap reaches the panel as `dblclick`, so the server-side delay in
  DECISION-02 also covers touch. The browser cases in STEP-04 settle it; if false, the delay
  moves into the gesture module's existing `initialize` payload.
- Was an open question, not ruled: whether the mark's own `onclick` and the panel activation
  should both fire for one click on a mark. Built as both, mark first; disposition under
  Deferred issues and accepted gaps.

## Deferred issues and accepted gaps

- Spec §2, inspection hand-over between overview and detail, is not built. defer: blocked-by
  the time-cursor feature, whose bindable inspection value is the hand-over mechanism → D-03
- Accepted gap: with the 200 ms activation delay, a double tap slower than the delay opens the
  detail and then resets the window. Seen in physical QA; README, demo README, Blazor guide and
  the calibration comment state it → closed: accepted by the engineer, DECISION-06
- Spec §4, hidden-plot suspension, is not built → closed: measured at 3.5 ms extra per refresh on the device and ruled not noticeable, DECISION-05
- A click on a mark fires the mark's `onclick` and then the panel activation. Never ruled; the
  spec requires activation on marks and leaves `onclick` unchanged, and the bUnit cases pin the
  order → closed: ships as built and is named in the completion report for the engineer to reopen
- Grid lines and axis labels do not move during a pan preview, in any panel; only marks do.
  Predates this Task and was in view during both QA passes without complaint → closed: not an obligation of this spec, named in the completion report
- A host binding `ViewChanged` to a method must name `T`, `TX` and `TY` on `<Plot>` → closed: documented in the Blazor guide and README

## Gates

Raised by physical QA on 2026-11-13.

| ID | Gate | Owner | Blocked work | Closure |
|---|---|---|---|---|
| ✅ ~~GATE-08~~ · settled | decision — is 3.5 ms extra per refresh with the hidden detail noticeable (spec §4, OD-5) | engineer | STEP-08 | ruled 2026-11-13: not noticeable, spec §4 is not built; DECISION-05 |
| ✅ ~~GATE-09~~ · settled | decision — the 300 ms activation delay felt slightly laggy: keep it or change it | engineer | STEP-08 | ruled 2026-11-13: 200 ms, retested on the device; DECISION-05 |
| ✅ ~~GATE-10~~ · settled | decision — the pan preview moves only the dragged sub-panel, the others follow at release. It predates this Task (`Panel.razor.js` is unchanged from `7c1855a`) and lies outside the bound spec: fix here or defer | engineer | STEP-08 | ruled 2026-11-13: fixed in this Task; DECISION-05, spec r3 §5 |

## Material notes

- 2026-11-12 — No commits. `AGENTS.md` forbids committing, staging or otherwise mutating history
  or index state on the engineer's behalf, and repository instructions outrank the Git policy's
  checkpoint delegation. Verified work stays uncommitted in the working tree; evidence is
  identified by `HEAD` plus the changed files. The Task started on `main` at `7c1855a`, clean.
- 2026-11-12 — This Task picks up D-05 in `plan/deferred.md`.
- 2026-11-12 — Spec superseded during STEP-01. `specs/overview-detail-focus.md` wrongly listed
  `LocalDateTime` among the axes that read back exactly. `specs/overview-detail-focus-r2.md`
  replaces it with the engineer's ruling (DECISION-03) and changes nothing else. Only STEP-01 was
  affected, and no code had been written.
- 2026-11-12 — Cadence switched to continuous by the engineer.
- 2026-11-12 — STEP-02 as built. The payload type is `ViewChange<TX, TY>` (the analyzers reject
  an `EventArgs` suffix on a non-`EventArgs` type). Causes: `Gesture` is wheel and pan; `Reset`
  is the double-click or double-tap gesture; `HostCommand` is everything a host calls,
  `ResetViewAsync` included. A change is detected by comparing the windows with the last
  reported ones, so a command that leaves the window as it was raises nothing, and a window set
  before the first render is the baseline, not a change.
- 2026-11-12 — STEP-03 as built. `.Panel(..., onActivate:, key:)`; the payload is
  `PanelActivation(Index, Key)`, where the index counts `.Panel(...)` calls. The plot area is
  wrapped in one group carrying the click handler, rendered only when the callback is set and no
  inspection mode is on. Under `Interactivity` the handler waits 300 ms; a second click
  (`detail` above 1) or the reset `dblclick` cancels it, so the touch double-tap assumption no
  longer depends on `dblclick` alone.
- 2026-11-12 — `Plot` infers its type arguments, and the Razor compiler cannot convert a method
  group through an inferred `EventCallback<T>`. A host binding `ViewChanged` to a method names
  `T`, `TX` and `TY` on the tag. Documented in the Blazor guide and the README; the alternative,
  a `Func` parameter like `onActivate`, would lose the receiver re-render and departs from OD-1.
- 2026-11-12 — Not verified by eye: the demo page's appearance, in particular the detail chart's
  activity panel. The browser cases assert behaviour, not looks; physical QA is the first look.
- 2026-11-12 — OD-5 measurement. The demo page has a **Measure refresh** control: twenty
  refreshes of the overview alone, then twenty with the hidden detail refreshed too, each timed
  in the browser from the request to the frame after the changed SVG is in the DOM. It is
  covered by `TheRefreshMeasurementCompletesEveryRoundForBothCases`. The threshold for
  "noticeable" is the engineer's call on the device figures.
- 2026-11-12 — Build published for the device, not yet copied: `linux-arm64`, self-contained,
  Release, identity `7c1855a.dirty.fc46e926`, 121 MB, in the session scratchpad
  (`focus-publish`). The identity hashes the working tree, so any later edit, this plan
  included, needs a fresh publish before deployment.
- 2026-11-12 — Deployed for QA. Build `2.0.0-beta.0.1+7c1855a.dirty.9bf3517f` (the page header
  on the device shows the same identity), `linux-arm64` self-contained Release, in
  `~/ggnet-focus-demo/app` on `ziggybrew`; process PID in `~/ggnet-focus-demo/demo.pid`,
  endpoint `http://127.0.0.1:15236` (loopback only), `/overview-detail` answers 200. QA window:
  a second Chromium (PID in `~/ggnet-focus-demo/browser.pid`, own profile
  `~/ggnet-focus-demo/chromium-profile`, `--kiosk`, the kiosk's rendering flags) above the
  existing kiosk browser, which still runs as PID 60093. Return to the previous screen:
  `kill $(cat ~/ggnet-focus-demo/browser.pid)`. Stop the demo:
  `kill $(cat ~/ggnet-focus-demo/demo.pid)`. Remove everything: `rm -r ~/ggnet-focus-demo`.
  `~/ggnet-touch-demo` from the earlier Task was left as found. Backlight read 16 on entry and
  was not changed. Plan edits after the publish are not in the deployed identity.

- 2026-11-13 — Physical touch QA by the engineer on the device, build `7c1855a.dirty.9bf3517f`.
  - Measurement: extra 3.5 ms per refresh with the hidden detail.
  - Open: the detail opens after the delay; "ok but feel maybe laggy".
  - Payload: `1 gravity` and `2 pressure` reported, detail stays closed. Correct.
  - Drag is not a tap: confirmed. Defect seen: while one sub-panel is dragged, the other
    sub-panels stay fixed until the drag ends.
  - Double-tap reset: resets without opening. Correct.
  - Carry-over: the window travels to the detail on open and back on close.
  - Drag started during the open transition: dropped, the next drag works, as expected.
  - Activation against inspection: tap inspects, the counter stays 0, the button works.
  - Plain chart: mark, grid line and background each activate at once.
  - Cause of the defect, read from the code: each panel's gesture module previews a drag by
    translating its own marks group (`Panel.razor.js`, `#marks`); nothing moves the sibling
    panels that share the axis until the committed frame arrives.
- 2026-11-13 — Spec superseded again. `specs/overview-detail-focus-r3.md` adds §5 (pan preview
  across sub-panels), ACCEPTANCE-09 and the rulings of DECISION-05; nothing else changes. Steps
  01 to 08 are unaffected except that ACCEPTANCE-07 is rerun on the changed tree.
- 2026-11-13 — STEP-09 as built. The `initialize` payload gained `group` (the plot id),
  `sharedX` and `sharedY` (true when the plot has one scale on that axis). At drag recognition
  the dragged panel collects the module's other panels of the same group and translates their
  marks by its own delta scaled by plot-area width or height, for each panned axis that is
  shared. Linked previews clear on any cancellation and are held after release until that
  panel's own marks mutate. Grid lines and axis labels still do not move during a preview, in
  any panel; that was so before and was not reported. The activation delay is 200 ms.
- 2026-11-13 — Redeployed for the retest. Build `7c1855a.dirty.bfc788f0` (the page on the
  device reports the same identity, and the served `Panel.razor.js` contains the linked
  preview), same location, port and launch as before; the QA browser profile was recreated so
  no cached module is used. The existing kiosk browser (PID 60093) still runs. The first
  redeploy of the day lacked the identity stamp and was replaced within minutes. Plan edits
  after the publish are not in the deployed identity.
- 2026-11-13 — Retest by the engineer on the device, build `7c1855a.dirty.bfc788f0`.
  - Pan preview: every sub-panel moves with the dragged one, no jump at release. As expected.
  - 200 ms delay: feels better than 300 ms.
  - Double tap: the detail opens. Ruled acceptable (DECISION-06); 200 ms stays.
  - Detail chart: its two panels move together.
- 2026-11-13 — After the retest only documentation changed (the double-tap limit in `README.md`
  and the demo README), so the deployed identity differs from the final tree by those lines and
  this plan. Local gates were rerun on the final tree.
- 2026-11-13 — Device cleaned up. The QA browser and the demo were stopped,
  `~/ggnet-focus-demo` was removed, nothing listens on port 15236, the kiosk browser (PID
  60093) still runs, backlight reads 16 as on entry. `~/ggnet-touch-demo` from the earlier Task
  was left as found.
