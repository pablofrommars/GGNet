# Time-cursor inspection

- Profile: Task
- Status: completed
- Cadence: continuous
- Spec context:
  - @specs/time-cursor-inspection.md

## Contents

- [Steps](#steps)
- [Acceptance](#acceptance)
- [Deferred issues and accepted gaps](#deferred-issues-and-accepted-gaps)
- [Gates](#gates)
- [Authority](#authority)
- [Decisions](#decisions)
- [Assumptions and open questions](#assumptions-and-open-questions)
- [Material notes](#material-notes)

## Steps

| ID | Checkable result | Affected surfaces | Acceptance | Verification |
|---|---|---|---|---|
| ✅ STEP-01 · completed | The DSL carries series configuration: an `Inspect<T>` record, the lookup-policy enum (nearest, previous, step, containing) and an `inspect:` parameter on every overload of `Geom_Line`, `Geom_Point`, `Geom_Area` and `Geom_Bar` (18 overloads), stored on the geom. It has no effect on any output. | `src/GGNet/BuilderExtensions.{Line,Point,Area,Bar}.cs`, `src/GGNet/Geoms/{Line,Point,Area,Bar}/*.cs`, new public types under `src/GGNet/`, `tests/GGNet.Headless.Tests` (overload and forwarding tests, API snapshot) | ACCEPTANCE-11, ACCEPTANCE-18 | Done 2026-11-13 on `HEAD` `5a5e90e` plus the uncommitted tree. `InspectSurfaceTests` (42 cases): one theory entry per overload, checked against reflection (18), each handing its `inspect:` to the geom and each rendering the same SVG with and without it; removing one forward made exactly its case fail. `dotnet test --project tests/GGNet.Headless.Tests` 1400/1400, no golden changed; the API snapshot gained exactly `GGNet.Inspect\`1` and `GGNet.InspectPolicy`. Solution build `-warnaserror` 0 warnings; both `dotnet format` checks exit 0. |
| ✅ STEP-02 · completed | A readings engine, independent of rendering: per-series index sorted by x, rebuilt when the data changes; one series per `colorBy`/`fillBy` group; nearest, previous and step lookups with typed maximum distance (`Duration` on `Instant`, the value type on numeric x); containing lookup on bars; NaN y skipped; a reading model carrying series, group, cursor time, sample time, kind and formatted value, with "missing" text. | new internal types under `src/GGNet/`, the reading model's public types, `tests/GGNet.Headless.Tests`, API snapshot | ACCEPTANCE-01, ACCEPTANCE-03, ACCEPTANCE-04, ACCEPTANCE-15 | Done 2026-11-13 on `HEAD` `5a5e90e` plus the uncommitted tree. `InspectReadingTests` (38 cases) through the DSL on numeric and `Instant` x: each policy at, between, before and beyond samples; the distance and age boundaries; two unevenly sampled series at one cursor; a NaN pen lift; grouped series and their renaming; bars inside, at the edge of, between and outside extents; a bar under `Step`; the limit that does not fit the axis; the index following appended data. Shifting the age boundary and the bar extent made four cases fail. Headless 1438/1438, no golden changed; the API snapshot gained exactly `GGNet.Reading\`1` and `GGNet.ReadingKind`. Solution build `-warnaserror` 0 warnings; both `dotnet format` checks exit 0. |
| ✅ STEP-03 · completed | `Plot` owns the inspection state: `@bind-Inspection` (x-domain value plus pinned state), `ClearInspectionAsync`, a separate "readings changed" notification, readings recomputed on data change, state kept across `Context` replacement, the outside-view flag, and the initialization error for an inspected series on `LocalDateTime` or discrete x under `Crosshair`. | `src/GGNet/Components/Plot.razor`, `Plot.razor.cs`, `Panel.razor.cs`, public inspection types, `tests/GGNet.Components.Tests`, API snapshot | ACCEPTANCE-06, ACCEPTANCE-07, ACCEPTANCE-09, ACCEPTANCE-17 | Done 2026-11-13 on `HEAD` `5a5e90e` plus the uncommitted tree. `PlotInspectionTests` (15 cases, bUnit): a host-supplied position is read and not echoed back; the host moves it by changing the parameter; clearing reports the position and the empty readout once; appended data changes the readings and raises no position change, in both interactive render modes; a refresh that changes no reading reports nothing (removing the comparison made exactly this case fail); a new `Context` keeps the position; a window excluding the position flags it and keeps its readings; `LocalDateTime` and discrete x throw under `Crosshair` and mount without it; a limit that does not fit the axis throws at mount and on a replacement context. Components 137/137, headless 1438/1438; the API snapshot gained exactly `GGNet.Inspection\`1` and `GGNet.InspectionReadings\`1`; build `-warnaserror` 0 warnings; both format checks exit 0. |
| ✅ STEP-04 · completed | The crosshair moves client-side and spans the plot, on the mouse path: hover shows an unpinned line and x label in every participating panel; a click pins; with `Pan` a drag pans and does not pin. Position requests are latest-wins with one exact final request. Sample markers sit at actual sample positions. `LocalDateTime` and discrete x snap as before; `freeX` and flipped panels keep a local transient line. The per-strip server round trip is gone. | `src/GGNet/Components/Panel.razor`, `Panel.razor.cs`, `Panel.razor.js`, `PanelInterop.cs`, `src/GGNet/Themes/Default.css`, `tests/GGNet.Components.Tests/CrosshairTests.cs` (rewritten), `tests/GGNet.Headless.Tests/ThemeContractTests.cs`, `tests/GGNet.E2ETests` | ACCEPTANCE-02, ACCEPTANCE-03, ACCEPTANCE-05, ACCEPTANCE-10, ACCEPTANCE-16 | Done 2026-11-13 on `HEAD` `5a5e90e` plus the uncommitted tree. `CrosshairTests` rewritten (12 bUnit cases): the cursor is emitted only under the opt-in and starts hidden, the hover strips are gone, the module is told `shared`/`continuous`, hover and pin positions, leave rules, one value in all three stacked panels from any origin, the clip per panel, a marker at its sample, no marker for a missing reading, discrete snapping, `freeX` local and never pinned. `TimeCursorTests` mouse path (13 browser cases on the new `/time-cursor` demo page): one line per panel at the same time from any origin, click pins and hover no longer moves it, leave ends an unpinned cursor, a burst of sixty moves sends exactly the first and the last position, a moved pin never shows at its old position, a drag with `Pan` pans and does not pin, a marker on its gravity sample with the distance named, the missing series, the plain crosshair, discrete snapping, `freeX`. Disabling the hold after a pin made exactly the trace case fail. |
| ✅ STEP-05 · completed | One-finger touch: a tap pins the tapped time, a horizontal drag scrubs and release pins, a vertical drag scrolls the page. The viewport is rendered under `Crosshair` with `pan-y pinch-zoom`. No tap or scrub pans, resets, fires a mark callback or an ancestor click; a touch double-tap does not reset; `CursorTooltip` still opens the nearest mark's tooltip on tap and a scrub closes it. | `src/GGNet/Components/Panel.razor`, `Panel.razor.js`, `PanelInterop.cs`, `tests/GGNet.E2ETests` (new cases; the crosshair cases of `TouchDemoTests.cs` rewritten), `src/GGNet.Demo/Components/Pages/Touch*.razor*` only if a rewritten case needs it | ACCEPTANCE-05, ACCEPTANCE-08, ACCEPTANCE-11 | Done 2026-11-13, same tree. `TimeCursorTests` touch path (10 browser cases, real touch input through DevTools at 1185×1920): a tap pins in every panel, with and without drift; a horizontal drag scrubs every panel under the finger and pins where it ends, with no pan and no page scroll; a vertical drag scrolls the page and selects nothing; a second finger abandons the scrub and restores the previous pin or clears, and lifting one of two fingers starts nothing; a tap on a mark pins without reaching the mark or an ancestor while a mouse click still reaches it; a double tap selects twice and does not reset; without `Pan` the computed `touch-action` is `pan-y pinch-zoom`. Letting taps propagate and disabling the restore made exactly their cases fail. Full browser run: 120/120 executed and passed, the four prior-regression cases included. Older cases changed, all about the crosshair: `TapNearAMark…` and `TapOpensTheTooltip…` now expect the line under the finger; `InspectingAnotherChart…` expects the pin to stay; the layout page's nested, dialog and remount charts dropped `Crosshair` so their one-finger pan cases run unmodified. Components 149/149. |
| ✅ STEP-06 · completed | Two-finger navigation under `Crosshair` with `Pan`: the viewport becomes `pan-y`; a second finger abandons a scrub and restores the previous pin; a two-finger horizontal drag pans and a pinch zooms x about the midpoint, previewed client-side in every panel sharing x and committed once on release; `AutoFitY` refits on commit; lifting one of two fingers starts nothing. | `src/GGNet/Components/Panel.razor.js`, `Panel.razor.cs` (pinch commit), `PanelInterop.cs`, `tests/GGNet.E2ETests`, `tests/GGNet.Components.Tests` | ACCEPTANCE-08, ACCEPTANCE-14 | Done 2026-11-13 on `HEAD` `5a5e90e` plus the uncommitted tree. Browser (`TimeCursorNavigationTests`, two contacts through DevTools touch input): a two-finger drag previews on all four panels before release, commits one `Gesture` change, the pinned line rides with the marks and keeps its time, and lifting the fingers one after the other selects nothing; a pinch of 2 and of 0.8 scales the window by that factor about the fingers with the pin still under them; two still fingers commit nothing; the computed `touch-action` is `pan-y` with `Pan`. Dropping the pinch factor from the commit and the line's ride-along made exactly those three cases fail. bUnit: `TwoFingerNavigationTests` (13 cases: commit arithmetic, pin kept, option guards, meaningless scales, `AutoFitY` in the same change) and the touch-policy theory (4). Full browser run 125/125; components 165/165; solution `dotnet test` green except `GalleryTests.SelfContained` (GATE-05); both format checks exit 0. |
| ✅ STEP-07 · completed | Keyboard: a plot with `Crosshair` is focusable; Left and Right step the cursor to the previous and next sample time across all inspected series; Escape clears. | `src/GGNet/Components/Plot.razor`, `Plot.razor.cs`, `Panel.razor.js` only if key handling lives there, `tests/GGNet.Components.Tests`, `tests/GGNet.E2ETests` | ACCEPTANCE-09, ACCEPTANCE-10 | Done 2026-11-13 on `HEAD` `5a5e90e` plus the uncommitted tree. `CrosshairKeyboardTests` (7 bUnit cases): `tabindex` only under `Crosshair`; Right and Left walk the union of both series' sample times in order; a step past the last sample stays; a step from between samples lands on the neighbour; with nothing inspected the first step enters from the edge of the view; Escape clears; no inspected series, nowhere to step. Browser: `AfterAClickTheArrowsStepThroughSampleTimesAndEscapeClears`, which failed (keys never arrived) until the module focused the plot on mouse-down, because the pan handler's `preventDefault` suppresses focus. Components 172/172. |
| ✅ STEP-08 · completed | Demo page for the feature with generated data: three time-aligned panels, a measured series with a step target, uneven sampling, a series that goes missing, activity bars, live append, a readout above the panels, clear and reset controls, `Pan` on. Demo-level browser cases at the device viewport (1185×1920, touch). | `src/GGNet.Demo/Components/Pages/` (new page), `src/GGNet.Demo/Data/`, demo navigation, `tests/GGNet.E2ETests` | ACCEPTANCE-01, ACCEPTANCE-02, ACCEPTANCE-03, ACCEPTANCE-04, ACCEPTANCE-06, ACCEPTANCE-07, ACCEPTANCE-09, ACCEPTANCE-15 | Done 2026-11-13, same tree. `/time-cursor` in `src/GGNet.Demo`: four time-aligned panels over 24 generated hours (temperature with a stepped target and an ambient probe that stops reporting, gravity sampled irregularly, pressure, cooling intervals as bars), no two series sampled at the same instant, a fixed-height readout above the chart, Clear, Reset view, Append readings and New context, `Crosshair` with `Pan`; plus a crosshair without readout or `Pan`, a discrete axis and a `freeX` facet. `TimeCursorLifetimeTests` (5 browser cases at 1185×1920): appended readings change the readout under a pin that does not move; a pin left outside the window keeps its readout, hides its line and returns with Reset; resizing and a new context keep the pin; only Clear ends a pin and a returning page starts clean with one hover and one pin reported once; the cooling bars read `on` inside an interval and `off` between. Looked at once by eye in a screenshot, which is how the undrawn x label was found (a nested `<text>` element); `AHoverInAnyPanel…` now asserts the label has width and failed against the old build. Full browser run 131/131. |
| ✅ STEP-09 · completed | Documentation matches the code: Blazor guide §5 (crosshair contract, gestures, touch policy, latest-wins requests), `README.md`, `skills/ggnet` where it describes interactivity and geoms, the demo README; the `Crosshair` option's `///` text. | `.github/instructions/blazor.instructions.md`, `README.md`, `skills/ggnet/**`, `src/GGNet.Demo/README.md`, `src/GGNet/InteractivityOptions.cs` | ACCEPTANCE-18 | Done 2026-11-13 on `HEAD` `5a5e90e` plus the uncommitted tree. Blazor guide (component list, parameters, the self-rendering overlays, the JS-invokable list, touch tap and click rules, touch policy, new sections "The crosshair" and "Readings", the landed tiers); DSL guide §2 (`inspect:`); `README.md` section "Crosshair and readout" with four consumer cautions; `skills/ggnet/reference/geoms.md`; demo README (the page and what changed on `/touch-layout`); the `Crosshair` and `Pan` option docs. Each claim was checked against the code as built; the README example compiles and renders under `dotnet run skills/ggnet/scripts/validate.cs`; the eval and skill-consistency tests pass in the solution run. |
| ✅ STEP-10 · completed | All local gates green on the final tree. | read-only | ACCEPTANCE-11, ACCEPTANCE-12 | Done 2026-11-13 on `HEAD` `5a5e90e` plus the uncommitted tree, after the re-pin of DECISION-11: `dotnet build GGNet.slnx -warnaserror` 0 warnings; `dotnet test --solution GGNet.slnx` 1799 total, 1670 passed, 129 browser cases skipped by design, 0 failed; `dotnet format whitespace` and `style` `--verify-no-changes` exit 0; `GGNET_E2E=1 dotnet test --project tests/GGNet.E2ETests` 131/131 executed and passed; `git-flow-tests.sh` 100 checks. The earlier run of the same day had one failure, the golden of GATE-05. |
| ✅ STEP-11 · completed | Independent audit (DECISION-10): dispatch persisted with the target pinned by content hashes, launch released through GATE-04, report captured, every finding challenged. Confirmed findings enter as new steps and the gates of STEP-10 are rerun after them. | `plan/time-cursor-inspection/audits/` | ACCEPTANCE-12 | Done 2026-11-13. `audits/audit-01-dispatch.md` (65 files pinned by SHA-256), launched under the grant in Authority through the guarded launcher; `audits/audit-01.md` captured, auditor Codex `gpt-6-astra`, read-only, all 65 hashes verified by the auditor at start and end; it executed nothing and read 21 files by diff or in part. Five findings, all confirmed in `audits/audit-01-challenge.md`; each is a step below (STEP-14 to STEP-18), and STEP-19 reruns the gates. |
| ✅ STEP-14 · completed | FINDING-01: the `Containing` lookup measures a bar's extent in the scale's mapped space, as the bar is drawn, so it agrees with the drawing on a log x axis. | `src/GGNet/Readings/SeriesIndex.cs`, `src/GGNet/Geoms/Bar/Bar.cs`, `tests/GGNet.Headless.Tests/InspectReadingTests.cs` | ACCEPTANCE-15 | Done 2026-11-13. `OnALogAxisContainingFollowsTheBarAsItIsDrawn` (8 cases on `Scale_X_Log10`, decades at half width): 5 failed on the audited code, all pass after. The linear and `Instant` containment cases still pass. |
| ✅ STEP-15 · completed | FINDING-02: a new touch contact clears the click block left by the gesture before it, so a tap right after a scrub or a two-finger gesture pins. | `src/GGNet/Components/Panel.razor.js`, `tests/GGNet.E2ETests` | ACCEPTANCE-08 | Done 2026-11-13. `ATapRightAfterAScrubPinsWhereItLands` (browser): failed on the audited module, passes after. |
| ✅ STEP-16 · completed | FINDING-03: a colour, line-type or fill group whose samples are all NaN keeps its series and reads as missing. | `src/GGNet/Readings/SeriesIndex.cs`, `tests/GGNet.Headless.Tests/InspectReadingTests.cs` | ACCEPTANCE-04 | Done 2026-11-13. `AGroupWithNoValidSampleStillReadsAsMissing`: failed on the audited code, passes after. |
| ✅ STEP-17 · completed | FINDING-04: Left and Right step through the sample times of every line, point, area and bar layer of a crosshair plot, with or without `inspect:`. Docs say the same. | `src/GGNet/Geoms/Geom.cs`, `src/GGNet/Geoms/IGeom.cs`, `src/GGNet/Components/Plot.razor.cs`, `tests/GGNet.Components.Tests/CrosshairKeyboardTests.cs`, `.github/instructions/blazor.instructions.md` | ACCEPTANCE-10 | Done 2026-11-13. `TheArrowsStepThroughPlottedSamplesWithoutAReadout` replaces the case that pinned the no-op; it failed on the audited code and passes after. `LayersThatAreNotSampledSeriesOfferNoSteps` pins that a ribbon offers none. Blazor guide corrected. |
| ✅ STEP-18 · completed | FINDING-05: a plot faceted with `freeX` never shares or pins, whatever the number of facets. | `src/GGNet/Components/Plot.razor.cs`, `tests/GGNet.Components.Tests/CrosshairTests.cs` | ACCEPTANCE-16 | Done 2026-11-13. `ASingleFreeXFacetIsStillLocalAndNeverPins` (bUnit): failed on the audited code, passes after. Blazor guide corrected. |
| ✅ STEP-19 · completed | All local gates green on the remediated tree. | read-only | ACCEPTANCE-11, ACCEPTANCE-12 | Done 2026-11-13 on `HEAD` `5a5e90e` plus the uncommitted, remediated tree: build `-warnaserror` 0 warnings; `dotnet test --solution GGNet.slnx` 1811 total, 1681 passed, 130 browser cases skipped by design, 0 failed; both format checks exit 0; `GGNET_E2E=1 dotnet test --project tests/GGNet.E2ETests` 132/132 executed and passed in four consecutive runs. The first run on this tree had one failure, in an older case; see Deferred issues. |
| ✅ STEP-12 · completed | Demo deployed to the QA device under the grant in Authority; the page on the device reports the published build identity. | device only | ACCEPTANCE-13 | Done 2026-11-13, before the engineer's pause arrived: build `5a5e90e.dirty.2bf6ed3c` published (`linux-arm64`, self-contained, Release, 121 MB) and copied to `~/ggnet-cursor-demo/app` on the device; `/time-cursor` answered 200 on `127.0.0.1:15236` and the page header reported `2.0.0-beta.0.1+5a5e90e.dirty.2bf6ed3c`, the published identity; the served `Panel.razor.js` contains the two-finger commit. A second kiosk Chromium window was opened on the page. Details and undo commands in Material notes. |
| ✅ STEP-13 · completed | Physical touch QA with the engineer, then device cleanup: tap, scrub, vertical scroll, two-finger pan and pinch, second finger during a scrub, pin under live data, readability of the readout while touching. | — | ACCEPTANCE-13 | Done 2026-11-13: the engineer ran the 14 checks on the device and reported all as expected, no findings (Material notes, one line per check). Cleanup done under the engineer's release of GATE-07: QA browser and demo stopped, `~/ggnet-cursor-demo` deleted, port 15236 closed, the kiosk's own browser running, backlight 16 as on entry. |

## Acceptance

| Criterion | Evidence |
|---|---|
| ✅ ACCEPTANCE-01 · satisfied | `ASampleMarkerSitsOnItsSampleWhileTheReadoutNamesTheDistance` and `ATapPinsTheTappedTimeInEveryPanel` (browser): one cursor time, each reading with its kind and sample note, the target as `Step` "in effect since". Lookup semantics in `InspectReadingTests`. |
| ✅ ACCEPTANCE-02 · satisfied | `AHoverInAnyPanelDrawsOneLinePerPanelAtTheSameTime` and `ATapPinsTheTappedTimeInEveryPanel` (browser, four panels, any origin): the same fraction in every panel, each line inside its own plot area, six readout rows. `APositionInAnyPanelIsDrawnInEveryPanelAtTheSameValue` and `EachLineIsClippedToItsOwnPlotArea` (bUnit). |
| ✅ ACCEPTANCE-03 · satisfied | `ASampleMarkerSitsOnItsSampleWhileTheReadoutNamesTheDistance` (browser) and `ASampleMarkerSitsAtTheSampleNotAtTheCursor` (bUnit); `EachSeriesReportsItsOwnSampleAtOneCursor` (engine). |
| ✅ ACCEPTANCE-04 · satisfied | `ASeriesWithNoNearbySampleSaysSoInsteadOfShowingADistantValue` (browser), `NearestRefusesASampleBeyondItsMaximumDistance`, `PreviousIsMissingBeforeTheFirstSampleOrPastItsMaximumAge`, `TheMissingTextIsTheSeriesOwn` (engine), `AMissingReadingDrawsNoMarker` (bUnit). |
| ✅ ACCEPTANCE-05 · satisfied | `AHorizontalDragScrubsEveryPanelAndPinsWhereItEnds` (browser: the line is under the finger before release, then pinned at the end) and `ABurstOfMovesSendsTheFirstAndTheLastPositionOnly` (sixty moves, exactly two requests, the last position final). |
| ✅ ACCEPTANCE-06 · satisfied | `AppendedReadingsChangeTheReadoutUnderAPinThatDoesNotMove` (browser) and `AppendedDataChangesTheReadingsAndNotThePosition`, `ARefreshThatChangesNoReadingReportsNothing` (bUnit). |
| ✅ ACCEPTANCE-07 · satisfied | `APinLeftOutsideTheWindowKeepsItsReadoutAndReturnsWithTheWindow` and `ResizingAndReplacingTheContextKeepThePin` (browser); `APositionOutsideTheWindowKeepsItsReadingsAndIsFlagged` (bUnit). |
| ✅ ACCEPTANCE-08 · satisfied | `ATapOnAMarkPinsWithoutReachingTheMarkOrAnAncestor`, `AHorizontalDragScrubsEveryPanelAndPinsWhereItEnds` (no view change, no ancestor click), `ADoubleTapSelectsTwiceAndDoesNotResetTheView`, `ASecondFingerAbandonsTheScrubAndRestoresWhatWasPinned` (both with and without a previous pin; lifting one of two fingers starts nothing). |
| ✅ ACCEPTANCE-09 · satisfied | `OnlyClearEndsAPinAndAReturningPageStartsCleanWithOneSetOfHandlers`, `ResizingAndReplacingTheContextKeepThePin` (browser); `ClearingReportsBothThePositionAndTheEmptyReadout`, `ANewContextKeepsThePositionAndIsReadAfresh`, `EscapeClears` (bUnit). |
| ✅ ACCEPTANCE-10 · satisfied | `AClickPinsAndTheHoverNoLongerMovesTheLine`, `LeavingThePlotAreaEndsAnUnpinnedCursor`, `WithPanAMouseDragPansAndDoesNotPin`, `AfterAClickTheArrowsStepThroughSampleTimesAndEscapeClears` (browser); `CrosshairKeyboardTests` (bUnit). |
| ⬛ ACCEPTANCE-11 · accepted exception | Behaviour without `Crosshair`: every older browser case on a chart without the crosshair passes unmodified (131/131 in total). Output: `InspectionChangesNothingThatIsDrawn` (18 overloads) and every gallery golden byte-identical except `GalleryTests.SelfContained`, re-pinned under DECISION-11 because it embeds the stylesheet, which gained the crosshair's two rules and one variable; the diff is exactly those lines. Also changed by design, not by exception: the crosshair cases the spec lists as rewritten, and three demo charts that dropped `Crosshair` (STEP-05). |
| ✅ ACCEPTANCE-12 · satisfied | STEP-19, 2026-11-13, `HEAD` `5a5e90e` plus the uncommitted tree after audit remediation: build 0 warnings; 1681 passed and 0 failed of 1811, 130 browser cases skipped by design; both format checks exit 0; browser run 132/132 executed and passed, four runs in a row. One earlier run on the same tree had one intermittent failure, recorded under Deferred issues. |
| ✅ ACCEPTANCE-13 · satisfied | Physical QA on the device touchscreen by the engineer, 2026-11-13, build `5a5e90e.dirty.2bf6ed3c`: 14 checks covering tap, scrub, page scroll, tap after scrub, second finger, two-finger pan, pinch, mark tap, double-tap, live append, off-screen pin, clear, readability and page pinch on the chart without navigation, all reported as expected. Recorded one line per check in Material notes. |
| ✅ ACCEPTANCE-14 · satisfied | `TwoFingersPanEveryPanelBeforeReleaseAndCommitOnce`, `APinchZoomsTheTimeWindowAboutTheFingers` (2 and 0.8), `TwoFingersThatDoNotMoveCommitNothing`, `WithPanThePlotAreaTakesTwoFingerGesturesFromThePage`, `WithoutPanThePlotAreaStillAllowsPagePinch` (browser); `TwoFingerNavigationTests` and the touch-policy theory (bUnit). |
| ✅ ACCEPTANCE-15 · satisfied | `ABarSeriesReadsTheIntervalThatHoldsTheTimeAndIsMissingBetweenBars` (browser); `ContainingReadsTheBarWhoseExtentHoldsTheCursor`, `ABarSeriesAlsoReadsLikeASampledOne` (engine). |
| ✅ ACCEPTANCE-16 · satisfied | `WithoutAReadoutTheLineAndItsLabelStillFollowAndPin`, `OnADiscreteAxisTheLineSnapsToTheCategory`, `WithIndependentXAxesTheLineStaysInItsPanelAndNeverPins` (browser, and their bUnit twins). `LocalDateTime` snapping runs through the same `Unmap` path as discrete and has no case of its own. |
| ✅ ACCEPTANCE-17 · satisfied | `ReadingsOnALocalDateTimeAxisAreRefusedUnderCrosshair`, `ReadingsOnADiscreteAxisAreRefusedUnderCrosshair`, `TheSameLayerMountsWithoutCrosshair`, `ALimitThatDoesNotFitTheAxisIsRefusedWhenThePlotMounts`, `AReplacementContextIsCheckedToo` (bUnit). |
| ✅ ACCEPTANCE-18 · satisfied | The API snapshot gained exactly `Inspect\`1`, `InspectPolicy`, `Inspection\`1`, `InspectionReadings\`1`, `Reading\`1`, `ReadingKind`. `OverloadConsistencyTests`, `BuilderForwardingTests` and `InspectSurfaceTests` (all 18 overloads) pass. Blazor guide §1, §3, §4, §5; DSL guide §2; `README.md` (Crosshair and readout); `skills/ggnet/reference/geoms.md`; demo README; the `Crosshair` and `Pan` option docs. The README example compiles and renders under `skills/ggnet/scripts/validate.cs`. |

## Deferred issues and accepted gaps

- Unresolved, intermittent: `TouchDemoTests.RemountingAndNavigatingAwayAndBackLeaveExactlyOneHandlerSet`
  failed once on 2026-11-13 with "an unhandled exception on the current circuit" after the page
  was left and reopened. It passed in three runs alone and in four full runs with detailed
  circuit errors on, so the exception text was never captured: about one failure in nine full
  runs that day. The chart it exercises has no crosshair since STEP-05. Not classified as new or
  pre-existing: there is no baseline run count on `5a5e90e`. One suspect in this Task's code,
  from reading only: the crosshair cursor re-renders outside the interactive handler's frame
  hand-off, so it could read a panel's scales while the render loop rebuilds them; that path
  needs an inspection to exist, which this case does not create. What would settle it: the
  server log of a failing run (the fixture keeps the demo's output only on startup failure), or
  twenty full runs on `5a5e90e`. `defer: blocked-by` → D-06
- The module does not watch for a changed viewport transform during a two-finger gesture, as the
  one-finger pan does. Physical QA (STEP-13) is where it would show.
  → closed: nothing showed in physical QA (checks 6, 7 and 11), and with `Pan` on the plot area's touch policy keeps the page from zooming under a two-finger gesture

## Gates

| ID | Gate | Owner | Blocked work | Closure |
|---|---|---|---|---|
| ✅ ~~GATE-01~~ · settled | decision — the spec's open decisions OD-1 to OD-9 | engineer | STEP-01 | ruled 2026-11-13; DECISION-01 to DECISION-09 |
| ✅ ~~GATE-02~~ · settled | decision — assurance interrogation: is an independent audit scheduled, and where | engineer | STEP-01 | ruled 2026-11-13: one audit before device QA; DECISION-10 |
| ✅ ~~GATE-03~~ · settled | authorization — deploy the demo to the QA device over SSH for physical acceptance | engineer | STEP-12 | granted 2026-11-13; Authority |
| ✅ ~~GATE-04~~ · settled | authorization — launch of the audit: repository content leaves the machine to the external auditor | engineer | STEP-11 | granted 2026-11-13; Authority |
| ✅ ~~GATE-05~~ · settled | decision — the new crosshair paint (`.crosshair.pinned`, `.crosshair-marker`, `--ggnet-crosshair-pinned`) is in `Default.css`, as the spec's constraints require. `GalleryTests.SelfContained` embeds that stylesheet, so its golden gains exactly those two rules and one variable and no drawing changes. ACCEPTANCE-11 says the goldens do not move: re-pin that one golden as an accepted exception, or paint the pinned line and markers without new theme rules | engineer | STEP-10 | ruled 2026-11-13: re-pin that one golden; DECISION-11 |
| ✅ ~~GATE-06~~ · settled | evidence — the QA device answers on the network. On 2026-11-13 `192.168.1.100` (host alias `ziggybrew`) timed out on SSH port 22 and on ping, and `ziggybrew.local` did not resolve, from this machine at `192.168.1.101` | engineer | STEP-12 | settled 2026-11-13: the device answered once the key `~/.ssh/id_ziggybrew_ed25519` was given explicitly; the default identity is refused |
| ✅ ~~GATE-07~~ · settled | authorization — the engineer paused device work on 2026-11-13 ("pause device deployment. ssh issues are being investigated"). No SSH to the device, for QA or for cleanup, until released | engineer | STEP-13 | released 2026-11-13: the engineer directed a restart of the demo for QA, then chose "Clean up now" after QA; cleanup done in one session |

## Authority

- 2026-11-13 — The engineer authorized deployment over SSH for this Task's device QA, on the terms
  of the overview/detail Task: publish `linux-arm64` self-contained locally, copy to a folder in
  the `ziggy` user's home on `ziggybrew.local`, run it as a plain user process on a loopback port,
  and open a second kiosk Chromium window above the existing one. No service, package or system
  change. The grant covers redeploying fixes found during this QA and the cleanup at its end,
  nothing else. Access is the `ziggybrew` host alias with `~/.ssh/id_ziggybrew_ed25519`, as named
  by the engineer on 2026-11-12.

- 2026-11-13 — The engineer authorized the launch of audit 01 (closes GATE-04): one run of the
  Codex CLI, model `gpt-6-astra`, read-only sandbox, at most one hour, against
  `audits/audit-01-dispatch.md`. Repository content read by the auditor leaves the machine to
  OpenAI under the engineer's Codex account. A re-launch is a new cycle and needs a new grant.

## Decisions

All ruled by the engineer on 2026-11-13 during planning; the spec's "Rulings at binding" section
carries the same rulings.

- DECISION-01 — Spec OD-1: no separate time-cursor mode. `Crosshair` itself is upgraded: one line
  across every participating panel, continuous on exactly invertible x, draggable on touch, moved
  client-side. The readout is an opt-in on top (`inspect:` on the geoms plus the bindable
  inspection value); without it a plot gets the upgraded line and nothing else. Departs from the
  spec's original recommendation (a `TimeCursor` record superseding `Crosshair`).
- DECISION-02 — Spec OD-2: two-finger pan and chart pinch are both in version 1, departing from
  the recommendation. One finger sideways scrubs, one finger vertically scrolls the page, two
  fingers pan and pinch the time window. `Pan` with the cursor does not throw. Native pinch is
  turned off on the plot area there, so page pinch-zoom is lost over the chart.
- DECISION-03 — Mouse mapping: hover previews, click pins, drag pans.
- DECISION-04 — OD-3, OD-4 and OD-6 stand as recommended: readings are recomputed when data
  changes, with a "readings changed" notification; a pin outside the window keeps its readout,
  hides its line and is flagged; no interpolation.
- DECISION-05 — OD-5: the consumer renders the readout and the clear control. GGNet gives
  structured readings and `ClearInspectionAsync`.
- DECISION-06 — OD-7: `Geom_Bar` is inspectable in version 1, with a lookup for the bar that
  contains the time. Departs from the recommendation.
- DECISION-07 — OD-8: readouts on `Instant` and numeric x only. The line keeps working on
  `LocalDateTime` and discrete x with the shipped snapping. Departs from the recommendation.
- DECISION-08 — OD-9: `inspect:` on the geom overloads, one `Inspect<T>` record.
- DECISION-09 — Paid or external evaluation runs: not scheduled. GGNet's evals are deterministic
  and run under `dotnet test`; nothing in this spec depends on model behaviour.
- DECISION-11 — Ruled by the engineer on 2026-11-13 (closes GATE-05): the crosshair's new paint
  stays in `Default.css`, and `GalleryTests.SelfContained`, which embeds that stylesheet, is
  re-pinned. This is an accepted exception to the "goldens do not move" half of ACCEPTANCE-11;
  no other golden changed.
- DECISION-10 — Independent audit: one, placed after the local gates pass on the implemented tree
  (STEP-10) and before deployment to the device (STEP-12). Its launch keeps its own authorization
  gate (GATE-04).

## Assumptions and open questions

- Premises re-verified against `main` at `5a5e90e` on 2026-11-13; the spec's premise table carries
  them, with the facts grounding added.
- Details the rulings left open, settled in the spec by the planning agent. Each is the smallest
  reading of the rulings; the engineer can reopen any of them before its step starts.
  - Two-finger gestures exist only with `Pan` on, and drive x only. Without `Pan` the plot area
    keeps native page pinch. Pan-only plots (no `Crosshair`) are unchanged.
  - A plot with `Crosshair` and no binding still pins on tap or click; Escape and
    `ClearInspectionAsync` clear it. This changes shipped crosshair plots, where the line vanished
    when the pointer left.
  - Once pinned, mouse hover does not move the cursor; a click does.
  - A touch double-tap does not reset under `Crosshair`; a mouse double click still does.
  - `CursorTooltip` with `Crosshair` stays allowed: a tap pins the time and opens the nearest
    mark's tooltip.
  - `freeX` and flipped panels keep a local, transient line with no pin and no readout.
  - `inspect:` on `LocalDateTime` or discrete x throws only when `Crosshair` is on; otherwise it is
    inert.
  - Bars accept the sampled policies as well as the containing lookup. ziggy-brew draws one bar
    per sample with a 0 or ±1 value, which reads best as "previous" or "step".
  - A bar's extent for the containing lookup is the key's full width as drawn, before dodging.
- Assumption: with `touch-action: pan-y`, Chromium delivers a two-finger horizontal drag and a
  pinch to the page as pointer events and still scrolls natively on a one-finger vertical drag.
  The touch QA notes support the first half. STEP-06 settles it in emulation, STEP-13 on the
  device; if a two-finger vertical drift cancels the stream on the device, that becomes a gate.
- Assumption: the pinch preview scales the marks group horizontally, so points and strokes distort
  until the committed frame. Judged in STEP-13; a complaint becomes a gate.
- Assumption: what a flipped plot's crosshair does today was not exercised during grounding.
  STEP-04 reads it before changing it.

## Material notes

- 2026-11-13 — No commits. `AGENTS.md` forbids committing, staging or otherwise mutating history
  or index state on the engineer's behalf, and repository instructions outrank the Git policy's
  checkpoint delegation. Verified work stays uncommitted in the working tree; evidence is
  identified by `HEAD` plus the changed files, and the audit pins its target by content hashes.
  The Task starts on `main` at `5a5e90e`, clean.
- 2026-11-13 — This Task picks up D-03 in `plan/deferred.md`.
- 2026-11-13 — The spec was revised in place before binding, on the engineer's rulings. The
  challenge "in essence it is a better crosshair" produced DECISION-01; "I don't want to lose
  panning" produced DECISION-02. The pre-ruling text is `specs/time-cursor-inspection.md` at
  `5a5e90e`.
- 2026-11-13 — `specs/overview-detail-focus-r3.md` left its §2 (inspection hand-over between
  overview and detail) blocked on this feature's bindable inspection value. Cross-`Plot`
  synchronization stays out of this spec's scope; the bindable value is what a host would use.
- 2026-11-13 — The consumer that motivated the feature, ziggy-brew's portal, sets no
  `Interactivity` today, uses `Instant` x on four pages and numeric x on one, and draws cooling and
  heating as one `Geom_Bar` per sample. Adopting the feature there is that repository's work, not
  this Task's.
- 2026-11-13 — STEP-01 as built. `Inspect<T>(Name, Value, Policy = Nearest)` with init properties
  `MaxDuration` (`Instant` x), `MaxDistance` (numeric x, a `double`) and `Missing`. The record
  carries no `TX`, so the typed distance is two properties rather than one; using the one that
  does not fit the axis is for STEP-03 to reject. `inspect:` is the last parameter of every
  overload and is forwarded by name. `Containing` on a line, point or area throws at the `Geom_*`
  call. Group naming for `colorBy`/`fillBy` series is left to STEP-02.
- 2026-11-13 — `dotnet` commands run outside the command sandbox in this session: sandboxed, the
  test runner cannot bind its local socket and a sandboxed build reported success on a tree that
  did not compile.
- 2026-11-13 — Cadence switched to continuous by the engineer after STEP-01. The `Inspect<T>` shape of STEP-01 was left as built.
- 2026-11-13 — STEP-02 as built. `src/GGNet/Inspection/`: `SeriesIndex<T>` (sorted coordinates and
  rows per series, binary search), `InspectAxis` (x value to a `double` coordinate: Unix
  milliseconds for `Instant`, the value itself for `double`; the typed limit), `InspectGroup`
  (group label from a `colorBy`, `lineTypeBy` or `fillBy` mapping). Lookups run on raw data
  coordinates, not scale-mapped ones, so a log axis measures distance in data units. A geom builds
  its indexes on first use after a render pass and drops them at the next one. `Reading<TX>`
  carries `Series`, `Group`, `Name`, `Cursor`, `Sample`, `Kind`, `Value`; `Inspect<T>` gained
  `GroupName`. On an equal distance `Nearest` takes the earlier sample. A bar's half extent is
  its `width` times the smallest spacing between distinct bar positions, over two.
- 2026-11-13 — STEP-03 as built. `Plot` parameters `Inspection` (`Inspection<TX>(X, Pinned)`),
  `InspectionChanged` and `ReadingsChanged` (`InspectionReadings<TX>(Inspection, Values,
  OutsideView)`), the `Readings` accessor and `ClearInspectionAsync`. `InspectionChanged` fires
  for a gesture or a clear, never for a host-set value; `ReadingsChanged` fires only when the
  readout differs from the last one reported. The outside-view flag sits on the readout, not on
  the bindable value, so a host can round-trip the value. The engine namespace is
  `GGNet.Readings` (the folder moved with it) because `GGNet.Inspection<TX>` took the other name.
  A geom rebuilds its indexes at the end of each render pass once they have been asked for, so a
  reader never sees a half-built index. A replacement `Context` is rendered once during parameter
  setting when `Crosshair` is on, so that an unreadable axis throws to the host, not into the
  render loop.
- 2026-11-13 — STEP-04 and STEP-05 as built.
  - Server: the plot holds one inspection; each panel hosts a small internal `CrosshairCursor`
    component that draws the line, its label and the sample markers from it and re-renders alone,
    so a cursor move does not re-render the marks. `Panel` gained `OnCursorAsync`,
    `OnCursorLeaveAsync` and `OnCursorRestoreAsync`. The plot remembers the last pinned value, so
    an abandoned scrub is restored from the domain value, not from pixels.
  - Browser: the module translates the cursor group to keep the line under the pointer between
    frames, and holds a pinned or restored preview until the committed line reaches it. Requests
    are latest-wins per plot. The `initialize` payload gained `cursor` (`shared` or `local`) and
    `cursorContinuous`.
  - A missing reading draws no marker; that is its distinct treatment.
  - A marker sits at the row's own value. In a stacked area or bar it is therefore not on the
    stacked outline.
  - Under a shared crosshair a touch tap no longer reaches a mark's `onclick` or an ancestor
    (spec ACCEPTANCE-08). Charts with `CursorTooltip` and no `Crosshair` keep the shipped
    click-follows-inspection.
  - Click handling for the crosshair is on the window. A click listener on the chart group made
    Chromium's touch adjustment move taps near the plot-area edge by several pixels.
  - `#inPanel` in the module accepts half a unit outside the plot area. A mark centred on the edge
    missed it by rounding once the chart gained the nested viewport.
  - A readout whose height changes when a reading arrives moves the chart under a resting pointer
    and ends the hover. The demo readout has a fixed height; the docs carry the caution (STEP-09).
  - The demo's committed Tailwind build (`src/GGNet.Demo/wwwroot/css/app.css`) was regenerated
    with `npm ci && npm run tailwind:build`, as its README instructs, for the new page's classes.
- 2026-11-13 — STEP-06 as built. The panel under the first finger owns the gesture; the second
  finger may rest on any panel of the same plot. The preview is `translate` plus a horizontal
  `scale` on each panel's marks, so points and strokes stretch until the committed frame. The
  commit is `OnNavigateEndAsync(from, to, scale)`: the value under the fingers' midpoint at the
  start ends under their midpoint at the end. One gesture scales by at most 10 either way (a
  calibration value). A third finger is ignored. The gesture commits when the first finger
  lifts. Sample markers are hidden during the preview.
- 2026-11-13 — STEP-07 as built. The plot's root `div` takes `tabindex="0"` and a key handler
  only under `Crosshair`. Arrow keys are not default-prevented: Blazor can only prevent a key for
  every key, which would trap Tab.
- 2026-11-13 — Acceptance rows 01 to 10 and 14 to 18 are marked from the evidence of the run in
  STEP-10. They are re-established against the final state after GATE-05 and after any audit
  remediation. ACCEPTANCE-11 and 12 wait for GATE-05; ACCEPTANCE-13 waits for the device.
- 2026-11-13 — Not built or not covered, named here so the completion report carries them: the
  module does not watch for a changed viewport transform during a two-finger gesture, as the
  one-finger pan does; `LocalDateTime` snapping has no test of its own; the demo page was looked
  at once, in one screenshot.
- 2026-11-13 — STEP-11: `audits/audit-01-dispatch.md` persisted. It pins 57 audited files and 8
  normative files by SHA-256 (65 rows), on `HEAD` `5a5e90e` plus the uncommitted tree; the
  launcher's dry run verified all 65 (dispatch SHA-256 `3cf9fd90…caaa06`). From launch to report
  capture those files are frozen; this plan is a neutral reference and is not pinned. The launch
  waits on GATE-04. The demo's generated stylesheet (`wwwroot/css/app.css`) is not in the
  manifest.
- 2026-11-13 — STEP-11 outcome. Audit 01 returned five findings, all confirmed and fixed with
  fail-first tests (STEP-14 to STEP-18): bar containment on a log axis, a tap discarded within
  half a second of a scrub, a group with no valid sample vanishing from the readout, arrow keys
  that needed `inspect:`, and a single `freeX` facet treated as shared. Arrow keys now step
  through every line, point, area and bar layer; a layer without `inspect:` builds a sample index
  on the first key press. No re-audit: the audit is not a bound acceptance requirement and the
  fixes carry directed tests.
- 2026-11-13 — STEP-12 not started on the device: it did not answer (GATE-06). Nothing was
  published or copied. The publish is made when the device is reachable, so that its identity is
  the final tree's.
- 2026-11-13 — State left on the device when the engineer paused device work. Nothing was touched
  after the pause message.
  - `~/ggnet-cursor-demo/app`: the published demo, build `5a5e90e.dirty.2bf6ed3c`.
  - Demo process: PID 49142 (in `~/ggnet-cursor-demo/demo.pid`), listening on
    `http://127.0.0.1:15236` only, log in `~/ggnet-cursor-demo/demo.log`.
  - QA browser: a second Chromium, PID 49315 (in `~/ggnet-cursor-demo/browser.pid`), own profile
    `~/ggnet-cursor-demo/chromium-profile`, `--kiosk` on `/time-cursor`, started above the
    kiosk's own browser. The kiosk's browser (PID 15926) was still running when last seen.
  - Not changed: backlight (16 on entry and after), services, packages, `~/ggnet-touch-demo`
    from an earlier Task.
  - To return the screen to the kiosk: `kill $(cat ~/ggnet-cursor-demo/browser.pid)`. To stop the
    demo: `kill $(cat ~/ggnet-cursor-demo/demo.pid)`. To remove everything:
    `rm -r ~/ggnet-cursor-demo`.
  - The first connection of the day timed out, then the default identity was refused
    ("Permission denied (publickey)"), then the named key worked. Six SSH sessions and one rsync
    were made from `192.168.1.101` as `ziggy`, all with that key.
- 2026-11-13 — Demo restarted for QA on the engineer's instruction ("restart the demo for QA",
  then "retry" after one attempt timed out on port 22). The device had rebooted about four minutes
  earlier, so the earlier PIDs were gone. Now: demo PID 3495, QA browser PID 3561, both PID files
  current, `/time-cursor` answers 200 with build `5a5e90e.dirty.2bf6ed3c`. The restart command
  wrote a stray `~/demo.pid`; it was removed. The undo commands above are unchanged.
- 2026-11-13 — Physical QA on the device touchscreen by the engineer, build
  `5a5e90e.dirty.2bf6ed3c`, after the restart above. The engineer's report: "1-14 as expected",
  with no further remark. One line per check:
  1. Tap pins in all four panels, readout shows six rows and "pinned": as expected.
  2. One-finger sideways drag scrubs, pins on lift, chart does not pan: as expected.
  3. One-finger vertical drag scrolls the page, nothing selected: as expected.
  4. Tap right after a scrub moves the pin: as expected.
  5. Second finger during a scrub returns the line to the pin; lifting selects nothing: as expected.
  6. Two-finger sideways drag pans all panels together; pinned time unchanged: as expected.
  7. Pinch zooms the time window, page does not zoom; stretch during the gesture acceptable: as expected.
  8. Tap on a gravity mark pins; mark and ancestor click counters stay 0: as expected.
  9. Double-tap makes two selections, view not reset: as expected.
  10. Append readings changes the temperature row, not the time: as expected.
  11. Pin panned off-screen shows the "outside the view" badge; Reset view brings it back: as expected.
  12. Clear removes line and readout; nothing else clears it: as expected.
  13. Readout readable while touching; time label and sample markers legible: as expected.
  14. Second chart: two fingers still pinch-zoom the page: as expected.
  No findings, no gate raised.
- 2026-11-13 — Device cleanup, on the engineer's release of GATE-07 ("Clean up now"). QA browser
  (PID 3561) and demo (PID 3495) stopped, `~/ggnet-cursor-demo` deleted, nothing listening on
  15236, the kiosk's own browser (PID 2132) running, backlight 16. `~/ggnet-touch-demo` from an
  earlier Task left as found.
- 2026-11-13 — Plan edits after the publish are not in the deployed identity. Publishing for
  `linux-arm64` re-restored the projects; the solution is rebuilt before the gates are run again.
