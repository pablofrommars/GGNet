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
| ⬜ D-03 · parked | Time-cursor inspection across panels. A touch tap or one-finger drag selects one x-domain time for every compatible panel of a `Plot` (all panels except `freeX` facets and polar); each series reports its own sample under an explicit policy (nearest within a maximum distance, previous, step, labelled interpolation) or "no nearby reading"; the pin is a bindable domain value that survives live data, resizing and window changes. Optional two-finger pan and chart pinch remove native page zoom over the panel (`touch-action: pan-y`). Why it matters: comparing related readings at one time is the consuming dashboard's core inspection need, and the current tap-to-inspect is per mark. Design constraints from the review: client-side cursor with latest-wins server lookups (no per-move round trip); tap does not fire mark `onclick` in this mode; only an explicit control clears the pin; double-tap reset off in this mode; mouse and keyboard equivalents; a `GGNetUserException` when combined with one-finger `Pan`; version 1 limited to Line, Point and Area. Supersedes the approved but unbuilt opt-in chart pinch and drag-to-scrub. Handoff spec: `specs/time-cursor-inspection.md` (open decisions OD-1–OD-9). Settled by a Task that binds that spec and passes physical QA. | out-of-scope | the engineer starts the time-cursor Task | touchscreen-support, GATE-01 and in-session proposal review, 2026-11-12 |
| ⬜ D-05 · parked | Overview and detail focus primitives. Consumers switch between an overview `Plot` and separately composed detail `Plot`s; GGNet should not build native maximize or composition switching, but should add an observable view window (read in data units plus a committed-change notification with its cause), reliable whole-area panel activation that never fires for input GGNet has claimed, and — measured first — suspension of hidden plots. Why it matters: today a consumer can set but not read or observe the view window, and sub-panel `onClick` covers only the background rectangle. Handoff spec: `specs/overview-detail-focus.md` (open decisions OD-1–OD-6). Settled by a Task that binds that spec and passes physical QA. | out-of-scope | the engineer starts the focus-primitives Task | touchscreen-support, in-session review of the consumer's overview/detail pattern, 2026-11-12 |

## Closed

| ID | Outcome |
|---|---|
| ✅ D-02 · closed | Done by the .NET 11 GA dependency update on `net11-bump`, 2026-11-12: `global.json` pins SDK `11.0.100` (the latest .NET 11 SDK) and all 12 `Directory.Packages.props` pins are at latest stable on NuGet; build `-warnaserror`, `dotnet test`, both format gates and `GGNET_E2E=1` E2E (78/78 executed) pass from a forced restore, with no golden change and no vulnerable package in the resolved graph. |
| ✅ D-04 · closed | Done by the same update, 2026-11-12: the advisory reached GGNet through its own central `Microsoft.AspNetCore.Components.Web` pin (→ `Components.Forms` → `Microsoft.Extensions.Validation`), not the SDK; moving the pins to `11.0.0` resolves the patched Validation `11.0.0`, and the build and both scratch-restore evals pass from a fresh restore with no audit switch (troubleshooting §17 generalized). |
