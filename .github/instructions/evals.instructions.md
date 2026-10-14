---
applyTo: "tests/GGNet.Evals/**"
---

# Evals Guide

Scope: `tests/GGNet.Evals`. Assumes [csharp.instructions.md](./csharp.instructions.md) and [testing.instructions.md](./testing.instructions.md) apply in full; this file documents only what is eval-specific.

**These are deterministic behavior-pin evals — not paid LLM evals.** There is no chat client, no API key, no model spend (grep the project for `IChatClient`/`ApiKey`/`HttpClient` returns nothing). `ModelContextProtocol` appears only as the **client** used to spawn the local stdio MCP server. Consequently there is no cost tiering, no `RunEvals` opt-out, no "treat every case as paid" framing — these run under `dotnet test` on every invocation like any other test. Do not import that framing from other codebases. **Exception:** tsu's separated, paid eval execution model is deliberately not adopted.

The evals pin the *behavior* of the chart-selection engine, the MCP surface, the profiler, the standalone validator, and the skill docs against curated expectations.

---

## 1. What the Golden Is

The "golden" reference is the embedded config `src/GGNet.ChartSelection/chart_selection.json` (an `<EmbeddedResource>`), loaded via `Selector.LoadConfig()` (`GetManifestResourceStream("GGNet.ChartSelection.chart_selection.json")`). It is **JSON, not YAML**. Eval *expectations* are **inline C# data** (a `Dictionary<string, Case>` fed through `[Theory]` + `[MemberData]`), not a separate dataset file. `GGNet.ChartSelection` grants `InternalsVisibleTo("GGNet.Evals")`.

How `Selector` reads the config — the grammar a config edit must respect (normalize → gate → filter → constrain → relax → rank):

- **Matching.** An absent leaf field is a wildcard; a null query field is no constraint. Mismatches are collected rather than failed fast, so a stat bridge can be judged against the whole mismatch.
- **Gates.** A leaf declaring a gated field true requires the query to affirm it — unknown never opens a gated door (this keeps spatial, relational and physical leaves out of ordinary queries), bridged or not.
- **Stat bridges.** A bridge admits a leaf only when it covers the entire axis mismatch: the query's raw shape is exactly what the declared `Stat.*` pre-processing turns into the leaf's expected shape.
- **Constraints.** Conditions AND together, in three forms: `"field": value` (equality — a null value matches an *unknown* query field, expressing "unverified" rules), `"field": [v1, v2]` (in-list), and `"field_gt": n` (numeric threshold; an unknown field never exceeds one). Allowlists reject only a non-null query value outside the list.
- **Never silent.** A leaf that matched intent and shape but broke a limit is reported in `excluded` with its reason — and in `structural_escapes` when faceting the same recipe is a way out — never silently dropped.

---

## 2. The Five Eval Files

- **`ChartSelectionEvals`** — the core selection behavior pin (~22 realistic queries ported from a retired Python suite). A `sealed record Case(...)` carries `ExpectAny` / `Forbid` / `ExpectError` / `CaveatOn` / `CaveatNotOn` / `AlternativesOn` / `EscapeOn` / `BridgeOn` / `TransformOn` / `ExcludedOn`; cases live in a `Dictionary<string, Case>` keyed by a natural-language scenario. Driven by `[Theory] [MemberData(nameof(CaseNames))]`, asserted inside an `AssertionScope` against `Selector.Select(...)` output (`top_charts`, `caveats`, `alternatives`, `structural_escapes`, `stat_bridge`, `excluded[].reason`, `transforms`). Structural `[Fact]`s also pin the config's integrity: `EveryLeafReachable`, `RecipesResolveToExampleFiles` (every recipe id resolves to a `skills/ggnet/examples/*.md`), `SupportedLeavesCarryGuidance`, `ConfigLoadsAndValidates` (pins the leaf count).
- **`McpServerEvals`** — end-to-end over the **real spawned stdio server**. `McpServerFixture : IAsyncLifetime` launches it via `StdioClientTransport { Command = "dotnet", Arguments = ["run", "--project", <src/GGNet.Mcp>] }` + `McpClient.CreateAsync`, consumed via `IClassFixture`. `ListsExpectedTools` is the authoritative tool-name pin (`select_chart`, `list_charts`, `explain_axes`, `list_geoms`, `list_scales`, `validate_plot`); `IntrospectionIsLive` asserts reflected geoms/scales include real members (`Geom_Point`, `Geom_Candlestick`, `Scale_Y_Log10`, `Facet_Wrap`); plus `MeasuredCardinalityBeatsSuppliedOne`, `StatBridgeSurfacesOverTheWire`, `ValidatePlotCompilesAndRenders`.
- **`ProfilerEvals`** — pins the measurement / anti-gaming half (`SkewedSampleIsDetected`, `SymmetricSampleClaimsNoShape`, `MissingValuesLowerCompleteness`, `[Theory]` `CardinalityBuckets`, `MeasuredFieldsOverrideSuppliedOnes`).
- **`ValidatorScriptEvals`** — CI smoke of the standalone `skills/ggnet/scripts/validate.cs` (the "dependency-free twin of the MCP `validate_plot` tool"): runs it over a snippet and asserts exit 0.
- **`SkillDocDriftEvals`** — `ChartTableAgreesWithConfig` parses `skills/ggnet/reference/chart-selection.md`, asserting its row count equals the leaf count, every function term is a real function/alias, and the `❌` markers agree with `ggnet.supported` in the config.

---

## 3. Conventions

- **Assert discretely.** Chart ids, tool names, enum-like outcomes, and excluded-reasons are exact-match checks (`Contain` / `NotContain` on ids), never similarity scores. Free-text similarity / LLM-as-judge is out of scope — GGNet's selection surface is deterministic.
- **Case keys are their own documentation** — the natural-language scenario key states the regression the case guards (e.g. `"market share of 25 competitors — pie must be excluded"`). A case that can't name what it catches doesn't earn its place.
- **The config is the single source of truth.** Selection cases assert against `chart_selection.json`; the skill docs are checked *against* it by `SkillDocDriftEvals`. When you change the config, expect `ChartSelectionEvals` and `SkillDocDriftEvals` to move together — reconcile both, don't silence one.
- Global usings for the project: `AwesomeAssertions`, `AwesomeAssertions.Execution`, `GGNet.ChartSelection`, `ModelContextProtocol.Client`, `ModelContextProtocol.Protocol`.
