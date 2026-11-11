# Touchscreen support

- Profile: Task
- Status: completed
- Cadence: continuous
- Spec context: the engineer's request of 2026-11-11 (no separate spec file). Closed by the
  engineer on 2026-11-12 ("close the touch QA"); open follow-ups are D-03 and D-04 in
  `plan/deferred.md`.

## Contents

- [Steps](#steps)
- [Acceptance](#acceptance)
- [Decisions](#decisions)
- [Deferred issues and accepted gaps](#deferred-issues-and-accepted-gaps)
- [Material notes](#material-notes)
- [Deployment record](#deployment-record)
- [Physical QA matrix](#physical-qa-matrix)

## Steps

| ID | Checkable result | Affected surfaces | Acceptance | Verification |
|---|---|---|---|---|
| ✅ STEP-01 · completed | Both reported defects reproduced on `net11-bump` at `cd27877` with failing browser tests before any fix. | `tests/GGNet.E2ETests/GestureModuleTests.cs` | ACCEPTANCE-01 | `GGNET_E2E=1 dotnet test tests/GGNet.E2ETests`: 3 new cases fail on the unfixed module. |
| ✅ STEP-02 · completed | Gesture module fixed: drag threshold, deferred capture, trailing-click suppression, capture-phase coordinate tracking, touch tooltip dismissal, pinch-aware cancellation. | `src/GGNet/Components/Panel.razor.js`, `.github/instructions/blazor.instructions.md` | ACCEPTANCE-02 | Module-level browser regressions in `GestureModuleTests`. |
| ✅ STEP-03 · completed | Demo touch pages and the contact diagnostic. | `src/GGNet.Demo/**`, `src/GGNet.Demo/README.md` | ACCEPTANCE-03 | `TouchDemoTests` over the spawned demo. |
| ✅ STEP-04 · completed | Standalone ARM64 demo deployed to the QA device and reachable over loopback. | device only (`~/ggnet-touch-demo`) | ACCEPTANCE-04 | `curl` on the device: pages 200, build identity in the page matches the published one. |
| ✅ STEP-05 · completed | Gesture-semantics decision (GATE-01). | — | ACCEPTANCE-05 | Engineer's ruling recorded under Decisions. |
| ✅ STEP-06 · completed | Assisted physical QA on the device's touchscreen, all twelve groups. | — | ACCEPTANCE-06 | The matrix below, filled from the engineer's observations. |
| ✅ STEP-07 · completed | Display restored, demo process stopped, final report. | device only | ACCEPTANCE-07 | Process list on the device. |

## Acceptance

| Criterion | Evidence |
|---|---|
| ✅ ACCEPTANCE-01 · satisfied | On the unfixed module: `TapWithFingerJitterClicksTheMarkWithoutPanning` failed for 2 px and 6 px (one `OnPanEndAsync` each, alongside the mark click); `StationaryTapsPositionTheCursorTooltipAtEachContact` failed with the tooltip at `12px, 12px` for taps at (300, 400) and (420, 250). |
| ✅ ACCEPTANCE-02 · satisfied | `GGNET_E2E=1 dotnet test tests/GGNet.E2ETests`: 78 total, 78 passed, 0 skipped on the final tree (33 before this Task). Covers stationary taps, jitter (touch and mouse), deliberate drags, trailing clicks, native pinch, a second finger joining, the owning finger lifting first, cancellation, lost capture, responsive transforms, disposal and re-initialization. |
| ✅ ACCEPTANCE-03 · satisfied | `TouchDemoTests` (30 cases, emulating the device's 1185×1920 viewport) pass: mark and ancestor counters, background tap, drag without clicks, near-miss inspection and click delivery, tooltip at the contact on three charts, stale-crosshair and reopen regressions, nested scroller, modal dialog scrolling and reopen, remount and navigation, double-tap reset, pinch scales the page only, preview after host reset, selection policy, contact diagnostic. |
| ✅ ACCEPTANCE-04 · satisfied | See [Deployment record](#deployment-record). |
| ✅ ACCEPTANCE-05 · satisfied | GATE-01, DECISION-02 and DECISION-03 below. Opt-in chart pinch and drag-to-scrub were approved but not built; the engineer's later time-cursor proposal supersedes both (D-03). |
| ✅ ACCEPTANCE-06 · satisfied | All twelve groups recorded from the engineer's observations on the device (matrix below): every group passes, six after a fix found during QA and redeployed. Limits: a tenth simultaneous contact was not practical to place (9 tested); group 11 was reported as one result for the whole group. |
| ✅ ACCEPTANCE-07 · satisfied | 2026-11-12: QA browser window and demo stopped; port 15236 free; the kiosk browser is the PID it was on entry (60093); backlight raw value 3, unchanged from the first read. `~/ggnet-touch-demo` (logs, `BUILD-PROVENANCE.txt`, app, QA profile; 266 MB) is kept as evidence; `rm -r ~/ggnet-touch-demo` removes it. |

## Decisions

- DECISION-01 — Defect fixes proceed without a semantics ruling. Each change below restores a
  behaviour the module already claimed, or removes a double effect; none introduces a gesture.
  - A press becomes a pan after 4 px (mouse), 6 px (pen) or 10 px (touch) of screen travel.
    Compatibility effect: a mouse drag shorter than 4 px no longer pans.
  - Pointer capture moves from press to recognition, so a tap or click is never retargeted.
  - The click trailing a recognized drag is discarded for marks and ancestors.
  - A touch `pointerdown` is no longer default-prevented. Compatibility effect: on a pan-enabled
    panel a tap now delivers the compatibility mouse events, so tap-to-hover tooltips and the
    crosshair work there as they already did on panels without pan.
  - A pan is cancelled, uncommitted, when a native pinch rescales the visual viewport. Before, a
    second finger joining a pan zoomed the page and also committed a pan.
  - A touch tooltip is dismissed by a touch pan, a scroll or a viewport change.
  - A tooltip that would flip past the left or top edge is clamped inside the viewport.
- GATE-01 — New gesture semantics. Proposal delivered 2026-11-11; the engineer replied "go"
  and did not object when told it was read as approving the recommended defaults (native page
  pinch by default with an opt-in chart pinch; touch tap-to-inspect under `CursorTooltip` and
  `Crosshair`; drag-to-scrub only without `Pan`; one-finger pan; double-tap reset). Chart pinch
  and drag-to-scrub were not built; on 2026-11-12 the engineer submitted a time-cursor proposal
  that subsumes both, reviewed in-session and parked as D-03.
- DECISION-02 — Tap-to-inspect, implemented after QA group 7. A touch tap inspects the nearest
  mark within 24 screen px; tooltip, crosshair and host hover callbacks follow that one mark; a
  tap with no mark in reach moves the crosshair to the finger. Touch compatibility hover is
  stopped inside the panel. Mouse and pen hover, and all click targeting (mark `onclick`
  included), are unchanged. The 24 px reach is a calibration value: on the device (1185 CSS px
  at scale 1, ≈0.11 mm per px) it is ≈2.7 mm and passed QA group 7.
- DECISION-03 — Click follows inspection (engineer ruled A, 2026-11-12, after QA group 10). On
  charts with `CursorTooltip` or `Crosshair`, a touch tap that inspects a mark delivers its
  click to that mark: the native click is stopped and one click is dispatched on the mark, so the
  mark callback and ancestors each see it once. A near-miss no longer reaches the panel's
  `onClick` on those charts. Mouse, pen and charts without those options keep native targeting.

## Deferred issues and accepted gaps

- Tapping a mark left the crosshair at the previous miss, and a dismissed mark could not be
  reopened by tapping it again. → closed: fixed by DECISION-02, pinned by `TouchDemoTests`
- Mark `onclick` needed a direct hit while the tooltip reached 24 px. → closed: fixed by
  DECISION-03, pinned by `TapThatInspectsAMarkDeliversOneClickToThatMark`
- Mouse plus touchscreen together could end an emulated hover early. → closed: touch compatibility
  hover is stopped inside inspection-enabled panels (DECISION-02); QA group 11 found no regression
- Unrelated gate failure, not repaired (engineer did not choose; the recommended "record, do not
  repair" applied): `NU1902` for transitive `Microsoft.Extensions.Validation 11.0.0-rc.3.26522.148`
  (GHSA-m28h-pj36-4364). defer: out-of-scope → D-04
- Time-cursor inspection across panels (engineer's proposal, reviewed 2026-11-12), superseding the
  approved opt-in chart pinch and drag-to-scrub. defer: out-of-scope → D-03
- A tenth simultaneous contact and host-linked separate `Plot` charts were not tested on the
  device. → closed: accepted as limits of this QA pass, recorded under ACCEPTANCE-06; nothing in
  this Task depends on them

## Material notes

- 2026-11-11 — The session ran in the `net11-bump` worktree
  (`GGNet-worktrees/net11-bump--340656a9…`), not `/Users/pablo/source/GGNet`, whose checkout is on
  `main`; a branch is checked out in one worktree at a time. Tree clean at `cd27877` on entry.
- 2026-11-11 — Baseline: build clean; `GGNET_E2E=1` browser suite 33/33, 0 skipped. No NodaTime
  build-assets mismatch on this checkout. `dotnet` hangs inside the agent sandbox and ran
  unsandboxed, as the troubleshooting guide §1 prescribes.
- 2026-11-11 — Taking capture mid-gesture releases a touch's implicit capture on the pressed mark;
  that `lostpointercapture` bubbles to the capture group. Cancellation is scoped to the capture
  element's own loss. A capture released before the browser activates it fires no event at all,
  so ownership is also checked with `hasPointerCapture`.
- 2026-11-11 — Chromium does not send `pointercancel` when a second finger starts a pinch on a
  pointer stream the page already owns, and the pinch does not change the screen CTM. The pan
  watches `visualViewport` scale and offset instead.
- 2026-11-12 — During QA a pattern-matching `pgrep -f` over SSH matched its own remote shell and
  ended that SSH session; no other process was affected. Use a self-excluding pattern
  (`[g]gnet-…`) when stopping processes by command line.
- 2026-11-12 — Final automated run on the working tree. `dotnet build GGNet.slnx -warnaserror`:
  6 errors, all `NU1902` (D-04); with `-p:NuGetAudit=false` on the command line only: 0 warnings,
  0 errors. `dotnet test GGNet.slnx -p:NuGetAudit=false`: 1584 total, 1506 passed, 2 failed
  (`ValidatorScriptEvals.ScriptValidatesAMinimalSnippet`, `McpServerEvals.ValidatePlotCompilesAndRenders`,
  both `NU1902` in their scratch restores), 76 skipped — the browser cases, which self-skip without
  `GGNET_E2E=1`. `GGNET_E2E=1 dotnet test tests/GGNet.E2ETests`: 78 passed, 0 skipped. Both
  `dotnet format --verify-no-changes` checks exit 0. `git-flow-tests.sh`: PASS, 100 checks. No golden
  or public-API snapshot changed. Emulated touch is supporting evidence; the matrix below is the
  physical acceptance.

## Deployment record

| Field | Value |
|---|---|
| Build identity | `2.0.0-beta.0.1+cd27877.dirty.2c391c71` (page header and `BUILD-PROVENANCE.txt`): two-layer contact diagnostic during QA group 12. Earlier builds: `…954ddc09` (click follows inspection), `…ccb9255e` (scrollable demo dialog), `…de4cb6f8` (long-press text selection fix), `…210aa398` (touch tap-to-inspect), `…3c6d32a3` (held release preview after group 3), `…a04e5027` (first), `…335b7021` (marks-group fix after QA group 2), `…8f7c0d42`. `…8f7c0d42` only moves the build id from the footer to the header: the engineer reported the footer "not displayed in full", presumably behind the fixed contact bar. |
| Source | `cd278770d804b917deb75c706b6f2e8b200eeb98`, branch `net11-bump`, plus the uncommitted changes listed in `BUILD-PROVENANCE.txt` |
| Target | `linux-arm64`, self-contained, Release; published 2026-11-11T21:41Z |
| Device | `ziggybrew.local` — Raspberry Pi 5, Debian 13 (trixie) aarch64, Chromium 156.0.8078.73, labwc 0.20.2; DSI panel mode 1200×1920 (portrait). Chromium runs it at device scale 1: the engineer measured 1185 CSS px across beside the scrollbar (≈0.11 mm per CSS px). `TouchDemoTests` now emulates 1185×1920. |
| Location | `~/ggnet-touch-demo/app` (log `../demo.log`, PID file `../demo.pid`) |
| Endpoint | `http://127.0.0.1:15236` (loopback only), pages `/touch` and `/touch-layout` |
| Process | plain user process started over SSH; no service, no autostart, no package installed |
| Prerequisites found | `libicu` 76, `libssl` 3, `libstdc++` 6; port 15236 free; no .NET SDK on the device |
| Stop | `kill $(cat ~/ggnet-touch-demo/demo.pid)` |
| Display handoff | Restored 2026-11-12 (ACCEPTANCE-07). Agreed by the engineer on 2026-11-11. A second Chromium window (PID in `~/ggnet-touch-demo/browser.pid`) with its own profile `~/ggnet-touch-demo/chromium-profile`, `--kiosk`, mirroring the kiosk's rendering flags and adding no sandbox or gesture flag. The existing kiosk browser (its profile and supervisor) keeps running underneath, untouched. |
| Return to the previous screen | `kill $(cat ~/ggnet-touch-demo/browser.pid)` closes only the QA window; the kiosk window is beneath it. |

The runbook is `src/GGNet.Demo/README.md`.

## Physical QA matrix

Result is one of pass, fail, uncertain. Class is one of implementation, browser, hardware,
preference. Nothing here is inferred from HTTP readiness or emulation.

| # | Group | Expected | Observed | Result | Class |
|---|---|---|---|---|---|
| 0 | Display handoff | QA page full screen, build id in the footer | Build `…a04e5027`: confirmed. | pass | — |
| 1 | Centre and edge taps, stationary taps, slight jitter | Each mark tap raises both counters by one; a background tap raises the ancestor only; the axis never moves | Build `…a04e5027`: "it works". The dots are quite small, so a mark is easy to miss. | pass | preference (mark size is the host's `size:`; the demo uses 9) |
| 2 | Mark callbacks: once per tap, none after a drag | A drag previews under the finger, shifts the axis on release and raises no counter | Build `…a04e5027`: the engineer suspected the dots stopped following the finger after **Reset view**. Reproduced in emulation: pan, Reset view, drag — no preview. Cause: the re-render replaced the marks group and the module kept the detached one (pre-existing; wheel zoom was stranded the same way). Fixed in build `…335b7021`; regression `PreviewStillFollowsTheFingerAfterTheHostResetsTheView`. Retest on build `…8f7c0d42`: "pass". | fail, then pass on retest | implementation |
| 3 | Each pan axis, diagonal movement, release, visual stability | Marks follow only the configured axes; no jump or flicker on release | Build `…8f7c0d42`: on lifting the finger the series appears for a brief moment at what seems its original position; the same on Pan y and Pan x and y. Axis restriction and commit not otherwise faulted. Cause: the preview transform was cleared at release, before the committed frame arrived (pre-existing). Fixed in build `…3c6d32a3`: the preview is held until the committed frame mutates the marks; regression `ReleasedPreviewIsHeldUntilTheCommittedFrameArrives`. Retest on build `…3c6d32a3`: "pass". | fail, then pass on retest | implementation |
| 4a | Page scrolling, starting inside and outside charts (`/touch`) | Page scrolls from text and from a chart's unused axis; the Both chart owns every drag | Build `…3c6d32a3`: "pass". | pass | — |
| 4b | Nested-container scrolling (`/touch-layout`) | Swipes on filler scroll the box both ways; on the chart a vertical swipe scrolls the box and a horizontal one pans; scrolling dismisses the bubble | Build `…de4cb6f8`: all five "as expected". | pass | — |
| 4c | Dialog embedding and scrolling (`/touch-layout`) | Bubble above the dialog; the chart pans; a vertical swipe on filler scrolls the dialog; reopening mounts a fresh chart | Build `…de4cb6f8`: open, bubble above the dialog, pan, close and reopen — "as expected". Vertical swipe on dialog filler: "the page scrolls, not the dialog". Measured at 1185×1920: dialog content 1099 px inside an 80vh (1536 px) limit, so the dialog had nothing to scroll and the browser chained the swipe to the page. Demo defect for this screen, not GGNet. Fixed in build `…ccb9255e`: dialog height 50vh with `overscroll-behavior: contain`; regression `VerticalSwipeInsideTheDialogScrollsTheDialogAndNotThePage`. Retest on build `…ccb9255e`: "as expected". | pass after demo fix | demo (content height), browser (scroll chaining) |
| 5 | Pinch policy: page scale versus chart domain | Under the current policy two fingers scale the browser page (`page scale` above 1.00) on text and inside charts; the chart domain never changes | Build `…3c6d32a3`: "pass". Native page pinch is therefore delivered by this device's Chromium. | pass | — |
| 6 | Second finger joining and leaving, primary lifting first, extra contacts, cancellation | A second finger joining a pan cancels it uncommitted while the page may scale; a leftover finger cannot pan; an interrupted drag leaves no displaced marks | Build `…3c6d32a3`. Second finger joins a pan: page scales, marks snap back, no pan committed — pass. Two fingers down, first lifted, drag with the remaining one: "pan" — unexpected if the chart panned with a non-owning finger; the first attempt did not control which finger landed first. Repeated in sequence (A down, B down, contact counts 2 then 1 confirmed on the diagnostic, lift A, drag B): "does nothing" — pass; the remaining, non-owning finger cannot pan. Finger resting on text outside the chart, drag inside with another: "scale" — the page pinch-zooms and the chart does not pan; the browser treats any two contacts as a pinch, consistent with the native-pinch policy, and a resting contact therefore blocks a chart pan. Drag leaving the chart before lifting: the pan commits (axis numbers changed), no marks left displaced, a normal drag works afterwards — pass. Diagnostic `cancel` read 64 by then: cumulative over the session, one per contact the browser took over for a native scroll or pinch. | pass, with one browser note | join, leftover finger, drag leaving the chart: pass; resting finger: browser |
| 7 | Tooltip and crosshair: position, first stationary touch, edges, dismissal, finger occlusion | Tap a mark: bubble at the contact for that mark; tap background: bubble closes, crosshair at the finger | Build `…3c6d32a3`: points are easy to miss, so the crosshair appears instead; the engineer suspects a tap may open the bubble of an adjacent point. Emulation probe (720×1280): marks render about 16 px across (≈3 mm); a tap 10 px off misses, with or without a 12 px contact radius (Chromium's touch adjustment does not retarget); all ten centred taps inspected the right mark; neighbouring marks are at least 35 px apart. Found instead: tapping a mark left the crosshair at the previous miss, so line and bubble could name different x — likely source of the adjacent-point impression, not confirmed on device. Fixed by approved tap-to-inspect in build `…210aa398`; Retest on build `…210aa398`: near-miss tap opens that mark's bubble with the crosshair through it, bubble and crosshair always agree across several marks including close neighbours, background tap closes the bubble and moves the crosshair to the finger — all "as expected". Edges: "no flip" — the device panel is 1200×1920 natively, so the bubble likely had room; the module flips only against the viewport edge, and the QA expectation (flip at the plot edge) was overstated; the engineer confirmed the bubble was fully readable — pass. Pan dismissal and same-mark reopen, scroll dismissal, press-and-hold placement, mark-anchored chart: "as expected". Press-and-hold also selected the first x-axis label (Chromium long-press selection; not reproducible from DevTools touch in headless). Fixed in build `…de4cb6f8` with `user-select: none` on the interactive gesture group; retest on the tooltip and X-only charts: "as expected". | pass after one fix | implementation (selection), QA expectation (edges) | implementation (targeting), preference (mark size) |
| 8 | Reset and native double-tap conflicts | Double-tap on background or on a mark restores the authored view without the page zooming; two slow taps do not reset | Build `…de4cb6f8`: all three "as expected". | pass | — |
| 9 | Ancestor click handlers and multiple charts | Mark tap raises mark and ancestor counters once; background tap raises only the ancestor; a drag raises neither; inspecting another chart releases the first; one bubble at a time across glued and mark-anchored charts | Build `…de4cb6f8`: all five "as expected". | pass | — |
| 10 | Navigation away and back, repeated interaction, stale or duplicate handlers | Every tap raises the cumulative mark counter by exactly one, after remounts and navigation; a drag pans once | Build `…ccb9255e`, first attempt: first tap "as expected"; after one unmount and mount the counter read 2, ambiguous because the counter is cumulative (1 → 2 is correct). The engineer asked to restart the group on a fresh page; the QA window was relaunched on `/touch-layout`. Restarted on a fresh page (relaunched twice at the engineer's request). Navigation and return: counts reset as expected; tapping a dot then showed its bubble but did not raise the callback. Emulation at 1185×1920: a tap 16 px from a 12 px-radius mark shows the bubble and fires no callback, with or without navigating — not a stale handler but the inspect/click split of DECISION-02. Engineer ruled option A (DECISION-03); implemented in build `…cd27877.dirty.954ddc09`. Retest on build `…954ddc09` from a fresh page — taps including near misses (+1 each), remount ×1 and ×3, drag, navigate away and back, and the back-navigation step: "all as expected" (the method used for back navigation was not stated). | pass after one fix | implementation (targeting split) |
| 11 | Mouse and keyboard regression, where available | Mouse hover drives bubble and crosshair; click-drag pans; 1–2 px click wiggle stays a click; wheel zooms x; Tab moves focus through nav and buttons | Build `…954ddc09`: "as expected" (reported for the group as a whole). Also covered on the Mac by the browser suite (`MouseJitterBelowTheThresholdIsAClickNotAPan`, `MouseHoverKeepsNativeTooltipAndCrosshairBehaviour`, wheel and drag smoke tests). | pass | — |
| 12 | Contact diagnostic up to ten fingers | Each finger counted and listed; all cleared on lift; several fingers on a chart do not pan it | Build `…954ddc09`: 5 contacts with one hand; adding a thumb from the other hand reset the count to 0. Ambiguous: the bar counted pointer events only, which the browser cancels when it claims a native gesture. Build `…2c391c71` adds a touch-event count (`touches`, `touch max`) beside the pointer count. Retest over plain text, fingers still: five fingers — touches 5, pointers 0, cancel 5; adding a thumb — touches 6, pointers 0, cancel 10 (Reset not pressed; the +5 is unexplained, possibly fingers re-placed); after Reset, fingers added without lifting — touches up to 9; a tenth contact was not practical to place. The earlier drop to 0 was the pointer count after Chromium cancelled the page's pointer streams for native gesture handling, not lost contacts. GGNet acts on one uncancelled pointer (pan) and leaves two or more to native pinch. On lifting: `touches` returned to 0; the bar read `touch max 5` and `up 5`, which does not match the 9 reached before — most likely a separate five-finger attempt after another Reset; not reconciled. | pass for delivery up to 9; 10 not tested | hardware delivery: ≥ 9 contacts; browser: pointer cancellation with ≥ 2 contacts |
