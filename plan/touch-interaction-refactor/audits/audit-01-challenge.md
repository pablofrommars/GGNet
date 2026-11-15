# Audit 01 challenge: touch interaction refactor

## 1. Identity, target state and independence

- Challenges `audits/audit-01.md` (FINDING-01 to FINDING-09) of `plan/touch-interaction-refactor/task-plan.md`, under the dispatch `audits/audit-01-dispatch.md`.
- Challenger: the pinned `protodog-challenge` agent (Claude Fable 5.2, `claude-fable-5-2`). It authored neither the audited state nor `audit-01.md`. The auditor did not take part.
- Target state: HEAD `7fd4b8e06098fb8cc730545c635a1770506bcc71` plus the uncommitted tree of dispatch section 10. Rechecked with `shasum -a 256 -c` over all 50 manifest rows: 49 match; only `plan/touch-interaction-refactor/task-plan.md` differs, as expected (it now records the capture and STEP-06).
- The challenge wrote this file only. No source, test, doc, plan, dispatch or report was edited; no git index or history change.

## 2. Method and executed evidence

Every finding was re-derived by reading the pinned code, then tested by execution where execution could discriminate. Two temporary probe files were created, run and deleted (section 7). They only observed and logged state; none asserted.

| Run | Command | Result |
|---|---|---|
| Build with probes | `dotnet build GGNet.slnx -warnaserror --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false` | 0 warnings, 0 errors |
| Component probes (bUnit) | `dotnet tests/GGNet.Components.Tests/bin/Debug/net11.0/GGNet.Components.Tests.dll --filter-class '*ZzChallengeProbeTests'` | 6 executed, 0 skipped |
| Browser probes, run 1 | `GGNET_E2E=1 dotnet tests/GGNet.E2ETests/bin/Debug/net11.0/GGNet.E2ETests.dll --filter-method '*ChallengeProbe*'`, outside the sandbox | 8 executed, 0 skipped, 1m53s |
| Browser probes, run 4 (fixed per-frame latency) | same filter | 2 executed, 0 skipped, 36s |
| Build after deleting probes | same build command | 0 warnings, 0 errors |

Browser runs 2 and 3 were discarded as invalid evidence: their latency emulation (pulsed `Hold`/`Release`) either starved the forwarder or opened zero-latency windows, so neither modelled a constant round trip. Run 4 delays every server frame by a fixed interval through `Hold` plus scheduled `ReleaseNext`.

Limits: Chromium on macOS arm64 with CDP touch input, loopback demo. No physical device, no Safari. Probes used the shipped demo pages unchanged.

## 3. Dispositions

| Finding | Disposition | Action |
|---|---|---|
| FINDING-01 | confirmed (reproduced under 100 ms and 300 ms latency; not reproduced at loopback) | remediate → STEP-07 |
| FINDING-02 | confirmed, and understated: the parameter path is deterministic, not a race | remediate → STEP-08 |
| FINDING-03 | confirmed, and understated: reachable on the shipped demo without panning | remediate → STEP-09 |
| FINDING-04 | confirmed (reproduced deterministically; the window is the round-trip time) | remediate → STEP-10 |
| FINDING-05 | confirmed for Loading-then-Render; context replacement confirmed as behaviour, its status needs a ruling | remediate → STEP-11; `defer: needs-decision` → GATE-07 for context replacement |
| FINDING-06 | confirmed | remediate → STEP-12 |
| FINDING-07 | confirmed (reproduced with an injected capture failure; natural occurrence rare) | remediate → STEP-13 |
| FINDING-08 | confirmed | remediate → STEP-14 |
| FINDING-09 | overstated | `defer: needs-decision` → GATE-06 |
| FINDING-10 (new) | confirmed by reading | remediate → STEP-11 |

### FINDING-01: watchdog measures time since last idle

- **Disposition:** confirmed.
- **Challenge evidence:**
  - `src/GGNet/Components/Panel.razor.js:1378` arms the timer only when none is armed; `finishWatchdog` (`1394-1399`) clears it only when `pendingView`, `pendingInspection`, `commands` and `inFlight` are all empty. `inspect` (`1345-1358`) replaces `pendingInspection` with a new revision on every scrub move, and settlement (`1430-1434`) needs that exact revision.
  - Browser probe, run 4: a hold then a continuous scrub (about 59 moves/s) on `/time-cursor` with every server frame delayed by a fixed interval. At 300 ms: `position-changes=19` at 6.0 s, `data-ggnet-interaction="unavailable"` first seen at 9959 ms with `position-changes=32`. At 100 ms: `position-changes=55` at 6.0 s, `unavailable` at 9974 ms with `position-changes=91`. The server acknowledged throughout; no operation was older than a few hundred milliseconds.
  - Browser probe, run 1, no added latency: the same scrub for 12 s (676 moves) stayed `inspecting` and pinned on release. At loopback the pipeline goes idle between moves.
- **Corrected claim:** the mechanism is as reported. It needs a round trip longer than the gap between pointer moves, which 100 ms already satisfies; it does not occur at loopback. The "five flick-pans at 2 s latency" example was not executed.
- **Material consequence:** on any real network a scrub or a pan sequence sustained for 10 s ends in the permanent quarantine on a healthy circuit. This contradicts "a recovery bound, not permission to treat elapsed time as acknowledgement" and leaves ACCEPTANCE-07 unsupported. Observed side effect: after the quarantine the host keeps the unpinned provisional selection (`pinned=following` after release), because `restore` returns early once `unavailable` (`1361`).
- **Gate:** none.
- **Action:** remediate → STEP-07.
- **Verification:** a new browser case with a fixed per-frame delay (100 ms and 300 ms): a 12 s continuous scrub stays `inspecting`, pins on release and never shows `unavailable`. It must fail on the pinned state (it does: about 9.96 s) and pass after. `WatchdogQuarantinesLateInspectionRepliesUntilRemount` and both disconnect cases must still pass, since a fully held circuit must still quarantine at 10 s.

### FINDING-02: epoch advances with no frame carrying it

- **Disposition:** confirmed, and understated.
- **Challenge evidence:**
  - Reading: `Plot.razor.cs:228-232` and `:216-219` increment `touchEpoch`; `InspectAsync` (`:250-257`) returns before any change when `!InspectionEnabled`; `RenderedTouchEpoch` moves only in `Compose` (`:487-491`); `Panel.razor.cs:445` emits the inspection stamp only for `Touch.Inspect`; `Panel.razor:161` renders the cursor component only when `InspectionEnabled`.
  - bUnit probe, Clear path: navigation-only plot, stamp `… 0 0 0 100` before and after `ClearInspectionAsync`, no `[data-ggnet-inspection]` element; `OnTouchNavigateAsync` with the rendered epoch left `XView` at `(0, 100)` and raised no `ViewChanged`. Control with `Crosshair = true`: the inspection stamp advanced to epoch 1.
  - bUnit probe, parameter path: a navigation-only plot mounted with a non-null `Inspection` rejected its **first** navigation (`XView` stayed `(0, 100)`). The mount order is `OnInitialized` → `Compose` (stamp epoch 0) → `OnParametersSetAsync` (epoch 1). No race is involved.
  - Browser probe, Clear path on `/overview-detail`: Navigation only → pan (`overview-changes=1`, settled) → Clear → pan: `pending` with a held transform and `overview-changes` still 1 at 1.5 s; `unavailable` at 11.5 s.
  - Browser probe, parameter path: pin in Owned, switch to Navigation only, six pans: `overview-changes=0` after every pan, `pending` throughout, `unavailable` 11 s later.
- **Corrected claim:** the Clear path is as reported. The parameter path is not race-dependent: in the demo's Navigation-only mode with a retained selection, every touch navigation is rejected from mount and the chart quarantines 10 s after the first gesture. The same holds for any host that passes a non-null `Inspection` to a plot whose inspection is disabled.
- **Material consequence:** the shipped Navigation-only mode does not navigate whenever a selection exists, which is its normal state after the Owned mode. ACCEPTANCE-03 ("navigation works with inspection disabled"), ACCEPTANCE-06 and ACCEPTANCE-09 are unsupported for that mode. Plan evidence is affected: physical QA 12 recorded Navigation-only pan and pinch as passing with a retained selection (12a). The probe shows the previews held as `pending` with zero committed view changes, so that pass was visual only; `All` supersedes and clears the pending state before the watchdog fires. `NavigationOnlyAcceptsContactsInDifferentPanelsWithoutACrosshair` passes only because it has no selection and no Clear.
- **Gate:** none. The repair restores specified behaviour.
- **Action:** remediate → STEP-08.
- **Verification:** fail-first component cases: navigation-only plot (a) after `ClearInspectionAsync`, (b) mounted with a non-null `Inspection`, (c) `Crosshair = true` with `Touch.Inspect = false` after an `Inspection` parameter change; each accepts a navigation carrying the rendered epoch. Fail-first browser cases on `/overview-detail`: Navigation only → Clear → pan commits; pin → Navigation only → pan commits; neither reaches `unavailable`. Rerun `ClearInvalidatesQueuedTouchEvenWhenItTargetsTheSameValue` and `HostClearWinsOverAHeldTouchReply`, because Clear must still invalidate a pending touch inspection.

### FINDING-03: unmappable touch inspection gets no acknowledgement

- **Disposition:** confirmed, and understated.
- **Challenge evidence:**
  - Reading: `Panel.razor.cs:632-636` returns before `AcceptTouch` when `Unmap` is null; `DiscretePosition.Unmap` (`Scales/DiscretePosition.cs:124-134`) and `DateTimePosition.Unmap` (`:230-242`) return null outside `[0, count)`; the base `Scales/Position.cs:76` returns null.
  - Browser probe on the `/time-cursor` "snap" chart (discrete x, `Crosshair = true`, inferred touch inspection, rendered view `-1.05 … 4.05`): a centre tap pinned normally. A tap 2 px inside the left edge left `pending` with one provisional line at 1.6 s and `unavailable` at 11.6 s. No pan was needed: the default padding puts the outer 1.05 units outside the index range.
- **Corrected claim:** reachability is high, not medium. One tap in the padding of a discrete or snapped date-time axis quarantines the chart, on a shipped demo page.
- **Material consequence:** a single tap in empty space disables the chart until remount. ACCEPTANCE-02 and ACCEPTANCE-07 are unsupported for snapped axes, which the spec keeps supported.
- **Gate:** none.
- **Action:** remediate → STEP-09.
- **Verification:** fail-first browser case: an edge tap on the snap chart settles (no provisional line, no interaction attribute) within the normal round trip, leaves the previous selection unchanged, and a following centre tap pins. Fail-first component case: `OnTouchInspectAsync` with an out-of-range mapped value produces an inspection stamp carrying that revision and no `InspectionChanged`.

### FINDING-04: mouse, wheel and pen commits make a pending touch navigation unmatchable

- **Disposition:** confirmed.
- **Challenge evidence:**
  - Reading: `Panel.razor.cs:344` and `:568` commit with `Gesture`, which `Plot.razor.cs:591` does not count; `InteractiveRenderModeHandler.cs:92-106` coalesces refreshes and then waits on a semaphore released by `OnAfterRender`.
  - Browser probe on `/time-cursor`: server frames held, "Append readings" clicked so that one render was in flight, one-finger pan, then a mouse wheel notch, then release. Result at 1.5 s: `view-changes=2`, every stamp `… 0 1 1782872787527.1228 1782943113927.1228` (the touch revision with the wheel-zoomed bounds), state `pending` with a preview transform `scale(0.8139…)`; `unavailable` at 11.5 s.
- **Corrected claim:** the coalescing window is not a sub-millisecond race. The render loop waits for the browser to acknowledge the previous batch, so the window is the round-trip time of any render in flight. With telemetry and latency it is wide open.
- **Material consequence:** on a hybrid device a wheel or mouse or pen pan shortly after a touch release quarantines the chart. "Latest intended operation wins" (ACCEPTANCE-07) fails for mixed input.
- **Gate:** none. Making the later non-touch gesture supersede the pending touch navigation is what the spec already requires.
- **Action:** remediate → STEP-10.
- **Verification:** the probe sequence as a fail-first browser case: after release the final window is the wheel-zoomed one, no transform and no interaction attribute remain, and `unavailable` never appears. A component case: a wheel commit after an accepted touch navigation yields a stamp the client treats as superseding (higher epoch). Rerun the mouse and pen pan cases (`PenDragStillPansWithoutPinning`, GestureModule mouse rows) for ACCEPTANCE-08.

### FINDING-05: epoch rule misses two forms of host viewport replacement

- **Disposition:** confirmed for Loading-then-Render. For context replacement the behaviour is confirmed; whether it is a defect needs a ruling.
- **Challenge evidence:**
  - Reading: `Plot.razor.cs:591-595` bumps only for `RenderTarget.Render` but overwrites `touchXView` unconditionally; `:196-199` overwrites it before the refresh of a replaced context.
  - bUnit probe, Loading-then-Render: `SetXView(10, 20)`, `RefreshAsync(Loading)`, `RefreshAsync(Render)`. Stamp went from `… 0 0 0 100` to `… 0 0 10 20` (epoch unchanged) and the older touch navigation (epoch 0) was accepted: `XView = (40, 90)`.
  - bUnit control, Render only: the same older navigation was rejected, `XView = (10, 20)`.
  - bUnit probe, context replacement: a new `Context` with window `(10, 20)` produced stamp `… 0 0 10 20` and the older navigation was accepted: `XView = (40, 90)`.
  - No in-repo host calls `RefreshAsync(RenderTarget.Loading)`; it is public API.
- **Corrected claim:** none for Loading-then-Render. For context replacement: the spec's "host viewport replacement" does not name a replaced `Context`. The pre-takeover rule (advance on every non-gesture render) did supersede it, so the current behaviour is a change introduced with the epoch rule, not a retained design.
- **Material consequence:** a host window delivered by either route can be overwritten by an older in-flight gesture whose absolute bounds were computed against the previous frame. After a context with a different x domain this can leave the window outside the data. Host precedence (ACCEPTANCE-06, ACCEPTANCE-07) is unproven for these routes.
- **Gate:** GATE-07 (decision, engineer): does a replaced `Context` whose x window differs from the last committed one supersede in-flight touch navigation? Recommendation: yes, by applying the existing "x window changed" comparison before the snapshot is overwritten; a replaced context with the same window stays telemetry-like and keeps the gesture. Default if unruled: no behaviour change for context replacement, recorded as `defer: needs-decision`; the Loading-then-Render repair proceeds regardless.
- **Action:** remediate → STEP-11 (Loading-then-Render, plus FINDING-10); context replacement `defer: needs-decision` → GATE-07, joining STEP-11 if ruled yes.
- **Verification:** fail-first component case for Loading-then-Render (older epoch rejected, `XView` stays the host window). If GATE-07 is ruled yes: fail-first component case for a replaced context with a different window, plus a case that a replaced context with the same window keeps a pending navigation valid. Rerun `ADataRefreshKeepsAPendingTouchNavigationValid` and `TelemetryDuringPendingNavigationKeepsTheIntendedWindowAndPin`.

### FINDING-06: `unavailable` hides the last acknowledged label, markers and readout

- **Disposition:** confirmed.
- **Challenge evidence:**
  - Reading: `src/GGNet/Themes/Interaction.css:4-7` hides `.crosshair-label` and `.crosshair-marker` for any value of `data-ggnet-interaction`; `src/GGNet.Demo/wwwroot/css/interaction.css:5-17` hides every readout child and substitutes text; the README recipe (`README.md:189-197`) does the same.
  - Browser probe: pin, hold frames, second tap, wait for `unavailable`, release, wait 1 s. Computed style: cursor line `visible`; label `hidden` (text `19:12`); marker `hidden`; `[data-qa=cursor-time]` `hidden` (text `Wed 19:12:00`); readout `::after` "Readings unavailable — reload to reconnect".
- **Corrected claim:** the cursor line stays visible at the authoritative position. The label, markers and host readout, which the server has rendered and which are authoritative, are hidden for the rest of the mount.
- **Material consequence:** the spec's "indicate unavailable while showing the last acknowledged state" is not met for the timestamp, markers and readout. The README recipe was written this way on purpose, but no plan decision ratifies a departure from the spec, so this is not a deliberate design.
- **Gate:** none; the repair aligns code and documentation with the bound spec. If the engineer prefers the current hiding, that is a spec change and needs its own ruling.
- **Action:** remediate → STEP-12.
- **Verification:** fail-first: extend `WatchdogQuarantinesLateInspectionRepliesUntilRemount` and `DisconnectDuringPendingNavigationShowsTheLastAcknowledgedFrame` to assert that label, markers and readout values are visible in `unavailable` while the unavailable indication is shown; `InspectionHidesStaleValuesUntilItsFrameArrives` must still pass (pending still hides). Rerun `ThemeContractTests` and the gallery goldens, since static output must stay byte-identical.

### FINDING-07: failed `setPointerCapture` cancels, callers continue

- **Disposition:** confirmed.
- **Challenge evidence:**
  - Reading: `Panel.razor.js:1536-1541`; callers at `1503-1505`, `1521-1523`, `1567-1570`.
  - Browser probe with `Element.prototype.setPointerCapture` replaced by a throwing stub (injected). One-finger pan: 1 s after release `view-changes=0`, yet every panel kept `translate(-9.17…, 0) scale(1, 1)` and the root kept `data-ggnet-navigation` (ticks hidden) with no gesture and no interaction attribute; both persisted through the next touch. Hold: the readout showed `following` with the finger down and still `following` 1 s after release, `position-changes=1`, so an unpinned provisional selection was left behind.
- **Corrected claim:** none. Natural occurrence needs the pointer or the capture element to disappear between the event and the call, which is rare; the post-cancel statements run every time it happens.
- **Material consequence:** a stuck preview with hidden ticks and a trailing provisional selection, against "each terminal path is idempotent" and ACCEPTANCE-04's "without trailing selection".
- **Gate:** none.
- **Action:** remediate → STEP-13.
- **Verification:** fail-first browser case with the injected stub, labelled as injected: after a pan attempt no transform and no `data-ggnet-navigation` remain; after a hold attempt the selection equals the pre-gesture pin; a following normal gesture works once the stub is removed.

### FINDING-08: evidence narrower than the ACCEPTANCE-04 and ACCEPTANCE-07 wording

- **Disposition:** confirmed.
- **Challenge evidence:**
  - A search of `tests/GGNet.E2ETests` for `pointercancel`, `lostpointercapture`, `blur`, `resize` and `touchCancel` finds touch interruptions only in `InterruptedScrubRestoresThePinAndAcceptsTheNextGesture` (`TimeCursorLifecycleTests.cs:7-71`), which interrupts a scrub. Pending hold has only the leave case (`:74`); pan has only the second-contact carry (`:101`); two-finger navigation has only the third-contact case (`TimeCursorTouchTests.cs:29`).
  - `Panel.razor.js:1577-1597`: on the injected `pointercancel`, `up` deletes the contact and clears `draining` while the real finger is down, so that row does not exercise the drain.
  - `GestureModuleTests.cs:671-674`: the cancellation row is an injected mouse `pointercancel`. No case disposes a panel or plot during an active touch gesture or pending touch operation; `OnlyClearEndsAPinAndAReturningPageStartsCleanWithOneSetOfHandlers` navigates away only when idle.
  - `TimeCursorFrameTests.cs:82-110`: the watchdog case never remounts.
  - `TimeCursorFrameTests.cs:51-79`: `HostClearWinsOverAHeldTouchReply` holds server frames only, so the tap is applied before Clear.
  - The probes above show that the existing cases do not detect FINDING-01 to FINDING-04 or FINDING-07.
- **Corrected claim:** none.
- **Material consequence:** ACCEPTANCE-04's required "browser transition matrix: pending hold/pan/scrub … cancel/lost capture … blur, resize/rotation and remount/disposal" is covered for scrub only; "until remount" and "no stuck transforms after … disposal" in ACCEPTANCE-07 are unproven. Reading `cancel` (`1611-1617`) and `remove` (`1463-1479`) finds no defect in those untested states, so this is an evidence gap, not a known behavioural failure.
- **Gate:** none.
- **Action:** remediate → STEP-14.
- **Verification:** new browser rows: {pointercancel, lostpointercapture, blur, resize} × {pending hold, one-finger pan, two-finger navigation}, each asserting no commit, no transform, no trailing selection and a working next gesture; leaving the page during an active scrub and during a pending held operation, then returning clean with one handler set and no errors; remount after `unavailable` accepts gestures again. These pin existing behaviour, so fail-first does not apply; a row that fails is a new finding to classify. The "old touch request arrives after Clear" ordering cannot be produced in the browser with a one-direction gate; it stays component-level evidence and the acceptance row should say so.

### FINDING-09: timeout and any-mutation settlement remain for mouse and pen

- **Disposition:** overstated.
- **Challenge evidence:**
  - The code facts hold: `Panel.razor.js:15`, `771`, `803`, `1078-1094`; touch is excluded from that path at `137-139`.
  - "The plan does not disclose it" is only partly right. The manifested normative guide `.github/instructions/blazor.instructions.md:138` states "Mouse/pen retain the translated cursor and geometric settlement. Touch uses a separate provisional line … and settles only on matching rendered inspection identity." The plan's ACCEPTANCE-07 row does not repeat it, and nothing states it for mouse and pen pan previews.
  - Scope evidence: the spec's outcome is "the same touch contract"; ACCEPTANCE-08 requires that mouse and pen "retain deliberate existing semantics"; the engineer input (`inputs/touch-interaction-refactor.md:75`) says "Preserve mouse/pen behavior deliberately". Against that, the spec's "Replace timeout/any-mutation settlement" and "Do not silently drop a valid preview at 1500 ms" carry no touch qualifier.
- **Corrected claim:** the replacement of timeout settlement is touch-only. That is consistent with the spec's outcome and ACCEPTANCE-08 and is disclosed for the cursor in the scoped guide, but it has no explicit ruling and the ACCEPTANCE-07 row does not bound its claim to touch.
- **Material consequence:** a mouse or pen pan preview still drops at 1500 ms under a 2 s round trip, and a telemetry frame settles it early. Whether that is in scope decides whether ACCEPTANCE-07 can be recorded as satisfied as worded.
- **Gate:** GATE-06 (decision, engineer): is mouse and pen settlement in scope of this Task? Recommendation: no. Rule the replacement touch-only, reword the ACCEPTANCE-07 evidence to say so, and lift mouse and pen identity settlement to the deferred register. Converting that path is more than one bounded step, touches the surface ACCEPTANCE-08 protects, and was not what the engineer asked for. Default if unruled: touch-only stands, disclosed in the acceptance row and recorded as a deferred issue.
- **Action:** `defer: needs-decision` → GATE-06. No remediation step unless the engineer rules it in scope.

## 4. Bounded search for misses

Scope searched: the epoch and revision paths in `Plot.razor.cs` and `Panel.razor.cs`, the coordinator in `Panel.razor.js:1237-1618`, view and range units across the position scales, the two Claude-authored epoch component tests, and the touch policies of the Touch and TouchLayout pages against the demo README rows. The auditor disclosed reduced scrutiny of the `GestureModuleTests`, `TouchDemoTests` and Touch page migrations; this search sampled their policies and cancellation rows and did not review every converted case.

Checked and found sound: view and range units agree for every scale that commits a view (`Scales/Position.cs:39-51`), so absolute bounds round-trip exactly; pinch scale is clamped client-side (`Panel.razor.js:687`); two coalesced touch navigations settle on the later revision; a Clear with touch inspection enabled reaches the client through the inspection stamp, and `effectiveView` (`1327-1338`) merges it.

### FINDING-10: a Claude-authored epoch test does not pin what its name claims

- **Claim:** `ResettingAnUnchangedWindowStillRejectsAnOlderTouchEpoch` does not exercise an unchanged window, so it cannot detect removal of the explicit epoch advance it was written to pin.
- **Evidence:** `tests/GGNet.Components.Tests/TwoFingerNavigationTests.cs:23-36`: `Mount` always calls `context.SetXView(0, 100)`. The test (`:176-195`) then calls `ResetViewAsync`, which clears the view, so the window changes. `Plot.razor.cs:572-578` advances the epoch explicitly, and `CommitViewAsync` (`:591`) advances it again because the x window differs from the snapshot. Removing the explicit `touchEpoch++` would leave the test green. Established by reading; not mutation-tested, because source edits are outside challenge authority.
- **Affected surface:** evidence for host precedence on the Claude-authored epoch rule (ACCEPTANCE-07).
- **Consequence:** the case "reset of a plot whose window is already the default" is unpinned. Behaviour is correct today.
- **Confidence:** high on the reading.
- **Disposition:** confirmed.
- **Action:** remediate → STEP-11.
- **Verification:** mount without a view window, capture the epoch, call `ResetViewAsync`, and assert that a navigation with the older epoch is rejected. Show it failing with the explicit advance temporarily removed, then passing; record both.

No other material miss was found within the searched scope.

## 5. Proposed remediation steps

All steps record their origin, use the top-model configuration, and stay inside the plan's existing write surfaces.

| ID | Origin | Checkable result | Affected surfaces | Verification |
|---|---|---|---|---|
| STEP-07 | from FINDING-01 | The 10 s bound measures an operation that makes no acknowledged progress, and restarts on each acknowledgement; sustained interaction under latency never quarantines while acknowledgements flow. | `Panel.razor.js` coordinator, `CircuitGate` (fixed per-frame delay), `TimeCursorFrameTests` | New 12 s scrub case at 100 ms and 300 ms fixed delay, fail-first then pass; watchdog and both disconnect cases still pass. |
| STEP-08 | from FINDING-02 | The host epoch never advances without a rendered stamp that carries it: Clear and `Inspection` parameter changes leave touch navigation valid when touch inspection is off. | `Plot.razor.cs`, `Panel.razor.cs` if the stamp changes, `TwoFingerNavigationTests` or `TouchRevisionTests`, `OverviewDetailTests` | Three fail-first component cases and two fail-first browser cases as listed under FINDING-02; Clear-wins cases still pass. |
| STEP-09 | from FINDING-03 | A touch inspection at an unmappable position is acknowledged as a no-op for its revision. | `Panel.razor.cs` (`OnTouchInspectAsync`), `TouchRevisionTests`, `TimeCursorTouchTests` | Fail-first edge-tap browser case on the snap chart and fail-first component case. |
| STEP-10 | from FINDING-04 | A wheel, mouse or pen view commit supersedes a pending touch navigation instead of leaving it unmatchable. | `Panel.razor.cs`, `Plot.razor.cs` (`CommitViewAsync`), component and frame tests, blazor guide sentence on epoch advances | Fail-first browser case (held render, pan, wheel) and component case; mouse and pen pan cases rerun. |
| STEP-11 | from FINDING-05, FINDING-10 | The x-window snapshot is taken only when a Render-target commit is evaluated; the reset test pins an unchanged window. Context replacement joins this step only if GATE-07 is ruled yes. | `Plot.razor.cs`, `TwoFingerNavigationTests`, README and blazor guide sentence if GATE-07 changes behaviour | Fail-first Loading-then-Render component case; corrected reset case with its mutation evidence; telemetry cases rerun. |
| STEP-12 | from FINDING-06 | In `unavailable` the last acknowledged label, markers and host readout stay visible beside the unavailable indication. | `Themes/Interaction.css`, demo `interaction.css`, README recipe, `TimeCursorFrameTests`, `ThemeContractTests` if it pins the selectors | Fail-first visibility assertions in the watchdog and disconnect cases; pending-hides case still passes; goldens unchanged. |
| STEP-13 | from FINDING-07 | A failed pointer capture ends the gesture with no further preview, state write or inspection. | `Panel.razor.js` (`capture` and its three callers), `TimeCursorLifecycleTests` | Fail-first browser case with the injected throwing stub. |
| STEP-14 | from FINDING-08 | The ACCEPTANCE-04 transition matrix and the ACCEPTANCE-07 remount and disposal claims have executed browser rows, or the acceptance rows state the remaining component-only evidence. | `TimeCursorLifecycleTests`, `TimeCursorFrameTests`, plan acceptance rows | New rows as listed under FINDING-08, executed with zero skipped; failures classified as new findings. |

After the steps: rerun the four repository gates and the full enabled E2E run, and record executed and skipped counts against the new state.

## 6. Gates and acceptance impact

| Gate | Type | Question | Recommendation | Default if unruled |
|---|---|---|---|---|
| GATE-06 | decision, engineer | Is the mouse and pen 1500 ms and any-mutation settlement in scope, given ACCEPTANCE-08? (FINDING-09) | No: rule the replacement touch-only, reword ACCEPTANCE-07's evidence, defer the rest. | Touch-only stands, disclosed and deferred. |
| GATE-07 | decision, engineer | Does a replaced `Context` with a different x window count as host viewport replacement? (FINDING-05) | Yes, through the existing window-changed comparison; same-window replacement keeps the gesture. | No change for context replacement, deferred; STEP-11 still repairs Loading-then-Render. |

- **Acceptance status.** The confirmed findings contradict the plan's "satisfied" marks. Until STEP-07 to STEP-14 are verified, ACCEPTANCE-04, ACCEPTANCE-06 and ACCEPTANCE-07 are not established, and ACCEPTANCE-02 (snapped axes), ACCEPTANCE-03 and ACCEPTANCE-09 (Navigation-only mode) are not established for the cases in FINDING-02 and FINDING-03. No finding is proposed as an accepted gap; none can be created without the engineer.
- **Re-audit.** The policy requires one when remediation invalidates the assurance the audit provided. STEP-07, STEP-08 and STEP-10 change the watchdog and the epoch rule, which are the core of what audit-01 examined. Recommendation: targeted verification by the fail-first cases above is enough if the repairs stay local to those rules; a second audit cycle is warranted if remediation adds a new protocol element, such as a negative acknowledgement returned from the touch invocations. That choice and any launch belong to the engineer.
- **Advisory, not required.** FINDING-02 to FINDING-04 share one weakness: the client cannot tell a silently dropped command from a slow one. The steps above repair each known route. A returned accepted/rejected result with the current epoch would close the class, at the cost of a wider interop change and the re-audit consideration above.

## 7. Temporary files

Created and deleted during this challenge; `git status` shows neither, and the solution was rebuilt clean afterwards so the test binaries match the pinned state again.

- `tests/GGNet.Components.Tests/ZzChallengeProbeTests.cs` (six bUnit probes).
- `tests/GGNet.E2ETests/ZzChallengeProbeTests.cs` (browser probes, rewritten twice between runs).

Probe logs are session-only files in the challenge scratchpad and are not retained evidence; the material results are quoted above.
