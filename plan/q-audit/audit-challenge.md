# Audit Challenge — verification of `plan/q-audit/audit.md`

Adversarial verification of the external review against the tree as it stands (branch `interactivity-draft`, 2026-07-28). Every verdict below was grounded by reading the cited code; none rest on plausibility. Where the review's mechanism was wrong but its conclusion right, the mechanism is corrected in place.

**Round 2:** the reviewing session counter-reviewed this document (`plan/q-audit/audit-challenge-codex.md`) and updated the canonical `audit.md` (MF-2 broadened to include additive expansion; new must-fix #11 for stacked-bar over-training). The counter-review's rulings were themselves verified against the code; adjudication is in **Counter-review reconciliation** below, and the coverage section and affected survivor entries have been revised in place.

Verdict vocabulary: `confirmed` / `overstated` / `false-positive` / `deliberate-design` / `unverifiable-here`.

## Verdict table

Compound findings are split into atomic sub-claims (`a`/`b`) where they allege distinct mechanisms.

| ID | Claim (compressed) | Verdict | Evidence | Corrected severity |
|---|---|---|---|---|
| GA-1 | Builds warning-free; 290 enabled tests pass | confirmed | Build: 0 warnings / 0 errors. Tests: 30 + 213 + 47 = 290 passed, 3 skipped (`GGNet.E2ETests`) | — (context) |
| MF-1 | Label markdown → `MarkupString` without encoding = injection | confirmed | `src/GGNet/Markdown.cs:21,26-46`; `src/GGNet/Components/Panel.razor:148,165,280,298,316`; `src/GGNet/BuilderExtensions.cs:459-511` | Must-fix stands |
| MF-2 | Explicit limits applied in transformed space | confirmed | `src/GGNet/Scales/Extended.cs:34`, `Log10.cs:29`, `Position.cs:76-83`; `BuilderExtensions.cs:112` ("in data units"), `:221-229` | Must-fix stands |
| MF-3 | Ribbon `cachedArea` survives `Clear`; unmapped ribbon vanishes on rerender | confirmed | `src/GGNet/Geoms/Ribbon/Ribbon.cs:126-141,220-225`; `Geoms/Geom.cs:127`; violates `PlotContext.cs:302-305` invariant | Must-fix stands |
| MF-4a | `FillContinuous` `(0,0)` sentinel; constant data → `string.Empty`, marks skipped | confirmed | `src/GGNet/Scales/FillContinuous.cs:21-31,62-72`; skip-on-empty at e.g. `Geoms/Bar/Bar.cs:155-163`, `Ribbon.cs:145-149` | Must-fix stands |
| MF-4b | `Size` same sentinel; constant range → `NaN` radii emitted | confirmed | `src/GGNet/Scales/Size.cs:50-57,105-112`; `Geoms/Point/Point.cs` guard `radius <= 0` is false for NaN → NaN reaches SVG | Must-fix stands |
| MF-5 | Stacked bars: one signed accumulator, negative height emitted | confirmed | `src/GGNet/Geoms/Bar/Bar.cs:246-274` vs sign-aware `Dodge` `:276-309`; default `PositionAdjustment.Stack` (`BuilderExtensions.Bar.cs:41,104`); `Scene/ShapeComposer.cs` `ComposeRectangle` | Must-fix stands |
| MF-6 | `DayMonth` reads `values[end]` with exclusive `end` → crash on month boundary | confirmed | `src/GGNet/Scales/DiscreteDates.cs:124-135,283-291`; `DiscretePosition.cs:58-64,97-100`; `Buffers/SortedBuffer.cs:10-14`. Exception type corrected: `ArgumentOutOfRangeException` (List indexer), not `IndexOutOfRangeException` | Must-fix stands |
| MF-7 | `DateTimePosition` drops out-of-order same-day observations | confirmed | `src/GGNet/Scales/DateTimePosition.cs:56-71` (insert only forward of `values[^1]`), `:181-190` (`Map` → NaN); NaN points dropped in `Point.Shape` | Must-fix stands |
| MF-8a | Empty faceted source → division by zero in panel dimensions | overstated | `src/GGNet/Facets/Utils.cs:23-26` (`DimWrap(0)` → `(0, 1)`); `PlotContext.cs:183-184` is **double** division → `Infinity`, no exception; no panel consumes it | Kernel true (degenerate dims), but the crash is MF-8b, not this |
| MF-8b | Legend construction indexes `Panels[0]` with zero panels → crash | confirmed | `src/GGNet/PlotContext.cs:291-299`; `Faceting1D.cs:37-41` returns empty facets; `EnsurePanels` clears + rebuilds (`PlotContext.cs:399-407`); `Render` calls `BuildLegends` when `grid` (`:319-322`) | Must-fix stands (this is the finding-8 crash) |
| MF-9 | `SortedBuffer` drops duplicates → boxplot percentiles wrong | confirmed | `src/GGNet/Buffers/SortedBuffer.cs:16-24`; `Geoms/Boxplot/Boxplot.cs:108-130` (samples in `SortedBuffer<double>`), `:245-260` (`Percentile`) | Must-fix stands |
| MF-10 | `Polygon.Hole` never read; no fill rule emitted | confirmed | `src/GGNet/Geospacial/Polygon.cs:5`; zero repo matches for `.Hole` reads, `fill-rule`, `evenodd`; `Scene/ShapeComposer.cs:251-282` (all rings → one `d`); `Components/Area.razor` `RenderPolygon` emits no fill rule | Must-fix stands |
| SF-1a | Palette exhaustion → silent `default!` mappings, marks skipped | confirmed | `src/GGNet/Palettes/Utils.cs:7-10` (`Sample` → null); `Palettes/Discrete.cs:28-40` (`Set` returns silently, values stay `default!`); geoms skip null/empty fill | Should-fix stands |
| SF-1b | Empty continuous palette → `colors[0]` throws | confirmed | `src/GGNet/Scales/FillContinuous.cs:62-71`: index clamps to `Max(…, 0)` = 0 → `IndexOutOfRangeException` on empty array | Should-fix stands |

## Scorecard

- Atomic sub-claims: **15** → confirmed **14** · overstated **1** (MF-8a) · false-positive **0** · deliberate-design **0** · unverifiable-here **0**.
- At the review's own granularity (11 findings + assessment): **11/11 land**; finding 8 is right in its headline ("faceting an empty source crashes") with the wrong proximate mechanism.
- Both stated exception mechanics that were checkable were slightly off (`IndexOutOfRangeException` vs `ArgumentOutOfRangeException` in MF-6; "division by zero" vs `Panels[0]` in MF-8) — neither changes a verdict.
- Nothing flagged is covered as intentional by the scoped guides. The opposite: MF-3 violates the pipeline's own documented invariant ("Rendering twice yields identical output", `PlotContext.cs:302-305`), and `DiscretePosition.cs:103-106` records prior `SortedBuffer` trouble, supporting MF-9's framing that set-like dedup is right for scales and wrong for samples.

This is an unusually accurate external review. Fix-then-ship is the correct verdict.

Round-2 tally on this challenge's own coverage additions (as adjudicated below): 1 confirmed defect (upgraded), 1 confirmed elaboration (wording corrected), 1 withdrawn (test-pinned as deliberate buffer behavior), 1 reclassified as a clarity note. The counter-review contributed 2 new verified gaps this challenge had missed.

## Coverage check (bounded to the review's claimed surfaces)

Issues inside surfaces the review audited but did not report, as revised after the round-2 counter-review:

1. **`Bar.Stack` trains X at double the drawn half-width** — `Shape(x - delta, x + delta)` (`src/GGNet/Geoms/Bar/Bar.cs:266`) while rects span `x ± delta/2`; `Dodge` trains its exact emitted bounds (`:292-308`). Confirmed defect; originally flagged here as minor, upgraded by the counter-review and adopted into the canonical audit as must-fix #11 (excess outer whitespace and shrunken marks on the default stacked path, worst for few-bar charts).
2. **MF-6's trailing loop is wrong even short of the crash** — `while (i <= end)` with exclusive `end` (`src/GGNet/Scales/DiscreteDates.cs:124-135`). Wording corrected per the counter-review: on the full-range path the `values[i]` read throws during `labels.Add` argument evaluation, *before* any break is appended; the out-of-window tick emission happens on the windowed path (`Limits`/view make `end < values.Count`, so `values[end]` is a valid element outside the window). Folded into MF-6's fix scope, not a separate finding.
3. **`Shapes.Area.Points` dedups by x** — *withdrawn as a correctness finding.* The mechanism is real (comparer on `x` alone, `src/GGNet/Shapes/Area.cs:19-28`; `Area` geom likewise, `Geoms/Area/Area.cs:14-19`), but `SortedBufferTests.CustomComparerDedupesOnCompareEqual` (`tests/GGNet.Headless.Tests/SortedBufferTests.cs`) pins the x-dedup explicitly — "a second point with the same x is dropped" — making this deliberate-design at the buffer level. What remains open is a contract question, not an established defect: whether silently keeping the first of two conflicting same-x observations is the intended geom-level policy deserves documentation or surface validation, but no repo contract says otherwise.
4. **`DateTimePosition.Commit` calls `values.Add(date)` mid-iteration** (`src/GGNet/Scales/DateTimePosition.cs:165`) — *reclassified as a clarity note.* It was reported here as a no-op plus latent hazard, never as a runtime defect (the counter-review's "false positive as a runtime defect" ruling rebuts a stronger claim than was made); its disposition — dead code worth deleting, not a material finding — is agreed.

## Counter-review reconciliation

Adjudication of `plan/q-audit/audit-challenge-codex.md`, each ruling re-verified against the code rather than accepted on authority.

**Rulings on this challenge, accepted:**

- *Area/ribbon x-dedup is not an established defect* — *accepted.* The counter's decisive evidence checks out: `SortedBufferTests.CustomComparerDedupesOnCompareEqual` pins x-dedup with an explicit intent comment, and the `Area` geom declares its own x-only comparer. Coverage item withdrawn (see revised item 3); the residual documentation/validation question is recorded there.
- *`Bar.Stack` over-training upgraded to must-fix* — *accepted.* Mechanism was already verified here (`Bar.cs:266` vs `:292-308`); the severity upgrade follows the canonical audit's rubric placing rendering correctness in must-fix. Now canonical finding #11.
- *MF-6 trailing-loop elaboration folded in, with corrected sequencing* — *accepted.* Verified: `labels.Add((i, values[i].Day…))` evaluates the out-of-bounds read before `breaks.Add(i)` can run on the full-range path; the spurious-tick emission is real only on the windowed path (`DiscretePosition.Commit` can set `end < values.Count` via `Limits` or the view window). This document's earlier phrasing implied the stray break also occurred on the crash path; corrected.
- *`DateTimePosition` mid-iteration `Add` is not a material finding* — *disposition accepted, characterization noted.* The counter rebuts "runtime defect", a claim this challenge did not make (it was reported as a no-op and latent hazard). Reclassified as a clarity note either way.

**New gaps raised by the counter-review, verified and confirmed:**

- *Additive expansion is applied in transformed space despite a data-units contract* — **confirmed.** `SetRange` adds `expand.minAdd`/`maxAdd` to transformed endpoints (`src/GGNet/Scales/Position.cs:43-48`), while `Scale_X_Continuous`, `Scale_X_Sqrt`, and `Scale_X_Log10` all document "additives are data units" (`src/GGNet/BuilderExtensions.cs:69,217,235`). Same wrong-space family as MF-2 and correctly folded into it in the updated canonical audit. A genuine miss by this challenge, which had read both sites without connecting them.
- *`fill-rule="evenodd"` alone cannot implement `Polygon.Hole`* — **confirmed as remediation guidance.** Under even-odd filling, overlapping *exterior* subpaths in one compound `d` cancel in their overlap, and a blanket fill rule still ignores the `Hole` flag. This corrects the original audit's suggested fix ("typically `fill-rule=evenodd`"); this challenge's MF-10 remedy description was mechanism-agnostic but did not flag the suggestion's incompleteness — a fair gap. Carried into the MF-10 survivor entry.

**Canonical `audit.md` updates verified:** MF-2 now spans limits + additive expansion with `Position.cs:43-48` and the Sqrt/Log10 doc sites added to its location line; new must-fix #11 records the stacked-bar training defect with accurate evidence. Both edits match the code; findings 1–10 and should-fix 1 are otherwise unchanged, so the verdict table above still maps 1:1.

## Survivors — detail

Blast radius classifies the fix: {text-only / config+data / multi-file+code}. Confidence reflects the strength of the evidence trail.

### MF-1 — Label injection through `Markdown.Text` + `MarkupString` · multi-file+code · high

`Markdown.Text` copies unmatched text (`Markdown.cs:21,46`) and matched group values (`:26-38`) into the output with no HTML encoding, and `Panel.razor` renders title, subtitle, caption, and both axis labels as `(MarkupString)`. Any `<`/`&` payload in a label lands in the DOM verbatim; via `GGNet.Headless.SaveAsync` the same payload lands in exported SVG files. Threat-model note: labels are developer-supplied DSL arguments, so exploitation requires the host to interpolate untrusted data into a title — a common pattern, and the library gives no warning that labels are a markup sink. Fix in `Markdown.Text` (encode all non-token text before wrapping tspans) + hostile-input tests; gallery goldens unaffected unless titles contain the affected characters.

### MF-2 — Transformed-scale limits in the wrong space · multi-file+code · high

Geoms train `_min`/`_max` through `Map`, i.e. `transformation.Apply` (`Position.cs:76-83`, call sites like `Ribbon.cs:208-217`), while `Commit` passes `Limits` raw into `SetRange` (`Extended.cs:34`, `Log10.cs:29`). `XLim` is documented "Clamps the x range, **in data units**" (`BuilderExtensions.cs:112`) and `Scale_X_Sqrt`/`Scale_Y_Sqrt`/`Scale_*_Log10` all forward `limits` untransformed. `Scale_X_Log10(limits: (1, 1000))` yields Range 1..1000 against data mapped to 0..3 — data collapses into the bottom fraction of the axis, silently. Round-2 extension (counter-review, verified): the same wrong-space contract violation applies to additive expansion — `SetRange` adds `expand.minAdd`/`maxAdd` to transformed endpoints (`Position.cs:43-48`) while the public docs promise data units (`BuilderExtensions.cs:69,217,235`). Fix defines the space conversion in one place covering both limits and additive expansion; expect golden churn on any pinned chart combining either with a transformation.

### MF-3 — Ribbon vanishes on rerender · multi-file+code · high

`Clear()` clears `Layer` and `areas` but not `cachedArea` (`Ribbon.cs:220-225`); on the next pass the non-null cache short-circuits the `Layer.Add` (`:134-141`), so an unmapped (constant-fill) ribbon is never re-added — and its shared `SortedBuffer` of points persists across passes. Directly violates the documented idempotence invariant and sits exactly where `RenderPipelineTests.RenderTwiceIdentical` should have caught it (ribbons not covered). One-line fix + a ribbon idempotence test.

### MF-4a — `FillContinuous` sentinel and degenerate range · multi-file+code · high

`Train` uses `(0,0)` as "uninitialized" (`FillContinuous.cs:23`), so a leading zero observation is erased by the next value; `Map` returns `string.Empty` whenever `limits.max == limits.min` (`:62-72`) and every fill consumer treats empty as "skip the mark". A constant-valued fill column renders nothing.

### MF-4b — `Size` NaN radii · multi-file+code · high

Same sentinel (`Size.cs:50-57`). For a constant range, `Map` computes `0/0` (`:105-112`) and returns `NaN`. The consumer guard `if (radius <= 0) return;` in `Point.Shape` does not trip on NaN (IEEE comparisons are false), so `Radius = NaN` flows through `SvgFormat.Num` into the SVG attribute — invalid output, and precisely the class of emission `rendering.instructions.md` treats as pinned correctness.

### MF-5 — Stacked bars with negative values · multi-file+code · high

`Stack()` (default position) keeps one running `sum` for both signs and passes `value` directly as rectangle height (`Bar.cs:259`); `ComposeRectangle` then produces a negative screen height, which SVG treats as an error (bar not rendered). Y-training covers only `(0, final sum)` (`:271`), so intermediate partial sums overrun the trained extent in mixed-sign stacks. `Dodge` (`:292-308`) shows the intended sign handling. Fix with dual accumulators + tests; goldens for stacked-bar charts may churn.

### MF-6 — Month-boundary crash in `DayMonth` · multi-file+code · high

`Commit` passes an exclusive `end` (`values.Count`, `DiscretePosition.cs:58-64`) into `Labeling`, which `DiscreteDates` routes to `DayMonth` for spans ≤ 128 (`DiscreteDates.cs:283-291`). The trailing loop `while (i <= end)` reads `values[end]` (`:124-135`). Hand-trace of Jan 30, Jan 31, Feb 1, Feb 2: breaks = [2] after the month scan, backward fill runs, trailing loop reaches `i = 4` = `Count` → `List<T>` indexer throws `ArgumentOutOfRangeException` (review said `IndexOutOfRangeException`; crash regardless). The `<=` is also wrong short of the crash: when `Limits` or the view window make `end < values.Count`, `values[end]` is a valid element outside the requested window and the loop emits an out-of-window tick (on the full-range path the read throws before any break is appended). The fix must cover both paths.

### MF-7 — Order-dependent data loss in `DateTimePosition` · multi-file+code · high

`Train` only extends minute samples forward from `values[^1]` for same-day keys (`DateTimePosition.cs:56-67`); a same-day key earlier than the current max inserts nothing, `Map` finds no index and returns NaN (`:181-190`), and the point is silently dropped. Cross-day keys are unaffected (`:68-71`), so the loss pattern depends on arrival order within the latest day — nasty for streaming sources.

### MF-8 — Empty faceted source crashes (mechanism corrected) · multi-file+code · high

With zero rows, `Faceting1D.Commit` gets `N = 0`, `DimWrap(0)` returns `(0, 1)`, and `Facets()` returns an empty array, so `BuildFacetPanels` adds no panels. The review's "division by zero" (`1.0 / NRows`) is double arithmetic — it yields `Infinity` and harms nothing since no panel consumes it (MF-8a overstated). The actual crash is `BuildLegends`' unconditional `Panels[0]` in the faceting branch (`PlotContext.cs:293`), reached every gridded render pass (MF-8b confirmed). Non-faceted empty sources render fine (default panel path), which sharpens the fix target: guard the faceted legend path and define the empty-facet panel state.

### MF-9 — Boxplot percentiles over deduplicated samples · multi-file+code · high

`SortedBuffer.Add` silently discards equal items (`SortedBuffer.cs:20-23`) — correct for its scale/facet consumers, wrong for `Boxplot`'s per-group sample buffers (`Boxplot.cs:108-130`): `[1,1,1,10]` becomes `[1,10]` before `Percentile` runs (`:245-260`). Any repeated measurement (integers, sensor quantization) skews quartiles, median, and whiskers. Fix is a multiplicity-preserving buffer for boxplot samples only.

### MF-10 — Polygon holes ignored · multi-file+code · high

`Polygon.Hole` (`Geospacial/Polygon.cs:5`) is public API with zero readers (repo-wide grep: no `.Hole` access, no `fill-rule`, no `evenodd`). `ComposeMultiPolygon` concatenates every ring into one path `d` (`ShapeComposer.cs:251-282`) and `RenderPolygon` emits no fill rule, so hole rendering depends entirely on caller winding under SVG's default `nonzero`. Remediation caveat (counter-review, verified): the original audit's suggested `fill-rule="evenodd"` is not sufficient on its own — it still ignores the `Hole` flag, and even-odd filling makes overlapping *exterior* subpaths cancel in their overlap. The fix must carry hole semantics through composition (normalize hole winding under `nonzero`, or split exterior groups) rather than blanket-apply a fill rule. Spans shape → screen primitive → `Area.razor` + map goldens; a do-nothing public property is squarely worth closing during the 2.0 breaking window.

### Canonical #11 — Stacked bars train X at twice the drawn half-width · multi-file+code · high

Added in round 2 (surfaced by this challenge's coverage pass, upgraded by the counter-review). `Stack` trains `Shape(x - delta, x + delta)` (`Bar.cs:266`) while its rectangles span `x ± delta/2`; `Dodge` trains its exact emitted bounds (`:292-308`). The default stacked path therefore pads the grouping axis on both sides beyond any documented expansion, shrinking marks — most visible on few-bar charts. One-line fix + stacked-bar goldens will churn (that churn is the eyeball-confirmation the rendering guide requires).

### SF-1a — Palette exhaustion fails silently · multi-file+code · high

`Utils.Sample` returns null when categories exceed the palette (`Palettes/Utils.cs:7-10`); `Discrete.Set` bails silently, leaving every mapping `default!` (`Discrete.cs:28-40`), and geoms skip marks with null/empty fill — an empty chart with no diagnostic. Should-fix is right; the repo's own convention (`dsl.instructions.md` §6: guard at the surface with `GGNetUserException`) prescribes the fix shape.

### SF-1b — Empty continuous palette throws raw · multi-file+code · high

`FillContinuous.Map`'s index clamp bottoms out at 0, so an empty `colors` array throws `IndexOutOfRangeException` from deep inside the render pass (`FillContinuous.cs:62-71`) instead of a `GGNetUserException` at the `Scale_Fill_Continuous` entry point.

## Environment note (not a repo finding)

`dotnet build`/`dotnet test` fail inside the session sandbox because MSBuild worker nodes and vstest testhosts cannot bind their IPC sockets (`SocketException (13): Permission denied`), presenting as "Build FAILED, 0 Errors" after a 5:00 stall. Building with `DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 … -m:1 -nodeReuse:false` works sandboxed; the test run required leaving the sandbox. GA-1 was verified outside the sandbox: 290 passed, 3 skipped, 0 failed.
