# Touch interaction refactor

## Why this document exists

Reconcile `inputs/touch-interaction-refactor.md` into a bounded GGNet implementation contract.
The engineer selected Task on 2026-11-14 and subsequently ruled: replace touch defaults in the
beta window; adopt tap/pan/400 ms hold/10 px slop and third-contact cancellation; hide stale
ticks/grid during navigation; schedule a final independent Claude audit. These rulings supersede
the input's unratified recommendations where they overlap. Ownership: agent generated from those
rulings and verified repository premises. This document binds only when the Task becomes ready.

## Outcome and boundaries

Give linked overview-like and detail-like charts the same touch contract, with navigation
independent of whether inspection or its crosshair is enabled. Preserve `Inspection<TX>` as the
data-coordinate selection model. Provide truthful provisional feedback on Blazor Server.

In scope: interaction configuration, component/JS interop, gesture arbitration, cursor and
navigation preview reconciliation, narrowly required SVG attributes/groups and theme paint,
demo scenarios, documentation, component/headless/browser tests and test delay instrumentation.

Out of scope: Portal changes; browser/kiosk flags; global page zoom/CSS policy; deployment;
dialog or duration controls; inertia; range presets; panel action slots; published panel geometry;
new geoms or scale families; a rendering redesign; fully live tick generation; dedicated
accessibility expansion. Preserve existing keyboard behavior. No repository history/index mutation.

## Verified premises

Grounded against main `7fd4b8e06098fb8cc730545c635a1770506bcc71`. The supplied input was the only
untracked content before planning. Evidence here is static inspection, not executed behavioral QA.

| Premise | Repository evidence |
|---|---|
| Touch navigation currently depends on inspection | `PanelInteropOptions.TwoFingerX` requires `Cursor == SharedCursor`; `Panel.OnNavigateEndAsync` requires `Pan`, `Crosshair`, shared cursor and an X zoom axis. |
| One finger switches from pan to scrub with shared inspection | `Panel.razor.js` pan pointerdown bypass and `#startCursor` / `#startScrub`. |
| Activation and reset arbitration exists | `Panel.razor.cs`: `activationDelay` is 200 ms; `Activates` excludes Crosshair/CursorTooltip; `OnResetViewAsync` resets the viewport. JS suppresses touch double-click for shared inspection. |
| Selection/readout APIs already exist | `Plot.razor.cs`: `Inspection`, `InspectionChanged`, `ReadingsChanged`, `ClearInspectionAsync`, `RestoreInspectionAsync`; `InspectAsync` retains the committed pin separately from a provisional unpinned value. |
| Shared inspection has existing limits | `InspectsSharedX` excludes Flip/freeX/multiple X scales; README documents numeric/Instant readings and snapped LocalDateTime/discrete cursor behavior. |
| Current previews can settle for the wrong reason | `Panel.razor.js`: `SETTLE_TIMEOUT_MS = 1500`, `#settleWith` ends on general mark mutation, `#observeCursor` ends navigation on cursor mutation; cursor settlement compares geometry, not an operation identity. |
| Cursor text moves with an old frame | `CrosshairCursor` places line and timestamp in the same translated group; markers and host readout are separately rendered. |
| Navigation already uses Euclidean pinch distance | `#beginNavigation` / `#moveNavigation` use `Math.hypot`, moving midpoint and inverse capture CTM. |
| Browser coverage requires explicit opt-in | `DemoAppFixture` launches Chromium only with `GGNET_E2E=1`, otherwise skips; bUnit does not execute the JS. |
| A pre-existing test uncertainty exists | Deferred D-06 describes an unresolved intermittent remount circuit error. This Task does not classify it as a new regression. |

External constraint checked against [Pointer Events §8.2](https://www.w3.org/TR/pointerevents3/#determining-supported-direct-manipulation-behavior):
changing touch-action after the browser starts its gesture does not reclaim ownership. The
scroll-permitting policy must therefore tolerate cancellation after hold recognition.

## Public policy and migration

Use one explicit touch-options value on `InteractivityOptions`, resolved once at initialization.
Proposed concrete names: `TouchInteractionOptions`, `TouchNavigation` (`None`, `TwoFingerX`,
`PanAndPinchX`) and `TouchOwnership` (`Owned`, `ScrollY`); a boolean `Inspect` enables tap/hold
inspection independently. The options record owns navigation, inspection and browser ownership;
do not expose separate overlapping pan/pinch/scrub booleans. Names may be refined without changing
these semantics. Interactivity remains initialization-only and requires remount to change.

For omitted touch options, resolve existing intent into the **new** contract: `Pan` with an X
zoom axis selects `PanAndPinchX`; otherwise X navigation is off. `Crosshair` enables tap/hold
inspection by default. The active new touch policy defaults to `Owned`. This is a beta behavior
change, not a retained legacy recognizer. Explicit touch options replace that inference in full:
navigation can work with `Crosshair = false`, inspection can work without navigation, and
`TwoFingerX` leaves one-finger drag unassigned before a hold. Explicit inspection must not require
the visual crosshair; the host can receive readings while the library supplies recognition/pending
feedback without a persistent crosshair. No second public pin model.

`Pan`, `Zoom`, `ZoomStep`, `CursorTooltip`, mouse hover/click and pen behavior retain their existing
non-touch meanings. The new X gesture policy does not promise Y or two-axis touch navigation.
Document this migration for current Y/Both users, including AutoFitY staying derived from X.
Do not silently introduce a new touch Y fallback. With both touch capabilities disabled, do not
intercept touch or take browser ownership. Static/noninteractive SVG stays byte-identical.

New shared touch inspection/navigation applies to Cartesian shared-X charts, including linked
panels with different dimensions. Preserve existing local mouse/pen cursor behavior on flipped,
free-X and polar layouts; do not invent a shared touch pin there. Explicit unsupported shared
touch requests fail early with a useful `GGNetUserException`; default inference disables the
unsupported capability. Preserve existing supported snapped-axis inspection and its readings
limitations rather than expanding reading support.

Touch inspection owns taps: no panel activation, mark click, ancestor click or reset. Touch
double-click reset is suppressed whenever the new touch policy owns the contact, even with
inspection disabled; two taps remain two ordinary selections when inspection is enabled. Mouse
double-click/reset and mouse activation keep their behavior. Existing touch activation remains
available only on charts with touch interaction disabled and no conflicting tooltip behavior.

Migration docs must explicitly cover immediate-pan replacing scrub, hold recognition, plot-owned
touch replacing native scrolling by default, independent two-finger navigation, unsupported Y
touch navigation, explicit ScrollY, and separate visible host controls for expansion and reset.

## Gesture state contract

Use one plot-scoped touch coordinator, with panel registration and one owner, independent of
cursor rendering. It owns contact bookkeeping, timers, previews, operation identity and terminal
cleanup. Keep the collocated JS module and existing initialization/disposal ownership.

| State/input | Result |
|---|---|
| First eligible contact | Snapshot committed pin/view, record fixed capture CTM and press position; pending hold only if inspection enabled. |
| Release before recognition within 10 screen px | Select/pin shared X immediately on release when inspection enabled; no 200 ms activation/double-tap wait. Suppress trailing compatibility click. |
| Horizontal-dominant travel reaches 10 px before hold | Cancel hold; pan X if configured, else consume as unassigned movement without a later tap. |
| Vertical-dominant travel reaches 10 px before hold | Cancel hold and tap eligibility. ScrollY may yield to browser; Owned consumes without a chart Y gesture. |
| 400 ms elapse with travel below 10 px | Enter inspection visibly even before movement; subsequent motion scrubs; release pins. Same rule when the contact starts on the cursor. |
| Second contact in same plot during pending hold | Cancel hold and tap; begin two-finger navigation if enabled. |
| Second contact during pan | Carry the current provisional pan into navigation's baseline; final navigation release commits the composed transform once, never an intermediate pan commit or visual jump. |
| Second contact during scrub | Abandon provisional inspection, restore pre-gesture committed pin, begin navigation from the committed viewport if enabled. |
| Second contact outside the plot | Cancel the local gesture; suppress its trailing click until all contacts lift. Never combine contacts from different plots. |
| Two-finger movement | Pan/zoom X about moving midpoint using Euclidean distance; cross-panel contacts permitted; preserve pin in data coordinates. |
| First of two contacts lifts | Commit at most once if movement exceeds existing meaningful-change tolerances. Drain remaining contacts; never begin a new gesture from the remaining finger. |
| Third contact | Cancel uncommitted gesture and provisional state, restore pre-gesture pin/view; drain all contacts. |
| pointercancel, unexpected lost capture, resize/rotation/invalid CTM, owner disposal | Cancel without new commit/click; invalidate old callbacks, clear timers/observers/capture and previews, restore committed state where still mounted. |
| Pointer leaves chart while owned capture remains valid | Continue recognized pan/scrub; scrub pointer may clamp to the plot edge. This never clamps an existing offscreen data-coordinate selection. Pending unrecognized hold leaving the plot cancels. |

Expected capture release after an intentional terminal transition is not a second cancellation.
The all-contacts-up condition is explicit; clear local state on window blur/page loss as well so
a missed pointerup cannot strand the chart. Each terminal path is idempotent.

`Owned` emits `touch-action: none` on the existing block nested plot-area SVG before contact.
`ScrollY` emits `pan-y` when chart pinch is enabled, otherwise `pan-y pinch-zoom`; cancellation
by the browser restores the pin. Labels/strips and the page outside the viewport remain outside
library ownership. No promise that native scrolling is irrevocably disabled by a recognized hold
under ScrollY. Physical browser behavior remains integration acceptance, not synthetic proof.

## Truthful feedback and acknowledgement

Keep a lightweight local provisional line/hold indicator, visually distinct from a committed
cursor. Hide stale timestamp and sample markers while that provisional position is unmatched.
During navigation hide stale ticks/grid on affected panels, preserve layout/axis titles, show
pending feedback, and preview marks and committed pin together. No client-side tick generation.
AutoFitY may update only with the committed frame; do not display stale Y ticks as live.

Expose a documented client-local state attribute on the Plot root (for example
`data-ggnet-interaction="inspecting|navigating|pending|unavailable"`, absent when settled).
Provide a demo/README host CSS recipe that hides stale readout values and shows a pending label
immediately through a containing element selector. A server EventCallback alone cannot supply
this immediate guarantee. Existing `ReadingsChanged` continues to carry the actual position of
its values; never relabel old values as the provisional position. No public preview data-value
API or parallel selection state is required for this phase.

Replace timeout/any-mutation settlement with an operation identity: mounted-session identity,
monotonic operation revision, and committed view/frame identity. Commands and rendered
acknowledgements carry enough identity to distinguish cursor updates, navigation, restoration,
host Clear/viewport updates, telemetry-only frames and remounts. Acknowledgement must describe
the state rendered, not merely completion of the interop method. Linked panels clear previews
only against their matching rendered frames; unrelated mutations cannot settle them.

Serialize/coalesce provisional requests, preserve terminal pin/restore ordering, reject stale
session/revision callbacks, and ensure a newer gesture cannot be overwritten by older responses.
A later gesture during pending navigation composes against the intended viewport rather than
the old server geometry. Host Clear/viewport replacement invalidates superseded pending gestures;
incoming samples alone retain pin and intended viewport. Callback reentrancy must be considered
at awaits; an old readout must not become the last published state after a newer operation.

Do not silently drop a valid preview at 1500 ms. At 300 ms and 2 s latency it remains pending
until the matching frame. On an explicit interop failure/disconnect, remove transient transforms
and indicate unavailable while showing the last acknowledged state; do not claim an unacknowledged
commit failed or succeeded. Use a bounded watchdog (initially 10 s) for an indefinitely unresolved
call: abandon transient display into unavailable, quarantine the old session's responses, and
require resynchronization or remount before accepting new gestures. Late server application must
be reconciled from authoritative state, never reversed by a guessed rollback. This is a recovery
bound, not permission to treat elapsed time as acknowledgement.

The exact render-acknowledgement seam is an execution exploration inside STEP-03: existing
RenderModeHandler refresh completion alone has not been proved to identify DOM delivery. Prove
the seam with delayed frames before relying on it. If it requires a general rendering redesign,
stop for a scope decision instead of broadening the work.

## Acceptance and evidence

| ID | Criterion | Required evidence |
|---|---|---|
| ACCEPTANCE-01 | Explicit touch policy separates navigation, inspection and browser ownership; default migration uses the new contract; init-only and static behavior retained. | Component policy/capability matrix, option validation, API manifest review, relevant headless goldens and migration docs. |
| ACCEPTANCE-02 | Tap pins shared X without activation/reset; immediate drag pans; 400 ms hold within 10 px visibly recognizes and then scrubs/pins; cursor hits use same rule. | Fail-first then passing enabled browser cases around threshold/tolerance, stationary hold, double taps and event counts. |
| ACCEPTANCE-03 | Two-finger navigation works with inspection disabled and across panels; moving midpoint and Euclidean pinch compose one viewport commit and preserve the pin. | bUnit window math/AutoFitY tests and browser same/cross-panel pan/pinch, including vertically separated contacts. |
| ACCEPTANCE-04 | Second/third contact and every cancellation/lifetime path restore or drain deterministically without trailing selection, double commit, orphan handler or timer. | Browser transition matrix: pending hold/pan/scrub, third contact, cancel/lost capture, outside pointer, blur, resize/rotation and remount/disposal. |
| ACCEPTANCE-05 | Owned and ScrollY policies are installed before contact, limited to the plot area, and browser cancellation restores inspection. | Component markup assertions, browser computed-style/native-scroll cases and cancellation tests; disclose Chromium versus physical Safari limits. |
| ACCEPTANCE-06 | Selected data coordinate survives navigation, telemetry and context refresh; outside-view selection is explicit; Clear and reset retain separate meanings. | Existing inspection/lifetime tests plus overlapping gesture/host-command and telemetry cases; demo Clear/All/expand-close state transfer. |
| ACCEPTANCE-07 | Provisional cursor, readout and navigation never present stale values as current at 300 ms/2 s delay; latest intended operation wins over old frames; no stuck transforms after disconnect/watchdog/disposal. | Controlled browser invocation and frame-delay cases plus component revision-order tests; inspect pending visuals/screenshots and callback counts; fail-first evidence for former 1500 ms timeout. |
| ACCEPTANCE-08 | Mouse, pen and keyboard retain deliberate existing semantics; unsupported layouts remain explicit; beta touch migration is documented. | Existing mouse/keyboard/component tests, separate pen browser cases, supported/unsupported capability tests, README/XML/scoped guide review. |
| ACCEPTANCE-09 | Demo exercises the same configuration in overview/detail-like views with visible All/Clear/Expand/Close, inspection-disabled navigation, Owned and ScrollY; all required local checks pass. | Four repository gates plus `GGNET_E2E=1 dotnet test tests/GGNet.E2ETests`; record executed/skipped counts. No physical-device claim. |
| ACCEPTANCE-10 | Final independent Claude audit of gesture lifecycle and delayed-frame correctness is captured and challenged, with confirmed findings resolved or explicitly ruled. | Pinned dispatch/report/challenge artifacts; launch authorization first; affected checks after remediation. |

New behavioral checks must demonstrate intended failure before the fix where meaningful; record
any uninformative fail-first omission. Existing snapshots are reviewed, never repinned reflexively.
Delay tests must distinguish delaying JS invocation from delaying server acknowledgement/DOM
application; one cannot stand in for both. Use real Chromium touch input for browser behavior and
clearly label injected pointer cancellation/pen events. Fixture diagnostics should retain server
output on circuit errors while testing these paths; do not investigate D-06 without recurrence
or the evidence-triggered scope decision required by repository policy.

Required final commands: `dotnet build GGNet.slnx -warnaserror`, `dotnet test GGNet.slnx`,
`dotnet format whitespace GGNet.slnx --verify-no-changes`,
`dotnet format style GGNet.slnx --verify-no-changes`, and explicitly enabled E2E above.
Read the troubleshooting guide before investigating any failed/hung gate. Physical iPhone Safari
and kiosk acceptance, along with consumer page-zoom policy, remain separate integration work.
