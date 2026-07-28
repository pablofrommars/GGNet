# Implementation Review

## Purpose

This document records the review of the staged implementation of `plan/q-audit/steps.md`, including regressions introduced by the fixes and discrepancies between the staged change and its implementation record.

## General assessment

Most reconciled audit findings are implemented with focused regression coverage, and the solution builds, tests, and formats cleanly. One must-fix ordering defect in the new `LocalDateTime` sampling logic nevertheless leaves rendered marks inconsistent with the committed scale. The test intended to cover that path reads the wrong SVG attributes and therefore passes despite the regression. The staged set also includes an intentional follow-on public API removal that the plan summary incorrectly describes as no public-surface change.

## Findings

### Must-fix

#### 1. LocalDateTime sampling occurs after marks have already been mapped

- **Location:** `src/GGNet/Scales/DateTimePosition.cs:63-112,206-215`, `src/GGNet/PlotContext.cs:318-327`, `tests/GGNet.Headless.Tests/DateTimePositionTests.cs:139-188`
- **Evidence:** `Train` now retains only observed keys. The render pipeline calls `Shape` before `CommitPositions`, so geoms map and store coordinates against that observed-only buffer. `DateTimePosition.Commit` then calls `Sample` and inserts minute samples, changing every later key's index after the shapes and `_min`/`_max` have been calculated. For 09:00, 09:30, and 10:00, marks retain indices `0, 1, 2`, while the committed scale subsequently maps the keys to `0, 30, 60`.
- **Impact:** LocalDateTime marks are compressed into the start of the axis and no longer align with ticks or the committed range. This affects both ordered and out-of-order input.
- **Test gap:** `DateTimePositionTests.Positions` compares each circle's constant `cx="0"` and `cy="0"` attributes. Point positions are carried by the parent `<g transform="translate(...)">`, so the test counts marks without checking their coordinates.
- **Suggested fix:** Finalize sampling after all training but before the first geom `Map`/`Shape` call, either through an explicit scale-preparation phase or guarded lazy preparation in `Map`. Invalidate that preparation on `Train` and `Clear`. Test the actual wrapper transforms and verify that marks and the committed range both use indices `0, 30, 60`.

### Should-fix

No should-fix findings.

### Consider

#### 1. The implementation record incorrectly claims that the public API did not change

- **Location:** `plan/q-audit/steps.md:38,91,93,123`, `tests/GGNet.Headless.Tests/Api/PublicApiTests.GGNet.verified.txt:8-14`, deleted `src/GGNet/Components/PlotBase.cs`, `SparkLine.razor`, `SparkLine.razor.cs`, and `SparkLineTooltip.razor`
- **Evidence:** The outcome says the public API manifests remained unchanged and the public surface never moved. The staged manifest removes `PlotBase`, `SparkLine`, and `SparkLineTooltip`. The material-delta log later identifies their removal as separately authorized follow-on work during the open 2.0 breaking window.
- **Impact:** The release record obscures an intentional breaking change and makes the staged scope appear narrower than it is.
- **Suggested fix:** Amend the outcome and acceptance evidence to acknowledge the removal, or stage the SparkLine follow-on separately from the audit implementation.

## Validation

- `dotnet build GGNet.slnx -warnaserror`: passed with zero warnings and zero errors.
- `dotnet test GGNet.slnx --no-build`: 361 enabled tests passed; three browser E2E tests skipped.
- `dotnet format whitespace GGNet.slnx --verify-no-changes`: passed.
- `dotnet format style GGNet.slnx --verify-no-changes`: passed.
- `git diff --cached --check`: passed.

The green suite does not cover the LocalDateTime coordinate mismatch described above.

**Verdict: fix-then-ship.**
