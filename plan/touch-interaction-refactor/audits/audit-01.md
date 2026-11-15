<!-- launched by audit-launch.cs: dispatch audit-01-dispatch.md sha256=b32fb3cde99b6fe96607b2e7dc358cd12e298a496e321c552f81bed1b896a81e auditor=claude model=fable resolved-model=claude-fable-5-2 sandbox=read-tools-only -->
# Audit 01: touch interaction refactor

## 1. Scope and target-state identity

- Audit `audit-01` of `plan/touch-interaction-refactor/task-plan.md`.
- Target state: HEAD `7fd4b8e06098fb8cc730545c635a1770506bcc71` plus the uncommitted working tree, identified by the SHA-256 manifest in section 10 of `plan/touch-interaction-refactor/audits/audit-01-dispatch.md`. I did not recompute hashes; the dispatch states the launcher verified them.
- Focus: gesture lifecycle and delayed-frame correctness, per dispatch section 4.
- I wrote, edited and created no file.

## 2. Sources used

- Normative: `specs/touch-interaction-refactor.md` (whole) and `plan/touch-interaction-refactor/task-plan.md` (whole: steps, acceptance table, decisions, material notes).
- Normative, by targeted search only: `.github/instructions/blazor.instructions.md`.
- Not opened: `AGENTS.md`, `CLAUDE.md`, the other scoped guides, and the neutral input `inputs/touch-interaction-refactor.md`.
- Unlisted supporting files read to resolve behaviour:
  - `src/GGNet/Rendering/InteractiveRenderModeHandler.cs` and `InteractiveAutoRenderModeHandler.cs`
  - `src/GGNet/Data/Position.cs`
  - `src/GGNet/Scales/Position.cs` and `DiscretePosition.cs` (partial), plus search hits across the other scales

## 3. Coverage

**Read in full**
- `src/GGNet/Components/Panel.razor.js`, `Panel.razor`, `Panel.razor.cs`, `Plot.razor.cs`, `PanelInterop.cs`, `CrosshairCursor.cs`, `CursorFrame.cs`, `Area.razor`
- `src/GGNet/InteractivityOptions.cs`, `TouchInteractionOptions.cs`, `Themes/Interaction.css`
- `src/GGNet.Demo/Components/Pages/OverviewDetail.razor`, `OverviewDetail.razor.cs`, `src/GGNet.Demo/wwwroot/css/interaction.css`
- `tests/GGNet.E2ETests/Fixtures/CircuitGate.cs`, `TimeCursorFrameTests.cs`, `TimeCursorLifecycleTests.cs`
- `tests/GGNet.Components.Tests/TwoFingerNavigationTests.cs`, `TouchRevisionTests.cs`

**Inspected only by search (test names, selected lines)**
- `GestureModuleTests.cs`, `TouchDemoTests.cs`, `OverviewDetailTests.cs`, `TimeCursorTests.cs`, `TimeCursorNavigationTests.cs`, `TimeCursorTouchTests.cs`
- `README.md`, `.github/instructions/blazor.instructions.md`

**Not inspected**
- `Touch.razor(.cs)`, `TouchLayout.razor(.cs)`, `TimeCursor.razor`, `App.razor`, `src/GGNet.Demo/README.md`
- `TouchNavigation.cs`, `TouchOwnership.cs`
- `PanInteropTests.cs`, `PanelLifecycleTests.cs`, `TouchPolicyTests.cs`, `PublicApiTests.GGNet.verified.txt`, `ThemeContractTests.cs`

A clean statement about any of these would be unsupported, and I make none. The Claude-authored migrations in `GestureModuleTests`, `TouchDemoTests` and the Touch/TouchLayout pages received less scrutiny than the dispatch requested; the Claude-authored epoch rule, `CircuitGate` and the frame tests received full scrutiny.

**Limits and method**
- Static reading only: no shell, tests, browser, device or HEAD diff. Every runtime consequence below is derived from code paths, not observed.
- Executed-verification claims in the plan (test counts, fail-first, physical QA) were checked for plausibility only.
- Method: I traced each state transition of `TouchCoordinator` against the spec's gesture table, and traced the identity and acknowledgement path (JS queue → `AcceptTouch` → `Compose` → Panel/Area/cursor stamps → `reconcileNavigation`). I looked for server paths that reject a command without rendering, and for epoch advances that no rendered frame carries.

## 4. Overall assessment

The core design matches the spec's intent: one plot-scoped coordinator, absolute mapped commands, session/epoch/revision identity, and settlement against stamped frames rather than time or arbitrary mutations. On the paths I traced, the contact arbitration table is implemented as specified, and the delay and telemetry browser cases are constructed so they would detect the failures they name.

The material weakness is the recovery path. The client has no negative acknowledgement, and the watchdog is not tied to one operation. Any command the server drops silently, any frame that can never match, or simply sustained use under latency ends in the permanent `unavailable` quarantine on a healthy circuit. FINDING-01 to FINDING-04 are four independent routes to that state; FINDING-02 is reachable in the shipped overview/detail demo. Under that reading, ACCEPTANCE-07 ("no stuck transforms… latest intended operation wins") and ACCEPTANCE-06 are not fully supported by the code, and the existing tests would not detect these routes.

## 5. Findings

### FINDING-01: The watchdog measures time since the pipeline was last idle, not the age of an unresolved call

- **Claim:** The 10 s watchdog starts at the first enqueue and is never restarted as later operations are acknowledged. It is cleared only when nothing at all is pending. Continuous legitimate interaction that keeps something pending for 10 s quarantines the chart, even though every individual operation is acknowledged promptly.
- **Evidence:** `Panel.razor.js:1378` `if (!this.watchdog) this.watchdog = setTimeout(() => this.failNavigation(), 10000);`. `finishWatchdog` (`1394-1399`) clears only when `!pendingView && !pendingInspection && !commands.length && !inFlight`. Each scrub move calls `inspect()` (`1345-1358`), which replaces `pendingInspection` with a new revision; it settles only on a frame with that exact revision (`1430-1434`). `failNavigation` (`1450-1461`) sets `unavailable = true`, and nothing ever resets it.
- **Affected surface:** Touch scrub and successive pan/pinch on any touch-enabled plot under non-trivial round-trip time.
- **Consequence:** A hold-scrub with pointer moves arriving faster than one round trip, sustained for 10 s, triggers the quarantine. So do successive releases spaced closer than the latency for 10 s — about five flick-pans at the spec's own 2 s latency point. The result is "Chart unavailable — reload to reconnect" on a healthy circuit, and the in-progress gesture is cancelled. This contradicts the spec's "bounded watchdog… for an indefinitely unresolved call" and "not permission to treat elapsed time as acknowledgement".
- **Confidence:** High on the mechanism (direct reading). Medium on how often it occurs at loopback latency; the tests and Pi QA use short gestures, and I found no test name covering sustained interaction.

### FINDING-02: The host epoch can advance with no rendered frame carrying it, so later touch commands are rejected silently

- **Claim:** `ClearInspectionAsync` and an `Inspection` parameter change increment `touchEpoch` unconditionally. The client learns the epoch only from the Area view stamp (written in `Compose`) or the cursor's inspection stamp. The inspection stamp exists only when touch inspection is enabled. On a plot with touch navigation but no touch inspection, neither stamp changes, the next navigation carries the old epoch, and `AcceptTouch` rejects it without rendering.
- **Evidence:**
  - `Plot.razor.cs:228-232` (`touchEpoch++` in `ClearInspectionAsync`) and `:216-219` (`touchEpoch++` in `OnParametersSetAsync`).
  - `:250-257`: `InspectAsync` returns early when `!InspectionEnabled`, so `inspection` stays null and never equals a non-null parameter.
  - `:487-491`: `RenderedTouchEpoch` is updated only in `Compose`.
  - `Panel.razor.cs:445`: `InspectionStamp = Plot.TouchOptions is { Inspect: true } ? … : null`.
  - `Panel.razor:161`: the cursor component is rendered only when `InspectionEnabled`.
  - `Plot.razor.cs:102`: `epoch != touchEpoch` returns false; `Panel.razor.cs:605-612` then returns with no render.
  - `OverviewDetail.razor.cs:223-228` calls `ClearInspectionAsync` on both plots in every mode, and `SetPolicy(false, …)` yields `Crosshair = false, Inspect = false`; both plots receive `Inspection=@inspection`.
- **Affected surface:** Navigation-only touch policies, and `Crosshair = true` with explicit `Touch.Inspect = false`. The demo's "Navigation only" mode is one.
- **Consequence:** In the demo: Navigation only → Clear → pan. The preview transform and hidden ticks stay for 10 s, then the chart goes permanently `unavailable`. Separately, with a retained non-null selection in that mode, every parent re-render bumps the epoch, including the re-render caused by the plot's own `ViewChanged`. The next gesture then either hits the same quarantine or is treated as superseded, depending on a race with the background render thread.
- **Confidence:** High for the Clear path (deterministic by reading). Medium for the parameter path (race-dependent). The only navigation-only browser case I found by search (`OverviewDetailTests.cs:239`) does not press Clear.

### FINDING-03: Touch inspection at a position the scale cannot unmap is dropped with no acknowledgement

- **Claim:** `OnTouchInspectAsync` returns without `AcceptTouch` and without a frame when `xscale.Unmap(mapped)` is null. The client's `pendingInspection` for that revision can then never settle.
- **Evidence:** `Panel.razor.cs:632-636`. `DiscretePosition.Unmap` (`:124-134`) returns null for an index outside `[0, count)`; `DateTimePosition.Unmap` has the same index guard; base `Scales/Position.cs:76` returns null. `OnTouchNavigateAsync` does not clamp the window to the data extent, so a view can extend past the last category or sample. The client always sends (`Panel.razor.js:1345-1358`).
- **Affected surface:** Tap or scrub release on discrete and `LocalDateTime` (snapped) shared x axes, which the spec keeps supported, when the contact lands beyond the first or last sample. Also any x scale that does not override `Unmap`.
- **Consequence:** The provisional line and pending state persist. Unless another accepted inspection replaces it within 10 s, the watchdog quarantines the chart. A tap in empty space becomes a permanent `unavailable`.
- **Confidence:** High on the server and client paths. Medium on reachability without panning, because I did not confirm the default discrete range padding. Panning past the extent is reachable by reading.

### FINDING-04: Mouse, wheel and pen commits share the touch frame stamp but carry no identity, so a pending touch navigation can become unmatchable

- **Claim:** `OnWheelAsync` and `OnPanEndAsync` commit with cause `Gesture`, which advances neither epoch nor navigation revision. If such a commit is coalesced into the same render as an accepted touch navigation, the only frame produced carries the touch revision with different bounds. Settlement requires exact bounds equality, so `pendingView` never settles.
- **Evidence:**
  - `Plot.razor.cs:590-595`: only Reset, or HostCommand with a changed window, advances the epoch.
  - `Panel.razor.cs:344, 568`: wheel and mouse/pen pan are relative to the server's current view.
  - `InteractiveRenderModeHandler.cs:38-53, 92-99`: `RefreshAsync` returns immediately and pending renders coalesce.
  - `Panel.razor.js:1424-1426`: settlement requires `frame.min === pendingView.min && frame.max === pendingView.max`.
- **Affected surface:** Hybrid mouse+touch or pen+touch use on one plot while a touch navigation is pending.
- **Consequence:** The preview is held until the 10 s watchdog, then permanent `unavailable`. Separately, the mouse/pen pan path and the coordinator both write `transform` on the same marks group with no arbitration between them.
- **Confidence:** Medium. The logic is direct, but it needs near-simultaneous mixed input and coalescing, which I could not observe.

### FINDING-05: The Claude-authored epoch rule misses two forms of host viewport replacement

- **Claim:** `CommitViewAsync` overwrites the `touchXView` snapshot on every call, including `RenderTarget.Loading`, and `OnParametersSetAsync` overwrites it before refreshing a replaced `Context`. A changed x window delivered either way never advances the epoch.
- **Evidence:**
  - `Plot.razor.cs:590-595`: the bump is conditional on `target == RenderTarget.Render`, but `touchXView = xView` is unconditional. So `SetXView` → `RefreshAsync(Loading)` → `RefreshAsync(Render)` compares equal on the second call.
  - `:196-209`: `touchXView = Context.Positions.X.View` precedes `RefreshAsync`, so a new context with a different window also compares equal.
  - `TouchSession` is per Plot component (`:86`), so the client session survives a context swap.
- **Affected surface:** Host precedence over in-flight touch gestures ("Host Clear/viewport replacement invalidates superseded pending gestures").
- **Consequence:** An older in-flight touch navigation is accepted after the host's replacement and overwrites it with absolute bounds computed against the previous frame. `ADataRefreshKeepsAPendingTouchNavigationValid` and `ResettingAnUnchangedWindowStillRejectsAnOlderTouchEpoch` pin only the single-call Render cases.
- **Confidence:** Medium for Loading-then-Render (clear by reading; I found no in-repo host using that sequence). Low to medium for context replacement, because the spec does not say whether a new context with a different window counts as viewport replacement.

### FINDING-06: `unavailable` hides the last acknowledged cursor label, markers and host readout

- **Claim:** The library and demo CSS hide stale-value elements for any value of `data-ggnet-interaction`, including the permanent `unavailable`.
- **Evidence:** `Themes/Interaction.css:4-7`: `.ggnet[data-ggnet-interaction] :is(.crosshair-label, .crosshair-marker) { visibility: hidden; }`. `src/GGNet.Demo/wwwroot/css/interaction.css:5-17` hides all readout children and substitutes "Readings unavailable — reload to reconnect". `reconcileNavigation` returns early when unavailable (`Panel.razor.js:1402`), while Blazor keeps applying server frames after a watchdog-only failure.
- **Affected surface:** Recovery display after watchdog or interop failure.
- **Consequence:** The spec requires "indicate unavailable while showing the last acknowledged state". Instead the acknowledged timestamp, sample markers and readout are hidden for the rest of the mount, even when the server is alive and those values are authoritative. Combined with FINDING-01 to FINDING-04, a healthy chart loses its readout permanently. Neither watchdog nor disconnect test asserts label or readout visibility in `unavailable`.
- **Confidence:** Medium. The CSS behaviour is certain; whether "last acknowledged state" includes label and readout is an interpretation.

### FINDING-07: A failed `setPointerCapture` cancels the gesture, but the caller keeps going

- **Claim:** `capture()` calls `cancel()` on exception, and its three callers continue as if capture had succeeded.
- **Evidence:** `Panel.razor.js:1536-1541`. Callers: `down` at `1503-1505` (then `state(…)` and `touchMove` → `previewNavigation`), `move` at `1567-1570` (then `touchPan`), and the hold timer at `1521-1523` (then `touchScrub`). After `cancel()` the gesture is null. `navigationView` is cleared only inside `if (this.pendingView && …)` (`1424-1429`) or by a later gesture.
- **Affected surface:** The terminal-path idempotency required by the gesture contract.
- **Consequence:** If capture throws (pointer no longer active), a navigation preview transform and hidden ticks remain with no owning gesture until the next gesture. In the hold case an unpinned scrub is sent after the restore, leaving a trailing provisional selection.
- **Confidence:** Low on occurrence (rare browser race); high that the post-cancel statements execute.

### FINDING-08: The evidence does not cover several claimed lifecycle and recovery behaviours

- **Claim:** Some ACCEPTANCE-04 and ACCEPTANCE-07 evidence is narrower than the plan's wording.
- **Evidence:**
  - `InterruptedScrubRestoresThePinAndAcceptsTheNextGesture` interrupts only a scrub. I found no browser case that applies `pointercancel`, lost capture, blur or resize to a pending hold, a one-finger pan or a two-finger navigation, although the spec's matrix lists "pending hold/pan/scrub".
  - For the injected `pointercancel`, the coordinator deletes the contact and clears `draining` while the real finger is still down, so the drain state is not exercised in that row.
  - The plan (STEP-04 note) says the cancellation and disposal module cases were converted to mouse. I found no case disposing a panel or plot during an active touch gesture or pending touch operation (`TouchCoordinator.remove`).
  - `WatchdogQuarantinesLateInspectionRepliesUntilRemount` never remounts; recovery by remount is untested.
  - `HostClearWinsOverAHeldTouchReply` holds server frames only, so the tap is already applied before Clear. The "old touch request arrives after Clear" ordering is proven only in bUnit (`ClearInvalidatesQueuedTouchEvenWhenItTargetsTheSameValue`).
  - No case covers the conditions of FINDING-01 to FINDING-03.
- **Affected surface:** Acceptance table rows ACCEPTANCE-04 and ACCEPTANCE-07.
- **Consequence:** "Every cancellation/lifetime path" and "no stuck transforms after disconnect/watchdog/disposal" are supported for scrub and for fully held traffic, not for the other states.
- **Confidence:** Medium. It rests on reading two E2E files fully and the others by test name, so a matching case inside an unread test body is possible.

### FINDING-09: Timeout and any-mutation settlement remain for mouse and pen, and the plan does not disclose it

- **Claim:** The 1500 ms expiry and any-mutation settlement that the spec says to replace are still live for mouse and pen pan previews and for mouse cursor previews.
- **Evidence:** `Panel.razor.js:15` `SETTLE_TIMEOUT_MS = 1500`; `#settleWith` (`1078-1094`) unsettles on any mark mutation or after the timeout; `#holdCursor`/`#leaveCursor` timers (`771, 803`); `#observeCursor` (`865-892`). Pen is routed through this path (`137-139`).
- **Affected surface:** Mouse and pen feedback under latency or telemetry; the wording of ACCEPTANCE-07.
- **Consequence:** A pen or mouse pan at 2 s latency drops its preview at 1500 ms, and a telemetry-only frame settles it early. The spec's feedback section is not explicitly limited to touch, while ACCEPTANCE-08 says mouse and pen retain existing semantics. The plan claims ACCEPTANCE-07 satisfied without stating that the replacement is touch-only.
- **Confidence:** High that the code remains. Low that it violates the spec, given the tension between those two clauses. This needs an explicit ruling or a disclosure rather than a presumed fix.

## 6. Advisory recommendations

- **FINDING-01:** Tie the watchdog to the oldest unacknowledged operation and restart it on each acknowledgement.
- **FINDING-02 to FINDING-04:** Give the client a negative acknowledgement — for example, return accepted/rejected plus the current epoch from `OnTouchNavigateAsync` and `OnTouchInspectAsync` — so a rejection resynchronises instead of waiting for the watchdog. Carry the epoch on a stamp that exists for every touch-enabled plot.
- **FINDING-05:** Update the `touchXView` snapshot only when a Render-target commit is evaluated, and decide explicitly whether context replacement advances the epoch.