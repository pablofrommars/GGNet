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
| ⬜ D-01 · parked | Text width is measured with a font-agnostic approximation (`src/GGNet/TextExtensions.cs`). Layout measures label width server-side, so labels set in fonts wider than the approximation can clip or collide. Settled by font-aware measurement (interop or per-font metrics) or by evidence that the approximation suffices. | trigger-blocked | a label-fit defect is reported, or interop text measurement is reconsidered | port-tsu-conventions-to-ggnet, Deferred issues, 2026-10-14 |
| ⬜ D-02 · parked | Package and SDK versions are not at latest stable, which tsu's cross-repo rulings §8 asks for ("always at their latest"). `Directory.Packages.props` carries 12 pinned versions (e.g. AngleSharp 1.5.2, Moq 4.20.72, xunit.v3.mtp-v2 3.2.2, Verify.XunitV3 31.27.0), and `global.json` pins SDK `11.0.100-preview.6.26359.118`. As of 2026-10-14, newer stable versions existed for 9 of them, and .NET 11 rc.2 was available. The engineer ruled the bumps a separate Task. Settled by a Task that bumps them and re-verifies build, tests, goldens and the resolved package graph. | actionable | — | port-tsu-conventions-to-ggnet, DECISION-05, 2026-10-14 |

## Closed

| ID | Outcome |
|---|---|
