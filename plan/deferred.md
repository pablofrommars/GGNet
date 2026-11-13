# Deferred register

Every obligation a terminal plan parked, in one place. Rows are self-sufficient — the condensed
claim, why it matters, and the revisit condition live here, because source plans are retired.
Provenance points into Git history, not the working tree.

## Contents

- [Parked](#parked)
- [Closed](#closed)

## Parked

| ID | Item | Disposition | Revisit | Provenance |
|---|---|---|---|---|
| ⬜ D-01 · parked | Text width is measured with a font-agnostic approximation (`src/GGNet/TextExtensions.cs`). Layout measures label width server-side, so labels set in fonts wider than the approximation can clip or collide. Settled by font-aware measurement (interop or per-font metrics) or by evidence that the approximation suffices. | blocked-by | a label-fit defect is reported, or interop text measurement is reconsidered | port-tsu-conventions-to-ggnet, Deferred issues, 2026-10-14 |
| ⬜ D-06 · parked | Intermittent browser-test failure, cause unknown. `TouchDemoTests.RemountingAndNavigatingAwayAndBackLeaveExactlyOneHandlerSet` (`tests/GGNet.E2ETests`, `GGNET_E2E=1`) failed once on 2026-11-13 with "an unhandled exception on the current circuit" after the Touch layout page (`/touch-layout`) was left for `/touch` and reopened: about one failure in nine full runs that day, never when run alone, and never with detailed circuit errors on, so the exception text was not captured. Not classified as new or pre-existing: there is no baseline run count before the time-cursor Task. Why it matters: it may be a real race in the render loop when a page is disposed and remounted, which a user would see as a broken circuit. One suspect, from reading only: the crosshair cursor re-renders outside the interactive frame hand-off and could read a panel's scales while they are rebuilt; that path needs an inspection to exist, which this case does not create. Settled by the server log of a failing run (the E2E fixture keeps the demo's output only on startup failure; attaching it on circuit errors is the first step) or by twenty full runs on the commit before the time-cursor Task (`5a5e90e`). | blocked-by | the failure recurs, or the E2E fixture is changed to attach the demo's server output on circuit errors | time-cursor-inspection, local gate runs, 2026-11-13 |

## Closed

| ID | Outcome |
|---|---|
| ✅ D-02 · closed | Done by the .NET 11 GA dependency update on `net11-bump`, 2026-11-12: `global.json` pins SDK `11.0.100` (the latest .NET 11 SDK) and all 12 `Directory.Packages.props` pins are at latest stable on NuGet; build `-warnaserror`, `dotnet test`, both format gates and `GGNET_E2E=1` E2E (78/78 executed) pass from a forced restore, with no golden change and no vulnerable package in the resolved graph. |
| ✅ D-03 · closed | Done by the time-cursor inspection Task (`d6b02bb`), closed at deferred review 2026-11-13: the crosshair is one x position shared by every panel, with per-series readings under an explicit policy, a bindable pin (`@bind-Inspection`), two-finger pan and pinch, and Bar alongside Line, Point and Area; all local gates, the full browser run and the engineer's 14-check device QA passed. The row also carried the overview/detail spec's §2 inspection hand-over: ruled met by `@bind-Inspection`, the mechanism that spec names; no demo or test carries a pin between two `Plot`s. |
| ✅ D-04 · closed | Done by the same update, 2026-11-12: the advisory reached GGNet through its own central `Microsoft.AspNetCore.Components.Web` pin (→ `Components.Forms` → `Microsoft.Extensions.Validation`), not the SDK; moving the pins to `11.0.0` resolves the patched Validation `11.0.0`, and the build and both scratch-restore evals pass from a fresh restore with no audit switch (troubleshooting §17 generalized). |
| ✅ D-05 · closed | Done by the overview/detail focus Task (`5a5e90e`), closed at deferred review 2026-11-13: the view window reads back in data units, `ViewChanged` reports each committed change with its cause, and `.Panel(...)` takes a whole-area activation callback; all local gates, the full browser run and the engineer's device QA passed. Hidden-plot suspension was measured at 3.5 ms extra per refresh on the device and ruled not built. |
