# Audit 01 challenge — time-cursor inspection

Challenge of `audit-01.md` by the Task's coordinating agent, on the top-model configuration,
against the pinned state of `audit-01-dispatch.md` (`HEAD` `5a5e90e` plus the uncommitted tree,
65 files by SHA-256, all verified by the auditor at the start and the end of its inspection).

The auditor read the browser module, the cursor component, the plot and panel code-behind, the
lookup engine and every new test in full, and 21 other manifest files by diff or in part. It ran
nothing. Its report has five findings. Each was checked by reading the cited lines in the pinned
state.

## Dispositions

### FINDING-01 — bar containment on a logarithmic x axis

- Disposition: **confirmed**.
- Evidence: `Bar.Delta()` (`src/GGNet/Geoms/Bar/Bar.cs`) takes the smallest spacing between
  `bars[i].x`, which are scale-mapped positions (`Positions.X.Map(item)`), and `Stack()`/`Dodge()`
  draw each bar that wide around its mapped x. `SeriesIndex<T>.Build` takes spacing and distance
  from raw data coordinates. On a linear axis the two agree; on `Scale_X_Log10` they do not. The
  auditor's numbers hold: bars at 1, 10 and 100 at width 0.5 are drawn over about 5.62 to 17.78
  around 10, and the index accepts 7.75 to 12.25.
- Consequence: `Containing` contradicts the drawn bar on a log x axis. ACCEPTANCE-15 was
  established on linear and `Instant` axes only.
- Action: remediate → STEP-14. The containing lookup measures in the scale's mapped space, like
  the drawing. Verification: new engine cases on a log axis, failing first.

### FINDING-02 — a tap within 500 ms of a scrub or a two-finger gesture is discarded

- Disposition: **confirmed**.
- Evidence: `#startScrub` in `Panel.razor.js` sets `#touchClickBlockedUntil` when a scrub ends
  and when the last finger of a multi-touch sequence lifts; the window click listener stops a
  touch click and returns while the deadline runs. Nothing clears the deadline when a new contact
  begins, so a separate tap inside the half second pins nothing.
- Consequence: scrub, then quickly tap elsewhere: the old pin stays. Spec §1 says a tap selects.
- Action: remediate → STEP-15. A fresh single contact clears the deadline: the block belongs to
  the gesture that ended, not to the clock. Verification: a browser case that taps immediately
  after a scrub, failing first.

### FINDING-03 — a group whose samples are all NaN has no reading

- Disposition: **confirmed**.
- Evidence: `SeriesIndex<T>.Build` skips a row with a NaN y before it looks at the row's group,
  so a group with no valid sample never gets an index and never reaches the "missing" branch of
  `Read`. An ungrouped all-NaN layer does get its (empty) index.
- Consequence: a probe that reports nothing vanishes from the readout instead of saying
  "missing". Spec §3 wants one series per group and a missing reading where no sample qualifies.
- Action: remediate → STEP-16. The group is registered before the sample is judged.
  Verification: a new engine case, failing first.

### FINDING-04 — the arrow keys do nothing without `inspect:`

- Disposition: **confirmed**.
- Evidence: `Plot.StepAsync` collects candidates only from `InspectSeries()`, which is empty for
  a layer without `inspect:`. Spec §1 puts the keyboard row under "a `Plot` whose
  `Interactivity` has `Crosshair` on", with no condition on a readout, and
  `WithoutInspectedSeriesTheArrowsHaveNowhereToStep` pins the narrower behaviour. The Blazor
  guide and the plan narrowed the contract without a ruling.
- Consequence: on a crosshair with no readout the mouse and a finger can select and the keyboard
  cannot step. That is a gap against ACCEPTANCE-10's "mouse and keyboard reach the same
  inspection" for such plots.
- Action: remediate → STEP-17, by widening the code to the spec rather than narrowing the spec:
  the arrows step through the sample times of every line, point, area and bar layer, inspected
  or not. The pinned test is rewritten to the spec's behaviour. No gate: the bound spec already
  says it.

### FINDING-05 — a `freeX` plot with one facet shares and pins

- Disposition: **confirmed**.
- Evidence: `Plot.InspectsSharedX` tests `Context.Positions.X.Scales.Count == 1`. `PlotContext`
  creates one x scale per facet under `freeX`, so a single facet leaves one scale and the plot is
  treated as sharing. The spec excludes "facets built with `freeX: true`" by declaration, not by
  how many facets the data produced.
- Consequence: the same chart pins or does not depending on its data. ACCEPTANCE-16's `freeX`
  clause fails for one facet.
- Action: remediate → STEP-18. Participation reads the faceting's `FreeX`. Verification: a bUnit
  case with one facet, failing first.

## Bounded search for misses

Looked for within the declared scope, by reading the pinned state again with the auditor's
findings as a guide to the kind of defect it found (a contract narrowed in code, a predicate that
holds only for the tested shape):

- **Other raw-versus-mapped mismatches.** `Nearest` and `Previous` measure in raw data units by
  ruling (distance in data units, typed per x), so a log axis is correct there. Markers are
  placed through the scale. Only `Containing` depends on drawn geometry. Nothing further.
- **Other time-based blocks in the module.** `#suppressClickUntil` after a pan is cleared on
  every `pointerdown` by `#track`. The settle timers end in a state reset, not a block. Nothing
  further.
- **Other predicates keyed on the tested shape.** `Panel` passes `SharedX` for the linked pan
  preview from the same scale count. That predates this Task and decides only whether a sibling
  previews a pan; with one facet there is no sibling. Not a finding of this scope.
- **Limits the auditor named and did not turn into findings.** The burst test counts
  notifications rather than concurrent requests; the single in-flight call follows from one
  boolean in `pumpCursor` and was read, not measured. A release while a request is outstanding
  is exercised by the scrub cases, which release 30 ms after the last move. The module does not
  watch for a changed viewport transform during a two-finger gesture; the plan already carries
  that as not built, and physical QA is where it would show.

No further finding. The next unused id, FINDING-06, is not assigned.

## After remediation

STEP-14 to STEP-18 change pinned files, so the audited state no longer exists once they land.
The local gates and the full browser run are repeated on the new tree (STEP-19). A re-audit is
not required by the policy: the audit is not a bound acceptance requirement, and the fixes are
verified by directed, fail-first tests.
