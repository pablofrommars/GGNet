<!-- launched by audit-launch.cs: dispatch audit-01-dispatch.md sha256=3cf9fd9048a0f7edbdf3a80137bf9e38eaf98627d2095c1bc8b35e31dacaaa06 auditor=codex model=gpt-6-astra resolved-model=gpt-6-astra sandbox=read-only -->
# Audit 01 — Time-cursor inspection

## Scope and target state

Audited the working-tree implementation specified by `plan/time-cursor-inspection/audits/audit-01-dispatch.md`.

- Verified HEAD: `5a5e90e00603847d7ad399d89221bca2d24839e4`.
- Checked all 65 SHA-256 entries: 57 audited artifacts and eight normative sources. Every hash matched, both initially and at the end of inspection.
- Examined tracked changes against `5a5e90e` and read the new implementation and tests directly.
- Modified no files or repository state. Ran no build, tests, browser, deployment or network operations.

The generated demo stylesheet, `src/GGNet.Demo/wwwroot/css/app.css`, is changed but excluded from the dispatch manifest. Its contents were not audited.

## Sources used

Normative sources read:

- `specs/time-cursor-inspection.md`
- `AGENTS.md`
- `.github/instructions/blazor.instructions.md`
- `.github/instructions/dsl.instructions.md`
- `.github/instructions/csharp.instructions.md`
- `.github/instructions/rendering.instructions.md`
- `.github/instructions/testing.instructions.md`
- `.github/instructions/skill.instructions.md`

Neutral and supporting sources read:

- The dispatch.
- `plan/time-cursor-inspection/task-plan.md`: implementation claims, acceptance evidence, decisions, exceptions and material notes.
- `plan/deferred.md`: row D-03.
- `.github/instructions/troubleshooting.instructions.md`.
- Relevant portions of `src/GGNet/PlotContext.cs`.
- `src/GGNet/Data/Position.cs`.
- Scale implementations and mapping contracts: `Continuous.cs`, `Position.cs`, `IAestheticMapping.cs`, `Discrete.cs`, `InstantPosition.cs`, `Log10.cs` and `Extended.cs` under `src/GGNet/Scales/`.
- `src/GGNet/Transformations/Log10.cs`.

The plan's claimed successful executions were treated as claims, not independently verified results.

## Coverage

Inspected the complete browser module, cursor components, interop wrapper, plot and panel code-behind, inspection models, lookup engine, base geom implementation and bar implementation. Read all new time-cursor browser tests, all listed component tests, both inspection headless test files, and the new demo page and data.

Methods were SHA-256 comparison, working-tree diff inspection, source reading, static event-order tracing and comparison of assertions with acceptance claims.

All manifest files were read in full **except the following**, which received partial-source or diff-only inspection:

| File | Inspection |
|---|---|
| `README.md` | Changed section |
| `skills/ggnet/reference/geoms.md` | Diff |
| `src/GGNet.Demo/Components/Layout/MainLayout.razor` | Diff |
| `src/GGNet.Demo/Components/Pages/TouchLayout.razor` | Diff |
| `src/GGNet.Demo/Components/Pages/TouchLayout.razor.cs` | Diff |
| `src/GGNet.Demo/README.md` | Diff |
| `src/GGNet/BuilderExtensions.Area.cs` | Diff |
| `src/GGNet/BuilderExtensions.Bar.cs` | Partial diff |
| `src/GGNet/BuilderExtensions.Line.cs` | Partial diff |
| `src/GGNet/BuilderExtensions.Point.cs` | Diff |
| `src/GGNet/Components/Panel.razor` | Lines 1–190 |
| `src/GGNet/Components/Plot.razor` | Partial source |
| `src/GGNet/Geoms/Area/Area.cs` | Diff |
| `src/GGNet/Geoms/Line/Line.cs` | Diff |
| `src/GGNet/Geoms/Point/Point.cs` | Diff |
| `src/GGNet/InteractivityOptions.cs` | Diff |
| `src/GGNet/Themes/Default.css` | Diff |
| `tests/GGNet.E2ETests/TouchDemoTests.cs` | Diff |
| `tests/GGNet.Headless.Tests/Api/PublicApiTests.GGNet.verified.txt` | Diff |
| `tests/GGNet.Headless.Tests/Gallery/GalleryTests.SelfContained.verified.svg` | Diff |
| `tests/GGNet.Headless.Tests/ThemeContractTests.cs` | Diff |

Coverage limitations:

- **ACCEPTANCE-12:** No gates were rerun. The reported 1670 passing tests, 129 skipped browser cases, successful formatting checks and 131 passing explicit browser cases remain unverified execution claims.
- **ACCEPTANCE-13:** Physical touchscreen acceptance remains pending by design. Readability, native gesture arbitration and device behavior were not observed.
- **ACCEPTANCE-11:** The inspected golden diff contains exactly the added theme variable and two rules covered by DECISION-11. Output was not independently rendered or byte-compared.
- **ACCEPTANCE-05:** The burst test dispatches mouse moves in one browser task and counts position-change notifications. It does not directly measure concurrent requests or exercise a release while a request remains outstanding; those properties were inspected statically.
- Runtime race behavior, frame-rate performance, disconnect recovery, trimmed publication and visual layout were not experimentally assessed. The plan's acknowledged lack of transform-change handling during two-finger navigation remains an unverified recovery risk.
- Compliance and consistency conclusions are limited to the inspected portions. No exhaustive security, complexity or whole-repository regression assessment was performed.
- An initial hash command using a shell here-document was blocked by the read-only sandbox; an inline Python command completed the check. Git emitted temporary-cache warnings but returned the requested identity and diffs. These are tooling limitations, not implementation findings.

## Assessment

The inspected implementation substantially follows the intended architecture: one JS module and initialization payload, plot-owned domain inspection, coalesced cursor requests, separate reading notifications, explicit lookup policies and consumer-owned readout presentation.

Five discrepancies remain in the inspected state. They affect bar containment, rapid touch interaction, missing grouped series, keyboard availability and single-facet `freeX` behavior. Consequently, the plan's completed acceptance claims should not be interpreted as full conformance across those cases. Physical acceptance remains outstanding independently of these findings.

## Findings

### FINDING-01 — Bar containment uses different extents from rendered bars on logarithmic x axes

**Claim:** `InspectPolicy.Containing` can report missing inside a drawn bar, or return a bar where none is drawn.

**Evidence:** [Bar.cs:341](/Users/pablo/source/GGNet/src/GGNet/Geoms/Bar/Bar.cs:341) builds inspection indexes from raw x coordinates. [SeriesIndex.cs:63](/Users/pablo/source/GGNet/src/GGNet/Readings/SeriesIndex.cs:63) derives one half-extent from raw spacing, and line 131 compares raw cursor distance with that extent. Rendering instead derives width from mapped bar positions in [Bar.cs:225](/Users/pablo/source/GGNet/src/GGNet/Geoms/Bar/Bar.cs:225), then draws around those mapped positions at line 261. `Scales/Log10.cs` maps x through `log10`.

For bars at x = 1, 10 and 100 with width 0.5, rendering gives the x = 10 bar the domain interval approximately 5.623–17.783. Inspection gives it 7.75–12.25. A cursor at 15 is inside the rendered bar but returns missing.

The containment test at [InspectReadingTests.cs:214](/Users/pablo/source/GGNet/tests/GGNet.Headless.Tests/InspectReadingTests.cs:214) exercises a linear axis only.

**Affected surface:** Bar readouts on logarithmic x axes; spec §3 and ACCEPTANCE-15.

**Consequence:** The readout contradicts the visible interval being inspected.

**Confidence:** High. The discrepancy follows directly from the separate raw-coordinate and mapped-coordinate width calculations.

### FINDING-02 — A fresh tap shortly after scrubbing or navigation is discarded

**Claim:** A valid new tap within 500 ms of a completed scrub or multi-touch sequence does not move the pin.

**Evidence:** [Panel.razor.js:738](/Users/pablo/source/GGNet/src/GGNet/Components/Panel.razor.js:738) and line 751 set `#touchClickBlockedUntil` after multi-touch completion and scrub completion. The click handler at [Panel.razor.js:617](/Users/pablo/source/GGNet/src/GGNet/Components/Panel.razor.js:617) stops a touch click and returns while that deadline remains active.

A subsequent single-finger press creates a new scrub candidate at line 680 without resetting this deadline. If that new contact is a tap, its release returns at lines 747–748, leaving selection to the click handler—which discards it. The block is time-based rather than associated with the preceding gesture's trailing click.

The inspected scrub and navigation tests wait before subsequent actions; the double-tap test starts without a preceding scrub.

**Affected surface:** Shared-crosshair touch interaction; spec §1's tap-to-select behavior.

**Consequence:** Rapidly scrubbing and then tapping another time leaves the old selection, despite the new contact being a separate valid gesture.

**Confidence:** High. The event sequence reaches an explicit suppression branch, and no new-press reset of this deadline exists.

### FINDING-03 — A grouped series with only NaN values disappears instead of reporting missing

**Claim:** An inspected group with no valid y samples produces no reading row, rather than its configured missing reading.

**Evidence:** [SeriesIndex.cs:31](/Users/pablo/source/GGNet/src/GGNet/Readings/SeriesIndex.cs:31) discards invalid samples before determining their group. Group registration occurs only at lines 43–52, and indexes are created only for registered groups at lines 67–77.

For an inspected, color-grouped line containing finite samples for group A and only NaN y samples for group B, B never receives an index. Although `Read` handles an empty index by returning missing, B cannot reach that branch.

The NaN test at [InspectReadingTests.cs:146](/Users/pablo/source/GGNet/tests/GGNet.Headless.Tests/InspectReadingTests.cs:146) uses an ungrouped series with remaining valid samples. The grouping test at line 183 uses valid samples for both groups.

**Affected surface:** Grouped inspection readouts; spec §3's one-series-per-group and missing-reading requirements.

**Consequence:** A failed or entirely missing series silently vanishes from the consumer's readout instead of communicating its missing state.

**Confidence:** High. Group discovery is explicitly downstream of sample exclusion.

### FINDING-04 — Keyboard stepping requires the optional readout configuration

**Claim:** A shared crosshair with ordinary plotted samples but no `inspect:` configuration cannot be moved with Left/Right, contrary to the interaction contract applying to every crosshair-enabled plot.

**Evidence:** Spec §1, including its keyboard row at `specs/time-cursor-inspection.md:73`, applies to plots with `Crosshair`. [Plot.razor.cs:268](/Users/pablo/source/GGNet/src/GGNet/Components/Plot.razor.cs:268) obtains step candidates exclusively through `InspectSeries()`. [Geom.cs:29](/Users/pablo/source/GGNet/src/GGNet/Geoms/Geom.cs:29) returns an empty collection without `Inspect`.

The test [CrosshairKeyboardTests.cs:149](/Users/pablo/source/GGNet/tests/GGNet.Components.Tests/CrosshairKeyboardTests.cs:149) explicitly locks in this no-op behavior despite supplying plotted samples and an existing inspection. The plan and Blazor guide narrow stepping to inspected series; the bound spec does not state that restriction. The README's crosshair introduction also promises stepping before introducing optional readouts.

**Affected surface:** Keyboard interaction without `inspect:`; ACCEPTANCE-10 and documentation consistency.

**Consequence:** Mouse and touch can select positions on these plots, while keyboard users cannot step through their samples. The test preserves the narrower implementation rather than detecting the contract discrepancy.

**Confidence:** High for the behavior and textual mismatch. Whether to narrow the product contract or extend stepping requires an explicit disposition.

### FINDING-05 — A single-facet `freeX` plot incorrectly receives shared, pinnable inspection

**Claim:** `freeX: true` is not sufficient to obtain the required local, transient cursor; behavior instead depends on the current number of scales.

**Evidence:** [Plot.razor.cs:190](/Users/pablo/source/GGNet/src/GGNet/Components/Plot.razor.cs:190) defines participation using `Context.Positions.X.Scales.Count == 1`, without checking `Faceting.FreeX`.

`src/GGNet/PlotContext.cs:213–220` creates one x scale per free-x facet. Therefore, a `freeX` plot containing one facet has exactly one scale and passes the shared-inspection predicate. `Panel.OnCursorAsync` then stores a plot-wide pinned inspection, and the interop payload selects shared cursor behavior.

Spec §2 explicitly excludes facets built with `freeX: true`; ACCEPTANCE-16 says a free-x plot does not pin. [CrosshairTests.cs:349](/Users/pablo/source/GGNet/tests/GGNet.Components.Tests/CrosshairTests.cs:349) covers two facets, which does not expose this case.

**Affected surface:** Single-facet `freeX` plots; ACCEPTANCE-16 and participation consistency.

**Consequence:** Such a plot pins and can produce readouts despite being configured for local, transient inspection. Its interaction semantics depend on facet cardinality rather than the declared policy.

**Confidence:** High. The scale-allocation path and participation predicate directly establish the single-facet case.