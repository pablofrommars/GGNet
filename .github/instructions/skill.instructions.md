---
applyTo: "skills/**"
---

# Skill-Authoring Guide

Scope: the packaged `skills/ggnet` skill — the distributable, model-facing manual for generating GGNet charts. Exposed via `plugin.json` and `.codex-plugin/plugin.json`. This skill is a *product surface*: it ships to agents that have never read the source, so its correctness is enforced by tests, not by care.

The governing principle: **extraction over generation. Signatures come from the source; examples come from the pinned gallery. Nothing here is hand-written prose that can drift unnoticed.**

---

## 1. Layout

```
skills/ggnet/
├── SKILL.md                       # entry: frontmatter + mental model + 4 DSL conventions + tables + MCP + validate
├── reference/                     # 5 files: geoms.md, scales.md, stats.md, theming.md, chart-selection.md
├── examples/                      # 30 chart recipes (scatter.md, boxplot.md, choropleth.md, …)
├── patterns/common-mistakes.md    # the 2.0 migration / anti-pattern notes
└── scripts/validate.cs            # the snippet validator (there is NO validate.sh)
```

`SKILL.md` frontmatter (`name: ggnet` + a `description` that scopes it to GGNet/.NET/Blazor and **explicitly excludes** Plotly/Chart.js/matplotlib) is what gates the skill's activation — keep the exclusions; they stop the skill firing on non-GGNet charting.

---

## 2. Extraction, Not Invention

- **Signatures are extracted from source.** `reference/geoms.md` header states it: "All signatures extracted from source (GGNet 2.0)." When the DSL changes, regenerate the reference from the actual `BuilderExtensions.*.cs` signatures — do not hand-edit a parameter name. "Do not guess parameter names; every documented signature is extracted from source" (`SKILL.md`).
- **Examples are verbatim gallery code.** Every ` ```csharp ` block in `examples/*.md` must appear **verbatim (modulo whitespace)** in `tests/GGNet.Headless.Tests/GalleryTests.cs`. This is enforced by `SkillExampleConsistencyTests.ExamplesMatchGallerySource` — the deliberate choice of extraction over generation. Each example also links its pinned `GalleryTests.*.verified.svg`, carries a `When:` note, and a `Source:` note.
- The four DSL conventions in `SKILL.md` (`xxxBy` = data-driven; positional stops at selectors; SVG vocabulary; uniform interactivity block) are the same ones the library enforces — keep them in sync with [dsl.instructions.md](./dsl.instructions.md).

---

## 3. The `scripts/validate.cs` Contract

The validator compiles a plot snippet **under warnings-as-errors** against the in-repo `GGNet` + `GGNet.Headless`, then executes it (the snippet renders SVG). Exit 0 = compiles and renders.

- Snippet format: a **top-level-statements** C# file that builds a plot and ends in a render call (e.g. `Console.WriteLine((await plot.AsStringAsync()).Length);`).
- Provided global usings: `GGNet, GGNet.Formats, GGNet.Headless, NodaTime`.
- Mechanics mirror the MCP `validate_plot` tool (temp `Snippet.csproj`, `TreatWarningsAsErrors`, `dotnet run`) — it is the "dependency-free twin." Keep the two aligned ([mcp.instructions.md](./mcp.instructions.md)).
- Run it as `dotnet run skills/ggnet/scripts/validate.cs -- path/to/Snippet.cs`. Validate generated plot code with it *before* presenting it.

---

## 4. Editing Discipline — Docs, Examples, Gallery Move Together

The skill is wired to the library and the goldens by tests, so a DSL change is a multi-file change:

1. Update the library (`BuilderExtensions.*.cs`, geom).
2. Regenerate the affected `reference/*.md` signatures from source.
3. Update / add the `examples/*.md` recipe **and** its matching `GalleryTests.cs` block + pinned `*.verified.svg` (so `SkillExampleConsistencyTests` passes).
4. If chart *selection* changed, update `chart_selection.json` — `SkillDocDriftEvals.ChartTableAgreesWithConfig` asserts `reference/chart-selection.md` agrees with it.

A skill edit that isn't reflected in the source/gallery/config is a drift bug the eval suite ([evals.instructions.md](./evals.instructions.md)) will catch — reconcile all of them in the same change rather than silencing a test.

---

## 5. Prefer the MCP Tools

`SKILL.md` steers agents to the `ggnet` MCP server when registered (`select_chart`, `list_geoms`/`list_scales`, `validate_plot`) because those read the live library and are deterministic. Keep that section accurate to the actual tool names ([mcp.instructions.md](./mcp.instructions.md)); the reference files are the equivalent manual path when the server isn't available.
