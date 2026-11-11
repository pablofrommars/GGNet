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
| ⬜ D-02 · parked | Package and SDK versions are not at latest stable, which tsu's cross-repo rulings §8 asks for ("always at their latest"). `Directory.Packages.props` carries 12 pinned versions (e.g. AngleSharp 1.5.2, Moq 4.20.72, xunit.v3.mtp-v2 3.2.2, Verify.XunitV3 31.27.0), and `global.json` pins SDK `11.0.100-preview.6.26359.118`. As of 2026-10-14, newer stable versions existed for 9 of them, and .NET 11 rc.2 was available. The engineer ruled the bumps a separate Task. Settled by a Task that bumps them and re-verifies build, tests, goldens and the resolved package graph. | out-of-scope | — | port-tsu-conventions-to-ggnet, DECISION-05, 2026-10-14 |
| ⬜ D-03 · parked | Time-cursor inspection across panels. A touch tap or one-finger drag selects one x-domain time for every compatible panel of a `Plot` (all panels except `freeX` facets and polar); each series reports its own sample under an explicit policy (nearest within a maximum distance, previous, step, labelled interpolation) or "no nearby reading"; the pin is a bindable domain value that survives live data, resizing and window changes. Optional two-finger pan and chart pinch remove native page zoom over the panel (`touch-action: pan-y`). Why it matters: comparing related readings at one time is the consuming dashboard's core inspection need, and the current tap-to-inspect is per mark. Design constraints from the review: client-side cursor with latest-wins server lookups (no per-move round trip); tap does not fire mark `onclick` in this mode; only an explicit control clears the pin; double-tap reset off in this mode; mouse and keyboard equivalents; a `GGNetUserException` when combined with one-finger `Pan`; version 1 limited to Line, Point and Area. Supersedes the approved but unbuilt opt-in chart pinch and drag-to-scrub. Handoff spec: `specs/time-cursor-inspection.md` (open decisions OD-1–OD-9). Settled by a Task that binds that spec and passes physical QA. | out-of-scope | the engineer starts the time-cursor Task | touchscreen-support, GATE-01 and in-session proposal review, 2026-11-12 |
| ⬜ D-04 · parked | `NU1902`: transitive `Microsoft.Extensions.Validation 11.0.0-rc.3.26522.148` has a moderate advisory (GHSA-m28h-pj36-4364). After any fresh NuGet restore it fails `dotnet build GGNet.slnx -warnaserror` (six projects) and the two evals that restore scratch projects (`ValidatorScriptEvals.ScriptValidatesAMinimalSnippet`, `McpServerEvals.ValidatePlotCompilesAndRenders`). Why it matters: the local and CI gates are red on a fresh restore independent of any change. Settled by a dependency Task that moves to a patched SDK or package and re-verifies the gates; suppressing the audit is a policy decision, not a fix. | out-of-scope | — | touchscreen-support, Deferred issues, 2026-11-11 |
| ⬜ D-05 · parked | Overview and detail focus primitives. Consumers switch between an overview `Plot` and separately composed detail `Plot`s; GGNet should not build native maximize or composition switching, but should add an observable view window (read in data units plus a committed-change notification with its cause), reliable whole-area panel activation that never fires for input GGNet has claimed, and — measured first — suspension of hidden plots. Why it matters: today a consumer can set but not read or observe the view window, and sub-panel `onClick` covers only the background rectangle. Handoff spec: `specs/overview-detail-focus.md` (open decisions OD-1–OD-6). Settled by a Task that binds that spec and passes physical QA. | out-of-scope | the engineer starts the focus-primitives Task | touchscreen-support, in-session review of the consumer's overview/detail pattern, 2026-11-12 |

## Closed

| ID | Outcome |
|---|---|
