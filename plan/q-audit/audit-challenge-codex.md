# Counter-review of the Audit Challenge

## Purpose

This document adversarially checks `plan/q-audit/audit-challenge.md` against the repository and the canonical audit. The objective is to retain supported corrections and new findings while rejecting speculative or overstated claims.

## General assessment

The challenge is strong on the original audit: all eleven finding headlines survive scrutiny, and its corrections to the exception type in MF-6 and the crash mechanism in MF-8 are right. Its confidence becomes less reliable in the coverage section. Of the four added claims, one is a new correctness defect, one is a useful elaboration of an existing defect, one establishes a mechanism without establishing a bug, and one is a false positive. It also misses a second transformed-scale unit mismatch and proposes an incomplete polygon-hole remedy.

The canonical `audit.md` has been updated only where the counter-review or this verification established a concrete change.

## Verdicts on the original audit

### Confirmed

The challenge correctly confirms the following findings:

- MF-1: unencoded label text reaches `MarkupString`.
- MF-2: explicit transformed-scale limits are passed to the mapped range without transformation.
- MF-3: `Ribbon.cachedArea` survives `Clear` and is not re-added on the next render.
- MF-4a: constant continuous fill maps to an empty string, and the `(0, 0)` sentinel loses a leading zero.
- MF-4b: constant continuous size produces `NaN`, which passes the point geom's `radius <= 0` guard.
- MF-5: stacked negative bars use a negative rectangle height and a single signed accumulator.
- MF-7: out-of-order same-day `LocalDateTime` observations can be absent from the scale and map to `NaN`.
- MF-9: boxplot samples lose multiplicity in `SortedBuffer`.
- MF-10: `Polygon.Hole` has no reader and polygon output has no deterministic hole handling.
- SF-1a: palette exhaustion leaves default mappings and silently removes marks.
- SF-1b: an empty continuous palette throws for a non-degenerate trained range.

The build and test figures in GA-1 also match the prior validation: zero build warnings, 290 enabled tests passed, and three browser E2E tests skipped.

### Corrections accepted

#### MF-6 throws `ArgumentOutOfRangeException`

- **Location:** `src/GGNet/Scales/DiscreteDates.cs:124-135`, `src/GGNet/Buffers/SortedBuffer.cs:10-14`
- **Verdict:** Confirmed correction.
- **Reason:** `values[i]` delegates to `List<T>`'s indexer, so reaching `i == values.Count` throws `ArgumentOutOfRangeException`, not `IndexOutOfRangeException`.
- **Effect on finding:** None. The cross-month rendering crash remains must-fix.

#### MF-8 crashes at `Panels[0]`, not at floating-point division

- **Location:** `src/GGNet/Facets/Utils.cs:23-26`, `src/GGNet/Facets/Faceting1D.cs:18-25,37-41`, `src/GGNet/PlotContext.cs:170-184,277-299,319-322`
- **Verdict:** Confirmed correction.
- **Reason:** `1.0 / 0` yields positive infinity; it does not throw. With no facet values, no panels are built, and `BuildLegends` then indexes `Panels[0]`.
- **Effect on finding:** The headline and must-fix severity stand, but the proximate mechanism in the original audit was partly wrong.

## Verdicts on the challenge's added coverage claims

### 1. Duplicate x values in area-like shapes

- **Location:** `src/GGNet/Shapes/Area.cs:19-28`, `src/GGNet/Geoms/Area/Area.cs:14-21,141-172`, `src/GGNet/Geoms/Ribbon/Ribbon.cs:172-176`, `tests/GGNet.Headless.Tests/SortedBufferTests.cs:149-169`
- **Vendor verdict:** Material omitted issue.
- **Counter-verdict:** Mechanism confirmed; defect not established.
- **Evidence:** The comparers intentionally key these buffers only by x, and `SortedBuffer.Add` discards compare-equal values. The test suite explicitly pins the same x-deduplication behavior for path-shaped data.
- **Why the challenge overreaches:** An area or ribbon represents one interval per x within a series. The repository does not promise how conflicting duplicate-x observations are combined, and the existing test documents x uniqueness as intentional buffer behavior. Silent rejection may deserve validation or documentation, but the supplied code does not establish that preserving multiple conflicting values is the correct rendering contract.
- **Disposition:** Do not add as a correctness finding without an API contract or a failing supported use case.

### 2. `DateTimePosition.Commit` mutates `values` during iteration

- **Location:** `src/GGNet/Scales/DateTimePosition.cs:139-168`, `src/GGNet/Buffers/SortedBuffer.cs:16-24`
- **Vendor verdict:** Latent index-shift hazard and dead code.
- **Counter-verdict:** False positive as a runtime defect.
- **Evidence:** The loop reads `date` from `values[i]` and immediately calls `values.Add(date)`. Because that exact value is already present, `BinarySearch` returns a non-negative index and `Add` performs no insertion. The collection does not mutate.
- **Impact:** None in the current implementation. It would become hazardous only after a hypothetical change to `SortedBuffer`'s duplicate semantics.
- **Disposition:** Removing the no-op could improve clarity, but it is not a material repo issue under this audit's criteria.

### 3. `Bar.Stack` trains twice the drawn half-width

- **Location:** `src/GGNet/Geoms/Bar/Bar.cs:246-267,276-309`
- **Vendor verdict:** Minor visual over-expansion.
- **Counter-verdict:** Confirmed new defect; severity understated.
- **Evidence:** The emitted stacked rectangle spans `x - delta / 2` through `x + delta / 2`, while the position scale is trained with `x - delta` through `x + delta`. The dodge implementation trains its exact emitted bounds.
- **Impact:** The default stacked-bar path adds excess range on both sides, shrinking marks and producing excessive outer whitespace, especially for few-bar charts.
- **Disposition:** Add to the canonical audit as must-fix because the requested rubric places rendering correctness in that category.

### 4. MF-6's trailing loop is wrong beyond the full-range crash

- **Location:** `src/GGNet/Scales/DiscreteDates.cs:124-135`, `src/GGNet/Scales/DiscretePosition.cs:76-100`
- **Vendor verdict:** Additional omitted issue.
- **Counter-verdict:** Confirmed elaboration, not a separate finding.
- **Evidence:** `end` is exclusive, so `i <= end` is incorrect. At the full buffer boundary, an attempted label at `i == end` throws before the break can be appended. When limits or a view make `end < values.Count`, the same branch can read a valid element outside the requested window and emit an out-of-window tick.
- **Disposition:** Fold this impact into MF-6; do not count it as another finding.

## Gaps in the challenge

### The MF-2 remediation is incomplete

- **Location:** `src/GGNet/Scales/Position.cs:43-48`, `src/GGNet/BuilderExtensions.cs:67-69,213-240`
- **Evidence:** `SetRange` applies additive expansion after values have been transformed, while the public API repeatedly documents `minAdd` and `maxAdd` as data units.
- **Impact:** Even after explicit limits are transformed, non-zero additive expansion on square-root or logarithmic scales remains in the wrong unit space.
- **Required correction:** Decide and implement one contract: convert additive expansion from data space at each endpoint, or document it as transformed-scale units. The canonical audit now includes this in MF-2.

### `evenodd` alone does not implement `Polygon.Hole`

- **Location:** `src/GGNet/Geospacial/Polygon.cs:3-9`, `src/GGNet/Scene/ShapeComposer.cs:208-274`, `src/GGNet/Components/Area.razor:130-141`
- **Evidence:** Applying `fill-rule="evenodd"` to every concatenated contour would create holes by geometric nesting, but it would still ignore the public `Hole` flag. Overlapping exterior polygons can also cancel in their overlap under even-odd filling.
- **Required correction:** Preserve explicit hole semantics during composition. Normalize hole winding under `nonzero`, split exterior groups, or otherwise guarantee that an even-odd compound path cannot turn overlapping exterior rings into holes.

## Scorecard

- Original audit finding headlines: **11 confirmed out of 11**.
- Accepted corrections to original mechanisms: **2**.
- Challenge coverage additions: **1 new defect**, **1 elaboration**, **1 unproven claim**, **1 false positive**.
- Additional gap found in this pass: transformed-scale additive expansion uses the wrong documented units.

**Verdict: fix-then-ship.**
