# Why this document exists

Dispatch pre-work for a GGNet touch-interaction refactor, arising from physical use of Ziggy Brew's multi-panel fermentation charts. The mechanics passed QA, but the interaction model obstructs the primary task: comparing readings at one time on the root chart. This is ideation and audit input, not an approved implementation plan. The next session should reconcile the current tree, settle the remaining API/behavior choices, and produce a bounded implementation plan.

## Provenance and authority

- Session: 2026-11-14, GGNet and Ziggy Brew touch audit and refactor discussion.
- Source baseline: GGNet `7fd4b8e06098fb8cc730545c635a1770506bcc71`, including time-cursor inspection. The standalone repository matched the audited consumer submodule when this input was written.
- Independent read-only audits: GPT-6 Astra and Claude Fable 5.2. Fable session: `ca94b6c8-962c-4f9d-94d5-ef2a0ebc22f4`. Neither audit ran new tests or physical checks.
- Read GGNet's own `AGENTS.md` and scoped guides before implementation. Consumer conventions do not govern this repository. No implementation, staging, commits, deployment, or history mutations are authorized by this persistence request.
- User explicitly requested familiar iOS touch behavior, not imitation native-app presentation. Phone is a web interface; kiosk should use tablet/iPad-sized ergonomics.
- User explicitly wants no page-level zoom on kiosk, phone, or other Portal touch surfaces. Intentional chart-domain zoom is distinct. Global browser policy belongs to the consumer, not the library.
- Accessibility is outside this personal-consumer phase; distribution would warrant dedicated work. This is a scope boundary for this request, not a permanent GGNet policy.
- User rejected blanket conversion of web dialogs into sheets. No sheet/swipe-dismiss work belongs here.
- The gesture contract below is the assistant's latest recommendation, informed by both audits. The user requested its persistence for dispatch; do not represent every proposed behavior as separately ratified.

## Problem and current inventory

The Portal overview configures `Pan = true`; detail adds `Crosshair = true`. As a result, the same gesture changes meaning when a panel opens.

| Input or capability | Overview today | Detail today |
|---|---|---|
| Tap | Delayed panel activation/expansion | Select and pin shared time |
| One-finger horizontal drag | Pan X | Scrub inspection; pin on release |
| Two-finger pan/pinch | Browser/page pinch permitted | Chart X pan/pinch |
| Double tap | Reset competes with 200 ms activation delay | Two selections; touch double-click reset suppressed |
| Hold | No dedicated recognizer | No dedicated recognizer |
| Vertical drag | Native `pan-y` permitted | Native `pan-y` permitted |
| Inspection readings | No inspected series registered | Registered and displayed |
| Explicit reset | No visible All control | No visible touch reset |

Code entry points, relative to this repository (line numbers are audit-baseline hints; revalidate):

- `src/GGNet/InteractivityOptions.cs`: current interactivity configuration; find current location if moved.
- `src/GGNet/Components/PanelInterop.cs:14`: two-finger navigation coupled to shared crosshair; touch-action matrix.
- `src/GGNet/Components/Panel.razor.cs:233`: activation delay and suppression when inspection is enabled; double-click reset around line 339.
- `src/GGNet/Components/Panel.razor.js:119`: one-finger pan; shared inspection around 611; touch double-click suppression around 631; scrub around 641; second-contact transition around 653; two-finger navigation around 773; cursor preview around 1040; handoff to browser around 1276.
- `src/GGNet/Components/Plot.razor.cs:36`: existing host-bound `Inspection` state; do not invent a second pin model.
- `src/GGNet/Components/CrosshairCursor.cs`: rendered cursor, timestamp label, and client preview relationship.
- `tests/GGNet.Components.Tests/`: discover `TouchPolicyTests`, `PanelActivationTests`, `TwoFingerNavigationTests`.
- `tests/GGNet.E2ETests/TimeCursorTouchTests.cs`, `TouchDemoTests.cs`, and `Fixtures/DemoAppFixture.cs`: gesture coverage and Chromium-only browser baseline.

Consumer references in `/Users/pablo/source/ziggy-brew`:

- `source/ZiggyBrew.Portal/Components/UI/Graph/FermentationCharts.razor{,.cs}`: overview/detail settings, readout, transitions and inspected layers.
- `source/ZiggyBrew.Portal/Components/Pages/ChartCalibration.razor`: currently misleading universal double-tap-reset instruction.
- `source/ZiggyBrew.Portal/Components/Layout/MainFixed.razor`: current chart hosts use fixed, non-scrolling layouts.
- `source/ZiggyBrew.Portal/Components/App.razor` and `os/config/kiosk/browser.sh`: consumer browser policy.
- `inputs/portal-ui/HANDOFF.md` and `os/HANDOFF.md`: recorded physical QA.

## Recommended GGNet outcome

Allow one coherent gesture contract on overview and detail without making cursor visibility determine navigation behavior. A host must be able to enable chart pinch without enabling inspection, and choose one-finger navigation independently of crosshair rendering.

| Input | Proposed first-refactor behavior |
|---|---|
| Tap plot | Select and pin shared-axis time; no panel activation in this policy |
| Immediate horizontal drag | Pan X |
| Hold, then drag | Enter inspection with visible recognition feedback; scrub; release pins |
| Two-finger translation/pinch | Pan/zoom X about moving finger midpoint; preserve committed selection |
| Second contact during scrub | Abandon provisional scrub, restore pre-gesture pin, transition to navigation |
| Two contacts become one | Finish navigation once; remaining contact does not start another gesture until all lift |
| Cancellation | Remove provisional state and restore pre-gesture state; no stray click or commit |
| Double tap | Leave unassigned as a separate shortcut in the first refactor; ordinary tap selection still applies |

Latest recommendation deliberately defers double-tap zoom. Both audits proposed a full-range/2× toggle but disagreed about whether the first tap changes the pin. Visible All gives recovery without that extra ambiguity. Do not implement either audit's double-tap variant by accident.

For today's fixed Portal chart pages, recommend that the plot area own touch from pointer-down (`touch-action: none`). For reusable GGNet, retain an explicit policy for consumers that need native vertical scrolling from within a chart. Under that policy, inspection can be cancelled by the browser; do not promise irrevocable ownership after a hold.

## Implementation boundaries and priorities

1. Separate navigation and inspection policy in the public configuration. Prefer a small coherent API over independent switches that permit contradictory states. Exact names, defaults, and migration behavior remain design work.
2. Introduce predictable hold/pan/scrub/two-contact arbitration, preserving current cross-panel shared-X behavior and cancellation machinery where sound.
3. Keep feedback truthful while Blazor Server responses lag. Audits found client cursor motion can carry an old timestamp label while readings await the server; navigation transforms marks while ticks/grid remain stationary; previews can expire around 1500 ms and snap back before a late frame. Address or explicitly bound these behaviors in the plan. Never imply stale values describe the live preview position.
4. Exercise the contract in GGNet's demo and meaningful tests. Preserve mouse/pen behavior deliberately rather than allowing touch changes to alter them accidentally.

Existing APIs already support host-bound inspection, inspection readings, and programmatic viewport changes. Portal should use these for selection persistence and visible controls. Panel action slots or published panel geometry are optional enhancements, not prerequisites: the consumer can place labeled expand controls outside SVG.

Out of scope for this GGNet dispatch: Portal implementation, kiosk flags, root CSS or viewport changes, Wi-Fi diagnosis, duration controls, dialog presentation, momentum/inertia, range presets, device deployment, unrelated rendering rewrites.

## Consumer follow-up, separate dispatch

Portal must add overview inspected-series registrations and a stable compact readout; turning on Crosshair alone is insufficient. It should provide explicit Expand, Close, All and Clear controls. Recommended state semantics: expansion/close preserve both selected time and viewport; Clear removes selection only; All resets viewport only; an offscreen selection remains explicit rather than clamped to an edge.

Preserving selection across close intentionally reverses the previously QA-accepted behavior of reopening detail empty. Record that change in consumer acceptance; it is not an existing library failure.

Portal-wide work remains separate: page-zoom suppression with real Safari/kiosk checks; explicit duration units instead of delayed double-tap acceleration; hold-to-repeat; reliable pressed/pending feedback and scroll cancellation. Keep responsive web dialogs.

## Open choices for planning

- Public policy shape, defaults, and compatibility for existing GGNet consumers during the beta breaking window.
- Hold threshold and movement tolerance; approximately 400 ms and existing 10 px touch slop are audit suggestions, not agreed constants.
- Whether dragging the existing cursor immediately scrubs or follows the general hold rule. Default recommendation: keep one rule initially.
- Third-contact behavior: audits do not agree on a prescribed outcome. Define and test deterministic ignore or cancellation, never an accidental extra commit.
- Exact preview/pending API and how delayed/out-of-order server frames reconcile with the final gesture.
- Whether complete tick/grid preview is necessary for this change or a separately bounded follow-up. Avoid silently expanding into a rendering redesign.
- Pinch distance: current calculation is Euclidean even though only X scales. Preserve useful cross-panel pinching deliberately; do not accidentally replace it with horizontal distance only.

## Acceptance and verification

Existing physical evidence covers kiosk detail tap inspection, scrub, two-finger navigation with stable pin, Clear preserving viewport, close carrying viewport and reopen empty. It does not establish iPhone Safari behavior or the proposed contract. Audits did not execute tests.

Required scenarios for the new contract:

1. Tap selects the same shared-axis time across all linked panels without invoking panel activation.
2. Horizontal drag pans; hold visibly enters inspection before dragging; release pins. Same configuration behaves identically in overview-like and detail-like demos.
3. Chart pan/pinch works with inspection disabled. Enabling inspection does not silently remap configured navigation.
4. Two fingers on different panels navigate one shared X viewport about their moving midpoint; lifting one finger produces at most one commit and no trailing selection.
5. Second-contact transitions during pending hold, pan, and scrub have explicit outcomes. Test third contact, pointer cancellation, lost capture, leaving the chart, resize/rotation, and disposal.
6. Owned-touch mode does not yield plot gestures to browser navigation. Scroll-permitting mode allows native scrolling and restores prior inspection on browser cancellation.
7. Selection remains a data-coordinate value through pan/zoom and incoming telemetry; outside-view selection is not silently moved.
8. Under 300 ms and 2 s server delay, and circuit interruption, provisional feedback never presents stale readings as current or leaves stuck transforms; final gesture wins over stale replies.
9. No new double-tap shortcut, delayed tap arbitration, or unexpected activation/reset in this policy.
10. Mouse and pen regression behavior is checked separately; existing touch consumers receive documented defaults/migration guidance.

Follow repository gates: `dotnet build GGNet.slnx -warnaserror`, `dotnet test GGNet.slnx`, and both whitespace/style format verification. Browser tests self-skip unless `GGNET_E2E=1`; report explicitly whether they executed. Existing Chromium tests cannot certify Safari behavior. Real iPhone and kiosk acceptance belongs to the integration follow-up; do not claim it from synthetic pointer events alone.

## External references and interpretation

- [Apple HIG: Gestures](https://developer.apple.com/design/human-interface-guidelines/gestures.md): consistency, immediate feedback, visible actions alongside shortcuts.
- [Apple HIG: Charts](https://developer.apple.com/design/human-interface-guidelines/charts.md): chart inspection and usable plot-area interaction.
- [Apple Stocks on iPad, iPadOS 15](https://support.apple.com/en-kz/guide/ipad/stocks-iph1ac0b1bc/15.0/ipados): app-specific hold inspection and two-finger comparison, not a universal navigation contract. Fable could not retrieve the current iPhone article body.
- [WWDC23 Swift Charts interactivity](https://developer.apple.com/videos/play/wwdc2023/10037/): another Apple chart precedent, not a requirement to copy its range gestures.
- [Pointer Events Level 3](https://www.w3.org/TR/pointerevents3/): touch-action ownership is decided before a hold; changing it mid-gesture cannot reclaim native scrolling.
- [WebKit iOS 10 interaction changes](https://webkit.org/blog/7367/new-interaction-behaviors-in-ios-10/): viewport scale metadata alone is insufficient to establish the consumer's no-page-zoom policy.
- [WebKit issue 238543](https://bugs.webkit.org/show_bug.cgi?id=238543): audit-raised browser caveat; verify applicability on target Safari rather than assuming all versions are affected.

Pin-on-release, two-finger chart navigation, and the proposed pan/hold division are deliberate product choices inspired by familiar gestures. They are not asserted to be universal iOS chart behavior.

## Suggested dispatch prompt

> Read `inputs/touch-interaction-refactor.md` and GGNet's repository instructions. Reconcile the audited baseline with the current tree. Produce a bounded implementation plan for independent inspection/navigation policy, consistent tap/pan/hold-scrub/two-finger gestures, and truthful feedback under server delay. Treat the document as pre-work: distinguish explicit user constraints from recommendations and resolve the listed design choices. Keep Portal changes and deployment out of scope. Leave double-tap shortcuts deferred. Identify meaningful component/browser tests, compatibility implications and physical-verification limits. Do not stage, commit, or mutate repository history.
