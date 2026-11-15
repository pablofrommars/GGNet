# Audit 02 challenge: touch interaction refactor

## 1. Identity, target state and independence

- Challenges `audits/audit-02.md` (FINDING-01 to FINDING-08) of `plan/touch-interaction-refactor/task-plan.md`, under the dispatch `audits/audit-02-dispatch.md`. It is the only report on file for this cycle. `audit-01.md` was challenged and remediated in cycle 01 and was read for context only.
- Challenger: the pinned `protodog-challenge` agent (Claude Fable 5.2, `claude-fable-5-2`), a fresh session. It authored neither the audited state, nor `audit-02.md`, nor any remediation under audit. It shares its model with the auditor and with the STEP-07 to STEP-14 and STEP-17 author; independence is by session only.
- Target state: HEAD `7fd4b8e06098fb8cc730545c635a1770506bcc71` plus the uncommitted tree of dispatch section 10. Rechecked with `shasum -a 256 -c` over all 57 manifest rows, before the probes and again after they were deleted: 56 match. Only `plan/touch-interaction-refactor/task-plan.md` differs, as expected (it records the capture, corrects the ACCEPTANCE-10 row and marks STEP-19 in progress).
- The challenge wrote this file only. No source, test, doc, plan, dispatch or report was edited, and no git index or history operation was run.

## 2. Method and executed evidence

Each finding was re-derived by reading the pinned code and then tested by execution where execution could discriminate. Three temporary probe files were created, run and deleted (section 8). They observed and logged state. Only the fixture probe asserted, because its evidence is the output attached to a failing case.

| Run | Command | Result |
|---|---|---|
| Build with probes | `dotnet build GGNet.slnx -warnaserror --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false` | 0 warnings, 0 errors |
| Component probes (bUnit) | `dotnet tests/GGNet.Components.Tests/bin/Debug/net11.0/GGNet.Components.Tests.dll --filter-class '*ChallengeProbe'` | 8 executed, 0 skipped |
| Browser probes A | `GGNET_E2E=1 dotnet tests/GGNet.E2ETests/bin/Debug/net11.0/GGNet.E2ETests.dll --filter-method '*Probe*'`, outside the sandbox | 6 executed, 0 skipped, 1m31s |
| Browser probes B (pen, dialog tooltip) | same binary, `--filter-class '*ChallengeProbe2'` | 4 executed, 0 skipped, 16s |
| Browser probe C (fixture diagnostics) | same binary, `--filter-method '*ProbeF11*'` | 2 executed, both failed by design, 0 skipped |
| Build after deleting probes | same build command | 0 warnings, 0 errors |

No circuit error or WebSocket 1006 drop occurred in any browser run except the two drops probe C injected through `CircuitGate.DisconnectAsync`.

Limits:

- Chromium on macOS arm64 with CDP touch and pen input, against the loopback demo. No physical device, no Safari.
- Probes used the shipped demo pages unchanged, so two things could not be staged in a browser: a host callback that throws (FINDING-05) and a context replacement that carries the same x window (FINDING-07). Both rest on a component probe plus reading, and are labelled so.
- No source mutation was made. Where a mutation would have been the natural check (FINDING-10), the probe neutralised the browser API in the page instead.
- Not read by this challenge: the bodies of `OverviewDetailTests.cs`, `TouchPolicyTests.cs`, `PanelLifecycleTests.cs`, `PanInteropTests.cs`, `ThemeContractTests.cs` and `ProcessOutputTailTests.cs`, and the demo README beyond a grep. Nothing here is a claim about them.

## 3. Dispositions

| Finding | Disposition | Action |
|---|---|---|
| FINDING-01 | confirmed (reproduced in the browser, with a passing control) | remediate → STEP-21 |
| FINDING-02 | confirmed (reproduced in a component probe); trigger narrower than it reads | remediate → STEP-22 |
| FINDING-03 | confirmed on the facts (reproduced); whether it is a defect needs a ruling | `defer: needs-decision` → GATE-09 |
| FINDING-04 | confirmed | remediate → STEP-23 |
| FINDING-05 | deliberate design for the terminal state; confirmed for the missing executed evidence | remediate → STEP-24 (evidence and one doc sentence); optional ruling → GATE-10 |
| FINDING-06 | confirmed (reproduced), and understated in confidence; the cause is not touch-specific | `defer: out-of-scope` (`src/GGNet/Scales/InstantPosition.cs`); surface-grant request → GATE-11 |
| FINDING-07 | confirmed | `defer: needs-decision` → GATE-12 |
| FINDING-08 | confirmed for the pinned state; already corrected | remediate → done under STEP-19, no new step |
| FINDING-09 (new) | confirmed (reproduced in the browser) | remediate → STEP-25 |
| FINDING-10 (new) | confirmed (reproduced in the browser); low consequence | remediate → STEP-24 |
| FINDING-11 (new) | confirmed (reproduced in the browser) | remediate → STEP-26 |

## 4. Findings

### FINDING-01: confirmed

**Evidence.** Browser probe on `/time-cursor`, following `ATelemetryFrameDoesNotSettleAPendingMousePan` step for step, with a mouse click-pin first and, in one row, a host Clear before the frames are held.

| Row | Marks stamp at start (epoch, revision) | Previews after the telemetry frame | `view-changes` then | Stamp of that frame |
|---|---|---|---|---|
| no Clear (control) | `0 0` | all four `translate(-61.15, 0)` | 0 | `0 0` |
| Clear first | `0 0` | all four empty | 0 | `1 0` |

With the Clear, the data-only frame dropped all four previews while the pan's own commit was still unrendered. The code path is the one the report gives: `Plot.razor.cs:227-234` advances `touchEpoch` without a `Compose`, `Panel.razor.js:1153-1163` expects rendered epoch + 1, and `Panel.razor.js:1109-1113` settles on `view.epoch >= settling.epoch`.

**Corrected claim.** None on the mechanism. The reach is as stated: any epoch advance that no view stamp carries. That is host Clear, Escape and a host-set `Inspection` on a plot with touch inspection, and the cancelled-token route of FINDING-02.

**Consequence.** After a Clear or a selection change, the next mouse or pen pan under latency with telemetry snaps back and then jumps. The Blazor guide (line 138) and the README (lines 168-169) say a data-only frame does not settle the preview. The STEP-17 part of the ACCEPTANCE-07 evidence is therefore not established on plots with touch inspection, which includes the default `Crosshair` + `Pan` configuration.

**Gate.** None. The repair restores STEP-17's own checkable result.

**Action.** remediate → STEP-21. One caution for the step: the auditor's suggestion (take the inspection stamp's epoch when greater, as `effectiveView` does) leaves a short window in which `Plot.TouchInspectionStamp` is null (`Plot.razor.cs:94-95`, while readings publish), and the expectation would fall back to the stale view epoch. The step's check should hold in that window too. An expectation that is too high is the opposite failure: the preview would stand until the 10 s bound.

**Verification.** Fail-first browser case: pin, Clear, hold frames, append, mouse drag, release only the telemetry frames, then assert that all previews are still present and `view-changes` is 0; then release and assert that they clear. Add rows for Escape and for a host-set `Inspection`. Rerun `ATelemetryFrameDoesNotSettleAPendingMousePan`, `AMouseOrPenPanPreviewPersistsUntilItsFrame`, `AHostViewportCommandSupersedesAPendingMousePan` and `TheSecondOfTwoQuickPansWaitsForItsOwnFrame`.

### FINDING-02: confirmed, trigger narrower than it reads

**Evidence.** Component probe, `RenderMode.Interactive`, `Pan = true`, x view `(0, 100)`:

- `ZoomToXAsync(5, 15, cancelledToken)` threw `TaskCanceledException`. After 500 ms the view stamp was still `0 0 0 100`, while `Context.XView` was `(5, 15)`.
- `OnTouchNavigateAsync(session, 0, 1, 40, 90)`, carrying the rendered epoch, was rejected: `XView` stayed `(5, 15)` and no frame followed.
- Control with no token: the stamp became `2 0 5 15`. The rendered epoch advances by two per `ZoomToXAsync`, as the report derives (`Plot.razor.cs:559` and `:607`).

**Corrected claim.** The trigger is a token that is already cancelled when the command is called, under `RenderMode.Interactive` only (`InteractiveRenderModeHandler.cs:40-43`). A token cancelled after the hand-off has no effect. The same call also leaves the host's window applied to the context but unrendered, which is a second defect on the same line of code. The stuck state heals when any later `Compose` lands.

**Consequence.** As reported: the next touch operation is rejected silently and ends in `unavailable` after ten seconds, unless a render arrives first. It also feeds FINDING-01, because the server epoch is ahead of the stamp.

**Gate.** None.

**Action.** remediate → STEP-22.

**Verification.** Fail-first component case under `RenderMode.Interactive`: after `ZoomToXAsync`, `ZoomToYAsync` or `ResetViewAsync` with a cancelled token, a touch command at the rendered epoch is accepted; and after a later uncancelled refresh the stamp carries the advance when the x window changed. Rerun `TwoFingerNavigationTests`, `TouchRevisionTests` and `PlotViewCommandTests`.

### FINDING-03: confirmed on the facts; the normative reading needs a ruling

**Evidence.** Browser probe on `/time-cursor`: mouse click-pin, frames held, mouse drag of -80 px.

- Two seconds after release: `data-ggnet-interaction` absent, `data-ggnet-navigation` absent, the first x tick label visible with unchanged text (`04:00`), the crosshair label visible, the readout visible.
- The marks carried `translate(-61.15, 0)` in all four panels while the pinned cursor line stayed at screen x 614. After the frame was released it sat at 534, 80 px away, where its data had been all along.
- The preview was still present six seconds after release and gone at eleven, with no `unavailable`.

At HEAD the same paint stood for at most 1500 ms after release (`git show HEAD:src/GGNet/Components/Panel.razor.js`, line 14 and line 1343). The paint itself is not new; its bound is.

**Corrected claim.** None on the facts. The report's own caveat stands: STEP-17's checkable result names settlement only. DECISION-10 says STEP-17 "reads the spec's truthful-feedback section as applying to mouse and pen previews", and that section requires hidden stale ticks, pending feedback and "preview marks and committed pin together". On that reading the mouse and pen preview does not conform. ACCEPTANCE-08 says mouse and pen "retain deliberate existing semantics", and hiding ticks during a mouse drag would change what a mouse user sees.

**Consequence.** If the truthful-feedback section binds mouse and pen previews, the ACCEPTANCE-07 row overstates its evidence for them. If it does not, DECISION-10's wording is wider than the engineer's intent and the row should say so.

**Gate.** GATE-09, decision. The disposition changes acceptance either way.

**Action.** `defer: needs-decision` → GATE-09. If the ruling is parity, the work is STEP-27.

**Verification, if parity is ruled.** Fail-first browser case at a 2 s frame delay: during a held mouse and a held pen pan preview the root carries the pending state, the x tick labels and the crosshair label and markers are hidden, and a pinned line stays on its data; at the 10 s bound the state clears without `unavailable`. Existing mouse, pen and keyboard cases rerun.

### FINDING-04: confirmed

**Evidence.**

- `rg OnNavigateEndAsync` over the tree outside `plan/`: the method and its `DynamicDependency` (`Panel.razor.cs:95`, `:576`), six call sites in `TwoFingerNavigationTests.cs` (`:60`, `:83`, `:383`, `:406`, `:427`, `:444`), and the spec's premise table. `Panel.razor.js:1-7` defines no constant for it and the module never invokes it.
- The Blazor guide, line 101, lists `OnNavigateEndAsync` and omits `OnTouchNavigateAsync` and `OnTouchInspectAsync`.
- `rg AutoFitY tests`: no case drives AutoFitY through `OnTouchNavigateAsync`. A component probe did, and the live path refits (x `(15, 35)`, y about `(8.95, 9.05)`). The behaviour is correct; it is unpinned.

**Corrected claim.** None.

**Consequence.** The ACCEPTANCE-03 evidence "Window math/AutoFitY component tests" exercises a method no browser calls. The live midpoint and pinch arithmetic is JavaScript and is covered by browser cases only. The live AutoFitY, pin-follows-window and capability-gating server behaviour has no component case.

**Gate.** None. The step removes a public member of the public `Panel<T, TX, TY>` type. The API manifest lists types only (`PublicApiTests.GGNet.verified.txt`), so it does not change, and the beta window permits the removal. The engineer may still flip this triage.

**Action.** remediate → STEP-23.

**Verification.** The six cases are re-pointed at `OnTouchNavigateAsync` where they pin server behaviour (AutoFitY in the same change, the pin following the window, no commit without inferred x navigation, rejected bounds) and removed where the arithmetic moved to the module, each removal naming the browser case that covers it. Fail-first is not informative for re-pointed tests of existing behaviour; record mutation evidence for the AutoFitY case instead (drop the `FitYToXView` call at `Panel.razor.cs:615-618`, observe the failure, restore). Build, component suite, guide line 101 corrected.

### FINDING-05: deliberate design for the terminal state; confirmed for the evidence gap

**Evidence.**

- The spec, "Truthful feedback and acknowledgement": "On an explicit interop failure/disconnect, remove transient transforms and indicate unavailable", and "require resynchronization or remount before accepting new gestures". `Panel.razor.js:1460` and `:1551-1563` do exactly that, and remount is one of the two recoveries the spec allows. The README (line 187) states it.
- Component probe: with a `ViewChanged` handler that throws, `OnTouchNavigateAsync` faulted with the handler's exception after the commit was applied (`XView` `(40, 90)`) and, under `InteractiveAuto`, after its frame was rendered (stamp `0 1 40 90`). The server half of the report's route is real.
- The client half (a rejected promise reaching `failNavigation`) is a one-line reading and was not executed: the demo has no throwing callback. No browser or module-harness case exercises the rejection branch; the plan itself records that disconnects reach `unavailable` through the watchdog only.

**Corrected claim.** "`unavailable` is terminal for any rejected touch call" describes the specified behaviour, not a defect. What is confirmed is narrower: the explicit-rejection branch has no executed evidence, and the docs do not say that an exception escaping a host callback during a touch commit has this effect while the same exception on the mouse path only drops a preview (`Panel.razor.js:219-226`).

One further route, by reading only and not executed: a queued touch command addressed to a panel that is removed while other panels of the plot remain would reject on its disposed reference and quarantine the surviving coordinator. Low confidence; it needs a panel count that changes within one context.

**Consequence.** A host bug in a callback disables touch on a chart whose server state is correct, under a label that says to reload. That is loud and recoverable, and within the spec.

**Gate.** GATE-10, optional. The disposition changes nothing unless the engineer wants resynchronisation without remount, which would be new scope.

**Action.** remediate → STEP-24 for the confirmed part: executed evidence for the rejection branch, and one README sentence. The terminal behaviour stands unless GATE-10 rules otherwise.

**Verification.** A module-harness case in `GestureModuleTests` whose `invokeMethodAsync` rejects a touch call: the root reads `unavailable`, no transform or provisional line remains, and a later touch gesture paints nothing. Mutation evidence: remove the `catch` at `Panel.razor.js:1460`, observe the failure, restore.

### FINDING-06: confirmed, understated in confidence, not touch-specific

**Evidence.** Component probes.

- `Instant.FromUnixTimeMilliseconds` throws `ArgumentOutOfRangeException` outside about -3.78e14 to 2.53e14 ms (`Instant.MinValue` and `MaxValue`); 2.5e14 is accepted, 2.6e14 throws.
- From a 4.5 hour `Instant` window, successive full pinch-outs (span times ten, as the module's 0.1 clamp allows) were accepted and rendered for gestures 1 to 7. Gesture 8 (span 1.62e15 ms):
  - `RenderMode.InteractiveAuto`: `OnTouchNavigateAsync` threw `ArgumentOutOfRangeException`, and a later data-only `RefreshAsync` threw as well.
  - `RenderMode.Interactive`: no exception reached the caller, the rendered revision stayed at 7, and a later data-only refresh rendered nothing either. The context stays unrenderable.

**Corrected claim.** The throw is real, so confidence is high, not low-medium. The defect belongs to `InstantPosition.Commit` (`InstantPosition.cs:37-40`), which converts any committed view to an `Instant`. `OnTouchNavigateAsync` is one of three ways to hand it such a view; the wheel and a host `SetXView` are the others, and both exist at HEAD. This Task did not introduce the defect. It added a faster route: eight gestures, against roughly eighty wheel notches at the default `ZoomStep` of 0.8 (by arithmetic, not executed).

**Consequence.** The touch path ends as the spec prescribes (transient paint cleared, `unavailable`). The damage is on the server: every later render of that context fails until the host resets the view, and under `Interactive` the failure is only a log line.

**Gate.** GATE-11. The repair is understood, small and checkable: keep `Range` equal to the committed view, so stamps still match, and bound only the instants used for breaks and labels. Its surface is `src/GGNet/Scales/InstantPosition.cs`, which is outside this plan's write surfaces and which the spec does not exclude. That is the Foundation's surface-grant case, and the plan records no standing grant.

**Action.** `defer: out-of-scope` (surface `src/GGNet/Scales/InstantPosition.cs`; check `DateTimePosition.cs` for the same conversion). A grant at GATE-11 turns it into STEP-28. A guard in `OnTouchNavigateAsync` alone is not a repair: a rejected or altered window is never acknowledged with matching bounds, so the client would still reach `unavailable`.

**Verification, if granted.** Fail-first component case, both render modes: twelve successive tenfold widenings through `OnTouchNavigateAsync` on an `Instant` axis each render a frame carrying their revision and exact bounds, and a later data refresh renders. A wheel row for the same axis. Goldens unchanged.

### FINDING-07: confirmed

**Evidence.**

- `Plot.razor:322` keys panels on `(Context, i)`. Component probe: after a same-window context replacement the `Panel` instance is a different object, the plot's session is unchanged, and the JS runtime received `dispose`.
- Browser probe (the FINDING-09 probe, navigation row): frames held, a render in flight, a touch pan released, then New context. The replacement frame carried epoch 1 against the pending command's epoch 0, yet the root kept its pending attributes. A surviving coordinator would have taken the supersede branch at `Panel.razor.js:1511-1523` and cleared them. The coordinator was destroyed with its pending state (`Panel.razor.js:1565-1582`).
- A same-window replacement could not be staged in a browser: the demo's New context builds a context with no view. The same-window case rests on the keyed teardown, which does not depend on the window.

**Corrected claim.** None. The "kept" row of `AReplacedContextSupersedesTouchNavigationOnlyWhenItsWindowDiffers` proves server acceptance only. In a browser, a command sent after the swap targets a disposed panel, and one accepted before it set the window on the replaced context.

**Consequence.** README lines 164-165 and the Blazor guide (line 141) promise that a same-window replacement keeps a pending gesture. The client cannot deliver that. Combined with FINDING-09 the loss is not clean either: the pending indication stays.

**Gate.** GATE-12, decision. The promise restates the second half of DECISION-08, an engineer ruling, so correcting the docs amends that decision and making it true is new work.

**Action.** `defer: needs-decision` → GATE-12. If the ruling is to correct the docs, the work is STEP-29.

**Verification, if the docs are corrected.** README and guide state that a replaced context ends an in-flight touch gesture in the browser, and that the same-window rule only decides whether the epoch advances; the component row is renamed or commented as server acceptance. Review against STEP-25's browser case.

### FINDING-08: confirmed for the pinned state; already corrected

**Evidence.** The pinned plan (manifest hash `f21bd169…5a32`) carried the stale row the report quotes. The current plan, line 47, reads "audit-01 captured and challenged … audit-02 captured 2026-11-15 (8 findings); challenge in STEP-19".

**Consequence.** None remaining.

**Action.** remediate → done by the coordinating session's plan update under STEP-19; no new step. Verification: plan line 47 and `protodog` plan validation at the next plan write.

## 5. Bounded miss search

Scope: the audit-02 manifest, with priority on what the auditor did not inspect.

### FINDING-09 (new): a context replacement during a pending or active touch gesture leaves the plot root stuck in `pending`

**Claim.** When every panel of a plot is disposed while its coordinator holds pending state, the coordinator discards that state without clearing the root's `data-ggnet-interaction` and `data-ggnet-navigation` attributes. The root belongs to `Plot`, which survives the replacement, so the attributes stay until the next touch gesture.

**Evidence.**

- `Panel.razor.js:1565-1582`: the last-panel branch of `remove` clears `commands`, `pendingView`, `pendingInspection` and the watchdogs, and never calls `feedback()` or touches the root. The new coordinator's `reconcileNavigation` returns at `:1507-1510` before `feedback()` when nothing is intended, so no frame clears them.
- Browser probe on `/time-cursor`: tap-pin, hold frames, Append readings (a render in flight), then the gesture, then New context, then release.

| Gesture | Root 3 s after release | After a mouse wheel zoom | 11 s later | After the next touch tap |
|---|---|---|---|---|
| touch pan, released | `pending`, `data-ggnet-navigation`, x ticks hidden, cursor label hidden, readout hidden | unchanged | unchanged | cleared |
| hold-scrub, finger still down | `pending`, cursor label hidden, readout hidden | unchanged | unchanged | cleared |
| tap (acknowledged before the swap) | clear | clear | clear | clear |

No watchdog fires, because `remove` cleared it, and nothing reaches `unavailable`.

**Affected surface.** `Panel.razor.js` `TouchCoordinator.remove`; `Themes/Interaction.css` and the demo `interaction.css`, which key on those attributes; the README readout recipe; ACCEPTANCE-04 (owner disposal clears previews) and ACCEPTANCE-07.

**Consequence.** A healthy chart shows "Updating chart…" with hidden ticks, and a host that follows the README recipe hides its readout, indefinitely. A mouse-only user has no way out. A host that rebuilds its context for new data, the case DECISION-08 was written for, reaches it whenever a rebuild lands on an unacknowledged touch gesture. By the same reading, a context replacement after `unavailable` leaves the "reload to reconnect" label over a chart that accepts gestures again.

**Confidence.** High: reproduced in two rows; the third row is the control.

**Action.** remediate → STEP-25. The fix-now test passes: the surface is in the plan, the cause is one missing cleanup, nothing in outcome or acceptance moves, and one browser case proves it.

**Verification.** Fail-first browser case with the rows above plus one after `unavailable`: once the replacement frame has rendered, with no further input, the root carries neither attribute, x ticks and readout are visible, and the next touch gesture works. `LeavingThePageMidGestureOrMidOperationReturnsClean`, `ARemountAfterUnavailableAcceptsGesturesAgain` and `ResizingAndReplacingTheContextKeepThePin` rerun.

### FINDING-10 (new): the migrated dialog case cannot detect loss of the module's touch-pan tooltip dismissal

**Claim.** `DialogChartShowsItsTooltipAboveTheDialogATouchPanDismissesItAndReopeningRemounts` asserts `dismissed.Should().Be(0, "a touch pan dismisses the tooltip")` (`TouchDemoTests.cs:408`, `:420`). It passes whether or not the module dismisses anything.

**Evidence.** Browser probe repeating the case with `HTMLElement.prototype.hidePopover` replaced by a no-op, so the module's dismissal has no effect: the popover count was still 0, mid-gesture and at the assertion. The page log shows why: a trusted `mouseout` on the mark fires when the touch starts and the server removes the tooltip (`hide@1955, mouseout:circle@1956, mouseover:rect@1956, removed@1957`). The unmodified row shows the same sequence.

**Affected surface.** `TouchDemoTests.cs`; the plan's STEP-04 note that this case replaces the retired module case `TouchPanDismissesTheTooltipWhileAMouseDragKeepsIt`; the Blazor guide (line 113) and `TouchLayout.razor:44`.

**Consequence.** The guide's claim that a touch pan dismisses the tooltip client-side has no discriminating check. The user-visible outcome in this scenario is covered by the browser itself, so the exposure is small: a regression would show only where no `mouseout` arrives.

**Confidence.** High on the test; the behaviour is not in doubt (the probe counted one `hidePopover` call).

**Action.** remediate → STEP-24, with the FINDING-05 harness case: both need the module harness to run with a touch policy.

**Verification.** A module-harness case where no server removes the tooltip: an open tooltip is closed by a touch pan and kept by a mouse drag. Mutation evidence: remove the dismissal in `#beginNavigation` (`Panel.razor.js:673`), observe the failure, restore.

### FINDING-11 (new): the circuit-error attachment is lost when the case ends within a second of the error

**Claim.** `AttachServerOutputOnCircuitError` writes the server output from a detached task one second after the console error, and swallows the `InvalidOperationException` the output helper throws once the test has finished (`DemoAppFixture.cs:114-139`).

**Evidence.** Browser probe: disconnect through `CircuitGate`, wait, then fail on `session.Errors`.

| Wait after the error | "Demo server output since the page opened" in the failure output |
|---|---|
| 2500 ms | present, with the server lines |
| 300 ms | absent |

**Affected surface.** `DemoAppFixture.cs`; STEP-20's checkable result ("attaches it to a failing browser case when the page reports a Blazor circuit error"); the testing guide's fixture paragraph; the D-06 trigger.

**Consequence.** A drop in the last second of a case, where many cases assert `session.Errors` right after a short wait, produces a failure with no server output. That is the evidence STEP-20 exists to capture, and D-06 was recorded at about one failure in nine full runs.

**Confidence.** High.

**Action.** remediate → STEP-26.

**Verification.** A temporary probe as above, deleted afterwards: the 300 ms row carries the server output. `ProcessOutputTailTests` and one browser class rerun with 0 skipped.

### Checked and found sound

- **Pen input is real pen input.** CDP `Input.dispatchMouseEvent` with `pointerType: "pen"` delivered `pointerdown:pen, pointermove:pen, pointerup:pen`. A 5 px pen drag did not pan (0 view changes, no preview) and a 7 px drag did, which separates the 6 px pen threshold from the 4 px mouse threshold. The pen rows of ACCEPTANCE-08 exercise the pen path, although no committed case asserts the pointer type or the threshold.
- **Exact-bounds matching cannot starve on other axis types.** Every position scale commits a view through `Position.CommitViewRange` (`Position.cs:39-52`; `DateTimePosition.cs:152`, `DiscretePosition.cs:101`, `Log10.cs:28`, `Extended.cs:32`, `InstantPosition.cs:37`), so `Range` equals the committed window and the stamp round-trips. The only failure found on this path is FINDING-06.
- **Font vendoring.** `App.razor` links `css/inter.css` through `Assets`, with no remote host; `rg rsms src/GGNet.Demo` finds only the comment in `inter.css`. Its `../fonts/inter/` URLs resolve from the fingerprinted stylesheet path. The licence file is beside the fonts, and the two binaries match their manifest hashes.
- **GestureModuleTests migration.** Each retired touch case names a real-app replacement that exists (`ASecondContactCarriesThePanWithoutAnIntermediateCommit`, `ThirdContactCancelsNavigationAndDrainsRemainingFingers`, `TwoFingersThatDoNotMoveCommitNothing`), except the tooltip dismissal (FINDING-10). `WithoutATouchPolicyATouchDragNeverPans` asserts zero calls and no preview.
- **Touch and TouchLayout pages.** The page prose matches the configured policies in `Touch.razor.cs` and `TouchLayout.razor.cs`.

## 6. Proposed plan steps

Rows follow the plan's step grammar. STEP-21 to STEP-26 pass the fix-now test and need no gate. STEP-27 to STEP-29 exist only if their gate rules for them.

| ID | Checkable result | Affected surfaces | Acceptance | Verification |
|---|---|---|---|---|
| STEP-21 | (audit-02 FINDING-01) A data-only frame never settles a mouse or pen pan preview, including after a host Clear, Escape or host-set `Inspection`, and while the inspection stamp is momentarily absent. | `Panel.razor.js` mouse/pen settlement, `Plot.razor.cs` if the stamp must carry the advance, `TimeCursorFrameTests` | ACCEPTANCE-07, ACCEPTANCE-08 | Fail-first browser rows (Clear, Escape, host `Inspection`) on the telemetry sequence; the four existing mouse/pen settlement cases rerun. |
| STEP-22 | (audit-02 FINDING-02) A host viewport command whose token is already cancelled advances no epoch that no frame will carry. | `Plot.razor.cs` host commands and `CommitViewAsync`, component tests | ACCEPTANCE-06, ACCEPTANCE-07 | Fail-first component rows under `RenderMode.Interactive` for the three commands; epoch and view-command component classes rerun. |
| STEP-23 | (audit-02 FINDING-04) `OnNavigateEndAsync` is gone; the server behaviour its six cases pinned is pinned through `OnTouchNavigateAsync`, or the removal names the browser case that covers it; the guide lists the live invokables. | `Panel.razor.cs`, `TwoFingerNavigationTests`, Blazor guide line 101, ACCEPTANCE-03 evidence text | ACCEPTANCE-03 | Build; component suite; mutation evidence for the AutoFitY case. |
| STEP-24 | (audit-02 FINDING-05, FINDING-10) The module harness runs with a touch policy and has executed cases for a rejected touch call reaching `unavailable` cleanly and for a touch pan dismissing an open tooltip; the README says what an exception escaping a host callback during a touch commit does. | `GestureModuleTests`, `TouchDemoTests` assertion text, README | ACCEPTANCE-07, ACCEPTANCE-08 | Two new harness cases with mutation evidence each; `GestureModuleTests` and `TouchDemoTests` classes pass with 0 skipped. |
| STEP-25 | (audit-02 FINDING-09) Disposing a plot's last panel clears the root's interaction and navigation attributes, whatever state the coordinator held. | `Panel.razor.js` `TouchCoordinator.remove`, `TimeCursorLifecycleTests` | ACCEPTANCE-04, ACCEPTANCE-07 | Fail-first browser rows {pending pan, active scrub, `unavailable`} × New context; the three lifecycle and lifetime cases named under FINDING-09 rerun. |
| STEP-26 | (audit-02 FINDING-11) The server output of a circuit error reaches the test's output even when the case ends immediately afterwards. | `DemoAppFixture.cs`, the three `OpenAsync` sessions if they must flush, testing guide paragraph | ACCEPTANCE-09 | Temporary probe (deleted): the 300 ms row carries the output; `ProcessOutputTailTests` and one browser class pass with 0 skipped. |
| STEP-27 (if GATE-09 rules parity) | Mouse and pen pan previews carry the same stale-value feedback as touch navigation, from recognition to settlement, and a pinned cursor travels with the marks. | `Panel.razor.js`, `Themes/Interaction.css` if needed, frame tests, README and guide | ACCEPTANCE-07, ACCEPTANCE-08 | As under FINDING-03. |
| STEP-28 (if GATE-11 grants the surface) | An `Instant` view outside NodaTime's range renders instead of throwing; `Range` still equals the committed view. | `src/GGNet/Scales/InstantPosition.cs` (and `DateTimePosition.cs` if it shares the conversion), component tests | ACCEPTANCE-07 | As under FINDING-06. |
| STEP-29 (if GATE-12 rules for the docs) | README, guide and the component row say what a context replacement does to an in-flight touch gesture in a browser. | README, Blazor guide, `TwoFingerNavigationTests` naming | ACCEPTANCE-06 | Doc review against STEP-25's browser case. |
| STEP-30 | Final verification after the steps above. | Whole tree, task plan | ACCEPTANCE-01 to ACCEPTANCE-10 | Four repository gates and the full enabled E2E with counts; ACCEPTANCE-03, 04, 06, 07 and 09 re-evidenced. |

## 7. Decision requests

Each request carries a recommendation and the default that holds if it is not ruled.

**GATE-09 (FINDING-03): do mouse and pen pan previews owe the stale-value feedback the spec requires of navigation?**

- Options: (a) parity with touch from drag recognition to settlement (STEP-27); (b) accepted gap: mouse and pen previews keep visible ticks and a stationary pinned cursor, the ACCEPTANCE-07 row and the README say so, and DECISION-10 is narrowed to settlement.
- Recommendation: (a). DECISION-10 already reads the section as applying to these previews, STEP-17 raised their bound from 1.5 s to 10 s, and the touch path's attributes and CSS can be reused. It changes what a mouse user sees during a drag, which is why it needs the engineer's word against ACCEPTANCE-08. A variant that shows the feedback only after release is not recommended: on a fast circuit it would blink the ticks on every pan.
- Default if unruled: the finding stays open and ACCEPTANCE-07 cannot be recorded as satisfied for mouse and pen previews.

**GATE-10 (FINDING-05): should a rejected touch call recover without a remount?**

- Options: (a) keep the specified behaviour; (b) add resynchronisation, which is new scope and a new protocol element.
- Recommendation: (a), with STEP-24's evidence and README sentence.
- Default if unruled: (a).

**GATE-11 (FINDING-06): grant `src/GGNet/Scales/InstantPosition.cs` as a write surface for one step?**

- Options: (a) grant, and STEP-28 runs in this Task; (b) register as a deferred issue for the engineer's deferred review.
- Recommendation: (a). The consequence is a context that cannot render until the host resets its view, the wheel reaches it at HEAD, this Task shortened the route to eight gestures, and the repair is small and checkable.
- Default if unruled: (b), as `defer: out-of-scope`, with the README unchanged.

**GATE-12 (FINDING-07): correct the same-window promise, or make it true?**

- Options: (a) correct README, guide and test naming (STEP-29), amending DECISION-08's second half to "a same-window replacement does not advance the epoch; the browser's in-flight gesture still ends"; (b) make the coordinator outlive its panels and re-target queued commands, which fails the fix-now test on cost and adds a protocol element.
- Recommendation: (a). With STEP-25 the loss becomes clean, and a host that carries its window across a rebuild sees the committed part of the gesture in the new frame.
- Default if unruled: the docs stay false, which the repository's working rules do not allow to stand at completion; the item blocks completion until ruled.

Re-audit: none of the policy's re-audit triggers requires a third cycle for STEP-21 to STEP-26; they are local repairs with fail-first checks. If GATE-09 rules (a) or GATE-12 rules (b), the resulting step changes client protocol state, and a directed review of that step is recommended before completion. A new audit cycle would be the engineer's selection.

## 8. Probes created and removed

All three files were deleted before the final build. `git status` shows none of them, and no file under `tests/` matches `*Probe*`.

- `tests/GGNet.Components.Tests/ChallengeProbe.cs`: eight bUnit probes (cancelled token and its control; NodaTime range; tenfold widening under both render modes; throwing host handler; AutoFitY through the live invokable; context replacement).
- `tests/GGNet.E2ETests/ChallengeProbe.cs`: a partial of `TimeCursorTests` with the FINDING-01, FINDING-03, FINDING-09 and FINDING-11 probes.
- `tests/GGNet.E2ETests/ChallengeProbe2.cs`: the pen pointer-type and dialog tooltip probes.

Probe logs were written to the session scratchpad (`component-probe.log`, `e2e-probe.log`, `f11.log`), outside the repository, and removed at the end of the session. The solution was rebuilt after the deletion: 0 warnings, 0 errors.
