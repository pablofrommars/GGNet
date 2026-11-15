<!-- launched by audit-launch.cs: dispatch audit-02-dispatch.md sha256=39a1a3fe571d17e8b766d8e636c830c54575d0d191e5324954d2f28af17b6052 auditor=claude model=fable resolved-model=claude-fable-5-2 sandbox=read-tools-only -->
# Audit 02 report: touch interaction refactor (re-audit after remediation)

## 1. Scope and target-state identity

- Audit `audit-02` of `plan/touch-interaction-refactor/task-plan.md`.
- Target state: repository HEAD `7fd4b8e06098fb8cc730545c635a1770506bcc71` plus the uncommitted working tree, as pinned by the SHA-256 manifest in section 10 of `plan/touch-interaction-refactor/audits/audit-02-dispatch.md` (by reference). I could not recompute hashes; the dispatch states the launcher verified them.
- Focus: gesture lifecycle and delayed-frame correctness for touch and for mouse/pen pan previews, with equal scrutiny of the Fable-authored remediation.
- Nothing under `plan/touch-interaction-refactor/audits/` was opened except the dispatch itself. I wrote, edited or created no file.

## 2. Sources used

Normative:
- `specs/touch-interaction-refactor.md`, read in full.
- `plan/touch-interaction-refactor/task-plan.md`, lines 1–140 and 141–176. The Decisions were treated as normative; the Material notes and Acceptance evidence were treated as claims.
- `.github/instructions/blazor.instructions.md`, lines 84–158 only.

Audited files read in full:
- `src/GGNet/Components/Panel.razor.js`, `Panel.razor`, `Panel.razor.cs`, `Plot.razor.cs`, `PanelInterop.cs`, `CrosshairCursor.cs`, `CursorFrame.cs`, `Area.razor`
- `src/GGNet/InteractivityOptions.cs`, `TouchInteractionOptions.cs`, `Themes/Interaction.css`
- `src/GGNet.Demo/Components/Pages/OverviewDetail.razor`, `OverviewDetail.razor.cs`, `wwwroot/css/interaction.css`
- `tests/GGNet.E2ETests/TimeCursorFrameTests.cs`, `TimeCursorLifecycleTests.cs`, `Fixtures/CircuitGate.cs`
- `tests/GGNet.Components.Tests/TouchRevisionTests.cs`, `TwoFingerNavigationTests.cs`

Audited files read in part: `README.md` lines 120–214.

Unpinned context, read to understand behaviour and not under audit:
- `src/GGNet/Components/Plot.razor`
- `src/GGNet/Rendering/InteractiveRenderModeHandler.cs`, `InteractiveAutoRenderModeHandler.cs`, `RenderModeHandler.cs`
- `src/GGNet/Scales/Position.cs`, `InstantPosition.cs`, `PositionRange.cs` (grep)
- `src/GGNet/InspectionReadings.cs` (grep)
- `src/GGNet.Demo/Components/Pages/TimeCursor.razor.cs` (grep)

## 3. Coverage, limitations and methods

**Method.** Static reading only (Read, Glob, Grep). I traced each protocol element end to end:
- the client queue, the per-kind watchdog and `reconcileNavigation`;
- `AcceptTouch`, the six sites that advance `touchEpoch`, `Compose` stamping, and `AcknowledgeInspection`;
- the mouse/pen `#expectCommit`, `#settleWith` and `#reconcilePan` path.

For each epoch advance I asked whether a rendered stamp carrying it must follow. For each pending state I asked what can clear it. Nothing was executed, so every behavioural statement below is a derivation from code, not an observation.

**Inspected in depth.** Contact arbitration and transitions, cancellation and disposal paths, session/epoch/revision identity, frame-matched settlement for touch and mouse/pen, watchdog arming and restart, host-command precedence, telemetry during pending operations, and the frame, lifecycle and component revision tests.

**Not inspected, or only grepped.**
- Tests: `TimeCursorTests.cs`, `TimeCursorTouchTests.cs`, `TimeCursorNavigationTests.cs`, `GestureModuleTests.cs`, `TouchDemoTests.cs`, `PanInteropTests.cs`, `PanelLifecycleTests.cs`, `TouchPolicyTests.cs`, `ThemeContractTests.cs`, `ProcessOutputTailTests.cs`, and the bodies of `OverviewDetailTests.cs` (method names only).
- Fixtures: `DemoAppFixture.cs`, `ProcessOutputTail.cs`.
- Demo: `Touch.razor(.cs)`, `TouchLayout.razor(.cs)`, `TimeCursor.razor` (grep only), `App.razor`, `inter.css`, the font `LICENSE.txt`, the demo README.
- Other: `TouchNavigation.cs`, `TouchOwnership.cs`, `PublicApiTests.GGNet.verified.txt`, `AGENTS.md`, `CLAUDE.md`, the csharp, dsl, rendering and testing guides, and the neutral input.

This audit therefore says nothing about ACCEPTANCE-01, ACCEPTANCE-05, the Touch/TouchLayout migration, the font vendoring (STEP-16) or the fixture diagnostics (STEP-20).

**Limitations.**
- No HEAD versions, so nothing is judged as a diff.
- No test logs: counts, fail-first and mutation evidence are plan claims that I checked only for plausibility.
- No browser, so event ordering in Chromium (capture timing, the order of Blazor disposal and initialisation calls) is reasoned, not observed.
- `Compose` runs on a background loop while handlers run on the dispatcher. I looked for permanent failures from that interleaving and found none, because the bounds comparison in the navigation match protects it. Transient glitches there were not ruled out.
- I share a model with the remediation author.

## 4. Overall assessment

Within the inspected surface, the touch protocol core is coherent and the audit-01 routes the plan describes appear closed as claimed:
- The view stamp carries session, epoch, revision and exact bounds; `Range` equals the committed window exactly (`Scales/Position.cs:47-49`), so equality matching is sound.
- Coalesced or reordered renders cannot settle a navigation early.
- The per-kind watchdog restarts only on revision progress of its own kind.
- An unmappable inspection is acknowledged.
- Capture failure exits cleanly.
- The interruption matrix rows exist and assert what the plan says.

The result is not clean. The mouse/pen settlement added in STEP-17 has a deterministic early-settle route on the shipped `/time-cursor` configuration (FINDING-01), and it stretches a preview that gives no stale-value feedback from 1.5 s to 10 s (FINDING-03). The invariant STEP-08 states, that the epoch never advances without a rendered stamp, does not hold on two routes that end in permanent `unavailable` (FINDING-02, FINDING-06). Part of the ACCEPTANCE-03 component evidence exercises a server method the browser no longer calls (FINDING-04). The remaining findings are narrower.

## 5. Findings

### FINDING-01

**Claim.** A mouse or pen pan preview is settled by a data-only frame whenever the server's epoch is ahead of the rendered view stamp. That happens after every host Clear, Escape, or host-set `Inspection` on a plot with touch inspection. It contradicts the STEP-17 claim and the guide sentence "A data-only frame therefore does not settle it".

**Evidence.**
- `Plot.razor.cs:227-234` (`ClearInspectionAsync`) and `:215-218` (`OnParametersSetAsync`) increment `touchEpoch` under `StampsInspection` and then call `InspectAsync`, which only refreshes cursor components. `RenderedTouchEpoch` is written only in `Compose` (`:493-497`), so the marks stamp keeps the old epoch R while the server holds R+1.
- `Panel.razor.js:1147-1163`: `#expectCommit` reads the rendered marks stamp and expects `max(rendered, commits) + 1`, which is R+1.
- A data-only `RefreshAsync` does not advance the epoch (`Plot.razor.cs:597-610`), but its `Compose` stamps the current `touchEpoch`, R+1.
- `Panel.razor.js:1109-1113`: `#reconcilePan` unsettles on `view.epoch >= settling.epoch`, so that telemetry frame drops the preview before the pan's own frame (R+2) arrives.
- Reproduction by reading, on `/time-cursor` (`TimeCursor.razor.cs:27`, `Crosshair = true, Pan = true`, touch inspection inferred): pin, Clear, then the exact sequence of `ATelemetryFrameDoesNotSettleAPendingMousePan`.
- That test (`TimeCursorFrameTests.cs:257-296`) has no preceding Clear. A grep found no browser case combining Clear or Escape with a mouse pan.

**Affected surface.** `Panel.razor.js` mouse/pen settlement; `Plot.razor.cs` epoch sites; blazor guide line 138; README lines 168–169; ACCEPTANCE-07 evidence for STEP-17.

**Consequence.** After a Clear or selection change, the next mouse or pen pan under latency with telemetry snaps back to the pre-pan frame and then jumps when its commit renders. This is the stale flash STEP-17 was meant to remove. It also occurs on a second plot that shares a host `Inspection`, since each selection change bumps that plot's epoch without a `Compose`.

**Confidence.** Medium-high. Each step is a direct code reading and the trigger is deterministic, but it was not executed.

**Recommendation (advisory).** Derive the expected epoch from the same lifted value the touch path uses (`effectiveView` takes the inspection stamp's epoch when greater), or re-stamp the view on those epoch advances.

### FINDING-02

**Claim.** A host viewport command given an already-cancelled token advances the epoch without any rendered frame. On an interactive plot with static data this leads to permanent `unavailable` on the next touch operation.

**Evidence.**
- `Plot.razor.cs:550-562`: `ZoomToXAsync` sets the view and runs `touchEpoch++` before `RefreshAsync`. `ZoomToYAsync` (`:568-574`) and `ResetViewAsync` (`:578-584`) do the same.
- `CommitViewAsync` (`:603-610`) increments again and updates `touchXView` before awaiting the handler.
- `InteractiveRenderModeHandler.cs:40-43` returns `Task.FromCanceled(token)` without queuing a render.
- The DOM stamps stay at epoch E while the server holds E+2. The inspection stamp becomes null because `publishedTouchEpoch != touchEpoch` (`Plot.razor.cs:94-95`).
- The next touch command carries E, and `AcceptTouch` (`:102`) returns false. `OnTouchNavigateAsync` and `OnTouchInspectAsync` then return with no frame and no error (`Panel.razor.cs:609-612`, `:633-636`).
- The client holds `pending` until the watchdog calls `failNavigation` (`Panel.razor.js:1470-1472`, `:1551-1563`). Nothing but a remount clears `unavailable`.
- A grep found no test that passes a cancelled token.

**Affected surface.** `Plot.razor.cs` host commands; the STEP-08 invariant; ACCEPTANCE-07 ("no route to unavailable on a healthy chart").

**Consequence.** A host that cancels a superseded zoom request leaves a healthy chart one touch away from "Chart unavailable — reload to reconnect". The route heals only if another `Compose` (telemetry) or a mouse cursor call lands within ten seconds.

**Confidence.** Medium. The code path is unambiguous; how often hosts pass cancelled tokens is unknown. `InteractiveAuto` is unaffected because it ignores the token.

### FINDING-03

**Claim.** The mouse/pen pan preview now stands for up to ten seconds with none of the stale-value feedback that the spec requires and that DECISION-10 extends to mouse and pen previews.

**Evidence.**
- `Panel.razor.js:1133-1136`: `#paintPan` only sets a transform on the marks group.
- The mouse path never sets `data-ggnet-interaction` or `data-ggnet-navigation`; those are written only in `TouchCoordinator.feedback` (`:1541-1549`).
- `Themes/Interaction.css:4-7` therefore hides neither ticks, grid nor crosshair label and markers during a mouse pan.
- The crosshair group is a sibling of `[data-ggnet-pan]` (`Panel.razor:154-170`), so a pinned line, its time label and its markers stay at the old screen position while the marks move underneath.
- The hold was bounded at 1500 ms before STEP-17 (plan note) and is now `RECOVERY_TIMEOUT_MS = 10000` (`Panel.razor.js:1096-1099`).
- `AMouseOrPenPanPreviewPersistsUntilItsFrame` (`TimeCursorFrameTests.cs:183-218`) asserts only that the transforms persist.

**Affected surface.** ACCEPTANCE-07 ("never present stale values as current at 300 ms/2 s delay") as applied to mouse/pen by DECISION-10; the spec section "Truthful feedback and acknowledgement".

**Consequence.** At 2 s latency a mouse pan shows old tick labels against shifted marks, and a pinned crosshair label and markers over data they do not belong to, with no pending indication and no host hook to hide a readout.

**Confidence.** High on the code facts. Medium on the normative reading: STEP-17's checkable result names only settlement, so the engineer may regard feedback as out of that step's scope.

### FINDING-04

**Claim.** `Panel.OnNavigateEndAsync` is dead in the browser, yet it carries the ACCEPTANCE-03 "window math/AutoFitY component tests" evidence, and the guide still lists it as the live invokable.

**Evidence.**
- `Panel.razor.js:1-7` defines no constant for it and the module never invokes it. Touch navigation commits only through `ON_TOUCH_NAVIGATE` with absolute bounds computed client-side in `previewNavigation` (`:1358-1369`).
- `Panel.razor.cs:575-600` keeps the method `[JSInvokable]`, with a `DynamicDependency` at `:95`.
- `TwoFingerNavigationTests.cs` drives it in:
  - `TheValueUnderTheMidpointStaysUnderItWhileTheWindowScales` (`:60`)
  - `ExplicitTouchNavigationWorksWithoutCrosshairOrMousePan` (`:83`)
  - `ThePinKeepsItsValueAndFollowsTheWindow` (`:383`)
  - `WithoutInferredXNavigationNothingIsCommitted` (`:406`)
  - `AMeaninglessScaleIsIgnored` (`:427`)
  - `AutoFitYRefitsInTheSameChange` (`:444`)
- The file header (`:5-7`) says bUnit "pins the arithmetic of its single commit".
- Blazor guide line 101 lists `OnNavigateEndAsync` and omits `OnTouchNavigateAsync` and `OnTouchInspectAsync`.
- In the files I read, no component test covers AutoFitY through `OnTouchNavigateAsync`.

**Affected surface.** ACCEPTANCE-03 evidence; `Panel.razor.cs`; `TwoFingerNavigationTests.cs`; the blazor guide.

**Consequence.** The midpoint and pinch arithmetic the browser actually uses is JS and is covered only by browser cases. Six component cases would stay green if the live arithmetic or the live AutoFitY path broke. The dead method remains a callable entry point that skips session, epoch and revision checks.

**Confidence.** High that it is unreferenced from the module and that those tests call it. Medium that no other unread test covers the live path's AutoFitY.

### FINDING-05

**Claim.** `unavailable` is terminal for any rejected touch call, including a server-side exception from a host callback on an otherwise healthy circuit. That branch has no executed evidence, and no resynchronisation path exists.

**Evidence.**
- `Panel.razor.js:1460`: `call.catch(() => this.failNavigation())`.
- `OnTouchNavigateAsync` awaits `CommitViewAsync`, which awaits the host's `ViewChanged` (`Plot.razor.cs:617-620`). `OnTouchInspectAsync` awaits `InspectionChanged` and `ReadingsChanged` (`:274-279`, `:424`). An exception in any host handler faults the invokable and rejects the JS promise, although the commit was applied and its frame will arrive.
- The mouse path only drops its preview on the same rejection (`Panel.razor.js:219-226`).
- The plan records that the disconnect cases reach `unavailable` only through the watchdog ("observed 9.8 s"), so the explicit-rejection branch is not exercised by any case I read.
- Only `remove()` of the last panel resets the coordinator (`:1565-1582`). A circuit that drops and reconnects successfully leaves the chart quarantined.

**Affected surface.** `TouchCoordinator.pump` and `failNavigation`; ACCEPTANCE-07 disconnect and recovery evidence.

**Consequence.** One throwing host handler, or a gesture during a brief reconnect, leaves touch disabled under "reload to reconnect" while mouse and wheel still work and the server state is correct.

**Confidence.** Medium. The spec permits "resynchronization or remount" and names explicit interop failure, so this is arguably conformant. It is reported because the dispatch asks for every route from a healthy chart to permanent `unavailable`.

### FINDING-06

**Claim.** A touch navigation whose render throws is accepted but never acknowledged, which ends in `unavailable` with the server context left unrenderable. Repeated maximum pinch-out reaches this on an `Instant` axis.

**Evidence.**
- `Panel.razor.js:692` clamps one gesture to scale 0.1, and each later gesture composes against the pending window, so the span grows tenfold per gesture with no bound.
- `Panel.razor.cs:608-614` accepts any finite `min < max`.
- `InstantPosition.cs:37-40` calls `Instant.FromUnixTimeMilliseconds((long)Range.min)` on the view. I expect that to throw outside NodaTime's range; the library was not consulted.
- `InteractiveRenderModeHandler.cs:99` and `:110-116` catch and log the render failure and emit no frame.
- Roughly eight full pinch-outs from a multi-hour window exceed the range.

**Affected surface.** `OnTouchNavigateAsync`; the watchdog path. The wheel reaches the same server failure, more slowly, and that part is not new to this Task.

**Consequence.** The chart goes `unavailable`, and every later render of that context fails until the host resets the view.

**Confidence.** Low-medium. The arithmetic and the catch are read directly; the throw is assumed from NodaTime's documented range and was not executed.

### FINDING-07

**Claim.** "A replaced `Context` with the same x window keeps a pending gesture" (DECISION-08, README lines 164–165, blazor guide line 141) holds only for server acceptance. In a browser the pending gesture is always lost.

**Evidence.**
- `Plot.razor:322` keys panels on `(Context, i)`, so a new context disposes every `Panel` and its interop.
- `Panel.razor.js:1565-1582`: removing the last panel clears `commands`, `pendingView` and `pendingInspection` and deletes the coordinator.
- A command accepted before the swap set the window on the old context (`Panel.razor.cs:614`). One arriving after it meets a disposed panel (`:605`).
- The "kept" row of `AReplacedContextSupersedesTouchNavigationOnlyWhenItsWindowDiffers` (`TwoFingerNavigationTests.cs:225-246`) calls `OnTouchNavigateAsync` on the new panel instance with the old epoch, which no browser client can do.
- The only browser replacement case found by grep, `ResizingAndReplacingTheContextKeepThePin`, covers the pin, not a pending navigation.

**Affected surface.** README and blazor guide wording; STEP-11 evidence; ACCEPTANCE-06.

**Consequence.** The documentation promises retention that the client cannot deliver. The loss itself is clean: no stuck transform and no `unavailable`.

**Confidence.** Medium-high on the keyed teardown. Medium on the ordering of Blazor disposal against new-panel initialisation, which was reasoned and not observed.

### FINDING-08

**Claim.** The plan's ACCEPTANCE-10 row is stale against its own steps.

**Evidence.** Plan line 47 reads "pending | STEP-05/STEP-06 scheduled; audit launch not yet authorized", while STEP-05 and STEP-06 are marked completed, GATE-04 is settled with audit-01 captured, and STEP-18 is in progress.

**Affected surface.** `task-plan.md` Acceptance table.

**Consequence.** A reader of the Acceptance table alone gets the wrong state of the audit obligation. No behavioural effect.

**Confidence.** High.